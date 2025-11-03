using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount
{
    public class EosAccountNetLimitDto
    {
        [JsonProperty("max")]
        public string Max { get; set; }

        [JsonProperty("available")]
        public string Available { get; set; }

        [JsonProperty("used")]
        public string Used { get; set; }
    }
}