using System;
using System.Collections.Generic;
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
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetResaleItemsHandler : IHandle<Response<GetResaleItemsResponseModel>, GetResaleItemsRequestModel>
    {
        private readonly HttpClient _httpClient;
        private readonly  ITokenStorage _tokenStorage;

        public GetResaleItemsHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
            _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
            _tokenStorage = TokenStorageFactory.GetMemoryCacheTokenStorage();
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

                var urlQuery = $"v3/get-resale-items{queryBuilder.GetQuery()}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_httpClient.BaseAddress + urlQuery),
                };
                if (request.Owned != null && request.Owned.Value)
                {
                    var accessToken = await _tokenStorage.GetToken();
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
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

    public class GetResaleItemsResponseModel
    {
        public class GetResaleItemData
        {
            [JsonProperty("limit")]
            public string Limit { get; set; }

            [JsonProperty("page")]
            public string Page { get; set; }

            [JsonProperty("totalPages")] 
            public string TotalPages { get; set; }

            [JsonProperty("results")] 
            public IEnumerable<ResaleItemV3> Results { get; set; }
        }
        
        [JsonProperty("err")]
        public bool Error { get; set; }

        [JsonProperty("status")] 
        public int Status { get; set; }
        
        [JsonProperty("data")]
        public GetResaleItemData Data { get; set; }
    }

    public class GetResaleItemsRequestModel
    {
        /// <summary>
        /// Optional. String. Display resale items by a given showcase.
        /// </summary>
        [JsonProperty("showcaseId")]
        public string ShowcaseId { get; set; }

        /// <summary>
        /// Optional. String. The ID of the project (smart contract) on Cargo. Use this over collectionAddress. 
        /// </summary>
        [JsonProperty("projectId")]
        public string ProjectId { get; set; }

        /// <summary>
        /// Optional. String. Display resale items for a given collection.
        /// </summary>
        [JsonProperty("collectionId")] 
        public string CollectionId { get; set; }
        
        /// <summary>
        /// Optional. String. Display resale items for a given collection address.
        /// </summary>
        [JsonProperty("collectionAddress")]
        public string CollectionAddress { get; set; }

        /// <summary>
        ///  Optional. eth or xdai. Specify chain.
        /// </summary>
        [JsonProperty("chain")]
        [JsonConverter(typeof(Chain))]
        public Chain? Chain { get; set; }

        /// <summary>
        /// Optional. String. Page in results to display.
        /// </summary>
        [JsonProperty("page")]
        public string Page { get; set; }

        /// <summary>
        /// Optional. String. Limit of results per page.
        /// </summary>
        [JsonProperty("limit")] 
        public string Limit { get; set; }

        /// <summary>
        /// Optional. Boolean. Display resale items that are owned by the current authenticated user only.
        /// </summary>
        [JsonProperty("owned")] 
        public bool? Owned { get; set; }

        /// <summary>
        /// Optional. String. Filter resale items by showcase slug. Can be used as an alternative to showcaseId. slugId required when this is passed.
        /// </summary>
        [JsonProperty("slug")] 
        public string Slug { get; set; }

        /// <summary>
        /// Optional. String. Required when slug is passed
        /// </summary>
        [JsonProperty("slugId")]
        public string SlugId { get; set; }

        /// <summary>
        /// Optional. String. Ethereum wallet address of seller. Will only return items for this seller.
        /// </summary>
        [JsonProperty("seller")]
        public string Seller { get; set; }

        /// <summary>
        /// Optional. String. One of new , high-to-low , or low-to-high
        /// </summary>
        [JsonProperty("sort")] 
        public string Sort { get; set; }
    }
}