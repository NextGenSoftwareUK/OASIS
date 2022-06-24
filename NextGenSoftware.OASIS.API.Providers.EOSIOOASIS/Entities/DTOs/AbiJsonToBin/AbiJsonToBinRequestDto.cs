using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.AbiJsonToBin
{
    public class AbiJsonToBinRequestDto
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("args")]
        public object Args { get; set; }
    }
}