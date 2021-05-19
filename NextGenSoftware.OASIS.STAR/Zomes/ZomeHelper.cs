using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.Zomes
{
    public static class ZomeHelper
    {
        public static void SetParentIdsForHolon(IStar star, IPlanet planet, IMoon moon, IZome zome, IHolon holon)
        {
            if (holon.Children != null)
            {
                foreach (Holon innerHolon in holon.Children)
                {
                    innerHolon.ParentHolonId = holon.Id;
                    innerHolon.ParentHolon = holon;
                    innerHolon.ParentStar = star;
                    innerHolon.ParentStarId = star.Id;
                    innerHolon.ParentPlanet = planet;
                    innerHolon.ParentPlanetId = planet.Id;

                    if (moon != null)
                    {
                        holon.ParentMoon = moon;
                        holon.ParentMoonId = moon.Id;
                    }

                    innerHolon.ParentZome = zome;
                    innerHolon.ParentZomeId = zome.Id;

                    if (innerHolon.Children != null)
                    {
                        foreach (Holon childHolon in innerHolon.Children)
                            SetParentIdsForHolon(star, planet, moon, zome, childHolon);
                    }
                }
            }
        }

        public static void SetParentIdsForZome(IStar star, IPlanet planet, IMoon moon, IZome zome)
        {
            if (zome.Holons != null)
            {
                foreach (Holon holon in zome.Holons)
                {
                    holon.ParentHolonId = zome.Id;
                    holon.ParentHolon = zome;
                    holon.ParentStar = star;
                    holon.ParentStarId = star.Id;
                    holon.ParentPlanet = planet;
                    holon.ParentPlanetId = planet.Id;

                    if (moon != null)
                    {
                        holon.ParentMoon = moon;
                        holon.ParentMoonId = moon.Id;
                    }

                    holon.ParentZome = zome;
                    holon.ParentZomeId = zome.Id;

                    if (holon.Children != null)
                    {
                        foreach (Holon childHolon in holon.Children)
                            SetParentIdsForHolon(star, planet, moon, zome, childHolon);
                    }
                }
            }
        }
    }
}
