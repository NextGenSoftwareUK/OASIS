using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class STARCelestialSpace : STARNETHolon, ISTARCelestialSpace
    {
        public STARCelestialSpace()
        {
            this.HolonType = HolonType.STARCelestialSpace;
        }

        public CelestialSpaceType CelestialSpaceType { get; set; }
        public ICelestialSpace CelestialSpace { get; set; }
        public Guid CelestialSpaceId { get; set; }
    }
}