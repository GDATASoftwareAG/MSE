using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SampleExchangeApi.Console.Models;

namespace SampleExchangeApi.Console.Database.TempSampleDB;

/// <summary>
/// 
/// </summary>
public interface ISampleMetadataReader
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="sampleSet"></param>
    /// <returns></returns>
    Task<IEnumerable<ExportSample>> GetSamplesAsync(DateTime start, DateTime? end, string sampleSet);
}
