using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount
{
    public class EosAccountPermissionDto
    {
        [JsonProperty("parent")]
        public string Parent { get; set; }

        [JsonProperty("perm_name")]
        public string PermName { get; set; }

        [JsonProperty("required_auth")]
        public EosAccountRequiredAuthDto RequiredAuth { get; set; }
    }
}