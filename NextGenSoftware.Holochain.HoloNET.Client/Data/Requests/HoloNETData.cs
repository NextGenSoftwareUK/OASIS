
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class HoloNETData : IHoloNETData
    {
        [Key("type")]
        public string type { get; set; }

        [Key("data")]
        public dynamic data { get; set; }
    }
}