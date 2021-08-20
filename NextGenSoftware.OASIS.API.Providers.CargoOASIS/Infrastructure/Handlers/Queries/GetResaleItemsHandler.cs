using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetResaleItemsHandler : IHandle<Response<GetShowcaseByIdResponse>, GetResaleItemsRequestModel>
    {
        public async Task<Response<GetShowcaseByIdResponse>> Handle(GetResaleItemsRequestModel request)
        {
            throw new System.NotImplementedException();
        }
    }

    public class GetResaleItemsRequestModel
    {
    }
}