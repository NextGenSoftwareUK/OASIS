using System;
using System.Net.Http;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands
{
    public class CancelSaleHandler : IHandle<Response<CancelSaleResponseModel>, CancelSaleRequestModel>
    {
        private readonly HttpClient _httpClient;

        public CancelSaleHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
            _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
        }
        
        /// <summary>
        /// Cancel a sale
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>Cancel sale response</returns>
        public Task<Response<CancelSaleResponseModel>> Handle(CancelSaleRequestModel request)
        {
            throw new System.NotImplementedException();
        }
    }

    public class CancelSaleResponseModel
    {
    }

    public class CancelSaleRequestModel
    {
    }
}