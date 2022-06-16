using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount
{
    public class EosAccountRequiredAuthDto
    {
        [JsonProperty("waits")]
        public List<EosAccountWaitDto> Waits { get; set; }

        [JsonProperty("keys")]
        public List<EosAccountKeyDto> Keys { get; set; }

        [JsonProperty("threshold")]
        public int Threshold { get; set; }

        [JsonProperty("accounts")]
        public List<EosAccountDto> Accounts { get; set; }
    }
}