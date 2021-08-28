using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetTokenDetailsHandler : IHandle<Response<GetTokenDetailsResponseModel>, GetTokenDetailsRequestModel>
    {
        private readonly HttpClient _httpClient;

        public GetTokenDetailsHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
        }
        
        /// <summary>
        /// Gets Token Details by Project Id and Token Id
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-collectible-details
        /// </summary>
        /// <param name="request">Request Parameters</param>
        /// <returns>Token Details</returns>
        public async Task<Response<GetTokenDetailsResponseModel>> Handle(GetTokenDetailsRequestModel request)
        {
            var response = new Response<GetTokenDetailsResponseModel>();
            try
            {
                var urlQuery = $"v5/get-token-details/{request.ProjectId}/{request.CollectibleId}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_httpClient.BaseAddress + urlQuery)
                };
                var httpResponse = await _httpClient.SendAsync(httRequest);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    response.Message = httpResponse.ReasonPhrase;
                    response.ResponseStatus = ResponseStatus.Fail;
                    return response;
                }
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<GetTokenDetailsResponseModel>(responseString);
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

    public class GetTokenDetailsRequestModel
    {
        /// <summary>
        /// The ID of the project on Cargo.
        /// This can be found in the URL bar when viewing the project on Cargo.
        /// </summary>
        public string ProjectId { get; set; }
        /// <summary>
        /// The ID of the collectible within the collection.
        /// </summary>
        public string CollectibleId { get; set; }
    }

    public class GetTokenDetailsResponseModel
    {
        [JsonProperty("err")]
        public bool Error { get; set; }

        [JsonProperty("status")] 
        public int Status { get; set; }

        [JsonProperty("data")]
        public TokenDetail Data { get; set; }
    }
}