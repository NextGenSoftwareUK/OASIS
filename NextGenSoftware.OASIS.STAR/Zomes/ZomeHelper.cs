using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using System;

namespace NextGenSoftware.OASIS.STAR.Zomes
{
    public static class ZomeHelper
    {
        public static void SetParentIdsForHolon(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star, IPlanet planet, IMoon moon, IZome zome, IHolon holon)
        {
            if (greatGrandSuperStar != null)
            {
                if (holon.ParentGreatGrandSuperStar == null) //TODO: Do we need a null check here? What if we want to override it? Same for all other props below...
                    holon.ParentGreatGrandSuperStar = greatGrandSuperStar;

                if (holon.ParentGreatGrandSuperStarId == Guid.Empty)
                    holon.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;
            }

            if (grandSuperStar != null)
            {
                if (holon.ParentGrandSuperStar == null)
                    holon.ParentGrandSuperStar = grandSuperStar;

                if (holon.ParentGrandSuperStarId == Guid.Empty)
                    holon.ParentGrandSuperStarId = grandSuperStar.Id;
            }

            if (superStar != null)
            {
                if (holon.ParentSuperStar == null)
                    holon.ParentSuperStar = superStar;

                if (holon.ParentSuperStarId == Guid.Empty)
                    holon.ParentSuperStarId = superStar.Id;
            }

            if (star != null)
            {
                if (holon.ParentStar == null)
                    holon.ParentStar = star;

                if (holon.ParentStarId == Guid.Empty)
                    holon.ParentStarId = star.Id;
            }

            if (planet != null)
            {
                if (holon.ParentStar == null)
                    holon.ParentPlanet = planet;

                holon.ParentPlanetId = planet.Id;
            }

            if (moon != null)
            {
                if (holon.ParentMoon == null)
                    holon.ParentMoon = moon;

                if (holon.ParentMoonId == Guid.Empty)
                    holon.ParentMoonId = moon.Id;
            }

            if (zome != null)
            {
                if (holon.ParentZome == null)
                    holon.ParentZome = zome;

                if (holon.ParentZomeId == Guid.Empty)
                    holon.ParentZomeId = zome.Id;
            }

            if (holon != null)
            {
                if (holon.ParentHolonId == Guid.Empty)
                    holon.ParentHolonId = holon.Id;

                if (holon.ParentHolon == null)
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
            //TODO: Not sure if we even need this?!
            //SetParentIdsForHolon(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon, null, zome); //A zome is also a holon (everything is a holon).
            //SetParentIdsForHolon(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon, zome, null); //A zome is also a holon (everything is a holon).

            if (zome.Holons != null)
            {
                foreach (IHolon holon in zome.Holons)
                    SetParentIdsForHolon(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon, zome, holon);
            }
        }
    }
}