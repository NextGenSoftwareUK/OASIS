using System;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public interface ISTARNETDependency
    {
        string Description { get; set; }
        Guid HolonId { get; set; }
        bool Install { get; set; }
        string InstalledFrom { get; set; }
        string InstalledTo { get; set; }
        DependencyInstallMode InstallMode { get; set; }
        string Name { get; set; }
        Guid STARNETHolonId { get; set; }
        DependencyType Type { get; set; }
        string Version { get; set; }
        int VersionSequence { get; set; }
    }
}