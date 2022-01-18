using System.Collections.Generic;
using SampleExchangeApi.Console.Models;

namespace SampleExchangeApi.Console.Database;

public interface IPartnerProvider
{
    bool AreCredentialsOkay(string username, string password);
    IEnumerable<Partner> Partners { get; }
}
