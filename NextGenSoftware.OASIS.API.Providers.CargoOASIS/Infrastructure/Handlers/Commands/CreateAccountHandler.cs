using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands
{
    public class CreateAccountHandler : IHandle<Response<CreateAccountResponseModel>, CreateAccountRequestModel>
    {
        private readonly HttpClient _httpClient;

        public CreateAccountHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
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

    public class CreateAccountRequestModel
    {
        /// <summary>
        /// Optional. Valid email address that will be tied to the account
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Optional. Username to be used for new account
        /// </summary>
        public string UserName { get; set; }
    }

    public class CreateAccountResponseModel
    {
        public class CreateAccountData
        {
            /// <summary>
            /// JSON Web Token
            /// </summary>
            [JsonProperty("token")]
            public string Token { get; set; }
        }
        
        [JsonProperty("err")]
        public bool Error { get; set; }

        [JsonProperty("status")] 
        public int Status { get; set; }

        [JsonProperty("data")] 
        public CreateAccountData Data { get; set; }
    }
}