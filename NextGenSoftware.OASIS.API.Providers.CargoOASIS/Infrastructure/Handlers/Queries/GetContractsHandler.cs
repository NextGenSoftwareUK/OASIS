using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetContractsHandler : IHandle<Response<GetContractsResponseModel>, GetContractsRequestHandler>
    {
        public Task<Response<GetContractsResponseModel>> Handle(GetContractsRequestHandler request)
        {
            throw new System.NotImplementedException();
        }
    }

    public class GetContractsRequestHandler
    {
    }

    public class GetContractsResponseModel
    {
    }
}