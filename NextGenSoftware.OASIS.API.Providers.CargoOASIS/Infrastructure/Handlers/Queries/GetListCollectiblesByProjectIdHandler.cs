using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Builder;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetListCollectiblesByProjectIdHandler : IHandle<Response<GetCollectiblesListByProjectIdResponseModel>, GetCollectiblesListByProjectIdRequestModel>
    {
        private readonly IHttpHandler _httpClient;

        public GetListCollectiblesByProjectIdHandler(IHttpHandler httpClient)
        {
            _httpClient = httpClient;
        }
        
        /// <summary>
        /// Gets collectibles list by project id
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-a-list-of-collectibles-by-project-id 
        /// </summary>
        /// <param name="request">Request Parameters</param>
        /// <returns>Collectibles list</returns>
        public async Task<Response<GetCollectiblesListByProjectIdResponseModel>> Handle(GetCollectiblesListByProjectIdRequestModel request)
        {
            var response = new Response<GetCollectiblesListByProjectIdResponseModel>();
            try
            {
                var queryBuilder = new UrlQueryBuilder();
                queryBuilder.AppendParameter("ownerAddress", request.OwnerAddress);
                queryBuilder.AppendParameter("limit", request.Limit.ToString());
                queryBuilder.AppendParameter("page", request.Page.ToString());
                var urlQuery = $"https://api2.cargo.build/v5/get-tokens-by-project/{request.ProjectId}{queryBuilder.GetQuery()}";
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
                var data = JsonConvert.DeserializeObject<GetCollectiblesListByProjectIdResponseModel>(responseString);
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