using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SampleExchangeApi.Console.Models;

namespace SampleExchangeApi.Console.Database.TempSampleDB
{
    /// <inheritdoc />
    public class MongoMetadataReader : ISampleMetadataReader
    {
        private readonly IMongoCollection<ExportSample> _sampleCollection;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="mongoClient"></param>
        public MongoMetadataReader(IConfiguration configuration, IMongoClient mongoClient)
        {
            var mongoDatabase = mongoClient.GetDatabase(configuration["MongoDb:DatabaseName"]);
            _sampleCollection = mongoDatabase.GetCollection<ExportSample>(configuration["MongoDb:CollectionName"]);
        }

        /// <inheritdoc />
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