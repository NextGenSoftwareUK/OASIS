using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class PaginationResponseWithResults<T>
    {
        [JsonProperty("page")]
        public string Page { get; set; }
        [JsonProperty("totalPage")]
        public string TotalPage { get; set; }
        [JsonProperty("limit")]
        public string Limit { get; set; }
        [JsonProperty("result")]
        public T Result { get; set; }
    }
}