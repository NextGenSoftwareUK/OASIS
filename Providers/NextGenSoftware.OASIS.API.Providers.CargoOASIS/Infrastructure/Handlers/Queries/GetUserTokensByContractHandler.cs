using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Builder;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetUserTokensByContractHandler : IHandle<OASISResult<GetUserTokensByContractResponseModel>, GetUserTokensByContractRequestModel>
    {
        private readonly IHttpHandler _httpClient;

        public GetUserTokensByContractHandler(IHttpHandler httpClient)
        {
            _httpClient = httpClient;
        }
        
        /// <summary>
        /// Get user tokens by contract
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-collectibles-for-a-user-by-collection
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>User tokens</returns>
        public async Task<OASISResult<GetUserTokensByContractResponseModel>> Handle(GetUserTokensByContractRequestModel request)
        {
            var response = new OASISResult<GetUserTokensByContractResponseModel>();
            try
            {
                var queryBuilder = new UrlQueryBuilder();
                queryBuilder.AppendParameter("limit", request.Limit);
                queryBuilder.AppendParameter("page", request.Page);
                queryBuilder.AppendParameter("address", request.Address);

                var urlQuery = $"https://api2.cargo.build/v3/get-user-tokens/{request.ContractId}{queryBuilder.GetQuery()}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(urlQuery),
                };
                if (request.SkipAuth != null && !request.SkipAuth.Value)
                {
                    if (string.IsNullOrEmpty(request.AccessJwtToken))
                    {
                        response.IsError = true;
                        response.Message = "Access JWT Token is empty, but Skip Auth is False";
                        ErrorHandling.HandleError(ref response, response.Message);
                        return response;
                    }
                    httRequest.Headers.Add("Authorization", $"Bearer {request.AccessJwtToken}");
                }
                var httpResponse = await _httpClient.SendAsync(httRequest);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    response.Message = httpResponse.ReasonPhrase;
                    response.IsError = true;
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<GetUserTokensByContractResponseModel>(responseString);
                response.Result = data;
                return response;
            }
            catch (Exception e)
            {
                response.Message = e.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, e.Message);
                return response;
            }
        }
    }
}