using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetRawAbi
{
    public class GetRawAbiResponseDto
    {
        [JsonProperty("account_name")]
        public string AccountName { get; set; }

        [JsonProperty("code_hash")]
        public string CodeHash { get; set; }

        [JsonProperty("abi_hash")]
        public string AbiHash { get; set; }

        [JsonProperty("abi")]
        public string Abi { get; set; }
    }
}