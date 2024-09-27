
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class DnaDefinitionResponseDetail : IDnaDefinitionResponseDetail
    {
        [Key("name")]
        public string name { get; set; }

        [Key("modifiers")]
        public DnaModifiers modifiers { get; set; }

        [Key("integrity_zomes")]
        public object integrity_zomes_raw { get; set; }

        [Key("coordinator_zomes")]
        public object coordinator_zomes_raw { get; set; }
    }
}