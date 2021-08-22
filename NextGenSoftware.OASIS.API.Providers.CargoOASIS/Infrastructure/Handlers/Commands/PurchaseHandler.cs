using System;
using System.Net.Http;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands
{
    public class PurchaseHandler : IHandle<Response<PurchaseResponseModel>, PurchaseRequestModel>
    {
        private readonly HttpClient _httpClient;
        public PurchaseHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
            _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
        }
        
        /// <summary>
        /// Creates a purchase request
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#purchase-a-collectible
        /// </summary>
        /// <param name="request">Request Parameters</param>
        /// <returns>Transaction Hash</returns>
        public Task<Response<PurchaseResponseModel>> Handle(PurchaseRequestModel request)
        {
            throw new System.NotImplementedException();
        }
    }

    public class PurchaseResponseModel
    {
        /// <summary>
        /// The response will contain a transaction hash.
        /// </summary>
        public string TransactionHash { get; set; }
    }

    public class PurchaseRequestModel
    {
        /// <summary>
        /// Required. The ID of the sale
        /// </summary>
        public string SaleId { get; set; }
        /// <summary>
        /// Required. Valid options arexdai or ethThe chain the NFT lives on.
        /// </summary>
        public string Chain { get; set; }
    }
}