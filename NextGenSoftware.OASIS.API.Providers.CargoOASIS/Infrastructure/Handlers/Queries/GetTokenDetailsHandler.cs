using System;
using System.Net.Http;
using System.Threading.Tasks;
using Models.Cargo;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetTokenDetailsHandler : IHandle<Response<GetTokenDetailsResponseModel>, GetTokenDetailsRequestModel>
    {
        private readonly HttpClient _httpClient;

        public GetTokenDetailsHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
        }
        
        public Task<Response<GetTokenDetailsResponseModel>> Handle(GetTokenDetailsRequestModel request)
        {
            throw new System.NotImplementedException();
        }
    }

    public class GetTokenDetailsRequestModel
    {
        /// <summary>
        /// The ID of the project on Cargo.
        /// This can be found in the URL bar when viewing the project on Cargo.
        /// </summary>
        public string ProjectId { get; set; }
        /// <summary>
        /// The ID of the collectible within the collection.
        /// </summary>
        public string CollectibleId { get; set; }
    }

    public class GetTokenDetailsResponseModel
    {
        [JsonProperty("err")]
        public bool Error { get; set; }

        [JsonProperty("status")] 
        public int Status { get; set; }

        [JsonProperty("data")]
        public TokenDetail Data { get; set; }
    }
}