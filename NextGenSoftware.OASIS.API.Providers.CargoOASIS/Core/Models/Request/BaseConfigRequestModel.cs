namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Request
{
    public abstract class BaseConfigRequestModel
    {
        public string SingingMessage { get; set; }
        public string PrivateKey { get; set; }
        public string HostUrl { get; set; }
    }
}