//using System.Collections.Generic;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
//using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;

//namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
//{
//    public abstract class OAPPBase : STARNETHolon, IOAPPBase
//    {
//        public OAPPBase(string STARNETDNAJSONName) : base(STARNETDNAJSONName)
//        {
           
//        }


//        //[CustomOASISProperty]
//        //public IList<ISTARNETDependency> RuntimesMetaData { get; set; } = new List<ISTARNETDependency>();

//        //[CustomOASISProperty]
//        //public IList<ISTARNETDependency> LibrariesMetaData { get; set; } = new List<ISTARNETDependency>();

//        //[CustomOASISProperty]
//        //public IList<ISTARNETDependency> OAPPTemplatesMetaData { get; set; } = new List<ISTARNETDependency>();


//        //These are wrapped in a STARNET Lib so appear in STARNET and work the same as the rest so can be searched, added, removed etc.//These are wrapped in a STARNET Lib so appear in STARNET and work the same as the rest so can be searched, added, removed etc.
//        //[CustomOASISProperty]
//        //public IList<IExternalDependency> ExternalDependencies { get; set; } = new List<IExternalDependency>();


//        //[CustomOASISProperty]
//        //public IList<string> LibraryIds { get; set; } = new List<string>();

//        //TODO: Dont think we need this? Because STARNET does the same thing anyway?! lol ;-)
//        //public bool UseNuGetForOASISRuntime { get; set; } = false; //if this is false then it will pull the runtime down from STARNET when creating an OAPP.
//        //public bool UseNuGetForSTARRuntime { get; set; } = false; //if this is false then it will pull the runtime down from STARNET when creating an OAPP.
//    }
//}