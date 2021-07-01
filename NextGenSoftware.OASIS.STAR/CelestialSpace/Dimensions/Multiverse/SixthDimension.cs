using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    // Sacred Geometry/Structure
    public class SixthDimension : Dimension, ISixthDimension
    {
        public IUniverse Universe { get; set; }

        public SixthDimension()
        {
            this.DimensionLevel = DimensionLevel.Sixth;
        }
    }
}