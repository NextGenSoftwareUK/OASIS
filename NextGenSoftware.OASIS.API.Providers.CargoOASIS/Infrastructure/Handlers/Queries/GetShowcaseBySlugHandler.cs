using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetShowcaseBySlugHandler : IHandle<OASISResult<GetShowcaseBySlugResponseModel>, GetShowcaseBySlugRequestModel>
    {
        private readonly IHttpHandler _httpClient;

        public GetShowcaseBySlugHandler(IHttpHandler httpClient)
        {
            _httpClient = httpClient;
        }
        
        /// <summary>
        /// Get showcase by slug
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-a-showcase-by-id
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>Showcase by slug</returns>
        public async Task<OASISResult<GetShowcaseBySlugResponseModel>> Handle(GetShowcaseBySlugRequestModel request)
        {
            var response = new OASISResult<GetShowcaseBySlugResponseModel>();
            try
            {
                var urlQuery = $"https://api2.cargo.build/v3/get-crate-by-id/{request.Slug}?slugId={request.SlugId}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(urlQuery)
                };
                var httpResponse = await _httpClient.SendAsync(httRequest);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    response.Message = httpResponse.ReasonPhrase;
                    response.IsError = true;
                    return response;
                }
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<GetShowcaseBySlugResponseModel>(responseString);
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