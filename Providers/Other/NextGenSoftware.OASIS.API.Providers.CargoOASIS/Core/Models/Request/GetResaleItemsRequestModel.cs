using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request
{
    public class GetResaleItemsRequestModel
    {
        /// <summary>
        /// Optional. String. Display resale items by a given showcase.
        /// </summary>
        [JsonProperty("showcaseId")]
        public string ShowcaseId { get; set; }

        /// <summary>
        /// Optional. String. The ID of the project (smart contract) on Cargo. Use this over collectionAddress. 
        /// </summary>
        [JsonProperty("projectId")]
        public string ProjectId { get; set; }

        /// <summary>
        /// Optional. String. Display resale items for a given collection.
        /// </summary>
        [JsonProperty("collectionId")] 
        public string CollectionId { get; set; }
        
        /// <summary>
        /// Optional. String. Display resale items for a given collection address.
        /// </summary>
        [JsonProperty("collectionAddress")]
        public string CollectionAddress { get; set; }

        /// <summary>
        ///  Optional. eth or xdai. Specify chain.
        /// </summary>
        [JsonProperty("chain")]
        [JsonConverter(typeof(Chain))]
        public Chain? Chain { get; set; }

        /// <summary>
        /// Optional. String. Page in results to display.
        /// </summary>
        [JsonProperty("page")]
        public string Page { get; set; }

        /// <summary>
        /// Optional. String. Limit of results per page.
        /// </summary>
        [JsonProperty("limit")] 
        public string Limit { get; set; }

        /// <summary>
        /// Optional. Boolean. Display resale items that are owned by the current authenticated user only.
        /// </summary>
        [JsonProperty("owned")] 
        public bool? Owned { get; set; }

        /// <summary>
        /// Optional. String. Filter resale items by showcase slug. Can be used as an alternative to showcaseId. slugId required when this is passed.
        /// </summary>
        [JsonProperty("slug")] 
        public string Slug { get; set; }

        /// <summary>
        /// Optional. String. Required when slug is passed
        /// </summary>
        [JsonProperty("slugId")]
        public string SlugId { get; set; }

        /// <summary>
        /// Optional. String. Ethereum wallet address of seller. Will only return items for this seller.
        /// </summary>
        [JsonProperty("seller")]
        public string Seller { get; set; }

        /// <summary>
        /// Optional. String. One of new , high-to-low , or low-to-high
        /// </summary>
        [JsonProperty("sort")] 
        public string Sort { get; set; }

        public string AccessJwtToken { get; set; }
    }
}