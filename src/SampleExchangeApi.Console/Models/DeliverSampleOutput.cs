using Newtonsoft.Json;

namespace SampleExchangeApi.Console.Models
{
    public class DeliverSampleOutput
    {
        [JsonProperty("sha256")] public string Sha256;
        [JsonProperty("partner")] public string Partner;
    }
}