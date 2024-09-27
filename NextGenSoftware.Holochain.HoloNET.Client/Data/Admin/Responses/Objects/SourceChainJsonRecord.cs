
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class SourceChainJsonRecord
    {
        [Key("signature")]
        public Signature[] signature { get; set; }

        [Key("action_address")]
        public byte[] action_address { get; set; }
    }
}