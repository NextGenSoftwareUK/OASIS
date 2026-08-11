using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using System.Collections.Immutable;
using System.Drawing;
using System.Reflection.Metadata;
using System.Text.Json;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {

        private List<IHolon> BuildAllChildHolonsList(IHolon holon, List<IHolon> childHolons, bool recursive = true, int maxChildDepth = 0, int currentChildDepth = 0, bool continueOnError = true)
        {
            currentChildDepth++;

            if ((recursive && currentChildDepth >= maxChildDepth && maxChildDepth > 0) || (!recursive && currentChildDepth > 1))
                return childHolons;

            //foreach (IHolon child in holon.Children) 
            foreach (IHolon child in holon.AllChildren)
            { 
                if (child.Id == Guid.Empty)
                    child.Id = Guid.NewGuid();

                if (child.ParentHolonId == Guid.Empty)
                    child.ParentHolonId = holon.Id;

                if (!childHolons.Where(x => x.Id == child.Id).Any())
                    childHolons.Add(child);
            }

            //foreach (IHolon childHolon in holon.Children)
            foreach (IHolon childHolon in holon.AllChildren)
            {
                foreach (IHolon innerChildHolon in BuildAllChildHolonsList(childHolon, childHolons, recursive, maxChildDepth, currentChildDepth, continueOnError))
                {
                    if (innerChildHolon.Id == Guid.Empty)
                        innerChildHolon.Id = Guid.NewGuid();

                    if (innerChildHolon.ParentHolonId == Guid.Empty)
                        innerChildHolon.ParentHolonId = holon.Id;

                    if (!childHolons.Where(x => x.Id == innerChildHolon.Id).Any())
                        childHolons.Add(innerChildHolon);
                }
            }

            return childHolons;
        }

        private List<IHolon> BuildAllChildHolonsList(IEnumerable<IHolon> holons, List<IHolon> childHolons, bool recursive = true, int maxChildDepth = 0, int currentChildDepth = 0, bool continueOnError = true)
        {
            foreach (IHolon holon in holons)
                BuildAllChildHolonsList(holon, childHolons, recursive, maxChildDepth, currentChildDepth, continueOnError);

            return childHolons;
        }

        private string BuildChildHolonIdList(IHolon holon)
        {
            string ids = "";

            foreach (IHolon child in holon.Children) 
                ids = string.Concat(ids, ",", child.Id);

            if (ids.Length > 1)
                ids = ids.Substring(1);

            return ids;
        }

        private string BuildAllChildHolonIdList(List<IHolon> allchildHolons)
        {
            string ids = "";

            foreach (IHolon holon in allchildHolons)
                ids = string.Concat(ids, ",", holon.Id);

            if (ids.Length > 1)
                ids = ids.Substring(1);

            return ids;
        }

        public T RemoveCelesialBodies<T>(T holon) where T : IHolon
        {
            //if (holon.Id == Guid.Empty)
            //{
            //    holon.Id = Guid.NewGuid();
            //    holon.IsNewHolon = true;
            //}

            ICelestialBody celestialBody = holon as ICelestialBody;

            if (celestialBody != null)
            {
                if (celestialBody.CelestialBodyCore != null)
                {
                    _core[holon.Id] = celestialBody.CelestialBodyCore;
                    celestialBody.Children = celestialBody.CelestialBodyCore.AllChildren.ToList(); //We need to set the children holons before the core is removed during saving... The AllChildren property of CelestialBody will default to Children if the core is not found.
                }
                
                celestialBody.CelestialBodyCore = null;
            }

            if (holon.ParentOmniverse != null)
                _parentOmiverse[holon.Id] = holon.ParentOmniverse;

            if (holon.ParentDimension != null)
                _parentDimension[holon.Id] = holon.ParentDimension;

            if (holon.ParentMultiverse != null)
                _parentMultiverse[holon.Id] = holon.ParentMultiverse;

            if (holon.ParentUniverse != null)
                _parentUniverse[holon.Id] = holon.ParentUniverse;

            if (holon.ParentGalaxyCluster != null)
                _parentGalaxyCluster[holon.Id] = holon.ParentGalaxyCluster;

            if (holon.ParentGalaxy != null)
                _parentGalaxy[holon.Id] = holon.ParentGalaxy;

            if (holon.ParentSolarSystem != null)
                _parentSolarSystem[holon.Id] = holon.ParentSolarSystem;

            if (holon.ParentGreatGrandSuperStar != null)
                _parentGreatGrandSuperStar[holon.Id] = holon.ParentGreatGrandSuperStar;

            if (holon.ParentGrandSuperStar != null)
                _parentGrandSuperStar[holon.Id] = holon.ParentGrandSuperStar;

            if (holon.ParentSuperStar != null)
                _parentSuperStar[holon.Id] = holon.ParentSuperStar;

            if (holon.ParentStar != null)
                _parentStar[holon.Id] = holon.ParentStar;

            if (holon.ParentPlanet != null)
                _parentPlanet[holon.Id] = holon.ParentPlanet;

            if (holon.ParentMoon != null)
                _parentMoon[holon.Id] = holon.ParentMoon;

            if (holon.ParentCelestialSpace != null)
                _parentCelestialSpace[holon.Id] = holon.ParentCelestialSpace;

            if (holon.ParentCelestialBody != null)
                _parentCelestialBody[holon.Id] = holon.ParentCelestialBody;

            if (holon.ParentZome != null)
                _parentZome[holon.Id] = holon.ParentZome;

            if (holon.ParentHolon != null)
                _parentHolon[holon.Id] = holon.ParentHolon;

            holon.ParentOmniverse = null;
            holon.ParentDimension = null;
            holon.ParentMultiverse = null;
            holon.ParentUniverse = null;
            holon.ParentGalaxyCluster = null;
            holon.ParentGalaxy = null;
            holon.ParentSolarSystem = null;
            holon.ParentGreatGrandSuperStar = null;
            holon.ParentGrandSuperStar = null;
            holon.ParentSuperStar = null;
            holon.ParentStar = null;
            holon.ParentPlanet = null;
            holon.ParentMoon = null;
            holon.ParentCelestialBody = null;
            holon.ParentCelestialSpace = null;
            holon.ParentZome = null;
            holon.ParentHolon = null;

            return holon;
        }

        public IEnumerable<IHolon> RemoveCelesialBodies(IEnumerable<IHolon> holons)
        {
            List<IHolon> holonsList = holons.ToList();

            for (int i = 0; i < holonsList.Count(); i++)
                holonsList[i] = RemoveCelesialBodies(holonsList[i]);

            return holonsList;
        }

        public IEnumerable<T> RemoveCelesialBodies<T>(IEnumerable<T> holons) where T : IHolon
        {
            List<T> holonsList = holons.ToList();

            for (int i = 0; i < holonsList.Count(); i++)
                holonsList[i] = (T)RemoveCelesialBodies(holonsList[i]);

            return holonsList;
        }

        //private IHolon RestoreCelesialBodies(IHolon originalHolon)
        public T RestoreCelesialBodies<T>(T originalHolon) where T : IHolon
        {
            if (originalHolon != null)
            {
                //dynamic paramsObject = new ExpandoObject();
                originalHolon.IsNewHolon = false;

                if (_parentOmiverse.ContainsKey(originalHolon.Id))
                    originalHolon.ParentOmniverse = _parentOmiverse[originalHolon.Id];

                if (_parentDimension.ContainsKey(originalHolon.Id))
                    originalHolon.ParentDimension = _parentDimension[originalHolon.Id];

                if (_parentMultiverse.ContainsKey(originalHolon.Id))
                    originalHolon.ParentMultiverse = _parentMultiverse[originalHolon.Id];

                if (_parentUniverse.ContainsKey(originalHolon.Id))
                    originalHolon.ParentUniverse = _parentUniverse[originalHolon.Id];

                if (_parentGalaxyCluster.ContainsKey(originalHolon.Id))
                    originalHolon.ParentGalaxyCluster = _parentGalaxyCluster[originalHolon.Id];

                if (_parentGalaxy.ContainsKey(originalHolon.Id))
                    originalHolon.ParentGalaxy = _parentGalaxy[originalHolon.Id];

                if (_parentSolarSystem.ContainsKey(originalHolon.Id))
                    originalHolon.ParentSolarSystem = _parentSolarSystem[originalHolon.Id];

                if (_parentGreatGrandSuperStar.ContainsKey(originalHolon.Id))
                    originalHolon.ParentGreatGrandSuperStar = _parentGreatGrandSuperStar[originalHolon.Id];

                if (_parentGrandSuperStar.ContainsKey(originalHolon.Id))
                    originalHolon.ParentGrandSuperStar = _parentGrandSuperStar[originalHolon.Id];

                if (_parentSuperStar.ContainsKey(originalHolon.Id))
                    originalHolon.ParentSuperStar = _parentSuperStar[originalHolon.Id];

                if (_parentStar.ContainsKey(originalHolon.Id))
                    originalHolon.ParentStar = _parentStar[originalHolon.Id];

                if (_parentPlanet.ContainsKey(originalHolon.Id))
                    originalHolon.ParentPlanet = _parentPlanet[originalHolon.Id];

                if (_parentMoon.ContainsKey(originalHolon.Id))
                    originalHolon.ParentMoon = _parentMoon[originalHolon.Id];

                if (_parentCelestialSpace.ContainsKey(originalHolon.Id))
                    originalHolon.ParentCelestialSpace = _parentCelestialSpace[originalHolon.Id];

                if (_parentCelestialBody.ContainsKey(originalHolon.Id))
                    originalHolon.ParentCelestialBody = _parentCelestialBody[originalHolon.Id];

                if (_parentZome.ContainsKey(originalHolon.Id))
                    originalHolon.ParentZome = _parentZome[originalHolon.Id];

                if (_parentHolon.ContainsKey(originalHolon.Id))
                    originalHolon.ParentHolon = _parentHolon[originalHolon.Id];

                _parentOmiverse.Remove(originalHolon.Id);
                _parentDimension.Remove(originalHolon.Id);
                _parentMultiverse.Remove(originalHolon.Id);
                _parentUniverse.Remove(originalHolon.Id);
                _parentGalaxyCluster.Remove(originalHolon.Id);
                _parentGalaxy.Remove(originalHolon.Id);
                _parentSolarSystem.Remove(originalHolon.Id);
                _parentGreatGrandSuperStar.Remove(originalHolon.Id);
                _parentGrandSuperStar.Remove(originalHolon.Id);
                _parentSuperStar.Remove(originalHolon.Id);
                _parentStar.Remove(originalHolon.Id);
                _parentPlanet.Remove(originalHolon.Id);
                _parentMoon.Remove(originalHolon.Id);
                _parentCelestialSpace.Remove(originalHolon.Id);
                _parentCelestialBody.Remove(originalHolon.Id);
                _parentZome.Remove(originalHolon.Id);
                _parentHolon.Remove(originalHolon.Id);

                ICelestialBody celestialBody = originalHolon as ICelestialBody;

                if (celestialBody != null)
                {
                    if (_core.ContainsKey(originalHolon.Id))
                        celestialBody.CelestialBodyCore = _core[originalHolon.Id];

                    _core.Remove(originalHolon.Id);
                    return (T)celestialBody;
                }

                //switch (originalHolon.HolonType)
                //{
                //    case HolonType.GreatGrandSuperStar:
                //        {
                //            if (_core.ContainsKey(originalHolon.Id))
                //            {
                //                GreatGrandSuperStar celestialBody = JsonConvert.DeserializeObject<GreatGrandSuperStar>(JsonConvert.SerializeObject(originalHolon));
                //                celestialBody.CelestialBodyCore = _core[originalHolon.Id];
                //                originalHolon = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(celestialBody));
                //            }
                //        }
                //        break;
                //}

                //if (originalHolon.HolonType == HolonType.GreatGrandSuperStar ||
                //    originalHolon.HolonType == HolonType.GrandSuperStar ||
                //    originalHolon.HolonType == HolonType.SuperStar ||
                //    originalHolon.HolonType == HolonType.Star ||
                //    originalHolon.HolonType == HolonType.Planet ||
                //    originalHolon.HolonType == HolonType.Moon)
                //{
                //    //celestialBody = originalHolon as ICelestialBody;

                //    // celestialBody = (ICelestialBody)originalHolon;

                //    if (_core.ContainsKey(originalHolon.Id))
                //    {
                //        PlayerData player = JsonConvert.DeserializeObject<PlayerData>(JsonConvert.SerializeObject(item));

                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "CelestialBodyCore", _core[originalHolon.Id]);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "Id", originalHolon.Id);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentHolonId", originalHolon.ParentHolonId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ProviderUniqueStorageKey", originalHolon.ProviderUniqueStorageKey);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "Name", originalHolon.Name);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "Description", originalHolon.Description);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "HolonType", originalHolon.HolonType);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "Children", originalHolon.Children);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "CreatedByAvatar", originalHolon.CreatedByAvatar);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "CreatedByAvatarId", originalHolon.CreatedByAvatarId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "CreatedDate", originalHolon.Name);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ModifiedByAvatar", originalHolon.ModifiedByAvatar);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ModifiedByAvatarId", originalHolon.ModifiedByAvatarId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "DeletedDate", originalHolon.DeletedDate);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "Version", originalHolon.Version);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "IsActive", originalHolon.IsActive);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "IsChanged", originalHolon.IsChanged);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "IsNewHolon", originalHolon.IsNewHolon);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "MetaData", originalHolon.MetaData);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ProviderMetaData", originalHolon.ProviderMetaData);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "Original", originalHolon.Original);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGreatGrandSuperStar", originalHolon.ParentGreatGrandSuperStar);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGreatGrandSuperStarId", originalHolon.ParentGreatGrandSuperStarId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGrandSuperStar", originalHolon.ParentGrandSuperStar);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGrandSuperStarId", originalHolon.ParentGrandSuperStarId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentStar", originalHolon.ParentStar);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentStarId", originalHolon.ParentStarId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentPlanet", originalHolon.ParentPlanet);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentPlanetId", originalHolon.ParentPlanetId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentMoon", originalHolon.ParentGreatGrandSuperStarId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentMoonId", originalHolon.ParentMoonId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentCelestialSpace", originalHolon.ParentCelestialSpace);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentCelestialSpaceId", originalHolon.ParentCelestialSpaceId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentCelestialBody", originalHolon.ParentCelestialBody);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentCelestialBodyId", originalHolon.ParentCelestialBodyId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentZome", originalHolon.ParentZome);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentZomeId", originalHolon.ParentZomeId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentHolon", originalHolon.ParentHolon);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentHolonId", originalHolon.ParentHolonId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentOmniverse", originalHolon.ParentOmniverse);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentOmniverseId", originalHolon.ParentOmniverseId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentMultiverse", originalHolon.ParentMultiverse);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentMultiverseId", originalHolon.ParentMultiverseId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentDimension", originalHolon.ParentDimension);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentDimensionId", originalHolon.ParentDimensionId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentUniverse", originalHolon.ParentUniverse);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentUniverseId", originalHolon.ParentUniverseId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGalaxyCluster", originalHolon.ParentGalaxyCluster);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGalaxyClusterId", originalHolon.ParentGalaxyClusterId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGalaxy", originalHolon.ParentGalaxy);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGalaxyId", originalHolon.ParentGalaxyId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentSolarSystem", originalHolon.ParentSolarSystem);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentSolarSystemId", originalHolon.ParentSolarSystemId);

                //        //return (T)paramsObject;
                //    }
            }

            return originalHolon;
        }

        public IEnumerable<IHolon> RestoreCelesialBodies(IEnumerable<IHolon> holons)
        {
            List<IHolon> restoredHolons = new List<IHolon>();

            if (holons != null)
            {
                foreach (IHolon holon in holons)
                    restoredHolons.Add(RestoreCelesialBodies(holon));
            }
            //else
            //    LoggingManager.Log("The list of holons to restore celestial bodies for is null.", LogType.Warning);

            return restoredHolons;
        }

        public IEnumerable<T> RestoreCelesialBodies<T>(IEnumerable<T> holons) where T : IHolon
        {
            List<T> restoredHolons = new List<T>();

            if (holons != null)
            {
                foreach (T holon in holons)
                    restoredHolons.Add(RestoreCelesialBodies(holon));
            }
            //else
            //    LoggingManager.Log("The list of holons to restore celestial bodies for is null.", LogType.Warning);

            return restoredHolons;
        }
    }
}
