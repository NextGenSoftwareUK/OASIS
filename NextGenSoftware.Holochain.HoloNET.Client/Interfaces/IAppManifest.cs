using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest;

namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IAppManifest
    {
        string description { get; set; }
        string manifest_version { get; set; }
        string name { get; set; }
        Roles[] roles { get; set; }
    }
}