using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public interface ISTARCelestialBody : ISTARNETHolon
    {
        ICelestialBody CelestialBody { get; set; }
        Guid CelestialBodyId { get; set; }
        CelestialBodyType CelestialBodyType { get; set; }
    }
}