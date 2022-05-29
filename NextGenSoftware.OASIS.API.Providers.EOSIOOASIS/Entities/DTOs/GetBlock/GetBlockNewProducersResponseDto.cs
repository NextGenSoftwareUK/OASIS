using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetBlock
{
    public class GetBlockNewProducersResponseDto
    {
        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("producers")]
        public List<GetBlockProducerResponseDto> Producers { get; set; }
    }
}