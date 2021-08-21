using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetUserTokensByContractHandler : IHandle<Response<GetUserTokensByContractResponseModel>, GetUserTokensByContractRequestModel>
    {
        /// <summary>
        /// Get user tokens by contract
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-collectibles-for-a-user-by-collection
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>User tokens</returns>
        public Task<Response<GetUserTokensByContractResponseModel>> Handle(GetUserTokensByContractRequestModel request)
        {
            throw new System.NotImplementedException();
        }
    }

    public class GetUserTokensByContractResponseModel
    {
        [JsonProperty("err")]
        public bool Error { get; set; }

        [JsonProperty("status")] 
        public int Status { get; set; }

        [JsonProperty("data")]
        public PaginationResponseWithResults<IEnumerable<GetUserTokensByContractResponse>> Data { get; set; }
    }

    public class GetUserTokensByContractRequestModel
    {
        /// <summary>
        /// Optional. String. Page of results to display.
        /// </summary>
        public string Page { get; set; }
        /// <summary>
        /// Optional. String. Limit of results to display per page.
        /// </summary>
        public string Limit { get; set; }
        /// <summary>
        /// Required. String. ID of collection 
        /// </summary>
        public string ContractId { get; set; }
        /// <summary>
        /// Optional. String. Ethereum wallet address of user. Should set skipAuth option to true when using address.
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// Optional. Boolean. Skips using the current logged in users address and will use the address value
        /// </summary>
        public bool? SkipAuth { get; set; }
    }
}