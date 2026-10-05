using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.AbiJsonToBin
{
    public class AbiJsonToBinResponseDto
    {
        [JsonProperty("binargs")]
        public string BinArgs { get; set; }
    }
}