using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons
{
    public interface IOAPP : IHolon
    {
        OAPPType OAPPType { get; set; }
        GenesisType GenesisType { get; set; }
        Guid CelestialBodyId { get; set; }
        ICelestialBody CelestialBody { get; set; }
        DateTime PublishedOn { get; set; }
        Guid PublishedByAvatarId { get; set; }
        string CreatedByAvatarUsername { get; set; }
        string PublishedByAvatarUsername { get; set; }
        byte[] PublishedOAPP { get; set; }
    }
}