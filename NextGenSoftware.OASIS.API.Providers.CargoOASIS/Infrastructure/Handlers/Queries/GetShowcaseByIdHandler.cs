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
    public class GetShowcaseByIdHandler : IHandle<Response<GetShowcaseByIdResponseModel>, GetShowcaseByIdRequestModel>
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessToken = string.Empty;

        public GetShowcaseByIdHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
        }
        
        public async Task<Response<GetShowcaseByIdResponseModel>> Handle(GetShowcaseByIdRequestModel request)
        {
            var response = new Response<GetShowcaseByIdResponseModel>();
            try
            {
                var urlQuery = $"v3/get-crate-by-id/{request.ShowcaseId}";
                var httRequest = new HttpRequestMessage()
                {
                    RequestUri = new Uri(_httpClient.BaseAddress + urlQuery),
                };
                if (request.Auth)
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                }
                var httpResponse = await _httpClient.SendAsync(httRequest);
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<GetShowcaseByIdResponseModel>(responseString);
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

    public class GetShowcaseByIdRequestModel
    {
        /// <summary>
        /// Required. String. The ID of the showcase
        /// </summary>
        public string ShowcaseId { get; set; }
        /// <summary>
        /// Optional. Boolean.
        /// If true you must be authenticated and your authentication token will be sent in with the request.
        /// This will be required to get information about a private showcase
        /// </summary>
        public bool Auth { get; set; }
    }

    public class GetShowcaseByIdResponseModel
    {
        [JsonProperty("err")]
        public bool Error { get; set; }

        [JsonProperty("status")] 
        public int Status { get; set; }
        
        [JsonProperty("data")]
        public GetShowcaseByIdResponse Data { get; set; }
    }
}