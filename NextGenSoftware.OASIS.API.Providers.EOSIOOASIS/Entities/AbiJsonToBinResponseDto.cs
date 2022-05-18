using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities
{
    public class AbiJsonToBinResponseDto
    {
        [JsonProperty("binargs")]
        public string BinArgs { get; set; }
    }
}