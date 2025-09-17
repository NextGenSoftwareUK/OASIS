using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Request;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Response;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.SignatureProviders;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands
{
    public class AuthenticateAccountHandler : IHandle<OASISResult<CreateAccountResponseModel>, AuthenticateAccountRequestModel>
    {
        private readonly IHttpHandler _httpClient;
        private readonly ISignatureProvider _signatureProvider;

        public AuthenticateAccountHandler(IHttpHandler httpClient, ISignatureProvider signatureProvider)
        {
            _httpClient = httpClient;
            _signatureProvider = signatureProvider;
        }
        
        /// <summary>
        /// Authenticate User
        /// </summary>
        /// <returns>Authenticate Token</returns>
        public async Task<OASISResult<CreateAccountResponseModel>> Handle(AuthenticateAccountRequestModel requestModel)
        {
            var response = new OASISResult<CreateAccountResponseModel>();
            try
            {
                var signatureResult = await _signatureProvider.GetSignature(requestModel.AccountAddress, 
                    requestModel.SingingMessage, requestModel.PrivateKey, requestModel.HostUrl);
                if (signatureResult.IsError)
                {
                    response.IsError = true;
                    response.IsSaved = false;
                    OASISErrorHandling.HandleError(ref signatureResult, signatureResult.Message);
                    return response;
                }

                var url = "https://api2.cargo.build/v3/authenticate";
                var requestContent = JsonConvert.SerializeObject(new
                {
                    address = requestModel.AccountAddress,
                    signatureResult.Result
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
                    response.IsError = true;
                    response.IsSaved = false;
                    OASISErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }
                var responseContent = await httpRes.Content.ReadAsStringAsync();
                response.Result = JsonConvert.DeserializeObject<CreateAccountResponseModel>(responseContent);
                return response;
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.IsSaved = false;
                response.Exception = e;
                response.Message = e.Message;
                OASISErrorHandling.HandleError(ref response, e.Message);
                return response;
            }
        }
    }
}