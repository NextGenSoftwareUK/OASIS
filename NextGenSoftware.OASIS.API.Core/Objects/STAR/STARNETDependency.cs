using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class STARNETDependency : ISTARNETDependency
    {
        public DependencyType Type { get; set; }
        public Guid HolonId { get; set; }
        public Guid STARNETHolonId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public int VersionSequence { get; set; }
        public bool Install { get; set; }
        public DependencyInstallMode InstallMode { get; set; }
        public string InstalledFrom { get; set; }
        public string InstalledTo { get; set; }
    }
}
