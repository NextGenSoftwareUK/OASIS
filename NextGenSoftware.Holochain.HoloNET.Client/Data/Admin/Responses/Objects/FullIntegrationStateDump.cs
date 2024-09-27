
using MessagePack;
using System;
using static System.Net.WebRequestMethods;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class FullIntegrationStateDump
    {
        [Key("validation_limbo")]
        public DhtOp[] validation_limbo { get; set; }

        [Key("integration_limbo")]
        public DhtOp[] integration_limbo { get; set; }

        [Key("integrated")]
        public DhtOp[] integrated { get; set; }

        [Key("dht_ops_cursor")]
        public UInt64 dht_ops_cursor { get; set; }
    }
}