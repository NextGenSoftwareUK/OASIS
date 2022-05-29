using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetTableRows
{
    public class GetTableRowsRequestDto
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("table")]
        public string Table { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("json")]
        public bool Json { get; set; } = true;

        [JsonProperty("index_position", NullValueHandling=NullValueHandling.Ignore)]
        public string? IndexPosition { get; set; }

        [JsonProperty("key_type", NullValueHandling=NullValueHandling.Ignore)]
        public string? KeyType { get; set; }

        [JsonProperty("encode_type", NullValueHandling=NullValueHandling.Ignore)]
        public string? EncodeType { get; set; }

        [JsonProperty("lower_bound", NullValueHandling=NullValueHandling.Ignore)]
        public string? LowerBound { get; set; }

        [JsonProperty("upper_bound, NullValueHandling=NullValueHandling.Ignore")]
        public string? UpperBound { get; set; }

        [JsonProperty("limit", NullValueHandling=NullValueHandling.Ignore)]
        public int? Limit { get; set; }

        [JsonProperty("reverse", NullValueHandling=NullValueHandling.Ignore)]
        public bool? Reverse { get; set; }

        [JsonProperty("show_payer", NullValueHandling=NullValueHandling.Ignore)]
        public bool? ShowPayer { get; set; }
    }
}