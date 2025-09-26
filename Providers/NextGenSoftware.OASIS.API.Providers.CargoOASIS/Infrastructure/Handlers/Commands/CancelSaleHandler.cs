using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands
{
    public class CancelSaleHandler : IHandle<OASISResult<CancelSaleResponseModel>, CancelSaleRequestModel>
    {
        private readonly IHttpHandler _httpClient;

        public CancelSaleHandler(IHttpHandler httpClient)
        {
            _httpClient = httpClient;
        }
        
        /// <summary>
        /// Cancel a sale
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>Cancel sale response</returns>
        public async Task<OASISResult<CancelSaleResponseModel>> Handle(CancelSaleRequestModel request)
        {
            var response = new OASISResult<CancelSaleResponseModel>();
            try
            {
                var url = "https://api2.cargo.build/v3/cancel-sale";
                var requestContent = JsonConvert.SerializeObject(new
                {
                    resaleItemId = request.ResaleItemId
                });
                var httpReq = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Content = new StringContent(requestContent),
                    Headers =
                    {
                        { "Content-Type", "application/json" },
                        { "Authorization", $"Bearer {request.AccessJwtToken}" }
                    }
                };
                var httpResp = await _httpClient.SendAsync(httpReq);
                if (!httpResp.IsSuccessStatusCode)
                {
                    response.Message = httpResp.ReasonPhrase;
                    response.IsError = true;
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                var responseContent = await httpResp.Content.ReadAsStringAsync();
                response.Result = JsonConvert.DeserializeObject<CancelSaleResponseModel>(responseContent);
                return response;
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.Message = e.Message;
                ErrorHandling.HandleError(ref response, e.Message);
                return response;
            }
        }
    }

    public class CancelSaleResponseModel
    {
        public class CancelSaleData
        {
            [JsonProperty("signatureGenerated")]
            public bool SignatureGenerated { get; set; }

            [JsonProperty("code")] 
            public string Code { get; set; }
        }
        
        [JsonProperty("err")]
        public bool Error { get; set; }

        [JsonProperty("status")] 
        public int Status { get; set; }

        [JsonProperty("data")]
        public CancelSaleData Data { get; set; }
    }

    public class CancelSaleRequestModel
    {
        /// <summary>
        /// The ID of the resale item.
        /// </summary>
        public string ResaleItemId { get; set; }

        public string AccessJwtToken { get; set; }
    }
}