using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities
{
    public class GetTableRowsResponseDto
    {
        [JsonProperty("rows")]
        public List<object> Rows { get; set; }
    }
}