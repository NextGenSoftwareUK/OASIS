
using MessagePack;
using System;

namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    [MessagePackObject]
    //[Serializable]
    public class HoloNETResponse
    {
        [Key("id")]
        public string id { get; set; }

        [Key("type")]
        public string type { get; set; }

        [Key("data")]
        public byte[] data { get; set; }
    }
}
