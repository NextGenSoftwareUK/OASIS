using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class Order
    { 
        [JsonProperty("seller")]
        public string Seller { get; set; }
        [JsonProperty("buyer")]
        public string Buyer { get; set; }
        [JsonProperty("uuid")]
        public string Uuid { get; set; }
        [JsonProperty("contract")]
        public string Contract { get; set; }
        [JsonProperty("tokenId")]
        public string TokenId { get; set; }
        [JsonProperty("price")]
        public string Price { get; set; }
        [JsonProperty("fee")]
        public Fee Fees { get; set; }
        [JsonProperty("beneficiaries")]
        public IEnumerable<Beneficiary> Beneficiaries { get; set; }
        [JsonProperty("crateId")]
        public string CrateId { get; set; }
        [JsonProperty("createdAt")]
        public string CreatedAt { get; set; }
    }
}