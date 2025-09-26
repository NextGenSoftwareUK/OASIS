using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetBlock
{
    public class GetBlockProducerResponseDto
    {
        [JsonProperty("producer_name")]
        public string ProducerName { get; set; }

        [JsonProperty("block_signing_key")]
        public string BlockSigningKey { get; set; }
    }
}