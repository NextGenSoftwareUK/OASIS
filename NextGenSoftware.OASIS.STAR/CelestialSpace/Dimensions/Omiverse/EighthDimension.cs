using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class EighthDimension : Dimension, IEighthDimension
    {
        //TODO: Eighth Dimension and above is at the Omiverse level so spans ALL Multiverses/Universes so not sure what we would have here? Needs more thought...
        public ISuperUniverse SuperUniverse { get; set; }

        public EighthDimension()
        {
            this.DimensionLevel = DimensionLevel.Eighth;
        }
    }
}