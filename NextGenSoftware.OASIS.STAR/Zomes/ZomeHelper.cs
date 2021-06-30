using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.Zomes
{
    public static class ZomeHelper
    {
        public static void SetParentIdsForHolon(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star, IPlanet planet, IMoon moon, IZome zome, IHolon holon)
        {
            holon.ParentGreatGrandSuperStar = greatGrandSuperStar;
            holon.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;
            holon.ParentGrandSuperStar = grandSuperStar;
            holon.ParentGrandSuperStarId = grandSuperStar.Id;
            holon.ParentSuperStar = superStar;
            holon.ParentSuperStarId = superStar.Id;
            holon.ParentStar = star;
            holon.ParentStarId = star.Id;
            holon.ParentPlanet = planet;
            holon.ParentPlanetId = planet.Id;

            if (moon != null)
            {
                holon.ParentMoon = moon;
                holon.ParentMoonId = moon.Id;
            }

            if (zome != null)
            {
                holon.ParentZome = zome;
                holon.ParentZomeId = zome.Id;
            }

            holon.ParentHolonId = zome.Id;
            holon.ParentHolon = zome;
        }

        public static void SetParentIdsForHolonAndAllChildren(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star, IPlanet planet, IMoon moon, IZome zome, IHolon holon)
        {
            SetParentIdsForHolon(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon, zome, holon);

            if (holon.Children != null)
            {
                foreach (IHolon childHolon in holon.Children)
                    SetParentIdsForHolonAndAllChildren(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon, zome, childHolon);
            }
        }

        public static void SetParentIdsForZome(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star, IPlanet planet, IMoon moon, IZome zome)
        {
            SetParentIdsForHolon(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon, null, zome); //A zome is also a holon (everything is a holon).

            if (zome.Holons != null)
            {
                foreach (IHolon holon in zome.Holons)
                    SetParentIdsForHolon(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon, zome, holon);
            }
        }
    }
}