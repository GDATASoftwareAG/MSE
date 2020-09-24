using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenTracing;
using SampleExchangeApi.Console.Models;

namespace SampleExchangeApi.Console.ListRequester
{
    public interface IListRequester
    {
        bool AreCredentialsOkay(string username, string password, string correlationToken);
        Task<List<Token>> RequestListAsync(string username, DateTime start, DateTime? end, string correlationToken);
    }
}