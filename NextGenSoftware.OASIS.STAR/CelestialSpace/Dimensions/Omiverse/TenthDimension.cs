using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class TenthDimension : OmniverseDimension, ITenthDimension
    {
        public TenthDimension()
        {
            this.DimensionLevel = DimensionLevel.Tenth;
        }
    }
}