using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface ISTARCelestialBody : ISTARNETHolon
    {
        ICelestialBody CelestialBody { get; set; }
        Guid CelestialBodyId { get; set; }
        CelestialBodyType CelestialBodyType { get; set; }
    }
}