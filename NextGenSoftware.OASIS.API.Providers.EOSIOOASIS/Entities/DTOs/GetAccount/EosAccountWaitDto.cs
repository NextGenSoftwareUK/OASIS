using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount
{
    public class EosAccountWaitDto
    {
        [JsonProperty("wait_sec")]
        public int WaitSec { get; set; }

        [JsonProperty("weight")]
        public int Weight { get; set; }
    }
}