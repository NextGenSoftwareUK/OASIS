using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetBlock
{
    public class GetBlockBlockRootMerkleResponseDto
    {
        [JsonProperty("_active_nodes")]
        public List<string> ActiveNodes { get; set; }

        [JsonProperty("_node_count")]
        public string NodeCount { get; set; }
    }
}