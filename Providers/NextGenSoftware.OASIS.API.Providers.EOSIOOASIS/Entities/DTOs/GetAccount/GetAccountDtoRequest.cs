using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount
{
    public class GetAccountDtoRequest
    {
        [JsonProperty("account_name")]
        public string AccountName { get; set; }
    }
}