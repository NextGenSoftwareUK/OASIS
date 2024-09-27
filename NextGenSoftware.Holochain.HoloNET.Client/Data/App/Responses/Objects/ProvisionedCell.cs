using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.App.Responses.Objects
{
    [MessagePackObject]
    public class ProvisionedCell : ICell
    {
        [Key("cell_id")]
        public byte[][] cell_id { get; set; }

        [Key("dna_modifiers")]
        public DnaModifiers dna_modifiers { get; set; } //pub dna_modifiers: DnaModifiers,

        [Key("name")]
        public string name { get; set; }
    }
}