using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount
{
    public class EosAccountDto
    {
        [JsonProperty("weight")]
        public int Weight { get; set; }

        [JsonProperty("permission")]
        public EosAccountPermissionDto Permission { get; set; }
    }
}