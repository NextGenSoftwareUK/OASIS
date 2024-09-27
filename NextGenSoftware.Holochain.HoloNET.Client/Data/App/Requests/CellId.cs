using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class CellId
    {
        //[Key("dna_hash")]
        [Key(0)]
        public byte[] dna_hash { get; set; }

        //[Key("agent_pubkey")]
        [Key(1)]
        public byte[] agent_pubkey { get; set; }
    }
}