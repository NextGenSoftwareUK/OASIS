using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        private static HolonManager _instance = null;
        private OASISResult<IEnumerable<IHolon>> _allHolonsCache = null;

        //public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        public static HolonManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new HolonManager(ProviderManager.Instance.CurrentStorageProvider);

                return _instance;
            }
        }

        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public HolonManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

        public void ClearCache()
        {
            _allHolonsCache.Result = null;
            _allHolonsCache = null;
        }

        /// <summary>
        /// Send's a given holon from one provider to another. 
        /// This method is only really needed if auto-replication is disabled or there is a use case for sending from one provider to another.
        /// By default this will NOT auto-replicate to any other provider (set autoReplicate to true if you wish it to). This param overrides the global auto-replication setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="sourceProviderType"></param>
        /// <param name="destinationProviderType"></param>
        /// <param name="autoReplicate"></param>
        /// <returns></returns>
        public OASISResult<T> SendHolon<T>(Guid id, ProviderType sourceProviderType, ProviderType destinationProviderType, bool autoReplicate = false) where T : IHolon, new()
        {
            // TODO: Finish Implementing ASAP...
            // Needs to load the holon from the source provider and then save to the destination provider.


            return new OASISResult<T>();
        }

        public async Task<OASISResult<IHolon>> AddHolonToCollectionAsync(IHolon parentHolon, IHolon holon, List<IHolon> holons, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            if (holons == null)
                holons = new List<IHolon>();

            else if (holons.Any(x => x.Name == holon.Name))
            {
                result.IsError = true;
                result.Message = string.Concat("The name ", holon.Name, " is already taken, please choose another.");
                return result;
            }

            holon.IsNewHolon = true; //TODO: I am pretty sure every holon being added to a collection using this method will be a new one?

            if (holon.ParentOmniverseId == Guid.Empty)
            {
                holon.ParentOmniverseId = parentHolon.ParentOmniverseId;
                holon.ParentOmniverse = parentHolon.ParentOmniverse;
            }

            if (holon.ParentMultiverseId == Guid.Empty)
            {
                holon.ParentMultiverseId = parentHolon.ParentMultiverseId;
                holon.ParentMultiverse = parentHolon.ParentMultiverse;
            }

            if (holon.ParentUniverseId == Guid.Empty)
            {
                holon.ParentUniverseId = parentHolon.ParentUniverseId;
                holon.ParentUniverse = parentHolon.ParentUniverse;
            }

            if (holon.ParentDimensionId == Guid.Empty)
            {
                holon.ParentDimensionId = parentHolon.ParentDimensionId;
                holon.ParentDimension = parentHolon.ParentDimension;
            }

            if (holon.ParentGalaxyClusterId == Guid.Empty)
            {
                holon.ParentGalaxyClusterId = parentHolon.ParentGalaxyClusterId;
                holon.ParentGalaxyCluster = parentHolon.ParentGalaxyCluster;
            }

            if (holon.ParentGalaxyId == Guid.Empty)
            {
                holon.ParentGalaxyId = parentHolon.ParentGalaxyId;
                holon.ParentGalaxy = parentHolon.ParentGalaxy;
            }

            if (holon.ParentSolarSystemId == Guid.Empty)
            {
                holon.ParentSolarSystemId = parentHolon.ParentSolarSystemId;
                holon.ParentSolarSystem = parentHolon.ParentSolarSystem;
            }

            if (holon.ParentGreatGrandSuperStarId == Guid.Empty)
            {
                holon.ParentGreatGrandSuperStarId = parentHolon.ParentGreatGrandSuperStarId;
                holon.ParentGreatGrandSuperStar = parentHolon.ParentGreatGrandSuperStar;
            }

            if (holon.ParentGrandSuperStarId == Guid.Empty)
            {
                holon.ParentGrandSuperStarId = parentHolon.ParentGrandSuperStarId;
                holon.ParentGrandSuperStar = parentHolon.ParentGrandSuperStar;
            }

            if (holon.ParentSuperStarId == Guid.Empty)
            {
                holon.ParentSuperStarId = parentHolon.ParentSuperStarId;
                holon.ParentSuperStar = parentHolon.ParentSuperStar;
            }

            if (holon.ParentStarId == Guid.Empty)
            {
                holon.ParentStarId = parentHolon.ParentStarId;
                holon.ParentStar = parentHolon.ParentStar;
            }

            if (holon.ParentPlanetId == Guid.Empty)
            {
                holon.ParentPlanetId = parentHolon.ParentPlanetId;
                holon.ParentPlanet = parentHolon.ParentPlanet;
            }

            if (holon.ParentMoonId == Guid.Empty)
            {
                holon.ParentMoonId = parentHolon.ParentMoonId;
                holon.ParentMoon = parentHolon.ParentMoon;
            }

            if (holon.ParentCelestialSpaceId == Guid.Empty)
            {
                holon.ParentCelestialSpaceId = parentHolon.ParentCelestialSpaceId;
                holon.ParentCelestialSpace = parentHolon.ParentCelestialSpace;
            }

            if (holon.ParentCelestialBodyId == Guid.Empty)
            {
                holon.ParentCelestialBodyId = parentHolon.ParentCelestialBodyId;
                holon.ParentCelestialBody = parentHolon.ParentCelestialBody;
            }

            if (holon.ParentZomeId == Guid.Empty)
            {
                holon.ParentZomeId = parentHolon.ParentZomeId;
                holon.ParentZome = parentHolon.ParentZome;
            }

            if (holon.ParentHolonId == Guid.Empty)
            {
                holon.ParentHolonId = parentHolon.ParentHolonId;
                holon.ParentHolon = parentHolon.ParentHolon;
            }

            switch (parentHolon.HolonType)
            {
                case HolonType.GreatGrandSuperStar:
                    holon.ParentGreatGrandSuperStarId = parentHolon.Id;
                    holon.ParentGreatGrandSuperStar = (IGreatGrandSuperStar)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.GrandSuperStar:
                    holon.ParentGrandSuperStarId = parentHolon.Id;
                    holon.ParentGrandSuperStar = (IGrandSuperStar)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.SuperStar:
                    holon.ParentSuperStarId = parentHolon.Id;
                    holon.ParentSuperStar = (ISuperStar)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Multiverse:
                    holon.ParentMultiverseId = parentHolon.Id;
                    holon.ParentMultiverse = (IMultiverse)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Universe:
                    holon.ParentUniverseId = parentHolon.Id;
                    holon.ParentUniverse = (IUniverse)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Dimension:
                    holon.ParentDimensionId = parentHolon.Id;
                    holon.ParentDimension = (IDimension)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.GalaxyCluster:
                    holon.ParentGalaxyClusterId = parentHolon.Id;
                    holon.ParentGalaxyCluster = (IGalaxyCluster)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Galaxy:
                    holon.ParentGalaxyId = parentHolon.Id;
                    holon.ParentGalaxy = (IGalaxy)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.SolarSystem:
                    holon.ParentSolarSystemId = parentHolon.Id;
                    holon.ParentSolarSystem = (ISolarSystem)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Star:
                    holon.ParentStarId = parentHolon.Id;
                    holon.ParentStar = (IStar)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Planet:
                    holon.ParentPlanetId = parentHolon.Id;
                    holon.ParentPlanet = (IPlanet)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Moon:
                    holon.ParentMoonId = parentHolon.Id;
                    holon.ParentMoon = (IMoon)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Zome:
                    holon.ParentZomeId = parentHolon.Id;
                    holon.ParentZome = (IZome)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Holon:
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;
            }

            holons.Add(holon);

            //OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, false);
            //OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, false); //TODO: Temp to test new code...

            if (saveHolon)
            {
                result = await SaveHolonAsync(holon, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType); //TODO: WE ONLY NEED TO SAVE THE NEW HOLON, NO NEED TO RE-SAVE THE WHOLE COLLECTION AGAIN! ;-)
                result.IsSaved = true;
            }
            else
            {
                result.Message = "Holon was not saved due to saveHolon being set to false.";
                result.IsSaved = false;
                result.Result = holon;
            }

            return result;
        }
    }
} 