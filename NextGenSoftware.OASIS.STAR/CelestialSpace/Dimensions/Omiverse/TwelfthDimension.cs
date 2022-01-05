using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    //Source/God Head?
    public class TwelfthDimension : OmniverseDimension, ITwelfthDimension
    {
        public TwelfthDimension(IOmiverse omniverse = null) : base(omniverse)
        {
            this.DimensionLevel = DimensionLevel.Twelfth;
        }
    }
}