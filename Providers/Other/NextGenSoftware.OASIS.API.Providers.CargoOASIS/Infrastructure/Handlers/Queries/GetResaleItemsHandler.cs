using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Builder;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Extensions;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetResaleItemsHandler : IHandle<OASISResult<GetResaleItemsResponseModel>, GetResaleItemsRequestModel>
    {
        private readonly IHttpHandler _httpClient;

        public GetResaleItemsHandler(IHttpHandler httpClient)
        {
            _httpClient = httpClient;
        }
        
        /// <summary>
        /// Get resale items
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-a-list-of-collectibles-that-are-for-sale
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>Resale items</returns>
        public async Task<OASISResult<GetResaleItemsResponseModel>> Handle(GetResaleItemsRequestModel request)
        {
            var response = new OASISResult<GetResaleItemsResponseModel>();
            try
            {
                var queryBuilder = new UrlQueryBuilder();
                queryBuilder.AppendParameter("limit", request.Limit);
                queryBuilder.AppendParameter("page", request.Page);
                queryBuilder.AppendParameter("seller", request.Seller);
                queryBuilder.AppendParameter("slug", request.Slug);
                queryBuilder.AppendParameter("slugId", request.SlugId);
                queryBuilder.AppendParameter("sort", request.Sort);
                queryBuilder.AppendParameter("collectionAddress", request.CollectionAddress);
                queryBuilder.AppendParameter("collectionId", request.CollectionId);
                queryBuilder.AppendParameter("projectId", request.ProjectId);
                queryBuilder.AppendParameter("showcaseId", request.ShowcaseId);
                queryBuilder.AppendParameter("chain", request.Chain.GetDescription());

                var urlQuery = $"https://api2.cargo.build/v3/get-resale-items{queryBuilder.GetQuery()}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(urlQuery),
                };
                if (request.Owned != null && request.Owned.Value)
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
                var data = JsonConvert.DeserializeObject<GetResaleItemsResponseModel>(responseString);
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