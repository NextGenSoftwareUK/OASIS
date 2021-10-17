using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands
{
    public class PurchaseHandler : IHandle<Response<PurchaseResponseModel>, PurchaseRequestModel>
    {
        private readonly IHttpHandler _httpClient;
        
        public PurchaseHandler(IHttpHandler httpClient)
        {
            _httpClient = httpClient;
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
                var url = "https://api2.cargo.build/v3/register/v4/purchase";
                var requestContent = JsonConvert.SerializeObject(new
                {
                    saleId = request.SaleId
                });
                var httpReq = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
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
}