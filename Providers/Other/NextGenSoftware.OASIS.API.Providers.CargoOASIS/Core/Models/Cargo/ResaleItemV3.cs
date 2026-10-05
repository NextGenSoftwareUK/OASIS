using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class ResaleItemV3
    {
        [JsonProperty("seller")]
        public string Seller { get; set; }
        
        /// <summary>
        /// ID of the collection
        /// </summary>
        [JsonProperty("contract")]
        public string Contract { get; set; }
        
        /// <summary>
        /// Id of the collectible
        /// </summary>
        [JsonProperty("tokenId")]
        public string TokenId { get; set; }
        
        /// <summary>
        /// Price in Wei that the collectible is selling for
        /// </summary>
        [JsonProperty("price")]
        public string Price { get; set; }
        
        /// <summary>
        /// Will only be in response if owned is set to true. This can be
        /// used to safely cancel a sale. You can use the cancelSale method
        /// and Cargo JS will handle this for you.
        /// </summary>
        [JsonProperty("groupId")]
        public string GroupId { get; set; }
        
        /// <summary>
        /// If owned is true this will show the user if purchase signatures
        /// have been generated. If they have cancelling the sale safely will
        /// require a transaction to be submitted.
        /// </summary>
        [JsonProperty("signatureGenerated")]
        public bool SignatureGenerated { get; set; }
        
        /// <summary>
        /// Showcase ID resale item belongs to
        /// </summary>
        [JsonProperty("crate")]
        public string Crate { get; set; }
        
        [JsonProperty("createAt")]
        public string CreateAt { get; set; }
        
        public IDictionary<string, string> Metadata { get; set; }
    }
}