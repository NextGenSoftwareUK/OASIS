using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    //public class OAPPTemplate : OAPPBase, IOAPPTemplate
    public class OAPPTemplate : STARNETHolon, IOAPPTemplate
    {
        public OAPPTemplate() : base("OAPPTemplateDNAJSON")
        {
            this.HolonType = HolonType.OAPPTemplate; 
        }

        //TODO: Dont think we need this? Because STARNET does the same thing anyway?! lol ;-)
        //public bool UseNuGetForOASISRuntime { get; set; } = false; //if this is false then it will pull the runtime down from STARNET when creating an OAPP.
        //public bool UseNuGetForSTARRuntime { get; set; } = false; //if this is false then it will pull the runtime down from STARNET when creating an OAPP.
    }
}