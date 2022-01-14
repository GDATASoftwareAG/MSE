using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SampleExchangeApi.Console.Models;

namespace SampleExchangeApi.Console.ListRequester;

public interface IListRequester
{
    Task<List<Token>> RequestListAsync(string username, DateTime start, DateTime? end, CancellationToken token = default);
}
