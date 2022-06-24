using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetRequiredKeys
{
    public class GetRequiredKeysRequestDto
    {
        [JsonProperty("transaction")] public GetRequiredKeysTransactionRequestDto Transaction { get; set; }

        [JsonProperty("available_keys")] public List<string> AvailableKeys { get; set; }
    }
}