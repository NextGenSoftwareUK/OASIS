using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.AbiBinToJson
{
    public class AbiBinToJsonRequestDto
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("binargs")]
        public string BinArgs { get; set; }
    }
}