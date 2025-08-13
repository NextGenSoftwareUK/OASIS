using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET
{
    public class STARNETDependencies : ISTARNETDependencies
    {
        public List<STARNETDependency> Runtimes { get; set; } = new List<STARNETDependency>();
        public List<STARNETDependency> Libraries { get; set; } = new List<STARNETDependency>();
        public List<STARNETDependency> Templates { get; set; } = new List<STARNETDependency>();
    }
}