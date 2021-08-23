using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
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
        public async Task<Response<PurchaseResponseModel>> Handle(PurchaseRequestModel request)
        {
            var response = new Response<PurchaseResponseModel>();
            try
            {
                var url = "v4/purchase";
                var requestContent = await JsonConvert.SerializeObjectAsync(new
                {
                    saleId = request.SaleId
                });
                var httpReq = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(_httpClient.BaseAddress + url),
                    Content = new StringContent(requestContent)
                };
                var httpRes = await _httpClient.SendAsync(httpReq);
                if (!httpRes.IsSuccessStatusCode)
                {
                    response.Message = httpRes.ReasonPhrase;
                    response.ResponseStatus = ResponseStatus.Fail;
                    return response;
                }
                var responseContent = await httpRes.Content.ReadAsStringAsync();
                response.Payload.TransactionHash = responseContent;
                return response;
            }
            catch (Exception e)
            {
                response.ResponseStatus = ResponseStatus.Fail;
                response.Message = e.Message;
                return response;
            }
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
    }
}