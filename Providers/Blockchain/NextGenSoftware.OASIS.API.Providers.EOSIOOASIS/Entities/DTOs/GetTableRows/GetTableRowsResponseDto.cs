using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetTableRows
{
    public class GetTableRowsResponseDto<T>
    {
        [JsonProperty("rows")]
        public List<T> Rows { get; set; }

        [JsonProperty("more")]
        public bool? More { get; set; }

        [JsonProperty("next_key")]
        public string NextKey { get; set; }
    }
}