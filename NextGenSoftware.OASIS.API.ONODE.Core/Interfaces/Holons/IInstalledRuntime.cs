using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IInstalledRuntime : IHolon
    {
        Guid InstalledBy { get; set; }
        string InstalledByAvatarUsername { get; set; }
        DateTime InstalledOn { get; set; }
        string InstalledPath { get; set; }
        IRuntimeDNA RuntimeDNA { get; set; }
    }
}