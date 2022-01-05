using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class NinthDimension : OmniverseDimension, INinthDimension
    {
        public NinthDimension(IOmiverse omniverse = null) : base(omniverse)
        {
            this.DimensionLevel = DimensionLevel.Ninth;
        }
    }
}