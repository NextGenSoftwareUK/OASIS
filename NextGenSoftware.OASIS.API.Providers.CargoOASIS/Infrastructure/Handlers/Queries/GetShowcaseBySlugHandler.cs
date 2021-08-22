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
    public class GetShowcaseBySlugHandler : IHandle<Response<GetShowcaseBySlugResponseModel>, GetShowcaseBySlugRequestModel>
    {
        private readonly HttpClient _httpClient;

        public GetShowcaseBySlugHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
        }
        
        /// <summary>
        /// Get showcase by slug
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-a-showcase-by-id
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>Showcase by slug</returns>
        public async Task<Response<GetShowcaseBySlugResponseModel>> Handle(GetShowcaseBySlugRequestModel request)
        {
            var response = new Response<GetShowcaseBySlugResponseModel>();
            try
            {
                var urlQuery = $"v3/get-crate-by-id/{request.Slug}?slugId={request.SlugId}";
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
                var data = JsonConvert.DeserializeObject<GetShowcaseBySlugResponseModel>(responseString);
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

    public class GetShowcaseBySlugRequestModel
    {
        /// <summary>
        /// Required. String. The slug of the showcase
        /// </summary>
        public string Slug { get; set; }
        /// <summary>
        /// Required. String. Slug ID of the showcase
        /// </summary>
        public string SlugId { get; set; }
    }

    public class GetShowcaseBySlugResponseModel
    {
        [JsonProperty("err")]
        public bool Error { get; set; }

        [JsonProperty("status")] 
        public int Status { get; set; }

        [JsonProperty("data")] 
        public GetShowcaseByIdResponse Data { get; set; }
    }
}