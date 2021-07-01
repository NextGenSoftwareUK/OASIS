using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    // Animals/vegetation plane.
    public class SecondDimension : Dimension, ISecondDimension
    {
        public IUniverse Universe { get; set; }

        public SecondDimension()
        {
            this.DimensionLevel = DimensionLevel.Second;
        }
    }
}