using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class NinthDimension : OmniverseDimension, INinthDimension
    {
        public NinthDimension()
        {
            this.DimensionLevel = DimensionLevel.Ninth;
        }
    }
}