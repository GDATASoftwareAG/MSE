using System;

namespace SampleExchangeApi.Console.Database;

public class PartnerProviderOptions
{
    public string FilePath { get; set; } = String.Empty;
    public string Url { get; set; } = String.Empty;
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(30);
}
