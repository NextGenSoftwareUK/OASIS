using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetAllUserCollectiblesHandler : IHandle<PaginationResponseWithResults<GetAllUserCollectiblesResponseModel>, GetAllUserCollectiblesRequestModel>
    {
        public Task<PaginationResponseWithResults<GetAllUserCollectiblesResponseModel>> Handle(GetAllUserCollectiblesRequestModel request)
        {
            throw new System.NotImplementedException();
        }
    }

    public class GetAllUserCollectiblesResponseModel
    {
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