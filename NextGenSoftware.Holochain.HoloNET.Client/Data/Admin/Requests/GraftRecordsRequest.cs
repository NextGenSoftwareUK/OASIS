
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests
{
    [MessagePackObject]
    public class GraftRecordsRequest
    {
        [Key("cell_id")]
        public byte[][] cell_id { get; set; }

        [Key("validate")]
        public bool validate { get; set; } 

        [Key("records")]
        //public Record[] records { get; set; }
        public object[] records { get; set; }
    }
}