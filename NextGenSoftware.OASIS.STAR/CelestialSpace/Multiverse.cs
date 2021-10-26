using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Multiverse : Holon, IMultiverse
    {
        public IGrandSuperStar GrandSuperStar { get; set; } = new GrandSuperStar(); //Lets you jump between universes/dimensions within this multiverse.
        public IMultiverseDimensions Dimensions { get; set; } = new MultiverseDimensions();

        public Multiverse()
        {
            this.HolonType = API.Core.Enums.HolonType.Multiverse;
        }
    }
}