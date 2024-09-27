
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class SignalResponse
    {
        [Key("App")]
        public SignalData App { get; set; }
    }
}