using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    //Source/God Head?
    public class TwelfthDimension : Dimension, ITwelfthDimension
    {
        //TODO: Eighth Dimension and above is at the Omiverse level so spans ALL Multiverses/Universes so not sure what we would have here? Needs more thought...
        public ISuperVerse SuperVerse { get; set; }

        public TwelfthDimension()
        {
            this.DimensionLevel = DimensionLevel.Twelfth;
        }
    }
}