using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SampleExchangeApi.Console.Database;
using SampleExchangeApi.Console.Database.TempSampleDB;
using SampleExchangeApi.Console.Models;
using SampleExchangeApi.Console.SampleDownload;

namespace SampleExchangeApi.Console.ListRequester;

public class ListRequester : IListRequester
{
    private readonly ILogger _logger;
    private readonly ListRequesterOptions _options;
    private readonly ISampleMetadataReader _sampleMetadataReader;
    private readonly List<Partner> _partners;
    private readonly ISampleGetter _sampleGetter;

    public ListRequester(ILogger<ListRequester> logger, IOptions<ListRequesterOptions> options,
        ISampleMetadataReader sampleMetadataReader, IPartnerProvider partnerProvider, ISampleGetter sampleGetter)
    {
        _logger = logger;
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _sampleMetadataReader = sampleMetadataReader;
        _partners = partnerProvider.GetPartners();
        _sampleGetter = sampleGetter;
    }

    public bool AreCredentialsOkay(string username, string password, string correlationToken)
    {
        Partner? partner = null;
        try
        {
            partner = _partners.SingleOrDefault(_ => _.Name == username);
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning($"Unknown user name: {username}");
        }

        if (partner == null)
        {
            return false;
        }

        var hash = Sha256.Hash(password, Sha256.StringToByteArray(partner.Salt));
        return partner.Password.Equals(Sha256.ByteArrayToString(hash));
    }

    public async Task<List<Token>> RequestListAsync(string username, DateTime start, DateTime? end,
        string correlationToken)
    {
        var includeFamilyName = _partners.Single(_ => _.Name == username).IncludeFamilyName;

        var sampleSet = _partners.SingleOrDefault(_ => _.Name == username)?.Sampleset;

        IEnumerable<ExportSample> samples =
            (await _sampleMetadataReader.GetSamplesAsync(start, end, sampleSet)).ToList();

        var tokenCollectionBag = new ConcurrentBag<Token>();
        Parallel.ForEach(samples, new ParallelOptions { MaxDegreeOfParallelism = 8 }, sample =>
        {
            if (sample.DoNotUseBefore > DateTime.Now)
            {
                return;
            }

            var fileSize = sample.FileSize == 0
                ? _sampleGetter.GetFileSizeForSha256(sample.Sha256)
                : sample.FileSize;

            if (fileSize <= 0)
            {
                return;
            }

            var token = new JwtBuilder()
                .WithAlgorithm(new HMACSHA512Algorithm())
                .WithSecret(_options.Secret)
                .AddClaim("exp", DateTimeOffset.UtcNow.AddSeconds(_options.Expiration)
                    .ToUnixTimeSeconds())
                .AddClaim("sha256", sample.Sha256)
                .AddClaim("filesize", fileSize)
                .AddClaim("platform", sample.Platform)
                .AddClaim("partner", username);

            if (includeFamilyName)
            {
                token.AddClaim("familyname", sample.FamilyName);
            }
            tokenCollectionBag.Add(new Token
            {
                _Token = token.Encode()
            });
        });
        var tokens = tokenCollectionBag.ToList();

        _logger.LogInformation($"Customer {username} receives a list with {tokens.Count} hashes.");
        return tokens;
    }
}
