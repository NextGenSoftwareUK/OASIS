using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class ZomeCall
    {
        //[Key(0)]
        [Key("provenance")]
        public byte[] provenance { get; set; } //AgentPubKey

        //[Key(1)]
        [Key("cell_id")]
        //public (byte[], byte[]) cell_id { get; set; }
        //public CellId cell_id { get; set; }
        public byte[][] cell_id { get; set; }

        //[Key(2)]
        [Key("zome_name")]
        public string zome_name { get; set; }

        // [Key(3)]
        [Key("fn_name")]
        public string fn_name { get; set; }

        //[Key(4)]
        [Key("cap_secret")]
        public byte[] cap_secret { get; set; }

        //[Key(5)]
        [Key("payload")]
        public byte[] payload { get; set; }

        //[Key(6)]
        [Key("nonce")]
        public byte[] nonce { get; set; }

        //[Key(7)]
        [Key("expires_at")]
        public long expires_at { get; set; }

        //[Key("provenance")]
        //public byte[] provenance { get; set; } //AgentPubKey

        //[Key("cell_id")]
        //public byte[][] cell_id { get; set; }

        //[Key("zome_name")]
        //public string zome_name { get; set; }

        //[Key("fn_name")]
        //public string fn_name { get; set; }

        //[Key("cap_secret")]
        //public byte[] cap_secret { get; set; }

        //[Key("payload")]
        //public byte[] payload { get; set; } 

        //[Key("nonce")]
        //public byte[] nonce { get; set; }

        //[Key("expires_at")]
        //public long expires_at { get; set; }
    }
}