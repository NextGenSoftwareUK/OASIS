using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    // Astral Plane (Thoughts/Emotions)
    public class FourthDimension : Dimension, IFourthDimension
    {
        //TODO: Not sure if this also has parallel universes like the third dimension does?
        public IUniverse Universe { get; set; }

        public FourthDimension()
        {
            this.DimensionLevel = DimensionLevel.Fourth;
        }
    }
}