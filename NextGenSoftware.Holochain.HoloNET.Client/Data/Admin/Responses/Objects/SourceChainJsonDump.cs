
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class SourceChainJsonDump
    {
        [Key("records")]
        public SourceChainJsonRecord[] records { get; set; }

        [Key("published_ops_count")]
        public int published_ops_count { get; set; }
    }
}