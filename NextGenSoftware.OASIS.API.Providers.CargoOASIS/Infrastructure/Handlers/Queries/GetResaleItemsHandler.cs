using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Builder;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Exceptions;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Extensions;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.TokenStorage;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetResaleItemsHandler : IHandle<Response<GetResaleItemsResponseModel>, GetResaleItemsRequestModel>
    {
        private readonly IHttpHandler _httpClient;
        private readonly  ITokenStorage _tokenStorage;

        public GetResaleItemsHandler(IHttpHandler httpClient, ITokenStorage tokenStorage)
        {
            _httpClient = httpClient;
            _tokenStorage = tokenStorage;
        }
        
        /// <summary>
        /// Get resale items
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-a-list-of-collectibles-that-are-for-sale
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>Resale items</returns>
        public async Task<Response<GetResaleItemsResponseModel>> Handle(GetResaleItemsRequestModel request)
        {
            var response = new Response<GetResaleItemsResponseModel>();
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
                    var accessToken = await _tokenStorage.GetToken();
                    httRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
                }
                var httpResponse = await _httpClient.SendAsync(httRequest);
                if(httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                    throw new UserNotAuthorizedException();     
                if (!httpResponse.IsSuccessStatusCode)
                {
                    response.Message = httpResponse.ReasonPhrase;
                    response.ResponseStatus = ResponseStatus.Fail;
                    return response;
                }
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<GetResaleItemsResponseModel>(responseString);
                response.Payload = data;
                return response;
            }
            catch (UserNotAuthorizedException e)
            {
                response.ResponseStatus = ResponseStatus.Unauthorized;
                response.Message = e.Message;
                return response;
            }
            catch (UserNotRegisteredException e)
            {
                response.ResponseStatus = ResponseStatus.NotRegistered;
                response.Message = e.Message;
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