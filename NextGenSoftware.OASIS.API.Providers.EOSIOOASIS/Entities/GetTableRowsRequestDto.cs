using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities
{
    public class GetTableRowsRequestDto
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("table")]
        public string Table { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("index_position")]
        public string IndexPosition { get; set; }

        [JsonProperty("key_type")]
        public string KeyType { get; set; }

        [JsonProperty("encode_type")]
        public string EncodeType { get; set; }

        [JsonProperty("lower_bound")]
        public string LowerBound { get; set; }

        [JsonProperty("upper_bound")]
        public string UpperBound { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("reverse")]
        public bool Reverse { get; set; }

        [JsonProperty("show_payer")]
        public bool ShowPayer { get; set; }
    }
}