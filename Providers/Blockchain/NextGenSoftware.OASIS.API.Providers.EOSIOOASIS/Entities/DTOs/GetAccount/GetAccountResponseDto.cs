using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount
{
    public class GetAccountResponseDto
    {
        [JsonProperty("account_name")]
        public string AccountName { get; set; }

        [JsonProperty("head_block_num")]
        public int HeadBlockNum { get; set; }

        [JsonProperty("head_block_time")]
        public string HeadBlockTime { get; set; }

        [JsonProperty("last_code_update")]
        public string LastCodeUpdate { get; set; }

        [JsonProperty("created")]
        public string Created { get; set; }

        [JsonProperty("refund_request")]
        public EosAccountRefundRequestDto RefundRequest { get; set; }

        [JsonProperty("ram_quota")]
        public string RamQuota { get; set; }

        [JsonProperty("net_limit")]
        public EosAccountNetLimitDto NetLimit { get; set; }

        [JsonProperty("cpu_limit")]
        public EosAccountCpuLimitDto CpuLimit { get; set; }

        [JsonProperty("total_resources")]
        public EosAccountTotalResourcesDto TotalResources { get; set; }

        [JsonProperty("core_liquid_balance")]
        public string CoreLiquidBalance { get; set; }

        [JsonProperty("self_delegated_bandwidth")]
        public EosAccountSelfDelegatedBandwidthDto SelfDelegatedBandwidth { get; set; }

        [JsonProperty("net_weight")]
        public string NetWeight { get; set; }

        [JsonProperty("cpu_weight")]
        public string CpuWeight { get; set; }

        [JsonProperty("ram_usage")]
        public string RamUsage { get; set; }

        [JsonProperty("privileged")]
        public bool Privileged { get; set; }

        [JsonProperty("permissions")]
        public List<EosAccountPermissionDto> Permissions { get; set; }

        [JsonProperty("voter_info")]
        public EosAccountVoterInfoDto VoterInfo { get; set; }
    }
}