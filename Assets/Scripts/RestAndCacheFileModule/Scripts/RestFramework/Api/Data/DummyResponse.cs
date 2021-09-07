using Newtonsoft.Json;

namespace Common.Api.Data
{
    public class DummyResponse
    {
        [JsonProperty("key")] public string Key { get; set; }
       
    }
}