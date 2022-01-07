using MessagePack;
using System;

namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    //[MessagePackObject]
    [Serializable]
    public struct HoloNETDataZomeCall
    {
       // [Key(0)]
        public byte[][] cell_id { get; set; }
      
       // [Key(1)]
        public string zome_name { get; set; }

       // [Key(2)]
        public string fn_name { get; set; }

      //  [Key(3)]
        public byte[] payload { get; set; } 

       // [Key(4)]
        public byte[] cap { get; set; } //CapSecret | null = string

       // [Key(5)]
        public byte[] provenance { get; set; } //AgentPubKey = string
    }

    //[MessagePackObject]
    [Serializable]
    public class HoloNETData
    {
      //  [Key(0)]
        public string type { get; set; }

       // [Key(1)]
        public HoloNETDataZomeCall data { get; set; }
    }
}