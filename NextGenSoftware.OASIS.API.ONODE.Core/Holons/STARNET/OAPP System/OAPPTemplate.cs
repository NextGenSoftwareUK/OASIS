using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class OAPPTemplate : STARNETHolon, IOAPPTemplate
    {
        public OAPPTemplate() : base("OAPPTemplateDNAJSON")
        {
            this.HolonType = HolonType.OAPPTemplate; 
        }

        //List of custom Runtimes the OAPP Template uses (on top of the built-in OASIS & STAR runtime).
        public List<IRuntime> Runtimes { get; set; } = new List<IRuntime>();
        public string RuntimeIds { get; set; }
        
        //TODO: Dont think we need this? Because STARNET does the same thing anyway?! lol ;-)
        //public bool UseNuGetForOASISRuntime { get; set; } = false; //if this is false then it will pull the runtime down from STARNET when creating an OAPP.
        //public bool UseNuGetForSTARRuntime { get; set; } = false; //if this is false then it will pull the runtime down from STARNET when creating an OAPP.
    }
}