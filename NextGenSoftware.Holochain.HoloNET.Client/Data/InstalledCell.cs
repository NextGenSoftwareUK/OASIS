using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class InstalledCell
    {
        [Key("cell_id")]
        public byte[][] cell_id { get; set; }
      
        [Key("role_id")]
        public string role_id { get; set; }
    }
}