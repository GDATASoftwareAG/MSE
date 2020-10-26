using System;
using MongoDB.Driver;
using SampleExchangeApi.Console.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SampleExchangeApi.Console.Database.TempSampleDB
{
    public class MongoMetadataReader : ISampleMetadataReader
    {
        private readonly IMongoCollection<ExportSample> _sampleCollection;
        private readonly ILogger _logger;

        public MongoMetadataReader(IConfiguration configuration, IMongoClient mongoClient, ILogger logger)
        {
            _logger = logger;

            try {
                var mongoDatabase = mongoClient.GetDatabase(configuration["MongoDb:DatabaseName"]);
                _sampleCollection = mongoDatabase.GetCollection<ExportSample>(configuration["MongoDb:CollectionName"]);
            }
            catch(Exception e)
            {
                _logger.LogError("Failed to open Mongodb collection.", e);
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
}