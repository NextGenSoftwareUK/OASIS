
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.App.Responses.Objects
{
    [MessagePackObject]
    public class StemCell : ICell
    {
        [Key("original_dna_hash")]
        public byte[] original_dna_hash { get; set; }

        [Key("dna_modifiers")]
        public DnaModifiers dna_modifiers { get; set; } //DnaModifiers

        [Key("name")]
        //public OptionType name { get; set; } // pub name: Option<String>,
        public string name { get; set; } // pub name: Option<String>,
    }
}