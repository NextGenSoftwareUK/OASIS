using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount
{
    public class EosAccountRefundRequestDto
    {
        [JsonProperty("owner")]
        public string Owner { get; set; }

        [JsonProperty("request_time")]
        public string RequestTime { get; set; }

        [JsonProperty("net_amount")]
        public string NetAmount { get; set; }

        [JsonProperty("cpu_amount")]
        public string CpuAmount { get; set; }
    }
}