using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Builder;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetAllUserCollectiblesHandler : IHandle<Response<PaginationResponseWithResults<IEnumerable<GetAllUserCollectiblesResponseModel>>>, GetAllUserCollectiblesRequestModel>
    {
        private readonly HttpClient _httpClient;

        public GetAllUserCollectiblesHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
        }
        
        /// <summary>
        /// Get All User Collectibles
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-all-collectibles-for-a-user
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>All User Collectibles</returns>
        public async Task<Response<PaginationResponseWithResults<IEnumerable<GetAllUserCollectiblesResponseModel>>>> Handle(GetAllUserCollectiblesRequestModel request)
        {
            var response = new Response<PaginationResponseWithResults<IEnumerable<GetAllUserCollectiblesResponseModel>>>();
            try
            {
                var queryBuilder = new UrlQueryBuilder();
                queryBuilder.AppendParameter("limit", request.Limit);
                queryBuilder.AppendParameter("page", request.Page);
                
                var urlQuery = $"v3/all-collectibles/{request.Address}{queryBuilder.GetQuery()}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_httpClient.BaseAddress + urlQuery),
                };
                var httpResponse = await _httpClient.SendAsync(httRequest);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    response.Message = httpResponse.ReasonPhrase;
                    response.ResponseStatus = ResponseStatus.Fail;
                    return response;
                }
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<PaginationResponseWithResults<IEnumerable<GetAllUserCollectiblesResponseModel>>>(responseString);
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

    public class GetAllUserCollectiblesResponseModel
    {
        [JsonProperty("tokenId")]
        public string TokenId { get; set; }
        [JsonProperty("metadata")]
        public IDictionary<string, object> Metadata { get; set; }
        [JsonProperty("tokenUrl")]
        public string TokenUrl { get; set; }
        [JsonProperty("resaleItem")]
        public ResaleItemV3 ResaleItem { get; set; }
        [JsonProperty("owner")]
        public string Owner { get; set; }
        [JsonProperty("collection")]
        public ContractV3 Collection { get; set; }
    }

    public class GetAllUserCollectiblesRequestModel
    {
        /// <summary>
        /// Required. String. The Ethereum wallet to fetch NFTs for.
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// Optional. String. The page used for pagination.
        /// </summary>
        public string Page { get; set; }
        /// <summary>
        /// Optional. String. The max number of results to return.
        /// </summary>
        public string Limit { get; set; }
    }
}