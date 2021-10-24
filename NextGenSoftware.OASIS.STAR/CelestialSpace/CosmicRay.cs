using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class CosmicRay : Holon, ICosmicRay
    {
        public CosmicRay()
        {
            this.HolonType = API.Core.Enums.HolonType.CosmicRay;
        }
    }
}