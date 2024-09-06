using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;

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
        byte[] PublishedOAPP { get; set; }
    }
}