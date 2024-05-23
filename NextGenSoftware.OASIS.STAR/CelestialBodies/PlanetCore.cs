using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class PlanetCore : CelestialBodyCore<Planet>, IPlanetCore
    {
        public IPlanet Planet { get; set; }

        public PlanetCore(IPlanet planet) : base()
        {
            this.Planet = planet;
        }

        //public PlanetCore(IPlanet planet, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        //{
        //    this.Planet = planet;
        //}

        public PlanetCore(IPlanet planet, string providerKey, ProviderType providerType) : base(providerKey, providerType)
        {
            this.Planet = planet;
        }

        public PlanetCore(IPlanet planet, Guid id) : base(id)
        {
            this.Planet = planet;
        }

        //public async Task<OASISResult<IMoon>> AddMoonAsync(IMoon moon)
        //{
        //    return OASISResultHelper<IHolon, IMoon>.CopyResult(
        //        await AddHolonToCollectionAsync(Planet, moon, (List<IHolon>)Mapper<IMoon, Holon>.MapBaseHolonProperties(
        //            Planet.Moons)), new OASISResult<IMoon>());
        //}

        //public OASISResult<IMoon> AddMoon(IMoon moon)
        //{
        //    return AddMoonAsync(moon).Result; //TODO: Is this the best way of doing this?
        //}

        public async Task<OASISResult<IEnumerable<IMoon>>> GetMoonsAsync(bool refresh = true, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IMoon>> result = new OASISResult<IEnumerable<IMoon>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(Planet.Moons, HolonType.Moon, refresh, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);
            OASISResultHelper.CopyResult(holonResult, result);
            return result;
        }

        public OASISResult<IEnumerable<IMoon>> GetMoons(bool refresh = true, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return GetMoonsAsync(refresh, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType).Result; //TODO: Is this the best way of doing this?
        }

        public async Task<OASISResult<IEnumerable<IMoon>>> SaveMoonsAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IMoon>> result = new OASISResult<IEnumerable<IMoon>>();

            if (Planet.Moons == null)
                Planet.Moons = new List<IMoon>();

            if (Planet.Moons.Count == 0)
            {
                result.IsWarning = true;
                result.Message = "No moons found to save.";
                return result;
            }

            OASISResult<IEnumerable<IHolon>> holonResult = await GlobalHolonData.SaveHolonsAsync(Planet.Moons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
            OASISResultHelper.CopyResult(holonResult, result);
            return result;
        }

        public OASISResult<IEnumerable<IMoon>> SaveMoons(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            return SaveMoonsAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType).Result; //TODO: Is this the best way of doing this?
        }
    }
}
