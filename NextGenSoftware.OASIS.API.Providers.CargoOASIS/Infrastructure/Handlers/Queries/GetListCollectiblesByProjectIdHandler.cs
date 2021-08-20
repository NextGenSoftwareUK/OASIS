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
    public class GetListCollectiblesByProjectIdHandler : IHandle<Response<GetCollectiblesListByProjectIdResponseModel>, GetCollectiblesListByProjectIdRequestModel>
    {
        private readonly HttpClient _httpClient;

        public GetListCollectiblesByProjectIdHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
        }
        
        public async Task<Response<GetCollectiblesListByProjectIdResponseModel>> Handle(GetCollectiblesListByProjectIdRequestModel request)
        {
            var response = new Response<GetCollectiblesListByProjectIdResponseModel>();
            try
            {
                var queryBuilder = new UrlQueryBuilder();
                queryBuilder.AppendParameter("ownerAddress", request.OwnerAddress);
                queryBuilder.AppendParameter("limit", request.Limit.ToString());
                queryBuilder.AppendParameter("page", request.Page.ToString());
                var urlQuery = $"v5/get-tokens-by-project/{request.ProjectId}{queryBuilder.GetQuery()}";
                var httRequest = new HttpRequestMessage()
                {
                    RequestUri = new Uri(_httpClient.BaseAddress + urlQuery)
                };
                var httpResponse = await _httpClient.SendAsync(httRequest);
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

    public class GetCollectiblesListByProjectIdResponseModel
    {
        public class CollectiblesListData
        {
            /// <summary>
            /// Contract name
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// Contract symbol
            /// </summary>
            public string Symbol { get; set; }
            /// <summary>
            /// Array of tokens
            /// </summary>
            public IEnumerable<TokenDetail> Results { get; set; }
        }

        [JsonProperty("err")]
        public bool Error { get; set; }

        [JsonProperty("status")] 
        public int Status { get; set; }

        /// <summary>
        /// Collectibles List
        /// </summary>
        [JsonProperty("data")] 
        public CollectiblesListData Data { get; set; }

        /// <summary>
        /// Total number of tokens in contract,
        /// or total number of tokens owned by address
        /// passed as "ownerAddress"
        /// </summary>
        [JsonProperty("totalSupply")]
        public string TotalSupply { get; set; }

        /// <summary>
        /// Current page
        /// </summary>
        [JsonProperty("page")] 
        public int Page { get; set; }

        /// <summary>
        /// Limit
        /// </summary>
        [JsonProperty("limit")]
        public int Limit { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }
    }

    public class GetCollectiblesListByProjectIdRequestModel
    {
        /// <summary>
        /// Required. ID of the contract you wish to query
        /// </summary>
        public string ProjectId { get; set; }
        /// <summary>
        /// Optional. Optional page for pagination.
        /// </summary>
        public int? Page { get; set; }
        /// <summary>
        /// Optional. Limit the number of results returned. Defaults to 10.
        /// </summary>
        public int? Limit { get; set; }
        /// <summary>
        /// Optional. Only tokens owned by this address will be returned in the response.
        /// </summary>
        public string OwnerAddress { get; set; }
    }
}