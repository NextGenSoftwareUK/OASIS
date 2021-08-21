using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetProjectMetadataHandler : IHandle<Response<GetProjectMetadataResponseModel>, GetProjectMetadataRequestModel>
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessToken = string.Empty;

        public GetProjectMetadataHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
        }
        
        /// <summary>
        /// Get project metadata
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-information-about-a-collection
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>Project metadata</returns>
        public async Task<Response<GetProjectMetadataResponseModel>> Handle(GetProjectMetadataRequestModel request)
        {
            var response = new Response<GetProjectMetadataResponseModel>();
            try
            {
                var urlQuery = $"v5/project-metadata/{request.ProjectId}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_httpClient.BaseAddress + urlQuery)
                };
                if (request.UseAuth != null && request.UseAuth.Value)
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                }
                var httpResponse = await _httpClient.SendAsync(httRequest);
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<GetProjectMetadataResponseModel>(responseString);
                response.Payload = data;
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

    public class GetProjectMetadataResponseModel
    {
        [JsonProperty("err")]
        public bool Error { get; set; }

        [JsonProperty("status")] 
        public int Status { get; set; }

        [JsonProperty("data")]
        public ContractMetadata Data { get; set; }
    }

    public class GetProjectMetadataRequestModel
    {
        /// <summary>
        /// Required. The ID of the project. This can be found in the URL bar when viewing the project on Cargo.
        /// </summary>
        public string ProjectId { get; set; }
        /// <summary>
        /// Optional. If true this method requires authentication. Will return isOwned boolean in response
        /// </summary>
        public bool? UseAuth { get; set; }
    }
}