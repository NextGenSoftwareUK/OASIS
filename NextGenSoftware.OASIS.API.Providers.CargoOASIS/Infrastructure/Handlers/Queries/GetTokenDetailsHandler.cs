using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetTokenDetailsHandler : IHandle<Response<GetTokenDetailsResponseModel>, GetTokenDetailsRequestModel>
    {
        private readonly IHttpHandler _httpClient;

        public GetTokenDetailsHandler(IHttpHandler httpClient)
        {
            _httpClient = httpClient;
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
                var urlQuery = $"https://api2.cargo.build/v5/get-token-details/{request.ProjectId}/{request.CollectibleId}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(urlQuery)
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
}