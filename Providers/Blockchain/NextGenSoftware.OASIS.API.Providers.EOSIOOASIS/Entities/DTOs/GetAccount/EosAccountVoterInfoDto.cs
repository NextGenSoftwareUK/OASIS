using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount
{
    public class EosAccountVoterInfoDto
    {
        [JsonProperty("owner")]
        public string Owner { get; set; }

        [JsonProperty("proxy")]
        public string Proxy { get; set; }

        [JsonProperty("producers")]
        public List<string> Producers { get; set; }

        [JsonProperty("staked")]
        public string Staked { get; set; }

        [JsonProperty("last_vote_weight")]
        public string LastVoteWeight { get; set; }

        [JsonProperty("proxied_vote_weight")]
        public string ProxiedVoteWeight { get; set; }

        [JsonProperty("is_proxy")]
        public int IsProxy { get; set; }

        [JsonProperty("flags1")]
        public int Flags1 { get; set; }

        [JsonProperty("reserved2")]
        public int Reserved2 { get; set; }

        [JsonProperty("reserved3")]
        public string Reserved3 { get; set; }
    }
}