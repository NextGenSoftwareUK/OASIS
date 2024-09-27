using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest;

namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IDnaDefinitionResponseDetail
    {
        object coordinator_zomes_raw { get; set; }
        object integrity_zomes_raw { get; set; }
        DnaModifiers modifiers { get; set; }
        string name { get; set; }
    }
}