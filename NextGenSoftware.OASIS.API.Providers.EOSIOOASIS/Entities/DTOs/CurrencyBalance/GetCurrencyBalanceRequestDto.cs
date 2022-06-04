using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.CurrencyBalance
{
    public class GetCurrencyBalanceRequestDto
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("account")]
        public string Account { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }
    }
}