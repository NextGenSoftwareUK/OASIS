using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    //Core crystal of planet plane?
    public class FirstDimension : Dimension, IFirstDimension
    {
        public IUniverse Universe { get; set; }

        public FirstDimension()
        {
            this.DimensionLevel = DimensionLevel.First;
        }
    }
}