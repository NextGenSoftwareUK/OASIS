using System.Collections.Generic;
using System.Linq;
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

        public static T2 MapParentCelestialBodyProperties(T1 sourceHolon)
        {
            return MapParentCelestialBodyProperties(sourceHolon, new T2());
        }

        public static T2 MapParentCelestialBodyProperties(T1 sourceCelestialBody, T2 targetCelestialBody)
        {
            targetCelestialBody.ParentGreatGrandSuperStar = sourceCelestialBody.ParentGreatGrandSuperStar;
            targetCelestialBody.ParentGreatGrandSuperStarId = sourceCelestialBody.ParentGreatGrandSuperStarId;
            targetCelestialBody.ParentGrandSuperStar = sourceCelestialBody.ParentGrandSuperStar;
            targetCelestialBody.ParentGrandSuperStarId = sourceCelestialBody.ParentGrandSuperStarId;
            targetCelestialBody.ParentSuperStar = sourceCelestialBody.ParentSuperStar;
            targetCelestialBody.ParentSuperStarId = sourceCelestialBody.ParentSuperStarId;
            targetCelestialBody.ParentStar = sourceCelestialBody.ParentStar;
            targetCelestialBody.ParentStarId = sourceCelestialBody.ParentStarId;
            targetCelestialBody.ParentPlanet = sourceCelestialBody.ParentPlanet;
            targetCelestialBody.ParentPlanetId = sourceCelestialBody.ParentPlanetId;
            targetCelestialBody.ParentMoon = sourceCelestialBody.ParentMoon;
            targetCelestialBody.ParentMoonId = sourceCelestialBody.ParentMoonId;
            targetCelestialBody.ParentZome = sourceCelestialBody.ParentZome;
            targetCelestialBody.ParentZomeId = sourceCelestialBody.ParentZomeId;
            targetCelestialBody.ParentHolon = sourceCelestialBody.ParentHolon;
            targetCelestialBody.ParentHolonId = sourceCelestialBody.ParentHolonId;
            targetCelestialBody.ParentOmiverse = sourceCelestialBody.ParentOmiverse;
            targetCelestialBody.ParentOmiverseId = sourceCelestialBody.ParentOmiverseId;
            targetCelestialBody.ParentUniverse = sourceCelestialBody.ParentUniverse;
            targetCelestialBody.ParentUniverseId = sourceCelestialBody.ParentUniverseId;
            targetCelestialBody.ParentGalaxy = sourceCelestialBody.ParentGalaxy;
            targetCelestialBody.ParentGalaxyId = sourceCelestialBody.ParentGalaxyId;
            targetCelestialBody.ParentSolarSystem = sourceCelestialBody.ParentSolarSystem;
            targetCelestialBody.ParentSolarSystemId = sourceCelestialBody.ParentSolarSystemId;
 
            return targetCelestialBody;
        }
        public static IEnumerable<T2> MapParentCelestialBodyProperties(IEnumerable<T1> sourceCelestialBodies)
        {
            List<T2> targetList = new List<T2>();

            foreach (T1 sourceCelestialBody in sourceCelestialBodies)
                targetList.Add(MapParentCelestialBodyProperties(sourceCelestialBody));

            return targetList;
        }
    }

    public static class MapperForCollections<T1, T2>
        where T1 : List<IHolon>
        where T2 : List<IHolon>, new()
    {

        public static T2 MapBaseHolonProperties(T1 sourceHolons)
        {
            return MapBaseHolonProperties(sourceHolons, new T2());
        }

        public static T2 MapBaseHolonProperties(T1 sourceHolons, T2 targetHolons)
        {
            for (int i = 0; i < sourceHolons.Count(); i++)
            {
                targetHolons[i].Id = sourceHolons[i].Id;
                targetHolons[i].ProviderKey = sourceHolons[i].ProviderKey;
                targetHolons[i].Name = sourceHolons[i].Name;
                targetHolons[i].Description = sourceHolons[i].Description;
                targetHolons[i].ParentGreatGrandSuperStar = sourceHolons[i].ParentGreatGrandSuperStar;
                targetHolons[i].ParentGreatGrandSuperStarId = sourceHolons[i].ParentGreatGrandSuperStarId;
                targetHolons[i].ParentGrandSuperStar = sourceHolons[i].ParentGrandSuperStar;
                targetHolons[i].ParentGrandSuperStarId = sourceHolons[i].ParentGrandSuperStarId;
                targetHolons[i].ParentSuperStar = sourceHolons[i].ParentSuperStar;
                targetHolons[i].ParentSuperStarId = sourceHolons[i].ParentSuperStarId;
                targetHolons[i].ParentStar = sourceHolons[i].ParentStar;
                targetHolons[i].ParentStarId = sourceHolons[i].ParentStarId;
                targetHolons[i].ParentPlanet = sourceHolons[i].ParentPlanet;
                targetHolons[i].ParentPlanetId = sourceHolons[i].ParentPlanetId;
                targetHolons[i].ParentMoon = sourceHolons[i].ParentMoon;
                targetHolons[i].ParentMoonId = sourceHolons[i].ParentMoonId;
                targetHolons[i].ParentZome = sourceHolons[i].ParentZome;
                targetHolons[i].ParentZomeId = sourceHolons[i].ParentZomeId;
                targetHolons[i].ParentHolon = sourceHolons[i].ParentHolon;
                targetHolons[i].ParentHolonId = sourceHolons[i].ParentHolonId;
                targetHolons[i].ParentOmiverse = sourceHolons[i].ParentOmiverse;
                targetHolons[i].ParentOmiverseId = sourceHolons[i].ParentOmiverseId;
                targetHolons[i].ParentUniverse = sourceHolons[i].ParentUniverse;
                targetHolons[i].ParentUniverseId = sourceHolons[i].ParentUniverseId;
                targetHolons[i].ParentGalaxy = sourceHolons[i].ParentGalaxy;
                targetHolons[i].ParentGalaxyId = sourceHolons[i].ParentGalaxyId;
                targetHolons[i].ParentSolarSystem = sourceHolons[i].ParentSolarSystem;
                targetHolons[i].ParentSolarSystemId = sourceHolons[i].ParentSolarSystemId;
                targetHolons[i].Children = sourceHolons[i].Children;
                targetHolons[i].Nodes = sourceHolons[i].Nodes;
                //targetHolon.CelestialBodyCore.Id = sourceHolon.Id; //TODO: Dont think need targetHolon now?
                //targetHolon.CelestialBodyCore.ProviderKey = sourceHolon.ProviderKey; //TODO: Dont think need targetHolon now?
                targetHolons[i].CreatedByAvatar = sourceHolons[i].CreatedByAvatar;
                targetHolons[i].CreatedByAvatarId = sourceHolons[i].CreatedByAvatarId;
                targetHolons[i].CreatedDate = sourceHolons[i].CreatedDate;
                targetHolons[i].ModifiedByAvatar = sourceHolons[i].ModifiedByAvatar;
                targetHolons[i].ModifiedByAvatarId = sourceHolons[i].ModifiedByAvatarId;
                targetHolons[i].ModifiedDate = sourceHolons[i].ModifiedDate;
                targetHolons[i].DeletedByAvatar = sourceHolons[i].DeletedByAvatar;
                targetHolons[i].DeletedByAvatarId = sourceHolons[i].DeletedByAvatarId;
                targetHolons[i].DeletedDate = sourceHolons[i].DeletedDate;
                targetHolons[i].Version = sourceHolons[i].Version;
                targetHolons[i].IsActive = sourceHolons[i].IsActive;
                targetHolons[i].IsChanged = sourceHolons[i].IsChanged;
                targetHolons[i].IsNewHolon = sourceHolons[i].IsNewHolon;
                targetHolons[i].MetaData = sourceHolons[i].MetaData;
                targetHolons[i].ProviderMetaData = sourceHolons[i].ProviderMetaData;
                targetHolons[i].Original = sourceHolons[i].Original;
            }

            return targetHolons;
        }

        public static IEnumerable<T2> MapBaseHolonProperties(IEnumerable<T1> sourceHolons)
        {
            List<T2> targetList = new List<T2>();

            foreach (T1 sourceHolon in sourceHolons)
                targetList.Add(MapBaseHolonProperties(sourceHolon));

            return targetList;
        }

        public static T2 MapParentCelestialBodyProperties(T1 sourceCelestialBodies)
        {
            return MapParentCelestialBodyProperties(sourceCelestialBodies, new T2());
        }

        public static T2 MapParentCelestialBodyProperties(T1 sourceCelestialBodies, T2 targetCelestialBodies)
        {
            for (int i = 0; i < sourceCelestialBodies.Count(); i++)
            {
                targetCelestialBodies[i].ParentGreatGrandSuperStar = sourceCelestialBodies[i].ParentGreatGrandSuperStar;
                targetCelestialBodies[i].ParentGreatGrandSuperStarId = sourceCelestialBodies[i].ParentGreatGrandSuperStarId;
                targetCelestialBodies[i].ParentGrandSuperStar = sourceCelestialBodies[i].ParentGrandSuperStar;
                targetCelestialBodies[i].ParentGrandSuperStarId = sourceCelestialBodies[i].ParentGrandSuperStarId;
                targetCelestialBodies[i].ParentSuperStar = sourceCelestialBodies[i].ParentSuperStar;
                targetCelestialBodies[i].ParentSuperStarId = sourceCelestialBodies[i].ParentSuperStarId;
                targetCelestialBodies[i].ParentStar = sourceCelestialBodies[i].ParentStar;
                targetCelestialBodies[i].ParentStarId = sourceCelestialBodies[i].ParentStarId;
                targetCelestialBodies[i].ParentPlanet = sourceCelestialBodies[i].ParentPlanet;
                targetCelestialBodies[i].ParentPlanetId = sourceCelestialBodies[i].ParentPlanetId;
                targetCelestialBodies[i].ParentMoon = sourceCelestialBodies[i].ParentMoon;
                targetCelestialBodies[i].ParentMoonId = sourceCelestialBodies[i].ParentMoonId;
                targetCelestialBodies[i].ParentZome = sourceCelestialBodies[i].ParentZome;
                targetCelestialBodies[i].ParentZomeId = sourceCelestialBodies[i].ParentZomeId;
                targetCelestialBodies[i].ParentHolon = sourceCelestialBodies[i].ParentHolon;
                targetCelestialBodies[i].ParentHolonId = sourceCelestialBodies[i].ParentHolonId;
                targetCelestialBodies[i].ParentOmiverse = sourceCelestialBodies[i].ParentOmiverse;
                targetCelestialBodies[i].ParentOmiverseId = sourceCelestialBodies[i].ParentOmiverseId;
                targetCelestialBodies[i].ParentUniverse = sourceCelestialBodies[i].ParentUniverse;
                targetCelestialBodies[i].ParentUniverseId = sourceCelestialBodies[i].ParentUniverseId;
                targetCelestialBodies[i].ParentGalaxy = sourceCelestialBodies[i].ParentGalaxy;
                targetCelestialBodies[i].ParentGalaxyId = sourceCelestialBodies[i].ParentGalaxyId;
                targetCelestialBodies[i].ParentSolarSystem = sourceCelestialBodies[i].ParentSolarSystem;
                targetCelestialBodies[i].ParentSolarSystemId = sourceCelestialBodies[i].ParentSolarSystemId;
            }

            return targetCelestialBodies;
        }

        public static IEnumerable<T2> MapParentCelestialBodyProperties(IEnumerable<T1> sourceCelestialBodies)
        {
            List<T2> targetList = new List<T2>();

            foreach (T1 sourceCelestialBody in sourceCelestialBodies)
                targetList.Add(MapParentCelestialBodyProperties(sourceCelestialBody));

            return targetList;
        }
    }
}