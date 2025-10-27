using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response
{
    public class GetShowcaseByIdResponseModel
    {
        [JsonProperty("err")]
        public bool Error { get; set; }

        [JsonProperty("status")] 
        public int Status { get; set; }
        
        [JsonProperty("data")]
        public GetShowcaseByIdResponse Data { get; set; }
    }
}