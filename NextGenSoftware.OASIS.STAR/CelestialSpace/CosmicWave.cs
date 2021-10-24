using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class CosmicWave : Holon, ICosmicWave
    {
        public CosmicWave()
        {
            this.HolonType = API.Core.Enums.HolonType.CosmicWave;
        }
    }
}