using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class TemporalRift : Holon, ITemporalRift
    {
        public TemporalRift()
        {
            this.HolonType = API.Core.Enums.HolonType.TemporalRift;
        }
    }
}