using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetAllUserCollectiblesHandler : IHandle<PaginationResponseWithResults<IEnumerable<GetAllUserCollectiblesResponseModel>>, GetAllUserCollectiblesRequestModel>
    {
        public Task<PaginationResponseWithResults<IEnumerable<GetAllUserCollectiblesResponseModel>>> Handle(GetAllUserCollectiblesRequestModel request)
        {
            throw new System.NotImplementedException();
        }
    }

    public class GetAllUserCollectiblesResponseModel
    {
        /*results: {
        tokenId: string;
        metadata: Record<string, any>;
        tokenURI: string;
        resaleItem?: {
            sellerAddress: string;
            tokenAddress: string;
            tokenId: string;
            resaleItemId: string;
            price: string;
            fromVendor: boolean;
            metadata: Record<string, any>;
        };
        owner: string;
        collection: {
            _id: string;
            address: string;
            name: string;
            symbol: string;
            supportsMetadata: boolean;
            tags?: string[];
            owned?: boolean;
            totalOwned?: number;
            createdAt: string;
        };
   }[];
   page: string;
   limit: string;
   totalPages: string;*/
    }

    public class GetAllUserCollectiblesRequestModel
    {
        /// <summary>
        /// Required. String. The Ethereum wallet to fetch NFTs for.
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// Optional. String. The page used for pagination.
        /// </summary>
        public string Page { get; set; }
        /// <summary>
        /// Optional. String. The max number of results to return.
        /// </summary>
        public string Limit { get; set; }
    }
}