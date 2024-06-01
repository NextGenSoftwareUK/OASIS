using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class Mapper
    {
        //public static IEnumerable<Holon> Convert(IEnumerable<IHolon> sourceHolons)
        //{
        //    if (sourceHolons != null)
        //    {
        //        List<Holon> targetList = new List<Holon>();

        //        foreach (IHolon sourceHolon in sourceHolons)
        //            targetList.Add((Holon)sourceHolon);

        //        return targetList;
        //    }
        //    else
        //        return null;
        //}

        //public static IEnumerable<IHolon> Convert(IEnumerable<Holon> sourceHolons)
        //{
        //    if (sourceHolons != null)
        //    {
        //        List<IHolon> targetList = new List<IHolon>();

        //        foreach (IHolon sourceHolon in sourceHolons)
        //            targetList.Add(sourceHolon);

        //        return targetList;
        //    }
        //    else
        //        return null;
        //}

        public static IEnumerable<T> Convert<T>(IEnumerable<IHolon> sourceHolons) where T : IHolon
        {
            if (sourceHolons != null)
            {
                List<T> targetList = new List<T>();

                foreach (IHolon sourceHolon in sourceHolons)
                    targetList.Add((T)sourceHolon);

                return targetList;
            }
            else
                return null;
        }

        public static IEnumerable<IHolon> Convert<T>(IEnumerable<T> sourceHolons) where T : IHolon
        {
            if (sourceHolons != null)
            {
                IEnumerable<IHolon> targetList = [.. sourceHolons];
                return targetList;
            }
            else
                return null;
        }

        public static IEnumerable<T2> Convert<T1, T2>(IEnumerable<T1> sourceHolons) where T1 : IHolon where T2 : IHolon
        {
            if (sourceHolons != null)
            {
                //IEnumerable<T2> targetList = [.. sourceHolons];

                List<T2> targetList = new List<T2>();

                foreach (IHolon sourceHolon in sourceHolons)
                    targetList.Add((T2)sourceHolon);

                return targetList;
            }
            else
                return null;
        }

        public static ICelestialBody ConvertIHolonToICelestialBody(IHolon holon)
        {
            ICelestialBody celestialBody = (ICelestialBody)holon;
            MapBaseHolonProperties(holon, celestialBody);

            celestialBody.Age = holon.MetaData != null && holon.MetaData.ContainsKey("Age") && holon.MetaData["Age"] != null ? System.Convert.ToInt32(holon.MetaData["Age"]) : 0;
            celestialBody.EclipticLatitute = holon.MetaData != null && holon.MetaData.ContainsKey("EclipticLatitute") && holon.MetaData["EclipticLatitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["EclipticLatitute"]) : 0;
            celestialBody.EclipticLongitute = holon.MetaData != null && holon.MetaData.ContainsKey("EclipticLongitute") && holon.MetaData["EclipticLongitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["EclipticLongitute"]) : 0;
            celestialBody.EquatorialLatitute = holon.MetaData != null && holon.MetaData.ContainsKey("EquatorialLatitute") && holon.MetaData["EquatorialLatitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["EquatorialLatitute"]) : 0;
            celestialBody.EquatorialLongitute = holon.MetaData != null && holon.MetaData.ContainsKey("EquatorialLongitute") && holon.MetaData["EquatorialLongitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["EquatorialLongitute"]) : 0;
            celestialBody.GalacticLatitute = holon.MetaData != null && holon.MetaData.ContainsKey("GalacticLatitute") && holon.MetaData["GalacticLatitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["GalacticLatitute"]) : 0;
            celestialBody.GalacticLongitute = holon.MetaData != null && holon.MetaData.ContainsKey("GalacticLongitute") && holon.MetaData["GalacticLongitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["GalacticLongitute"]) : 0;
            celestialBody.HorizontalLatitute = holon.MetaData != null && holon.MetaData.ContainsKey("HorizontalLatitute") && holon.MetaData["HorizontalLatitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["HorizontalLatitute"]) : 0;
            celestialBody.HorizontalLongitute = holon.MetaData != null && holon.MetaData.ContainsKey("HorizontalLongitute") && holon.MetaData["HorizontalLongitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["HorizontalLongitute"]) : 0;
            celestialBody.Mass = holon.MetaData != null && holon.MetaData.ContainsKey("Mass") && holon.MetaData["Mass"] != null ? System.Convert.ToInt32(holon.MetaData["Mass"]) : 0;
            celestialBody.NumberActiveAvatars = holon.MetaData != null && holon.MetaData.ContainsKey("NumberActiveAvatars") && holon.MetaData["NumberActiveAvatars"] != null ? System.Convert.ToInt32(holon.MetaData["NumberActiveAvatars"]) : 0;
            celestialBody.NumberRegisteredAvatars = holon.MetaData != null && holon.MetaData.ContainsKey("NumberRegisteredAvatars") && holon.MetaData["NumberRegisteredAvatars"] != null ? System.Convert.ToInt32(holon.MetaData["NumberRegisteredAvatars"]) : 0;
            celestialBody.OrbitPositionFromParentStar = holon.MetaData != null && holon.MetaData.ContainsKey("OrbitPositionFromParentStar") && holon.MetaData["OrbitPositionFromParentStar"] != null ? System.Convert.ToInt32(holon.MetaData["OrbitPositionFromParentStar"]) : 0;
            celestialBody.RotationSpeed = holon.MetaData != null && holon.MetaData.ContainsKey("RotationSpeed") && holon.MetaData["RotationSpeed"] != null ? System.Convert.ToInt32(holon.MetaData["RotationSpeed"]) : 0;
            celestialBody.Size = holon.MetaData != null && holon.MetaData.ContainsKey("Size") && holon.MetaData["Size"] != null ? System.Convert.ToInt32(holon.MetaData["Size"]) : 0;
            celestialBody.SpaceQuadrant = holon.MetaData != null && holon.MetaData.ContainsKey("SpaceQuadrant") && holon.MetaData["SpaceQuadrant"] != null ? (SpaceQuadrantType)Enum.Parse(typeof(SpaceQuadrantType), holon.MetaData["SpaceQuadrant"].ToString()) : SpaceQuadrantType.None;
            celestialBody.SpaceSector = holon.MetaData != null && holon.MetaData.ContainsKey("SpaceSector") && holon.MetaData["SpaceSector"] != null ? System.Convert.ToInt32(holon.MetaData["SpaceSector"]) : 0;
            celestialBody.SubDimensionLevel = holon.MetaData != null && holon.MetaData.ContainsKey("SubDimensionLevel") && holon.MetaData["SubDimensionLevel"] != null ? (SubDimensionLevel)Enum.Parse(typeof(SubDimensionLevel), holon.MetaData["SubDimensionLevel"].ToString()) : SubDimensionLevel.None;
            celestialBody.SuperGalacticLatitute = holon.MetaData != null && holon.MetaData.ContainsKey("SuperGalacticLatitute") && holon.MetaData["SuperGalacticLatitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["SuperGalacticLatitute"]) : 0;
            celestialBody.SuperGalacticLongitute = holon.MetaData != null && holon.MetaData.ContainsKey("SuperGalacticLongitute") && holon.MetaData["SuperGalacticLongitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["SuperGalacticLongitute"]) : 0;
            celestialBody.Temperature = holon.MetaData != null && holon.MetaData.ContainsKey("Temperature") && holon.MetaData["Temperature"] != null ? System.Convert.ToInt32(holon.MetaData["Temperature"]) : 0;
            celestialBody.TiltAngle = System.Convert.ToInt32(holon.MetaData["TiltAngle"]);
            celestialBody.Weight = System.Convert.ToInt32(holon.MetaData["Weight"]);

            return celestialBody;
        }

        public static IEnumerable<ICelestialBody> ConvertIHolonsToICelestialBodies(IEnumerable<IHolon> holons)
        {
            List<ICelestialBody> celestialBodies = new List<ICelestialBody>();

            foreach (IHolon holon in holons)
                celestialBodies.Add(ConvertIHolonToICelestialBody(holon));

            return celestialBodies;
        }

        public static IEnumerable<ICelestialBody> ConvertIHolonsToICelestialBodies<T>(IEnumerable<T> holons)
        {
            List<ICelestialBody> celestialBodies = new List<ICelestialBody>();

            foreach (IHolon holon in holons)
                celestialBodies.Add(ConvertIHolonToICelestialBody(holon));

            return celestialBodies;
        }

        public static ICelestialSpace ConvertIHolonToICelestialSpace(IHolon holon)
        {
            ICelestialSpace celestialSpace = (ICelestialSpace)holon;
            MapBaseHolonProperties(holon, celestialSpace);

            celestialSpace.Age = holon.MetaData != null && holon.MetaData.ContainsKey("Age") && holon.MetaData["Age"] != null ? System.Convert.ToInt32(holon.MetaData["Age"]) : 0;
            celestialSpace.EclipticLatitute = holon.MetaData != null && holon.MetaData.ContainsKey("EclipticLatitute") && holon.MetaData["EclipticLatitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["EclipticLatitute"]) : 0;
            celestialSpace.EclipticLongitute = holon.MetaData != null && holon.MetaData.ContainsKey("EclipticLongitute") && holon.MetaData["EclipticLongitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["EclipticLongitute"]) : 0;
            celestialSpace.EquatorialLatitute = holon.MetaData != null && holon.MetaData.ContainsKey("EquatorialLatitute") && holon.MetaData["EquatorialLatitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["EquatorialLatitute"]) : 0;
            celestialSpace.EquatorialLongitute = holon.MetaData != null && holon.MetaData.ContainsKey("EquatorialLongitute") && holon.MetaData["EquatorialLongitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["EquatorialLongitute"]) : 0;
            celestialSpace.GalacticLatitute = holon.MetaData != null && holon.MetaData.ContainsKey("GalacticLatitute") && holon.MetaData["GalacticLatitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["GalacticLatitute"]) : 0;
            celestialSpace.GalacticLongitute = holon.MetaData != null && holon.MetaData.ContainsKey("GalacticLongitute") && holon.MetaData["GalacticLongitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["GalacticLongitute"]) : 0;
            celestialSpace.HorizontalLatitute = holon.MetaData != null && holon.MetaData.ContainsKey("HorizontalLatitute") && holon.MetaData["HorizontalLatitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["HorizontalLatitute"]) : 0;
            celestialSpace.HorizontalLongitute = holon.MetaData != null && holon.MetaData.ContainsKey("HorizontalLongitute") && holon.MetaData["HorizontalLongitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["HorizontalLongitute"]) : 0;
            //celestialBody.Mass = System.Convert.ToInt32(holon.MetaData["Mass"]);
            //celestialBody.NumberActiveAvatars = System.Convert.ToInt32(holon.MetaData["NumberActiveAvatars"]);
            //celestialBody.NumberRegisteredAvatars = System.Convert.ToInt32(holon.MetaData["NumberRegisteredAvatars"]);
            //celestialBody.OrbitPositionFromParentStar = System.Convert.ToInt32(holon.MetaData["OrbitPositionFromParentStar"]);
            //celestialBody.RotationSpeed = System.Convert.ToInt32(holon.MetaData["RotationSpeed"]);
            celestialSpace.Size = holon.MetaData != null && holon.MetaData.ContainsKey("Size") && holon.MetaData["Size"] != null ? System.Convert.ToInt32(holon.MetaData["Size"]) : 0;
            celestialSpace.SpaceQuadrant = holon.MetaData != null && holon.MetaData.ContainsKey("SpaceQuadrant") && holon.MetaData["SpaceQuadrant"] != null ? (SpaceQuadrantType)Enum.Parse(typeof(SpaceQuadrantType), holon.MetaData["SpaceQuadrant"].ToString()) : SpaceQuadrantType.None;
            celestialSpace.SpaceSector = holon.MetaData != null && holon.MetaData.ContainsKey("SpaceSector") && holon.MetaData["SpaceSector"] != null ? System.Convert.ToInt32(holon.MetaData["SpaceSector"]) : 0;
            //celestialBody.SubDimensionLevel = System.Convert.ToInt32(holon.MetaData["SubDimensionLevel"]);
            celestialSpace.SuperGalacticLatitute = holon.MetaData != null && holon.MetaData.ContainsKey("SuperGalacticLatitute") && holon.MetaData["SuperGalacticLatitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["SuperGalacticLatitute"]) : 0;
            celestialSpace.SuperGalacticLongitute = holon.MetaData != null && holon.MetaData.ContainsKey("SuperGalacticLongitute") && holon.MetaData["SuperGalacticLongitute"] != null ? (float)System.Convert.ToDouble(holon.MetaData["SuperGalacticLongitute"]) : 0;
            celestialSpace.Temperature = holon.MetaData != null && holon.MetaData.ContainsKey("Temperature") && holon.MetaData["Temperature"] != null ? System.Convert.ToInt32(holon.MetaData["Temperature"]) : 0;
            //celestialBody.TiltAngle = System.Convert.ToInt32(holon.MetaData["TiltAngle"]);
            //celestialBody.Weight = System.Convert.ToInt32(holon.MetaData["Weight"]);

            return celestialSpace;
        }

        public static IEnumerable<ICelestialSpace> ConvertIHolonsToICelestialSpaces<T>(IEnumerable<T> holons)
        {
            List<ICelestialSpace> celestialSpaces = new List<ICelestialSpace>();

            foreach (IHolon holon in holons)
                celestialSpaces.Add(ConvertIHolonToICelestialSpace(holon));

            return celestialSpaces;
        }

        //public static IHolon MapBaseHolonProperties(IHolon sourceHolon, IHolon targetHolon, bool mapCelestialProperties = true)
        //{
        //    if (sourceHolon != null && targetHolon != null)
        //    {
        //        targetHolon.Id = sourceHolon.Id;
        //        targetHolon.ProviderUniqueStorageKey = sourceHolon.ProviderUniqueStorageKey;
        //        targetHolon.Name = sourceHolon.Name;
        //        targetHolon.Description = sourceHolon.Description;
        //        targetHolon.HolonType = sourceHolon.HolonType;

        //        if (mapCelestialProperties)
        //        {
        //            targetHolon.ParentGreatGrandSuperStar = sourceHolon.ParentGreatGrandSuperStar;
        //            targetHolon.ParentGreatGrandSuperStarId = sourceHolon.ParentGreatGrandSuperStarId;
        //            targetHolon.ParentGrandSuperStar = sourceHolon.ParentGrandSuperStar;
        //            targetHolon.ParentGrandSuperStarId = sourceHolon.ParentGrandSuperStarId;
        //            targetHolon.ParentSuperStar = sourceHolon.ParentSuperStar;
        //            targetHolon.ParentSuperStarId = sourceHolon.ParentSuperStarId;
        //            targetHolon.ParentStar = sourceHolon.ParentStar;
        //            targetHolon.ParentStarId = sourceHolon.ParentStarId;
        //            targetHolon.ParentPlanet = sourceHolon.ParentPlanet;
        //            targetHolon.ParentPlanetId = sourceHolon.ParentPlanetId;
        //            targetHolon.ParentMoon = sourceHolon.ParentMoon;
        //            targetHolon.ParentMoonId = sourceHolon.ParentMoonId;
        //            targetHolon.ParentCelestialSpaceId = sourceHolon.ParentCelestialSpaceId;
        //            targetHolon.ParentCelestialBodyId = sourceHolon.ParentCelestialBodyId;
        //            targetHolon.ParentZome = sourceHolon.ParentZome;
        //            targetHolon.ParentZomeId = sourceHolon.ParentZomeId;
        //            targetHolon.ParentHolon = sourceHolon.ParentHolon;
        //            targetHolon.ParentHolonId = sourceHolon.ParentHolonId;
        //            targetHolon.ParentOmniverse = sourceHolon.ParentOmniverse;
        //            targetHolon.ParentOmniverseId = sourceHolon.ParentOmniverseId;
        //            targetHolon.ParentMultiverse = sourceHolon.ParentMultiverse;
        //            targetHolon.ParentMultiverseId = sourceHolon.ParentMultiverseId;
        //            targetHolon.ParentDimension = sourceHolon.ParentDimension;
        //            targetHolon.ParentDimensionId = sourceHolon.ParentDimensionId;
        //            targetHolon.ParentUniverse = sourceHolon.ParentUniverse;
        //            targetHolon.ParentUniverseId = sourceHolon.ParentUniverseId;
        //            targetHolon.ParentGalaxyCluster = sourceHolon.ParentGalaxyCluster;
        //            targetHolon.ParentGalaxyClusterId = sourceHolon.ParentGalaxyClusterId;
        //            targetHolon.ParentGalaxy = sourceHolon.ParentGalaxy;
        //            targetHolon.ParentGalaxyId = sourceHolon.ParentGalaxyId;
        //            targetHolon.ParentSolarSystem = sourceHolon.ParentSolarSystem;
        //            targetHolon.ParentSolarSystemId = sourceHolon.ParentSolarSystemId;
        //        }

        //        targetHolon.Children = sourceHolon.Children;
        //        targetHolon.Nodes = sourceHolon.Nodes;
        //        //targetHolon.CelestialBodyCore.Id = sourceHolon.Id; //TODO: Dont think need targetHolon now?
        //        //targetHolon.CelestialBodyCore.ProviderUniqueStorageKey = sourceHolon.ProviderUniqueStorageKey; //TODO: Dont think need targetHolon now?
        //        targetHolon.CreatedByAvatar = sourceHolon.CreatedByAvatar;
        //        targetHolon.CreatedByAvatarId = sourceHolon.CreatedByAvatarId;
        //        targetHolon.CreatedDate = sourceHolon.CreatedDate;
        //        targetHolon.ModifiedByAvatar = sourceHolon.ModifiedByAvatar;
        //        targetHolon.ModifiedByAvatarId = sourceHolon.ModifiedByAvatarId;
        //        targetHolon.ModifiedDate = sourceHolon.ModifiedDate;
        //        targetHolon.DeletedByAvatar = sourceHolon.DeletedByAvatar;
        //        targetHolon.DeletedByAvatarId = sourceHolon.DeletedByAvatarId;
        //        targetHolon.DeletedDate = sourceHolon.DeletedDate;
        //        targetHolon.Version = sourceHolon.Version;
        //        targetHolon.IsActive = sourceHolon.IsActive;
        //        targetHolon.IsChanged = sourceHolon.IsChanged;
        //        targetHolon.IsNewHolon = sourceHolon.IsNewHolon;
        //        targetHolon.MetaData = sourceHolon.MetaData;
        //        targetHolon.ProviderMetaData = sourceHolon.ProviderMetaData;
        //        targetHolon.Original = sourceHolon.Original;
        //    }

        //    return targetHolon;
        //}


        public static IHolon MapBaseHolonProperties(IHolon sourceHolon, IHolon targetHolon, bool mapCelestialProperties = true) 
        {
            if (sourceHolon != null && targetHolon != null)
            {
                targetHolon.Id = sourceHolon.Id;
                targetHolon.ProviderUniqueStorageKey = sourceHolon.ProviderUniqueStorageKey;
                targetHolon.Name = sourceHolon.Name;
                targetHolon.Description = sourceHolon.Description;
                targetHolon.HolonType = sourceHolon.HolonType;

                if (mapCelestialProperties)
                {
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
                    targetHolon.ParentCelestialSpaceId = sourceHolon.ParentCelestialSpaceId;
                    targetHolon.ParentCelestialBodyId = sourceHolon.ParentCelestialBodyId;
                    targetHolon.ParentZome = sourceHolon.ParentZome;
                    targetHolon.ParentZomeId = sourceHolon.ParentZomeId;
                    targetHolon.ParentHolon = sourceHolon.ParentHolon;
                    targetHolon.ParentHolonId = sourceHolon.ParentHolonId;
                    targetHolon.ParentOmniverse = sourceHolon.ParentOmniverse;
                    targetHolon.ParentOmniverseId = sourceHolon.ParentOmniverseId;
                    targetHolon.ParentMultiverse = sourceHolon.ParentMultiverse;
                    targetHolon.ParentMultiverseId = sourceHolon.ParentMultiverseId;
                    targetHolon.ParentDimension = sourceHolon.ParentDimension;
                    targetHolon.ParentDimensionId = sourceHolon.ParentDimensionId;
                    targetHolon.ParentUniverse = sourceHolon.ParentUniverse;
                    targetHolon.ParentUniverseId = sourceHolon.ParentUniverseId;
                    targetHolon.ParentGalaxyCluster = sourceHolon.ParentGalaxyCluster;
                    targetHolon.ParentGalaxyClusterId = sourceHolon.ParentGalaxyClusterId;
                    targetHolon.ParentGalaxy = sourceHolon.ParentGalaxy;
                    targetHolon.ParentGalaxyId = sourceHolon.ParentGalaxyId;
                    targetHolon.ParentSolarSystem = sourceHolon.ParentSolarSystem;
                    targetHolon.ParentSolarSystemId = sourceHolon.ParentSolarSystemId;
                }

                targetHolon.Children = sourceHolon.Children;
                targetHolon.Nodes = sourceHolon.Nodes;
                //targetHolon.CelestialBodyCore.Id = sourceHolon.Id; //TODO: Dont think need targetHolon now?
                //targetHolon.CelestialBodyCore.ProviderUniqueStorageKey = sourceHolon.ProviderUniqueStorageKey; //TODO: Dont think need targetHolon now?
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
            }

            return targetHolon;
        }

        public static IEnumerable<IHolon> MapBaseHolonProperties(IEnumerable<IHolon> sourceHolons, IEnumerable<IHolon> targetHolons, bool mapCelestialProperties = true)
        {
            if (sourceHolons != null && targetHolons != null)
            {
                List<IHolon> sourceList = sourceHolons.ToList();
                List<IHolon> targetList = targetHolons.ToList();

                for (int i = 0; i < sourceHolons.Count(); i++)
                    targetList[i] = MapBaseHolonProperties(sourceList[i], targetList[i], mapCelestialProperties);

                return targetList;
            }
            else
                return null;
        }

        public static T2 MapBaseHolonProperties<T1, T2>(T1 sourceHolon, T2 targetHolon, bool mapCelestialProperties = true) where T1 : IHolon where T2 : IHolon, new()
        {
            if (targetHolon == null)
                targetHolon = new T2();

            return MapBaseHolonProperties(sourceHolon, targetHolon, mapCelestialProperties);
        }

        public static IEnumerable<T2> MapBaseHolonProperties<T1, T2>(IEnumerable<T1> sourceHolons, IEnumerable<T2> targetHolons, bool mapCelestialProperties = true) where T1 : IHolon where T2 : IHolon, new()
        {
            if (sourceHolons != null && targetHolons != null)
            {
                List<T1> sourceList = sourceHolons.ToList();
                List<T2> targetList = targetHolons.ToList();

                for (int i = 0; i < sourceHolons.Count(); i++)
                    targetList[i] = MapBaseHolonProperties(sourceList[i], targetList[i], mapCelestialProperties);

                return targetList;
            }
            else
                return null;
        }

        //public static IEnumerable<IHolon> MapBaseHolonProperties(IEnumerable<IHolon> sourceHolons, IEnumerable<IHolon> targetHolons)
        //{
        //    if (sourceHolons != null && targetHolons != null)
        //    {
        //        List<IHolon> sourceList = sourceHolons.ToList();
        //        List<IHolon> targetList = targetHolons.ToList();

        //        for (int i = 0; i < sourceHolons.Count(); i++)
        //            targetList[i] = (MapBaseHolonProperties(sourceList[i], targetList[i]));

        //        return targetList;
        //    }
        //    else
        //        return null;
        //}

        //public static IEnumerable<IHolon> MapBaseHolonProperties(IEnumerable<IHolon> sourceHolons)
        //{
        //    return MapBaseHolonProperties(sourceHolons, new List<IHolon>());
        //}

        //public static IEnumerable<dynamic> MapBaseHolonProperties(IEnumerable<dynamic> sourceHolons, IEnumerable<dynamic> targetHolons)
        //{
        //    if (sourceHolons != null && targetHolons != null)
        //    {
        //        List<dynamic> sourceList = sourceHolons.ToList();
        //        List<dynamic> targetList = targetHolons.ToList();

        //        for (int i = 0; i < sourceHolons.Count(); i++)
        //            targetList[i] = (MapBaseHolonProperties(sourceList[i], targetList[i]));

        //        return targetList;
        //    }
        //    else
        //        return null;
        //}

        //public static IEnumerable<dynamic> MapBaseHolonProperties(IEnumerable<dynamic> sourceHolons)
        //{
        //    return MapBaseHolonProperties(sourceHolons, new List<dynamic>());
        //}

        //TODO: Need to get working properly.
        //public static IEnumerable<IHolon> CastCollection(IEnumerable<IHolon> sourceHolons) //where T1 : IHolon//, T2 : IHolon
        //{
        //    if (sourceHolons != null)
        //    {
        //        List<IHolon> sourceList = sourceHolons.ToList();
        //        List<IHolon> targetList = new List<IHolon>();

        //        for (int i = 0; i < sourceHolons.Count(); i++)
        //            targetList[i] = sourceList[i];

        //        return targetList;
        //    }
        //    else
        //        return null;
        //}

        public static IHolon MapParentCelestialBodyProperties(IHolon sourceCelestialBody, IHolon targetCelestialBody, bool onlyMapIfTargetIsNull = true)
        {
            if (sourceCelestialBody != null && targetCelestialBody != null)
            {
                if (targetCelestialBody.ParentGreatGrandSuperStar == null || (targetCelestialBody.ParentGreatGrandSuperStar != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGreatGrandSuperStar = sourceCelestialBody.ParentGreatGrandSuperStar;

                if (targetCelestialBody.ParentGreatGrandSuperStarId == Guid.Empty || (targetCelestialBody.ParentGreatGrandSuperStarId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGreatGrandSuperStarId = sourceCelestialBody.ParentGreatGrandSuperStarId;

                if (targetCelestialBody.ParentGrandSuperStar == null || (targetCelestialBody.ParentGrandSuperStar != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGrandSuperStar = sourceCelestialBody.ParentGrandSuperStar;

                if (targetCelestialBody.ParentGrandSuperStarId == Guid.Empty || (targetCelestialBody.ParentGrandSuperStarId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGrandSuperStarId = sourceCelestialBody.ParentGrandSuperStarId;

                if (targetCelestialBody.ParentSuperStar == null || (targetCelestialBody.ParentSuperStar != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentSuperStar = sourceCelestialBody.ParentSuperStar;

                if (targetCelestialBody.ParentSuperStarId == Guid.Empty || (targetCelestialBody.ParentSuperStarId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentSuperStarId = sourceCelestialBody.ParentSuperStarId;

                if (targetCelestialBody.ParentStar == null || (targetCelestialBody.ParentStar != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentStar = sourceCelestialBody.ParentStar;

                if (targetCelestialBody.ParentStarId == Guid.Empty || (targetCelestialBody.ParentStarId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentStarId = sourceCelestialBody.ParentStarId;

                if (targetCelestialBody.ParentPlanet == null || (targetCelestialBody.ParentPlanet != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentPlanet = sourceCelestialBody.ParentPlanet;

                if (targetCelestialBody.ParentPlanetId == Guid.Empty || (targetCelestialBody.ParentPlanetId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentPlanetId = sourceCelestialBody.ParentPlanetId;

                if (targetCelestialBody.ParentMoon == null || (targetCelestialBody.ParentMoon != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentMoon = sourceCelestialBody.ParentMoon;

                if (targetCelestialBody.ParentMoonId == Guid.Empty || (targetCelestialBody.ParentMoonId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentMoonId = sourceCelestialBody.ParentMoonId;

                if (targetCelestialBody.ParentCelestialSpace == null || (targetCelestialBody.ParentCelestialSpace != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentCelestialSpace = sourceCelestialBody.ParentCelestialSpace;

                if (targetCelestialBody.ParentCelestialSpaceId == Guid.Empty || (targetCelestialBody.ParentCelestialSpaceId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentCelestialSpaceId = sourceCelestialBody.ParentCelestialSpaceId;

                if (targetCelestialBody.ParentCelestialBody == null || (targetCelestialBody.ParentCelestialBody != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentCelestialBody = sourceCelestialBody.ParentCelestialBody;

                if (targetCelestialBody.ParentCelestialBodyId == Guid.Empty || (targetCelestialBody.ParentCelestialBodyId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentCelestialBodyId = sourceCelestialBody.ParentCelestialBodyId;

                if (targetCelestialBody.ParentZome == null || (targetCelestialBody.ParentZome != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentZome = sourceCelestialBody.ParentZome;

                if (targetCelestialBody.ParentZomeId == Guid.Empty || (targetCelestialBody.ParentZomeId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentZomeId = sourceCelestialBody.ParentZomeId;

                if (targetCelestialBody.ParentHolon == null || (targetCelestialBody.ParentHolon != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentHolon = sourceCelestialBody.ParentHolon;

                if (targetCelestialBody.ParentHolonId == Guid.Empty || (targetCelestialBody.ParentHolonId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentHolonId = sourceCelestialBody.ParentHolonId;

                if (targetCelestialBody.ParentOmniverse == null || (targetCelestialBody.ParentOmniverse != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentOmniverse = sourceCelestialBody.ParentOmniverse;

                if (targetCelestialBody.ParentOmniverseId == Guid.Empty || (targetCelestialBody.ParentOmniverseId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentOmniverseId = sourceCelestialBody.ParentOmniverseId;

                if (targetCelestialBody.ParentMultiverse == null || (targetCelestialBody.ParentMultiverse != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentMultiverse = sourceCelestialBody.ParentMultiverse;

                if (targetCelestialBody.ParentMultiverseId == Guid.Empty || (targetCelestialBody.ParentMultiverseId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentMultiverseId = sourceCelestialBody.ParentMultiverseId;

                if (targetCelestialBody.ParentDimension == null || (targetCelestialBody.ParentDimension != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentDimension = sourceCelestialBody.ParentDimension;

                if (targetCelestialBody.ParentDimensionId == Guid.Empty || (targetCelestialBody.ParentDimensionId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentDimensionId = sourceCelestialBody.ParentDimensionId;

                if (targetCelestialBody.ParentUniverse == null || (targetCelestialBody.ParentUniverse != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentUniverse = sourceCelestialBody.ParentUniverse;

                if (targetCelestialBody.ParentUniverseId == Guid.Empty || (targetCelestialBody.ParentUniverseId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentUniverseId = sourceCelestialBody.ParentUniverseId;

                if (targetCelestialBody.ParentGalaxyCluster == null || (targetCelestialBody.ParentGalaxyCluster != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGalaxyCluster = sourceCelestialBody.ParentGalaxyCluster;

                if (targetCelestialBody.ParentGalaxyClusterId == Guid.Empty || (targetCelestialBody.ParentGalaxyClusterId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGalaxyClusterId = sourceCelestialBody.ParentGalaxyClusterId;

                if (targetCelestialBody.ParentGalaxy == null || (targetCelestialBody.ParentGalaxy != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGalaxy = sourceCelestialBody.ParentGalaxy;

                if (targetCelestialBody.ParentGalaxyId == Guid.Empty || (targetCelestialBody.ParentGalaxyId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGalaxyId = sourceCelestialBody.ParentGalaxyId;

                if (targetCelestialBody.ParentSolarSystem == null || (targetCelestialBody.ParentSolarSystem != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentSolarSystem = sourceCelestialBody.ParentSolarSystem;

                if (targetCelestialBody.ParentSolarSystemId == Guid.Empty || (targetCelestialBody.ParentSolarSystemId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentSolarSystemId = sourceCelestialBody.ParentSolarSystemId;
            }

            return targetCelestialBody;
        }
    }

    public static class Mapper<T1, T2>
        where T1 : IHolon
        where T2 : IHolon, new()
    {

        public static T2 MapBaseHolonProperties(T1 sourceHolon, bool mapCelestialProperties = true)
        {
            return MapBaseHolonProperties(sourceHolon, new T2(), mapCelestialProperties);
        }

        public static T2 MapBaseHolonProperties(T1 sourceHolon, T2 targetHolon, bool mapCelestialProperties = true)
        {
            if (sourceHolon != null && targetHolon != null)
            {
                targetHolon.Id = sourceHolon.Id;
                targetHolon.ProviderUniqueStorageKey = sourceHolon.ProviderUniqueStorageKey;
                targetHolon.Name = sourceHolon.Name;
                targetHolon.Description = sourceHolon.Description;
                targetHolon.HolonType = sourceHolon.HolonType;

                if (mapCelestialProperties)
                {
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
                    targetHolon.ParentCelestialSpaceId = sourceHolon.ParentCelestialSpaceId;
                    targetHolon.ParentCelestialBodyId = sourceHolon.ParentCelestialBodyId;
                    targetHolon.ParentZome = sourceHolon.ParentZome;
                    targetHolon.ParentZomeId = sourceHolon.ParentZomeId;
                    targetHolon.ParentHolon = sourceHolon.ParentHolon;
                    targetHolon.ParentHolonId = sourceHolon.ParentHolonId;
                    targetHolon.ParentOmniverse = sourceHolon.ParentOmniverse;
                    targetHolon.ParentOmniverseId = sourceHolon.ParentOmniverseId;
                    targetHolon.ParentMultiverse = sourceHolon.ParentMultiverse;
                    targetHolon.ParentMultiverseId = sourceHolon.ParentMultiverseId;
                    targetHolon.ParentDimension = sourceHolon.ParentDimension;
                    targetHolon.ParentDimensionId = sourceHolon.ParentDimensionId;
                    targetHolon.ParentUniverse = sourceHolon.ParentUniverse;
                    targetHolon.ParentUniverseId = sourceHolon.ParentUniverseId;
                    targetHolon.ParentGalaxyCluster = sourceHolon.ParentGalaxyCluster;
                    targetHolon.ParentGalaxyClusterId = sourceHolon.ParentGalaxyClusterId;
                    targetHolon.ParentGalaxy = sourceHolon.ParentGalaxy;
                    targetHolon.ParentGalaxyId = sourceHolon.ParentGalaxyId;
                    targetHolon.ParentSolarSystem = sourceHolon.ParentSolarSystem;
                    targetHolon.ParentSolarSystemId = sourceHolon.ParentSolarSystemId;
                }

                targetHolon.Children = sourceHolon.Children;
                targetHolon.Nodes = sourceHolon.Nodes;
                //targetHolon.CelestialBodyCore.Id = sourceHolon.Id; //TODO: Dont think need targetHolon now?
                //targetHolon.CelestialBodyCore.ProviderUniqueStorageKey = sourceHolon.ProviderUniqueStorageKey; //TODO: Dont think need targetHolon now?
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
            }

            return targetHolon;
        }

        public static IEnumerable<T2> MapBaseHolonProperties(IEnumerable<T1> sourceHolons, IEnumerable<T2> targetHolons, bool mapCelestialProperties = true)
        {
            if (sourceHolons != null)
            {
                List<T2> targetList = new List<T2>();

                foreach (T1 sourceHolon in sourceHolons)
                    //targetList.Add(MapBaseHolonProperties(sourceHolon, new T2(), mapCelestialProperties));
                    targetList.Add(MapBaseHolonProperties(sourceHolon, mapCelestialProperties));

                targetHolons = targetList;
                return targetHolons;
            }
            else
                return null;
        }

        public static IEnumerable<T2> MapBaseHolonProperties(IEnumerable<T1> sourceHolons, bool mapCelestialProperties = true)
        {
            return MapBaseHolonProperties(sourceHolons, new List<T2>(), mapCelestialProperties);
        }

        public static IEnumerable<IHolon> Convert(IEnumerable<T1> sourceHolons)
        {
            if (sourceHolons != null)
            {
                List<IHolon> targetList = new List<IHolon>();

                foreach (T1 sourceHolon in sourceHolons)
                    targetList.Add(sourceHolon);

                return targetList;
            }
            else
                return null;
        }

        public static T2 MapParentCelestialBodyProperties(T1 sourceHolon)
        {
            return MapParentCelestialBodyProperties(sourceHolon, new T2());
        }

        public static T2 MapParentCelestialBodyProperties(T1 sourceCelestialBody, T2 targetCelestialBody, bool onlyMapIfTargetIsNull = true)
        {
            if (sourceCelestialBody != null && targetCelestialBody != null)
            {
                if (targetCelestialBody.ParentGreatGrandSuperStar == null || (targetCelestialBody.ParentGreatGrandSuperStar != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGreatGrandSuperStar = sourceCelestialBody.ParentGreatGrandSuperStar;

                if (targetCelestialBody.ParentGreatGrandSuperStarId == Guid.Empty || (targetCelestialBody.ParentGreatGrandSuperStarId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGreatGrandSuperStarId = sourceCelestialBody.ParentGreatGrandSuperStarId;

                if (targetCelestialBody.ParentGrandSuperStar == null || (targetCelestialBody.ParentGrandSuperStar != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGrandSuperStar = sourceCelestialBody.ParentGrandSuperStar;

                if (targetCelestialBody.ParentGrandSuperStarId == Guid.Empty || (targetCelestialBody.ParentGrandSuperStarId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGrandSuperStarId = sourceCelestialBody.ParentGrandSuperStarId;

                if (targetCelestialBody.ParentSuperStar == null || (targetCelestialBody.ParentSuperStar != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentSuperStar = sourceCelestialBody.ParentSuperStar;

                if (targetCelestialBody.ParentSuperStarId == Guid.Empty || (targetCelestialBody.ParentSuperStarId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentSuperStarId = sourceCelestialBody.ParentSuperStarId;

                if (targetCelestialBody.ParentStar == null || (targetCelestialBody.ParentStar != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentStar = sourceCelestialBody.ParentStar;

                if (targetCelestialBody.ParentStarId == Guid.Empty || (targetCelestialBody.ParentStarId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentStarId = sourceCelestialBody.ParentStarId;

                if (targetCelestialBody.ParentPlanet == null || (targetCelestialBody.ParentPlanet != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentPlanet = sourceCelestialBody.ParentPlanet;

                if (targetCelestialBody.ParentPlanetId == Guid.Empty || (targetCelestialBody.ParentPlanetId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentPlanetId = sourceCelestialBody.ParentPlanetId;

                if (targetCelestialBody.ParentMoon == null || (targetCelestialBody.ParentMoon != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentMoon = sourceCelestialBody.ParentMoon;

                if (targetCelestialBody.ParentMoonId == Guid.Empty || (targetCelestialBody.ParentMoonId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentMoonId = sourceCelestialBody.ParentMoonId;

                if (targetCelestialBody.ParentCelestialSpace == null || (targetCelestialBody.ParentCelestialSpace != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentCelestialSpace = sourceCelestialBody.ParentCelestialSpace;

                if (targetCelestialBody.ParentCelestialSpaceId == Guid.Empty || (targetCelestialBody.ParentCelestialSpaceId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentCelestialSpaceId = sourceCelestialBody.ParentCelestialSpaceId;

                if (targetCelestialBody.ParentCelestialBody == null || (targetCelestialBody.ParentCelestialBody != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentCelestialBody = sourceCelestialBody.ParentCelestialBody;

                if (targetCelestialBody.ParentCelestialBodyId == Guid.Empty || (targetCelestialBody.ParentCelestialBodyId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentCelestialBodyId = sourceCelestialBody.ParentCelestialBodyId;

                if (targetCelestialBody.ParentZome == null || (targetCelestialBody.ParentZome != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentZome = sourceCelestialBody.ParentZome;

                if (targetCelestialBody.ParentZomeId == Guid.Empty || (targetCelestialBody.ParentZomeId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentZomeId = sourceCelestialBody.ParentZomeId;

                if (targetCelestialBody.ParentHolon == null || (targetCelestialBody.ParentHolon != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentHolon = sourceCelestialBody.ParentHolon;

                if (targetCelestialBody.ParentHolonId == Guid.Empty || (targetCelestialBody.ParentHolonId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentHolonId = sourceCelestialBody.ParentHolonId;

                if (targetCelestialBody.ParentOmniverse == null || (targetCelestialBody.ParentOmniverse != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentOmniverse = sourceCelestialBody.ParentOmniverse;

                if (targetCelestialBody.ParentOmniverseId == Guid.Empty || (targetCelestialBody.ParentOmniverseId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentOmniverseId = sourceCelestialBody.ParentOmniverseId;

                if (targetCelestialBody.ParentMultiverse == null || (targetCelestialBody.ParentMultiverse != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentMultiverse = sourceCelestialBody.ParentMultiverse;

                if (targetCelestialBody.ParentMultiverseId == Guid.Empty || (targetCelestialBody.ParentMultiverseId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentMultiverseId = sourceCelestialBody.ParentMultiverseId;

                if (targetCelestialBody.ParentDimension == null || (targetCelestialBody.ParentDimension != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentDimension = sourceCelestialBody.ParentDimension;

                if (targetCelestialBody.ParentDimensionId == Guid.Empty || (targetCelestialBody.ParentDimensionId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentDimensionId = sourceCelestialBody.ParentDimensionId;

                if (targetCelestialBody.ParentUniverse == null || (targetCelestialBody.ParentUniverse != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentUniverse = sourceCelestialBody.ParentUniverse;

                if (targetCelestialBody.ParentUniverseId == Guid.Empty || (targetCelestialBody.ParentUniverseId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentUniverseId = sourceCelestialBody.ParentUniverseId;

                if (targetCelestialBody.ParentGalaxyCluster == null || (targetCelestialBody.ParentGalaxyCluster != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGalaxyCluster = sourceCelestialBody.ParentGalaxyCluster;

                if (targetCelestialBody.ParentGalaxyClusterId == Guid.Empty || (targetCelestialBody.ParentGalaxyClusterId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGalaxyClusterId = sourceCelestialBody.ParentGalaxyClusterId;

                if (targetCelestialBody.ParentGalaxy == null || (targetCelestialBody.ParentGalaxy != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGalaxy = sourceCelestialBody.ParentGalaxy;

                if (targetCelestialBody.ParentGalaxyId == Guid.Empty || (targetCelestialBody.ParentGalaxyId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentGalaxyId = sourceCelestialBody.ParentGalaxyId;

                if (targetCelestialBody.ParentSolarSystem == null || (targetCelestialBody.ParentSolarSystem != null && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentSolarSystem = sourceCelestialBody.ParentSolarSystem;

                if (targetCelestialBody.ParentSolarSystemId == Guid.Empty || (targetCelestialBody.ParentSolarSystemId != Guid.Empty && !onlyMapIfTargetIsNull))
                    targetCelestialBody.ParentSolarSystemId = sourceCelestialBody.ParentSolarSystemId;
            }

            return targetCelestialBody;
        }
        public static IEnumerable<T2> MapParentCelestialBodyProperties(IEnumerable<T1> sourceCelestialBodies)
        {
            if (sourceCelestialBodies != null)
            {
                List<T2> targetList = new List<T2>();

                foreach (T1 sourceCelestialBody in sourceCelestialBodies)
                    targetList.Add(MapParentCelestialBodyProperties(sourceCelestialBody));

                return targetList;
            }
            else
                return null;
        }
    }
}