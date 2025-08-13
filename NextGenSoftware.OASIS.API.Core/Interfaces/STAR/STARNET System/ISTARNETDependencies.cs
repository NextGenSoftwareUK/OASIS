using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET
{
    public interface ISTARNETDependencies
    {
        List<STARNETDependency> Libraries { get; set; }
        List<STARNETDependency> Runtimes { get; set; }
        List<STARNETDependency> Templates { get; set; }
    }
}