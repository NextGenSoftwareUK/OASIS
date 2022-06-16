using MessagePack;
using System;

namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    [MessagePackObject]
   // [Serializable]
    public struct HoloNETDataZomeCall
    {
        [Key("cell_id")]
        public byte[][] cell_id { get; set; }
      
        [Key("zome_name")]
        public string zome_name { get; set; }

        [Key("fn_name")]
        public string fn_name { get; set; }

        [Key("payload")]
        public byte[] payload { get; set; } 

        [Key("cap")]
        public byte[] cap { get; set; } //CapSecret | null = string

        [Key("provenance")]
        public byte[] provenance { get; set; } //AgentPubKey = string
    }

    [MessagePackObject]
    //[Serializable]
    public class HoloNETData
    {
        [Key("type")]
        public string type { get; set; }

        [Key("data")]
        public HoloNETDataZomeCall data { get; set; }
    }
}