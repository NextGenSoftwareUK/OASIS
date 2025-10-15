using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Builder;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetContractsHandler : IHandle<OASISResult<GetContractsResponseModel>, GetContractsRequestHandler>
    {
        private readonly IHttpHandler _httpClient;

        public GetContractsHandler(IHttpHandler httpClient)
        {
            _httpClient = httpClient;
        }
        
        /// <summary>
        /// Get contracts
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-a-list-of-collections-on-cargo
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>Contracts</returns>
        public async Task<OASISResult<GetContractsResponseModel>> Handle(GetContractsRequestHandler request)
        {
            var response = new OASISResult<GetContractsResponseModel>();
            try
            {
                var queryBuilder = new UrlQueryBuilder();
                queryBuilder.AppendParameter("limit", request.Limit);
                queryBuilder.AppendParameter("owned", request.Owned.ToString());
                queryBuilder.AppendParameter("page", request.Page);
                queryBuilder.AppendParameter("address", request.Address);
                queryBuilder.AppendParameter("cargoContract", request.CargoContract.ToString());
                queryBuilder.AppendParameter("hasTokens", request.HasTokens.ToString());
                queryBuilder.AppendParameter("showcaseId", request.ShowcaseId);
                queryBuilder.AppendParameter("useAuthToken", request.UseAuthToken.ToString());

                var urlQuery = $"https://api2.cargo.build/v3/get-contracts{queryBuilder.GetQuery()}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(urlQuery),
                    Headers = { { "Content-Type", "application/json" } }
                };
                if (request.SkipAuth != null && !request.SkipAuth.Value)
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
                if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    response.Message = httpResponse.ReasonPhrase;
                    response.IsError = true;
                    return response;
                }
                
                if (!httpResponse.IsSuccessStatusCode)
                {
                    response.IsError = false;
                    response.Message = httpResponse.ReasonPhrase;
                    return response;
                }
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<GetContractsResponseModel>(responseString);
                response.Result = data;
                return response;
            }
            catch (Exception e)
            {
                response.Message = e.Message;
                return response;
            }
        }
    }
}