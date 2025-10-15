using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetProjectMetadataHandler : IHandle<OASISResult<GetProjectMetadataResponseModel>, GetProjectMetadataRequestModel>
    {
        private readonly IHttpHandler _httpClient;

        public GetProjectMetadataHandler(IHttpHandler httpClient)
        {
            _httpClient = httpClient;
        }
        
        /// <summary>
        /// Get project metadata
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-information-about-a-collection
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>Project metadata</returns>
        public async Task<OASISResult<GetProjectMetadataResponseModel>> Handle(GetProjectMetadataRequestModel request)
        {
            var response = new OASISResult<GetProjectMetadataResponseModel>();
            try
            {
                var urlQuery = $"https://api2.cargo.build/v5/project-metadata/{request.ProjectId}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(urlQuery)
                };
                if (request.UseAuth != null && request.UseAuth.Value)
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
                    response.Message = httpResponse.ReasonPhrase;
                    return response;
                }
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<GetProjectMetadataResponseModel>(responseString);
                response.Result = data;
                return response;
            }
            catch (Exception e)
            {
                response.Message = e.Message;
                return response;
            }        
        }
    }
}