using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetShowcaseByIdHandler : IHandle<OASISResult<GetShowcaseByIdResponseModel>, GetShowcaseByIdRequestModel>
    {
        private readonly IHttpHandler _httpClient;

        public GetShowcaseByIdHandler(IHttpHandler httpClient)
        {
            _httpClient = httpClient;
        }
        
        /// <summary>
        /// Get Showcase by Id
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-a-showcase-by-id
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>Showcase by id</returns>
        public async Task<OASISResult<GetShowcaseByIdResponseModel>> Handle(GetShowcaseByIdRequestModel request)
        {
            var response = new OASISResult<GetShowcaseByIdResponseModel>();
            try
            {
                var urlQuery = $"https://api2.cargo.build/v3/get-crate-by-id/{request.ShowcaseId}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(urlQuery),
                };
                if (request.Auth)
                {
                    if (string.IsNullOrEmpty(request.AccessJwtToken))
                    {
                        response.IsError = true;
                        response.Message = "Access JWT Token is empty, but Skip Auth is False";
                        return response;
                    }
                    httRequest.Headers.Add("Authorization", $"Bearer {request.AccessJwtToken}");
                }
                var httpResponse = await _httpClient.SendAsync(httRequest);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    response.IsError = true;
                    response.Message = httpResponse.ReasonPhrase;
                    return response;
                }
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<GetShowcaseByIdResponseModel>(responseString);
                response.Result = data;
                return response;
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.Message = e.Message;
                return response;
            }
        }
    }
}