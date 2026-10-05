using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetBlock
{
    public class GetBlockRequestDto
    {
        [JsonProperty("block_num_or_id")]
        public string BlockNumOrId { get; set; }
    }
}