using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    // Asscended Master plane (unity conciousness/ONENESS?)
    public class SeventhDimension : Dimension, ISeventhDimension
    {
        public IUniverse Universe { get; set; } = new Universe();

        public SeventhDimension(IMultiverse multiverse = null)
        {
            this.DimensionLevel = DimensionLevel.Seventh;
            this.Universe = new Universe(multiverse);
        }
    }
}