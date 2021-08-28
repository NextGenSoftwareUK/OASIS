
using MessagePack;
using System;

namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    [MessagePackObject]
    public struct Data
    {
        [Key(0)]
        public byte[][] cell_id { get; set; } //= new byte[2][];
        //public UInt32[] cell_id { get; set; } //= new byte[][1, 2]; //CellId = [HoloHash, AgentPubKey] = [string, string] = 2 dimensional array.

        [Key(1)]
        public string zome_name { get; set; }

        [Key(2)]
        public string fn_name { get; set; }

        [Key(3)]
        public byte[] payload { get; set; } //Payload - What is Payload object?

        [Key(4)]
        //public string cap { get; set; } //CapSecret | null = string
        public byte[] cap { get; set; } //CapSecret | null = string

        [Key(5)]
        public byte[] provenance { get; set; } //AgentPubKey = string
        //public string provenance { get; set; } //AgentPubKey = string
    }

    [MessagePackObject]
    public class HoloNETData
    {
        [Key(0)]
        public string type { get; set; }

        [Key(1)]
        public Data data { get; set; }
    }
}