
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class AppResponse : IAppResponse
    {
        [Key("type")]
        public string type { get; set; }

        [Key("data")]
        //public byte[] data { get; set; }
        public dynamic data { get; set; }
    }
}
