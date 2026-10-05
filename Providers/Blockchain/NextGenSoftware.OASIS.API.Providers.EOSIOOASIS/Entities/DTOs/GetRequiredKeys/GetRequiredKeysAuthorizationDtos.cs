using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetRequiredKeys
{
    public class GetRequiredKeysAuthorizationDtos
    {
        [JsonProperty("actor")] public string Actor { get; set; }

        [JsonProperty("permission")] public string Permission { get; set; }
    }
}