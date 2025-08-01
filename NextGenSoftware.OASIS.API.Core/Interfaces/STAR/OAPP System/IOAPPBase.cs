using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IOAPPBase : ISTARNETHolon
    {
        IList<ILibrary> Libraries { get; set; }
        IList<ISTARNETHolonMetaData> LibrariesMetaData { get; set; }
        IList<IRuntime> Runtimes { get; set; }
        IList<ISTARNETHolonMetaData> RuntimesMetaData { get; set; }
        IList<IOAPPTemplate> OAPPTemplates { get; set; }
        IList<ISTARNETHolonMetaData> OAPPTemplatesMetaData { get; set; }
    }
}