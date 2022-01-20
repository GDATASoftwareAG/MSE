using System;

namespace SampleExchangeApi.Console.Database;

public class MongoMetadataOptions
{
    public string ConnectionString { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public string CollectionName { get; set; } = "";
    public string[] Indexes { get; set; } = Array.Empty<string>();
    public string? TimeSpanIndex { get; set; }
    public TimeSpan Duration { get; set; }
}
