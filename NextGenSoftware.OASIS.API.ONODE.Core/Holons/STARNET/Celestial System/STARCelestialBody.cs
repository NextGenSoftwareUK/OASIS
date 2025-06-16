using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class STARCelestialBody : STARNETHolon, ISTARCelestialBody
    {
        public STARCelestialBody()
        {
            this.HolonType = HolonType.STARCelestialBody;
        }

        public CelestialBodyType CelestialBodyType { get; set; }
        public ICelestialBody CelestialBody { get; set; }
        public Guid CelestialBodyId { get; set; }
    }
}