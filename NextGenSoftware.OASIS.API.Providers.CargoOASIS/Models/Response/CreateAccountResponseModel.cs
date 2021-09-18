using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response
{
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