
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using System.Collections.Generic;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public class DnaDefinition : IDnaDefinition
    {
        public string Name { get; set; }
        public IDnaModifiers Modifiers { get; set; }
        public List<ZomeDefinition> IntegrityZomes { get; set; } = new List<ZomeDefinition>();
        public List<ZomeDefinition> CoordinatorZomes { get; set; } = new List<ZomeDefinition>();
    }
}