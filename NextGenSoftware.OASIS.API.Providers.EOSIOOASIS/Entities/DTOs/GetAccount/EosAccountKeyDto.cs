using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount
{
    public class EosAccountKeyDto
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("weight")]
        public int Weight { get; set; }
    }
}