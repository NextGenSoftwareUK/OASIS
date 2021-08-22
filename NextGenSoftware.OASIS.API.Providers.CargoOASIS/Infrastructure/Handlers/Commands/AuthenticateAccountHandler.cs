using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands
{
    public class AuthenticateAccountHandler : ISingleHandler<Response<CreateAccountResponseModel>>
    {
        public AuthenticateAccountHandler()
        {
            
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<Response<CreateAccountResponseModel>> Handle()
        {
            throw new System.NotImplementedException();
        }
    }
}