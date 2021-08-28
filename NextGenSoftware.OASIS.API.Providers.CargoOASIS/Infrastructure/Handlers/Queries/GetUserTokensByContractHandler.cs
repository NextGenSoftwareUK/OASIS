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
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.TokenStorage;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetUserTokensByContractHandler : IHandle<Response<GetUserTokensByContractResponseModel>, GetUserTokensByContractRequestModel>
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenStorage _tokenStorage;

        public GetUserTokensByContractHandler()
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
        /// Get user tokens by contract
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-collectibles-for-a-user-by-collection
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>User tokens</returns>
        public async Task<Response<GetUserTokensByContractResponseModel>> Handle(GetUserTokensByContractRequestModel request)
        {
            var response = new Response<GetUserTokensByContractResponseModel>();
            try
            {
                var queryBuilder = new UrlQueryBuilder();
                queryBuilder.AppendParameter("limit", request.Limit);
                queryBuilder.AppendParameter("page", request.Page);
                queryBuilder.AppendParameter("address", request.Address);

                var urlQuery = $"v3/get-user-tokens/{request.ContractId}{queryBuilder.GetQuery()}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_httpClient.BaseAddress + urlQuery),
                };
                if (request.SkipAuth != null && !request.SkipAuth.Value)
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
                var data = JsonConvert.DeserializeObject<GetUserTokensByContractResponseModel>(responseString);
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

    public class GetUserTokensByContractResponseModel
    {
        [JsonProperty("err")]
        public bool Error { get; set; }

        [JsonProperty("status")] 
        public int Status { get; set; }

        [JsonProperty("data")]
        public PaginationResponseWithResults<IEnumerable<GetUserTokensByContractResponse>> Data { get; set; }
    }

    public class GetUserTokensByContractRequestModel
    {
        /// <summary>
        /// Optional. String. Page of results to display.
        /// </summary>
        public string Page { get; set; }
        /// <summary>
        /// Optional. String. Limit of results to display per page.
        /// </summary>
        public string Limit { get; set; }
        /// <summary>
        /// Required. String. ID of collection 
        /// </summary>
        public string ContractId { get; set; }
        /// <summary>
        /// Optional. String. Ethereum wallet address of user. Should set skipAuth option to true when using address.
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// Optional. Boolean. Skips using the current logged in users address and will use the address value
        /// </summary>
        public bool? SkipAuth { get; set; }
    }
}