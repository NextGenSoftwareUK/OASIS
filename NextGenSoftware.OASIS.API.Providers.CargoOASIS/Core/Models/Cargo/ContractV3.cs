using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class ContractV3
    {
        /// <summary>
        /// ID of collection
        /// </summary>
        [JsonProperty("_id")]
        public string Id { get; set; }
        [JsonProperty("Address")]
        public string Address { get; set; }
        [JsonProperty("Name")]
        public string Name { get; set; }
        [JsonProperty("Symbol")]
        public string Symbol { get; set; }
        [JsonProperty("supportsMetadata")]
        public bool SupportsMetadata { get; set; }
        [JsonProperty("tags")]
        public IEnumerable<string> Tags { get; set; }
        /// <summary>
        /// Available only when useAuthToken === true
        /// </summary>
        [JsonProperty("owned")]
        public bool Owned { get; set; }
        /// <summary>
        /// total number of owned collectibles in collection
        /// Available only when useAuthToken === true
        /// </summary>
        [JsonProperty("totalOwned")]
        public int TotalOwned { get; set; }
        [JsonProperty("createAt")]
        public string CreateAt { get; set; }
    }
}