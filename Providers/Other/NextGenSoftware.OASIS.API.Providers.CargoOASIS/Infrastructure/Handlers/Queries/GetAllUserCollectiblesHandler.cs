using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Builder;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetAllUserCollectiblesHandler : IHandle<OASISResult<PaginationResponseWithResults<IEnumerable<GetAllUserCollectiblesResponseModel>>>, GetAllUserCollectiblesRequestModel>
    {
        private readonly IHttpHandler _httpClient;

        public GetAllUserCollectiblesHandler(IHttpHandler httpClient)
        {
            _httpClient = httpClient;
        }
        
        /// <summary>
        /// Get All User Collectibles
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-all-collectibles-for-a-user
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>All User Collectibles</returns>
        public async Task<OASISResult<PaginationResponseWithResults<IEnumerable<GetAllUserCollectiblesResponseModel>>>> Handle(GetAllUserCollectiblesRequestModel request)
        {
            var response = new OASISResult<PaginationResponseWithResults<IEnumerable<GetAllUserCollectiblesResponseModel>>>();
            try
            {
                var queryBuilder = new UrlQueryBuilder();
                queryBuilder.AppendParameter("limit", request.Limit);
                queryBuilder.AppendParameter("page", request.Page);
                
                var urlQuery = $"https://api2.cargo.build/v3/all-collectibles/{request.Address}{queryBuilder.GetQuery()}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(urlQuery),
                };
                var httpResponse = await _httpClient.SendAsync(httRequest);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    response.IsError = true;
                    response.Message = httpResponse.ReasonPhrase;
                    return response;
                }
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<PaginationResponseWithResults<IEnumerable<GetAllUserCollectiblesResponseModel>>>(responseString);
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