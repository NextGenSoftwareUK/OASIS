
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    [MessagePackObject]
    public class HoloNETResponse
    {
        [Key(0)]
        public string id { get; set; }

        [Key(1)]
        public string type { get; set; }

        [Key(2)]
        public byte[] data { get; set; }
    }
}
