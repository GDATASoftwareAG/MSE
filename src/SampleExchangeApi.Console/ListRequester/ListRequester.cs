using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SampleExchangeApi.Console.Database.TempSampleDB;
using SampleExchangeApi.Console.Models;

namespace SampleExchangeApi.Console.ListRequester
{
    public class ListRequester : IListRequester
    {
        private readonly ILogger _logger;
        private readonly ISampleMetadataReader _sampleMetadataReader;
        private readonly Settings _settings;
        private readonly string _secret;
        private readonly double _expiration;
        private readonly string _storagePath;

        public ListRequester(IConfiguration configuration, ILogger logger,
            ISampleMetadataReader sampleMetadataReader, Settings settings)
        {
            _logger = logger;
            _sampleMetadataReader = sampleMetadataReader;
            _settings = settings;
            _secret = configuration["Token:Secret"];
            _expiration = double.Parse(configuration["Token:Expiration"]);
            _storagePath = configuration["Storage:Path"];
        }

        public bool AreCredentialsOkay(string username, string password, string correlationToken)
        {
            Partner partner = null;
            try
            {
                partner = _settings.Partners.SingleOrDefault(_ => _.Name == username);
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning($"Unknown user name: {username}");
            }

            if (partner == null) return false;
            var hash = Sha256.Hash(password, Sha256.StringToByteArray(partner.Salt));
            return partner.Password.Equals(Sha256.ByteArrayToString(hash));
        }

        public async Task<List<Token>> RequestListAsync(string username, DateTime start, DateTime? end,
            string correlationToken)
        {
            var sampleSet = _settings.Partners.SingleOrDefault(_ => _.Name == username)?.Sampleset;

            IEnumerable<ExportSample> samples = (await _sampleMetadataReader.GetSamplesAsync(start, end, sampleSet)).ToList();

            var tokenCollectionBag = new ConcurrentBag<Token>();
            Parallel.ForEach(samples, new ParallelOptions {MaxDegreeOfParallelism = 8}, sample =>
            {
                if (sample.DoNotUseBefore <= DateTime.Now)
                {
                    var fileSize = sample.FileSize == 0 ? GetFileSizeForSha256(sample.Sha256, correlationToken) : sample.FileSize;

                    if (fileSize > 0)
                    {
                        tokenCollectionBag.Add(new Token
                        {
                            _Token = new JwtBuilder().WithAlgorithm(new HMACSHA512Algorithm())
                                .WithSecret(_secret)
                                .AddClaim("exp", DateTimeOffset.UtcNow.AddSeconds(_expiration)
                                    .ToUnixTimeSeconds())
                                .AddClaim("sha256", sample.Sha256)
                                .AddClaim("filesize", fileSize)
                                .AddClaim("platform", sample.Platform)
                                .AddClaim("partner", username)
                                .Build()
                        });
                    }
                }
            });
            var tokens = tokenCollectionBag.ToList();

            _logger.LogInformation($"Customer {username} receives a list with {tokens.Count} hashes.");
            return tokens;
        }

        private long GetFileSizeForSha256(string sha256, string correlationToken)
        {
            try
            {
                var pathPartSha256 = sha256.ToString().ToLower();
                var pathPartOne = pathPartSha256.Substring(0, 2);
                var pathPartTwo = pathPartSha256.Substring(2, 2);
                var filename =
                    $"{_storagePath}/{pathPartOne}/{pathPartTwo}/{pathPartSha256}";

                var fileInfo = new FileInfo(filename);

                return fileInfo.Length;
            }
            catch (Exception e)
            {
                _logger.LogError(e,$"File for SHA256 {sha256} should be there, but isn't.");
                return 0;
            }
        }
    }
}