using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.Zomes
{
    public static class ZomeHelper
    {
        public static void SetParentIdsForHolon(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star, IPlanet planet, IMoon moon, IZome zome, IHolon holon)
        {
            if (greatGrandSuperStar != null)
            {
                holon.ParentGreatGrandSuperStar = greatGrandSuperStar;
                holon.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;
            }

            if (grandSuperStar != null)
            {
                holon.ParentGrandSuperStar = grandSuperStar;
                holon.ParentGrandSuperStarId = grandSuperStar.Id;
            }

            if (superStar != null)
            {
                holon.ParentSuperStar = superStar;
                holon.ParentSuperStarId = superStar.Id;
            }

            if (star != null)
            {
                holon.ParentStar = star;
                holon.ParentStarId = star.Id;
            }

            if (planet != null)
            {
                holon.ParentPlanet = planet;
                holon.ParentPlanetId = planet.Id;
            }

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

            if (holon != null)
            {
                holon.ParentHolonId = holon.Id;
                holon.ParentHolon = holon;
            }
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