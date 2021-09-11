using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Exceptions;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.TokenStorage;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetShowcaseByIdHandler : IHandle<Response<GetShowcaseByIdResponseModel>, GetShowcaseByIdRequestModel>
    {
        private readonly IHttpHandler _httpClient;
        private readonly  ITokenStorage _tokenStorage;

        public GetShowcaseByIdHandler(IHttpHandler httpClient, ITokenStorage tokenStorage)
        {
            _httpClient = httpClient;
            _tokenStorage = tokenStorage;
        }
        
        /// <summary>
        /// Get Showcase by Id
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-a-showcase-by-id
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>Showcase by id</returns>
        public async Task<Response<GetShowcaseByIdResponseModel>> Handle(GetShowcaseByIdRequestModel request)
        {
            var response = new Response<GetShowcaseByIdResponseModel>();
            try
            {
                var urlQuery = $"https://api2.cargo.build/v3/get-crate-by-id/{request.ShowcaseId}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(urlQuery),
                };
                if (request.Auth)
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
                var data = JsonConvert.DeserializeObject<GetShowcaseByIdResponseModel>(responseString);
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