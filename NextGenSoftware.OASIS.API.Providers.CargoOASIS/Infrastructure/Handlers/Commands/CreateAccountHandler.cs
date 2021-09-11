using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.SignatureProviders;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.TokenStorage;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands
{
    public class CreateAccountHandler : IHandle<Response<CreateAccountResponseModel>, CreateAccountRequestModel>
    {
        private readonly IHttpHandler _httpClient;
        private readonly ITokenStorage _tokenStorage;
        private readonly ISignatureProvider _signatureProvider;

        public CreateAccountHandler(IHttpHandler httpClient, ITokenStorage tokenStorage, ISignatureProvider signatureProvider)
        {
            _httpClient = httpClient;
            _tokenStorage = tokenStorage;
            _signatureProvider = signatureProvider;
        }
        
        /// <summary>
        /// Creates an account in Cargo
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#create-an-account
        /// </summary>
        /// <param name="request">Request Parameters</param>
        /// <returns>Create Account Response</returns>
        public async Task<Response<CreateAccountResponseModel>> Handle(CreateAccountRequestModel request)
        {
            var response = new Response<CreateAccountResponseModel>();
            try
            {
                var (error, message) = await _signatureProvider.GetSignature();
                if (error)
                {
                    response.ResponseStatus = ResponseStatus.Fail;
                    response.Message = message;
                    return response;
                }
                
                var url = "https://api2.cargo.build/v3/register";
                var requestContent = JsonConvert.SerializeObject(new
                {
                    address = _tokenStorage.GetToken(),
                    signature = message,
                    email = request.Email,
                    username = request.UserName
                });
                var httpReq = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Content = new StringContent(requestContent),
                    Headers =
                    {
                        { "Content-Type", "application/json" }
                    }
                };
                var httpRes = await _httpClient.SendAsync(httpReq);
                if (!httpRes.IsSuccessStatusCode)
                {
                    response.Message = httpRes.ReasonPhrase;
                    response.ResponseStatus = ResponseStatus.Fail;
                    return response;
                }
                var responseContent = await httpRes.Content.ReadAsStringAsync();
                response.Payload = JsonConvert.DeserializeObject<CreateAccountResponseModel>(responseContent);
                if (response.Payload != null) await _tokenStorage.SetToken(response.Payload.Data.Token);
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