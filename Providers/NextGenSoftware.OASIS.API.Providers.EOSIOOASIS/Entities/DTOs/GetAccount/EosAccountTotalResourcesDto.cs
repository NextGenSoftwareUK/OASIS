using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount
{
    public class EosAccountTotalResourcesDto
    {
        [JsonProperty("owner")]
        public string Owner { get; set; }

        [JsonProperty("ram_bytes")]
        public string RamBytes { get; set; }

        [JsonProperty("net_weight")]
        public string NetWeight { get; set; }

        [JsonProperty("cpu_weight")]
        public string CpuWeight { get; set; }
    }
}