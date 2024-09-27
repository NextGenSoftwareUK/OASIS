
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class AppInfoResponse : IAppInfoResponse
    {
        [Key("type")]
        public string type { get; set; }

        [Key("data")]
        public AppInfo data { get; set; }
    }
}
