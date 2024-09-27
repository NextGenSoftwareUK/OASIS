
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    [MessagePackObject]
    public class AppBundle
    {
        [Key("manifest")]
        public AppManifest.AppManifest manifest { get; set; }
    }
}