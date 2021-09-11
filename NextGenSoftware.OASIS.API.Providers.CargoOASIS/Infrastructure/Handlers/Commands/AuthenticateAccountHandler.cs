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
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands
{
    public class AuthenticateAccountHandler : ISingleHandler<Response<CreateAccountResponseModel>>
    {
        private readonly ITokenStorage _tokenStorage;
        private readonly ISignatureProvider _signatureProvider;
        private readonly IHttpHandler _httpClient;
        public AuthenticateAccountHandler(ITokenStorage tokenStorage, ISignatureProvider signatureProvider, IHttpHandler httpClient)
        {
            _tokenStorage = tokenStorage;
            _signatureProvider = signatureProvider;
            _httpClient = httpClient;
        }
        
        /// <summary>
        /// Authenticate User
        /// </summary>
        /// <returns>Authenticate Token</returns>
        public async Task<Response<CreateAccountResponseModel>> Handle()
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
                
                var url = "https://api2.cargo.build/v3/authenticate";
                var requestContent = JsonConvert.SerializeObject(new
                {
                    address = _tokenStorage.GetToken(),
                    signature = message
                });
                var httpReq = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Content = new StringContent(requestContent),
                    Headers = { { "Content-Type", "application/json" } }
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