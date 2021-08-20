using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetShowcaseBySlugHandler : IHandle<Response<GetShowcaseBySlugResponseModel>, GetShowcaseBySlugRequestModel>
    {
        public Task<Response<GetShowcaseBySlugResponseModel>> Handle(GetShowcaseBySlugRequestModel request)
        {
            throw new System.NotImplementedException();
        }
    }

    public class GetShowcaseBySlugRequestModel
    {
        /// <summary>
        /// Required. String. The slug of the showcase
        /// </summary>
        public string Slug { get; set; }
        /// <summary>
        /// Required. String. Slug ID of the showcase
        /// </summary>
        public string SlugId { get; set; }
    }

    public class GetShowcaseBySlugResponseModel
    {
        [JsonProperty("err")]
        public bool Error { get; set; }

        [JsonProperty("status")] 
        public int Status { get; set; }

        [JsonProperty("data")] 
        public GetShowcaseByIdResponse Data { get; set; }
    }
}