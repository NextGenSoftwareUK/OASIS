using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest;
using System.Collections.Generic;

namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IDnaDefinition
    {
        List<ZomeDefinition> CoordinatorZomes { get; set; }
        List<ZomeDefinition> IntegrityZomes { get; set; }
        IDnaModifiers Modifiers { get; set; }
        string Name { get; set; }
    }
}