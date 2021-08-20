using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetShowcaseBySlugHandler : IHandle<Response<GetShowcaseBySlugResponseModel>, GetShowcaseBySlugRequestModel>
    {
        public Task<Response<GetShowcaseBySlugResponseModel>> Handle(GetShowcaseBySlugRequestModel request)
        {
            throw new System.NotImplementedException();
        }
    }

    public class GetShowcaseBySlugRequestModel
    {
    }

    public class GetShowcaseBySlugResponseModel
    {
    }
}