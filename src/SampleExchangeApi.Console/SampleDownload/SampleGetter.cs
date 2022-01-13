using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SampleExchangeApi.Console.Models;

namespace SampleExchangeApi.Console.SampleDownload;

public interface ISampleGetter
{
    long GetFileSizeForSha256(string sha256);
    Task<FileStreamResult> GetAsync(string sha256, string partner);
}

public class SampleGetter : ISampleGetter
{
    private readonly ILogger _logger;
    private readonly StorageOptions _options;

    public SampleGetter(ILogger logger, IOptions<StorageOptions> options)
    {
        _logger = logger;
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public long GetFileSizeForSha256(string sha256)
    {
        try
        {
            var filename = GetPath(sha256);
            var fileInfo = new FileInfo(filename);
            return fileInfo.Length;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"File for SHA256 {sha256} should be there, but isn't.");
            return 0;
        }
    }

    public Task<FileStreamResult> GetAsync(string sha256, string partner)
    {
        _logger.LogInformation(JsonConvert.SerializeObject(new DeliverSampleOutput
        {
            Sha256 = sha256,
            Partner = partner
        }));

        var filename = GetPath(sha256);
        _logger.LogInformation($"Loading from: '{filename}' for partner {partner}!");

        return Task.FromResult(new FileStreamResult(new FileStream(
                filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true),
            "application/octet-stream"));
    }

    private string GetPath(string sha256)
    {
        var pathPartSha256 = sha256.ToLower();
        var pathPartOne = pathPartSha256.Substring(0, 2);
        var pathPartTwo = pathPartSha256.Substring(2, 2);
        var filename =
            $"{_options.Path}/{pathPartOne}/{pathPartTwo}/{pathPartSha256}";
        return filename;
    }
}
