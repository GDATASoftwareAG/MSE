using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SampleExchangeApi.Console.Models;

namespace SampleExchangeApi.Console.SampleDownload;

public interface ISampleGetter
{
    FileStreamResult Get(string sha256, string partner);
}

public class SampleGetter : ISampleGetter
{
    private readonly ILogger _logger;
    private readonly string _storagePath;

    public SampleGetter(IConfiguration configuration, ILogger logger)
    {
        _logger = logger;
        _storagePath = configuration["Storage:Path"];
    }

    public FileStreamResult Get(string sha256, string partner)
    {
        _logger.LogInformation(JsonConvert.SerializeObject(new DeliverSampleOutput
        {
            Sha256 = sha256,
            Partner = partner
        }));

        var pathPartSha256 = sha256.ToLower();
        var pathPartOne = pathPartSha256.Substring(0, 2);
        var pathPartTwo = pathPartSha256.Substring(2, 2);
        var filename =
            $"{_storagePath}/{pathPartOne}/{pathPartTwo}/{pathPartSha256}";
        _logger.LogInformation($"Loading from: '{filename}' for partner {partner}!");

        return new FileStreamResult(new FileStream(
                filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true),
            "application/octet-stream");
    }
}
