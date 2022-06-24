using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.Transaction
{
    public class PerformTransactionRequestDto
    {
        [JsonProperty("signatures")]
        public List<string> Signatures { get; set; }

        [JsonProperty("compression")]
        public bool Compression { get; set; }

        [JsonProperty("packed_context_free_data")]
        public string PackedContextFreeData { get; set; }

        [JsonProperty("packed_trx")]
        public string PackedTrx { get; set; }
    }
}