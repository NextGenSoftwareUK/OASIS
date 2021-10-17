using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    //Love/wisdom dimension?
    public class FifthDimension : Dimension, IFifthDimension
    {
        public IUniverse Universe { get; set; } = new Universe();

        public FifthDimension()
        {
            this.DimensionLevel = DimensionLevel.Fifth;
        }
    }
}