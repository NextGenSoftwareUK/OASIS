using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class Mapper<T1, T2> 
        where T1 : IHolon
        where T2 : IHolon, new()
    {

        public static T2 MapBaseHolonProperties(T1 sourceHolon)
        {
            return MapBaseHolonProperties(sourceHolon, new T2());
        }

        public static T2 MapBaseHolonProperties(T1 sourceHolon, T2 targetHolon)
        {
            targetHolon.Id = sourceHolon.Id;
            targetHolon.ProviderKey = sourceHolon.ProviderKey;
            targetHolon.Name = sourceHolon.Name;
            targetHolon.Description = sourceHolon.Description;
            targetHolon.HolonType = sourceHolon.HolonType;
            targetHolon.ParentGreatGrandSuperStar = sourceHolon.ParentGreatGrandSuperStar;
            targetHolon.ParentGreatGrandSuperStarId = sourceHolon.ParentGreatGrandSuperStarId;
            targetHolon.ParentGrandSuperStar = sourceHolon.ParentGrandSuperStar;
            targetHolon.ParentGrandSuperStarId = sourceHolon.ParentGrandSuperStarId;
            targetHolon.ParentSuperStar = sourceHolon.ParentSuperStar;
            targetHolon.ParentSuperStarId = sourceHolon.ParentSuperStarId;
            targetHolon.ParentStar = sourceHolon.ParentStar;
            targetHolon.ParentStarId = sourceHolon.ParentStarId;
            targetHolon.ParentPlanet = sourceHolon.ParentPlanet;
            targetHolon.ParentPlanetId = sourceHolon.ParentPlanetId;
            targetHolon.ParentMoon = sourceHolon.ParentMoon;
            targetHolon.ParentMoonId = sourceHolon.ParentMoonId;
            targetHolon.ParentZome = sourceHolon.ParentZome;
            targetHolon.ParentZomeId = sourceHolon.ParentZomeId;
            targetHolon.ParentHolon = sourceHolon.ParentHolon;
            targetHolon.ParentHolonId = sourceHolon.ParentHolonId;
            targetHolon.ParentOmiverse = sourceHolon.ParentOmiverse;
            targetHolon.ParentOmiverseId = sourceHolon.ParentOmiverseId;
            targetHolon.ParentUniverse = sourceHolon.ParentUniverse;
            targetHolon.ParentUniverseId = sourceHolon.ParentUniverseId;
            targetHolon.ParentGalaxy = sourceHolon.ParentGalaxy;
            targetHolon.ParentGalaxyId = sourceHolon.ParentGalaxyId;
            targetHolon.ParentSolarSystem = sourceHolon.ParentSolarSystem;
            targetHolon.ParentSolarSystemId = sourceHolon.ParentSolarSystemId;
            targetHolon.Children = sourceHolon.Children;
            targetHolon.Nodes = sourceHolon.Nodes;
            //targetHolon.CelestialBodyCore.Id = sourceHolon.Id; //TODO: Dont think need targetHolon now?
            //targetHolon.CelestialBodyCore.ProviderKey = sourceHolon.ProviderKey; //TODO: Dont think need targetHolon now?
            targetHolon.CreatedByAvatar = sourceHolon.CreatedByAvatar;
            targetHolon.CreatedByAvatarId = sourceHolon.CreatedByAvatarId;
            targetHolon.CreatedDate = sourceHolon.CreatedDate;
            targetHolon.ModifiedByAvatar = sourceHolon.ModifiedByAvatar;
            targetHolon.ModifiedByAvatarId = sourceHolon.ModifiedByAvatarId;
            targetHolon.ModifiedDate = sourceHolon.ModifiedDate;
            targetHolon.DeletedByAvatar = sourceHolon.DeletedByAvatar;
            targetHolon.DeletedByAvatarId = sourceHolon.DeletedByAvatarId;
            targetHolon.DeletedDate = sourceHolon.DeletedDate;
            targetHolon.Version = sourceHolon.Version;
            targetHolon.IsActive = sourceHolon.IsActive;
            targetHolon.IsChanged = sourceHolon.IsChanged;
            targetHolon.IsNewHolon = sourceHolon.IsNewHolon;
            targetHolon.MetaData = sourceHolon.MetaData;
            targetHolon.ProviderMetaData = sourceHolon.ProviderMetaData;
            targetHolon.Original = sourceHolon.Original;

            return targetHolon;
        }

        public static IEnumerable<T2> MapBaseHolonProperties(IEnumerable<T1> sourceHolons)
        {
            List<T2> targetList = new List<T2>();

            foreach (T1 sourceHolon in sourceHolons)
                targetList.Add(MapBaseHolonProperties(sourceHolon));

            return targetList;
        }
    }
}