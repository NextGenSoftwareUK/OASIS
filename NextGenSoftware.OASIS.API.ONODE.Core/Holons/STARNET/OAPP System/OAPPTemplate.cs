using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
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
        [CustomOASISProperty]
        public IList<IRuntime> Runtimes { get; set; } = new List<IRuntime>();

        [CustomOASISProperty]
        public IList<string> RuntimeIds { get; set; } = new List<string>();

        [CustomOASISProperty]
        public IList<ILibrary> Libraries { get; set; } = new List<ILibrary>();

        [CustomOASISProperty]
        public IList<string> LibraryIds { get; set; } = new List<string>();

        //TODO: Dont think we need this? Because STARNET does the same thing anyway?! lol ;-)
        //public bool UseNuGetForOASISRuntime { get; set; } = false; //if this is false then it will pull the runtime down from STARNET when creating an OAPP.
        //public bool UseNuGetForSTARRuntime { get; set; } = false; //if this is false then it will pull the runtime down from STARNET when creating an OAPP.
    }
}