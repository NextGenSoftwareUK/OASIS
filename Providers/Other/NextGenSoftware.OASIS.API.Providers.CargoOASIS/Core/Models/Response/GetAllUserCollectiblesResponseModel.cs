using System.Collections.Generic;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response
{
    public class GetAllUserCollectiblesResponseModel
    {
        [JsonProperty("tokenId")]
        public string TokenId { get; set; }
        [JsonProperty("metadata")]
        public IDictionary<string, object> Metadata { get; set; }
        [JsonProperty("tokenUrl")]
        public string TokenUrl { get; set; }
        [JsonProperty("resaleItem")]
        public ResaleItemV3 ResaleItem { get; set; }
        [JsonProperty("owner")]
        public string Owner { get; set; }
        [JsonProperty("collection")]
        public ContractV3 Collection { get; set; }
    }
}