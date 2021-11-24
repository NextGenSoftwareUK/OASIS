using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Multiverse : Holon, IMultiverse
    {
        public IGrandSuperStar GrandSuperStar { get; set; } = new GrandSuperStar() { CreatedOASISType = new API.Core.Helpers.EnumValue<API.Core.Enums.OASISType>(API.Core.Enums.OASISType.STARCLI)}; //Lets you jump between universes/dimensions within this multiverse.
        public IMultiverseDimensions Dimensions { get; set; } = new MultiverseDimensions();

        public Multiverse()
        {
            this.HolonType = API.Core.Enums.HolonType.Multiverse;
        }
    }
}