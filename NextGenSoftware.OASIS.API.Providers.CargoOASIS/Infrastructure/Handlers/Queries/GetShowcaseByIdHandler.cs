using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetShowcaseByIdHandler : IHandle<Response<GetShowcaseByIdResponseModel>, GetShowcaseByIdRequestModel>
    {
        public Task<Response<GetShowcaseByIdResponseModel>> Handle(GetShowcaseByIdRequestModel request)
        {
            
        }
    }

    public class GetShowcaseByIdRequestModel
    {
        /// <summary>
        /// Required. String. The ID of the showcase
        /// </summary>
        public string ShowcaseId { get; set; }
        /// <summary>
        /// Optional. Boolean.
        /// If true you must be authenticated and your authentication token will be sent in with the request.
        /// This will be required to get information about a private showcase
        /// </summary>
        public bool Auth { get; set; }
    }

    public class GetShowcaseByIdResponseModel
    {
    }
}