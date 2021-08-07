using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class EighthDimension : OmniverseDimension, IEighthDimension
    {
        public EighthDimension()
        {
            this.DimensionLevel = DimensionLevel.Eighth;
        }
    }
}