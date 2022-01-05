using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class EighthDimension : OmniverseDimension, IEighthDimension
    {
        public EighthDimension(IOmiverse omniverse = null) : base(omniverse)   
        {
            this.DimensionLevel = DimensionLevel.Eighth;
        }
    }
}