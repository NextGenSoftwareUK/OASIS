
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class HoloNETResponse : IHoloNETResponse
    {
        [Key("id")]
        public ulong id { get; set; }

        [Key("type")]
        public string type { get; set; }

        [IgnoreMember]
        public HoloNETResponseType HoloNETResponseType { get; set; }

        [Key("data")]
        public byte[] data { get; set; }

        [IgnoreMember]
        public bool IsError { get; set; }
    }
}
