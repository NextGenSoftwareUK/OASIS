using System.Collections.Generic;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response
{
    public class GetCollectiblesListByProjectIdResponseModel
    {
        public class CollectiblesListData
        {
            /// <summary>
            /// Contract name
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// Contract symbol
            /// </summary>
            public string Symbol { get; set; }
            /// <summary>
            /// Array of tokens
            /// </summary>
            public IEnumerable<TokenDetail> Results { get; set; }
        }

        [JsonProperty("err")]
        public bool Error { get; set; }

        [JsonProperty("status")] 
        public int Status { get; set; }

        /// <summary>
        /// Collectibles List
        /// </summary>
        [JsonProperty("data")] 
        public CollectiblesListData Data { get; set; }

        /// <summary>
        /// Total number of tokens in contract,
        /// or total number of tokens owned by address
        /// passed as "ownerAddress"
        /// </summary>
        [JsonProperty("totalSupply")]
        public string TotalSupply { get; set; }

        /// <summary>
        /// Current page
        /// </summary>
        [JsonProperty("page")] 
        public int Page { get; set; }

        /// <summary>
        /// Limit
        /// </summary>
        [JsonProperty("limit")]
        public int Limit { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }
    }
}