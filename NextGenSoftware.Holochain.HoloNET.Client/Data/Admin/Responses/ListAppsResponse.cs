
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class ListAppsResponse
    {
        [Key("type")]
        public string type { get; set; }

        [Key("data")]
        public AppInfo[] Apps { get; set; }
    }
}