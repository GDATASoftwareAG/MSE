using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SampleExchangeApi.Console.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SampleExchangeApi.Console.Database;

public interface IPartnerProvider
{
    List<Partner> GetPartners();
}

public class PartnerProvider : IPartnerProvider
{
    private readonly List<Partner> _partners;

    public PartnerProvider(ILogger<PartnerProvider> logger, IOptions<PartnerProviderOptions> options)
    {
        try
        {
            var document = File.ReadAllText(options.Value.YAML);
            var input = new StringReader(document);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            _partners = deserializer.Deserialize<Settings>(input).Partners ?? throw new ArgumentException("partners is not set");
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
            throw;
        }
    }

    private class Settings
    {
        public List<Partner>? Partners { get; set; }
    }

    public List<Partner> GetPartners() => _partners;
}
