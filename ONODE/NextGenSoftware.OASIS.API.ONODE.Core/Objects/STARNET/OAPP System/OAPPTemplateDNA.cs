using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Objects
{
    public class OAPPTemplateDNA : STARNETDNA, IOAPPTemplateDNA
    {
        //public OAPPTemplateType OAPPTemplateType { get; set; }

        //TODO: Need to finish the refactor of the STARNETManagerBase to allow cutom DNA's to be injected in before these properties are usable here...
        //List of custom Runtimes the OAPP Template uses (on top of the built-in OASIS & STAR runtime).
        //public IList<IRuntime> Runtimes { get; set; } = new List<IRuntime>();

        //public IList<string> RuntimeIds { get; set; } = new List<string>();

        //public IList<ILibrary> Libraries { get; set; } = new List<ILibrary>();

        //public IList<string> LibraryIds { get; set; } = new List<string>();
    }
}