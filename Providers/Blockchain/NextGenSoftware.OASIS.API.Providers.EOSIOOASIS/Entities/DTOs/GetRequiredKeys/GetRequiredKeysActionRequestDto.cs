using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetRequiredKeys
{
    public class GetRequiredKeysActionRequestDto
    {
        [JsonProperty("account")] public string Account { get; set; }

        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("authorization")] public List<GetRequiredKeysAuthorizationDtos> Authorization { get; set; }

        [JsonProperty("data")] public GetRequiredKeysDataRequestDto Data { get; set; }

        [JsonProperty("hex_data")] public string HexData { get; set; }
    }
}