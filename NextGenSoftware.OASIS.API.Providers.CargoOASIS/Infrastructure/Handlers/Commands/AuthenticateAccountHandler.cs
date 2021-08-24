using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.TokenStorage;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands
{
    public class AuthenticateAccountHandler : ISingleHandler<Response<CreateAccountResponseModel>>
    {
        private readonly ITokenStorage _tokenStorage;
        private readonly HttpClient _httpClient;
        public AuthenticateAccountHandler()
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
        /// Authenticate User
        /// </summary>
        /// <returns>Authenticate Token</returns>
        public async Task<Response<CreateAccountResponseModel>> Handle()
        {
            var response = new Response<CreateAccountResponseModel>();
            try
            {
                var url = "v3/authenticate";
                var requestContent = JsonConvert.SerializeObject(new
                {
                    address = "",
                    signature = ""
                });
                var httpReq = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(_httpClient.BaseAddress + url),
                    Content = new StringContent(requestContent)
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
                await _tokenStorage.SetTaken(response.Payload.Data.Token);
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