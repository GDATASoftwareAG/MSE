using System;
using MongoDB.Driver;
using SampleExchangeApi.Console.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SampleExchangeApi.Console.Database.TempSampleDB;

public class MongoMetadataReader : ISampleMetadataReader
{
    private readonly IMongoCollection<ExportSample> _sampleCollection;

    public MongoMetadataReader(ILogger<MongoMetadataReader> logger, IOptions<MongoMetadataOptions> options)
    {
        try
        {
            var mongoClient = new MongoClient(options.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(options.Value.DatabaseName);
            _sampleCollection = mongoDatabase.GetCollection<ExportSample>(options.Value.CollectionName);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to open Mongodb collection.");
            throw;
        }
    }

    public async Task<IEnumerable<ExportSample>> GetSamplesAsync(DateTime start, DateTime? end, string sampleSet)
    {
        var list = (end == null)
            ? await _sampleCollection
                .FindAsync(_ => _.SampleSet == sampleSet && _.Imported >= start)
            : await _sampleCollection
                .FindAsync(_ => _.SampleSet == sampleSet && _.Imported >= start && _.Imported <= end);

        return list.ToList();
    }
}
