using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetOrdersHandler : IHandle<Response<PaginationResponseWithResults<IEnumerable<Order>>>, OrderParams>
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessToken = string.Empty;

        public GetOrdersHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }
        
        public Task<Response<PaginationResponseWithResults<IEnumerable<Order>>>> Handle(OrderParams request)
        {
            throw new System.NotImplementedException();
        }
    }
}