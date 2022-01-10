using System.Collections.Generic;

namespace SampleExchangeApi.Console.Models;
#pragma warning disable 1591
public class Partner
{
    public string Name { get; set; }
    public string Password { get; set; }
    public string Salt { get; set; }
    public bool Enabled { get; set; }
    public string Sampleset { get; set; }
}

public class Settings
{
    public List<Partner> Partners { get; set; }
}
