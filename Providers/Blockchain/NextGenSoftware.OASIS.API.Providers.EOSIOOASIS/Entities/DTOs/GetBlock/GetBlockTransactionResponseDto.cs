using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetBlock
{
    public class GetBlockTransactionResponseDto
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("cpu_usage_us")]
        public int CpuUsageUs { get; set; }

        [JsonProperty("net_usage_words")]
        public int NetUsageWords { get; set; }

        [JsonProperty("trx")]
        public string Trx { get; set; }
    }
}