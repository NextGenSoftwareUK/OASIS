using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetRequiredKeys
{
    public class GetRequiredKeysTransactionRequestDto
    {
        [JsonProperty("expiration")] public string Expiration { get; set; }

        [JsonProperty("ref_block_num")] public int RefBlockNum { get; set; }

        [JsonProperty("ref_block_prefix")] public int RefBlockPrefix { get; set; }

        [JsonProperty("max_net_usage_words")] public string MaxNetUsageWords { get; set; }

        [JsonProperty("max_cpu_usage_ms")] public string MaxCpuUsageMs { get; set; }

        [JsonProperty("delay_sec")] public int DelaySec { get; set; }

        [JsonProperty("context_free_actions")]
        public List<GetRequiredKeysContextFreeActionDtos> ContextFreeActions { get; set; }

        [JsonProperty("actions")] public List<GetRequiredKeysActionRequestDto> Actions { get; set; }

        [JsonProperty("transaction_extensions")]
        public List<List<int>> TransactionExtensions { get; set; }
    }
}