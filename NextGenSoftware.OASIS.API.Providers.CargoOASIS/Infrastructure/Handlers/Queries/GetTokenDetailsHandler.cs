using System;
using System.Net.Http;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetTokenDetailsHandler : IHandle<GetTokenDetailsResponseModel, GetTokenDetailsRequestModel>
    {
        private readonly HttpClient _httpClient;

        public GetTokenDetailsHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
        }
        
        public Task<GetTokenDetailsResponseModel> Handle(GetTokenDetailsRequestModel request)
        {
            throw new System.NotImplementedException();
        }
    }

    public class GetTokenDetailsRequestModel
    {
    }

    public class GetTokenDetailsResponseModel
    {
    }
}