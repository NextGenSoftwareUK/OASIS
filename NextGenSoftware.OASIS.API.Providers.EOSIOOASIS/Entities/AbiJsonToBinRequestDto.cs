using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities
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