using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SampleExchangeApi.Console.ListRequester;
using SampleExchangeApi.Console.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SampleExchangeApi.Console.Database;

public class PartnerProvider : IPartnerProvider, IDisposable
{
    private readonly ILogger<PartnerProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly PartnerProviderOptions _options;
    private List<Partner> _partners = new();
    private readonly Timer _timer;

    public PartnerProvider(ILogger<PartnerProvider> logger, IOptions<PartnerProviderOptions> options,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _options = options.Value;
        LoadPartners(null);
        _timer = new Timer(LoadPartners, null, TimeSpan.Zero, _options.RefreshInterval);
    }

    private void LoadPartners(object? _)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_options.Url))
            {
                var result = _httpClient.GetStringAsync(_options.Url).GetAwaiter().GetResult();
                _partners = System.Text.Json.JsonSerializer.Deserialize<Settings>(result)
                    ?.Partners ?? throw new InvalidDataException("Settings shouldn't be null");
            }

            var document = File.ReadAllText(_options.FilePath);
            var input = new StringReader(document);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            _partners = deserializer.Deserialize<Settings>(input)
                .Partners ?? throw new ArgumentException("partners is not set");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "failed to LoadPartners");
            throw;
        }
    }

    private class Settings
    {
        public List<Partner>? Partners { get; set; }
    }

    public bool AreCredentialsOkay(string username, string password)
    {
        var partner = _partners.SingleOrDefault(_ => _.Name == username, null);
        if (partner == null)
        {
            return false;
        }

        var hash = Sha256.Hash(password, Sha256.StringToByteArray(partner.Salt));
        return partner.Password.Equals(Sha256.ByteArrayToString(hash));
    }

    public IEnumerable<Partner> Partners => _partners;

    public void Dispose()
    {
        _timer.Dispose();
    }
}
