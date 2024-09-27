
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class EnableAppResponse
    {
        [Key("type")]
        public string type { get; set; }

        [Key("data")]
        public EnableAppResponseDetails data { get; set; }
    }
}