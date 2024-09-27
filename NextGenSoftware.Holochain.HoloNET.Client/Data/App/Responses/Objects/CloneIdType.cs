
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.App.Responses.Objects
{
    [MessagePackObject]
    public struct CloneIdType
    {
        [Key("cell_id")]
        public byte[][] cell_id { get; set; }

        [Key("clone_id")]
        public CloneId clone_id { get; set; }

        [Key("original_dna_hash")]
        public byte[] original_dna_hash { get; set; }

        [Key("name")]
        public string name { get; set; }

        [Key("enabled")]
        public bool enabled { get; set; }
    }
}