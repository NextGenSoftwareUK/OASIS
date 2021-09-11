using System;
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
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetContractsHandler : IHandle<Response<GetContractsResponseModel>, GetContractsRequestHandler>
    {
        private readonly IHttpHandler _httpClient;
        private readonly ITokenStorage _tokenStorage;

        public GetContractsHandler(IHttpHandler httpClient, ITokenStorage tokenStorage)
        {
            _httpClient = httpClient;
            _tokenStorage = tokenStorage;
        }
        
        /// <summary>
        /// Get contracts
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-a-list-of-collections-on-cargo
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>Contracts</returns>
        public async Task<Response<GetContractsResponseModel>> Handle(GetContractsRequestHandler request)
        {
            var response = new Response<GetContractsResponseModel>();
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
                    var accessToken = await _tokenStorage.GetToken();
                    httRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
                }
                var httpResponse = await _httpClient.SendAsync(httRequest);
                if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                    throw new UserNotAuthorizedException();
                if (!httpResponse.IsSuccessStatusCode)
                {
                    response.Message = httpResponse.ReasonPhrase;
                    response.ResponseStatus = ResponseStatus.Fail;
                    return response;
                }
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<GetContractsResponseModel>(responseString);
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