using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    // Asscended Master plane (unity conciousness/ONENESS?)
    public class SeventhDimension : Dimension, ISeventhDimension
    {
        public IUniverse Universe { get; set; } = new Universe();

        public SeventhDimension()
        {
            this.DimensionLevel = DimensionLevel.Seventh;
        }
    }
}