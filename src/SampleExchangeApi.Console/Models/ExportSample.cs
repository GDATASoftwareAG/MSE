using System;
using MongoDB.Bson.Serialization.Attributes;

namespace SampleExchangeApi.Console.Models;

public class ExportSample
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    [BsonElement("_id")] public string Sha256SampleSet { get; set; }
    public string Sha256 { get; set; }
    public string Platform { get; set; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public long FileSize { get; set; }
    public DateTime Imported { get; set; }
    public DateTime DoNotUseBefore { get; set; }
    public string SampleSet { get; set; }
}
