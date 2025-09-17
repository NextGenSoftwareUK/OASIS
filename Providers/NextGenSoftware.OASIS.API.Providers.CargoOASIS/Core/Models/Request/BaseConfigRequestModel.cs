using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Request
{
    public abstract class BaseConfigRequestModel
    {
        [JsonIgnore]
        public string SingingMessage { get; set; }
        [JsonIgnore]
        public string PrivateKey { get; set; }
        [JsonIgnore]
        public string HostUrl { get; set; }
    }
}