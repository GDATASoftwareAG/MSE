using System;
using MongoDB.Driver;
using SampleExchangeApi.Console.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SampleExchangeApi.Console.Database.TempSampleDB;

public class MongoMetadataHandler : ISampleMetadataHandler
{
    private readonly IMongoCollection<ExportSample> _sampleCollection;

    public MongoMetadataHandler(ILogger<MongoMetadataHandler> logger, IOptions<MongoMetadataOptions> options)
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

    public async Task<IEnumerable<ExportSample>> GetSamplesAsync(DateTime start, DateTime? end, string sampleSet, CancellationToken token = default)
    {
        var list = end == null
            ? await _sampleCollection
                .FindAsync(_ => _.SampleSet == sampleSet && _.Imported >= start, cancellationToken: token)
            : await _sampleCollection
                .FindAsync(_ => _.SampleSet == sampleSet && _.Imported >= start && _.Imported <= end, cancellationToken: token);
        return list.ToList();
    }
}
