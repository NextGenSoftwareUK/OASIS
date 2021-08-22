using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands
{
    public class SellHandler : IHandle<Response<SellResponseModel>, SellRequestModel>
    {
        public SellHandler()
        {
            
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<Response<SellResponseModel>> Handle(SellRequestModel request)
        {
            throw new System.NotImplementedException();
        }
    }

    public class SellResponseModel
    {
    }

    public class SellRequestModel
    {
    }
}