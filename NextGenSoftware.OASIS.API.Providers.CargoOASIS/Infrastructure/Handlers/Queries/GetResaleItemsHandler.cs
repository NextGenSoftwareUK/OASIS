using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetResaleItemsHandler : IHandle<Response<GetResaleItemsResponseModel>, GetResaleItemsRequestModel>
    {
        public async Task<Response<GetResaleItemsResponseModel>> Handle(GetResaleItemsRequestModel request)
        {
            throw new System.NotImplementedException();
        }
    }

    public class GetResaleItemsResponseModel
    {
        public class GetResaleItemData
        {
            /*
             *     data: {
        limit: string;
        page: string;
        totalPages: string;
        results: {
            // Will be blank if ERC-721. If ERC-1155
            // type will be '1155'
            type: '1155' | void;
            seller: string;
            // ID of the collection
            contract: string;
            // Id of the collectible
            tokenId: string;
            // Price in Wei that the collectible is selling for
            price: string;
            // If owned is true this will show the user if purchase signatures
            // have been generated. If they have cancelling the sale safely will
            // require a transaction to be submitted.
            signatureGenerated?: boolean;
            // Will only be in response if owned is set to true. This can be
            // used to safely cancel a sale. You can use the cancelSale method
            // and Cargo JS will handle this for you.
            groupId?: string;
            // Showcase ID resale item belongs to
            crate?: string;
            createdAt: string;
        }[]
             */
        }
        
        [JsonProperty("err")]
        public bool Error { get; set; }

        [JsonProperty("status")] 
        public int Status { get; set; }
        
        [JsonProperty("data")]
        public IEnumerable<GetResaleItemData> Data { get; set; }
    }

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
        public Chain Chain { get; set; }

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
        public string Owned { get; set; }

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
    }
}