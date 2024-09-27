using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class HoloNETRequest : IHoloNETRequest
    {
        [Key("id")]
        public ulong id { get; set; }

        [Key("type")]
        public string type { get; set; }

        [Key("data")]
        public byte[] data { get; set; }
    }
}
