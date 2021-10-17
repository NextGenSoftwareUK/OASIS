using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class EleventhDimension : OmniverseDimension, IEleventhDimension
    {
        public EleventhDimension()
        {
            this.DimensionLevel = DimensionLevel.Eleventh;
        }
    }
}