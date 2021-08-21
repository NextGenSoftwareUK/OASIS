using System;
using System.Net.Http;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands
{
    public class CreateAccountHandler : IHandle<Response<CreateAccountResponseModel>, CreateAccountRequestModel>
    {
        private readonly HttpClient _httpClient;

        public CreateAccountHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
        }
        
        /// <summary>
        /// Creates an account in Cargo
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#create-an-account
        /// </summary>
        /// <param name="request">Request Parameters</param>
        /// <returns>Create Account Response</returns>
        public Task<Response<CreateAccountResponseModel>> Handle(CreateAccountRequestModel request)
        {
            throw new System.NotImplementedException();
        }
    }

    public class CreateAccountRequestModel
    {
    }

    public class CreateAccountResponseModel
    {
    }
}