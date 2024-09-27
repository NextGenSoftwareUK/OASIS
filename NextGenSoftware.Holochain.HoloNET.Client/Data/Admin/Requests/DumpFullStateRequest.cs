
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests
{
    [MessagePackObject]
    public class DumpFullStateRequest
    {
        [Key("cell_id")]
        public byte[][] cell_id { get; set; }

        [Key("dht_ops_cursor")]
        public int? dht_ops_cursor { get; set; }
    }
}