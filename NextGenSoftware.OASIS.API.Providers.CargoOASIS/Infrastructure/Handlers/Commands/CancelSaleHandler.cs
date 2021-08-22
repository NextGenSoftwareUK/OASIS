using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands
{
    public class CancelSaleHandler : IHandle<Response<CancelSaleResponseModel>, CancelSaleRequestModel>
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessToken = string.Empty;

        public CancelSaleHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
            _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }
        
        /// <summary>
        /// Cancel a sale
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>Cancel sale response</returns>
        public async Task<Response<CancelSaleResponseModel>> Handle(CancelSaleRequestModel request)
        {
            var response = new Response<CancelSaleResponseModel>();
            try
            {
                var url = "v3/cancel-sale";
                var requestContent = await JsonConvert.SerializeObjectAsync(new
                {
                    resaleItemId = request.ResaleItemId
                });
                var httpReq = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(_httpClient.BaseAddress + url),
                    Content = new StringContent(requestContent)
                };
                var httpResp = await _httpClient.SendAsync(httpReq);
                if (!httpResp.IsSuccessStatusCode)
                {
                    response.Message = httpResp.ReasonPhrase;
                    response.ResponseStatus = ResponseStatus.Fail;
                    return response;
                }
                var responseContent = await httpResp.Content.ReadAsStringAsync();
                response.Payload = JsonConvert.DeserializeObject<CancelSaleResponseModel>(responseContent);
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
    }
}