using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetRawAbi
{
    public class GetRawAbiRequestDto
    {
        [JsonProperty("account_name")]
        public string AccountName { get; set; }
    }
}