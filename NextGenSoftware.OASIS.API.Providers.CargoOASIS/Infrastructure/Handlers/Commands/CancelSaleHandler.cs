using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Exceptions;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.TokenStorage;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands
{
    public class CancelSaleHandler : IHandle<Response<CancelSaleResponseModel>, CancelSaleRequestModel>
    {
        private readonly IHttpHandler _httpClient;
        private readonly ITokenStorage _tokenStorage;

        public CancelSaleHandler(IHttpHandler httpClient, ITokenStorage tokenStorage)
        {
            _httpClient = httpClient;
            _tokenStorage = tokenStorage;
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
                var accessToken = await _tokenStorage.GetToken();
                
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
                        { "Authorization", $"Bearer {accessToken}" }
                    }
                };
                var httpResp = await _httpClient.SendAsync(httpReq);
                if (httpResp.StatusCode == HttpStatusCode.Unauthorized)
                    throw new UserNotAuthorizedException();

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
            catch (UserNotAuthorizedException e)
            {
                response.ResponseStatus = ResponseStatus.Unauthorized;
                response.Message = e.Message;
                return response;
            }
            catch (UserNotRegisteredException e)
            {
                response.ResponseStatus = ResponseStatus.NotRegistered;
                response.Message = e.Message;
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