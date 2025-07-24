
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IOAPPTemplate : ISTARNETHolon
    {
        //IOAPPTemplateDNA OAPPTemplateDNA { get; set; }
        //byte[] PublishedOAPPTemplate { get; set; }

        public IList<IRuntime> Runtimes { get; set; }
        public IList<string> RuntimeIds { get; set; }

        public IList<ILibrary> Libraries { get; set; }
        public IList<string> LibraryIds { get; set; }
    }
}