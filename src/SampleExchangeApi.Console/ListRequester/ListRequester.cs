using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SampleExchangeApi.Console.Database;
using SampleExchangeApi.Console.Models;
using SampleExchangeApi.Console.SampleDownload;

namespace SampleExchangeApi.Console.ListRequester;

public class ListRequester : IListRequester
{
    private readonly ILogger _logger;
    private readonly ListRequesterOptions _options;
    private readonly ISampleMetadataHandler _sampleMetadataHandler;
    private readonly IPartnerProvider _partnerProvider;
    private readonly ISampleStorageHandler _sampleStorageHandler;

    public ListRequester(ILogger<ListRequester> logger, IOptions<ListRequesterOptions> options,
        ISampleMetadataHandler sampleMetadataHandler, IPartnerProvider partnerProvider,
        ISampleStorageHandler sampleStorageHandler)
    {
        _logger = logger;
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _sampleMetadataHandler = sampleMetadataHandler;
        _partnerProvider = partnerProvider;
        _sampleStorageHandler = sampleStorageHandler;
    }

    public async Task<List<Token>> RequestListAsync(string username, DateTime start, DateTime? end,
        CancellationToken token = default)
    {
        var includeFamilyName = _partnerProvider.Partners.Single(_ => _.Name == username).IncludeFamilyName;

        var sampleSet = _partnerProvider.Partners.SingleOrDefault(_ => _.Name == username)?.Sampleset;
        var samples = await _sampleMetadataHandler.GetSamplesAsync(start, end, sampleSet, token);

        var tokens = new List<Token>();
        foreach (var sample in samples.Where(sample => sample.DoNotUseBefore <= DateTime.Now))
        {

            var fileSize = sample.FileSize == 0
                ? _sampleStorageHandler.GetFileSizeForSha256(sample.Sha256)
                : sample.FileSize;

            if (fileSize <= 0)
            {
                continue;
            }

            var builder = new JwtBuilder()
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
                builder.AddClaim("familyname", sample.FamilyName);
            }
            tokens.Add(new Token
            {
                _Token = builder.Encode()
            });
        };

        _logger.LogInformation($"Customer {username} receives a list with {tokens.Count} hashes.");
        return tokens;
    }
}
