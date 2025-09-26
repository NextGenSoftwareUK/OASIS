using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Request;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Response;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.SignatureProviders;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands
{
    public class CreateAccountHandler : IHandle<OASISResult<CreateAccountResponseModel>, CreateAccountRequestModel>
    {
        private readonly IHttpHandler _httpClient;
        private readonly ISignatureProvider _signatureProvider;

        public CreateAccountHandler(IHttpHandler httpClient, ISignatureProvider signatureProvider)
        {
            _httpClient = httpClient;
            _signatureProvider = signatureProvider;
        }
        
        /// <summary>
        /// Creates an account in Cargo
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#create-an-account
        /// </summary>
        /// <param name="request">Request Parameters</param>
        /// <returns>Create Account Response</returns>
        public async Task<OASISResult<CreateAccountResponseModel>> Handle(CreateAccountRequestModel request)
        {
            var response = new OASISResult<CreateAccountResponseModel>();
            try
            {
                var signatureResult = await _signatureProvider.GetSignature(request.AccountAddress,
                    request.SingingMessage, request.PrivateKey, request.HostUrl);
                if (signatureResult.IsError)
                {
                    response.Message = signatureResult.Message;
                    response.IsError = true;
                    response.IsSaved = false;
                    ErrorHandling.HandleError(ref response, signatureResult.Message);
                    return response;
                }
                
                var url = "https://api2.cargo.build/v3/register";
                var requestContent = JsonConvert.SerializeObject(new
                {
                    address = request.AccountAddress,
                    signature = signatureResult.Result,
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
                    response.IsError = true;
                    response.IsSaved = false;
                    ErrorHandling.HandleError(ref response, response.Message);
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
                response.Message = e.Message;
                response.Exception = e;
                ErrorHandling.HandleError(ref response, e.Message);
                return response;
            }
        }
    }
}