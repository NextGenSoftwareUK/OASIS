using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.STAR.CelestialContainers;

namespace NextGenSoftware.OASIS.STAR
{
    public class SuperStarCore : CelestialBodyCore, ISuperStarCore // TODO: Currently cannot inherit from StarCore because SuperStar is static (may change soon?)
    {
        public ISuperStar SuperStar { get; set; }

        public SuperStarCore(Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {

        }

        public SuperStarCore(Guid id) : base(id)
        {

        }

        public async Task<OASISResult<ISolarSystem>> AddSolarSystemAsync(ISolarSystem solarSystem)
        {
            return OASISResultHolonToHolonHelper<IHolon, ISolarSystem>.CopyResult(
                await AddHolonToCollectionAsync(SuperStar, solarSystem, (List<IHolon>)Mapper<ISolarSystem, Holon>.MapBaseHolonProperties(
                    SuperStar.ParentGalaxy.SolarSystems)), new OASISResult<ISolarSystem>());
        }

        public OASISResult<ISolarSystem> AddSolarSystem(ISolarSystem solarSystem)
        {
            return AddSolarSystemAsync(solarSystem).Result;
        }

        public async Task<OASISResult<IStar>> AddStarAsync(IStar star)
        {
            return OASISResultHolonToHolonHelper<IHolon, IStar>.CopyResult(
                await AddHolonToCollectionAsync(SuperStar, star, (List<IHolon>)Mapper<IStar, Holon>.MapBaseHolonProperties(
                    SuperStar.ParentGalaxy.Stars)), new OASISResult<IStar>());
        }

        public OASISResult<IStar> AddStar(IStar star)
        {
            return AddStarAsync(star).Result;
        }

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(SuperStar.ParentGalaxy.SolarSystems, HolonType.SoloarSystem, refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<ISolarSystem>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, SolarSystem>.MapBaseHolonProperties(holonResult.Result);
            return result;
        }

        public OASISResult<IEnumerable<ISolarSystem>> GetSolarSystems(bool refresh = true)
        {
            return GetSolarSystemsAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsAsync(bool refresh = true)
        {
            //TODO: See if can make this even more efficient! ;-)
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(SuperStar.ParentGalaxy.Stars, HolonType.Star, refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IStar>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, Star>.MapBaseHolonProperties(holonResult.Result);
            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetStars(bool refresh = true)
        {
            return GetStarsAsync(refresh).Result;
        }



        // DONT NEED BECAUSE INNERSTAR CONTAINS THIS.
        //public async Task<List<IPlanet>> GetPlanets()
        //{
        //    if (string.IsNullOrEmpty(ProviderKey))
        //        throw new System.ArgumentException("ERROR: ProviderKey is null, please set this before calling this method.", "ProviderKey");

        //    return (List<IPlanet>)await base.LoadHolonsAsync(ProviderKey, API.Core.HolonType.Planet);
        //    //return (List<IPlanet>)await base.CallZomeFunctionAsync(STAR_GET_PLANETS, ProviderKey);
        //}

        //TODO: I think we need to also add back in these Moon functions because Star can also create Moons...

        //public async Task<IMoon> AddMoonAsync(IMoon moon)
        //{
        //    return (IMoon)await base.CallZomeFunctionAsync(STAR_ADD_MOON, moon);
        //}

        //public async Task<List<IMoon>> GetMoons()
        //{
        //    if (string.IsNullOrEmpty(ProviderKey))
        //        throw new System.ArgumentException("ERROR: ProviderKey is null, please set this before calling this method.", "ProviderKey");

        //    return (List<IMoon>)await base.CallZomeFunctionAsync(STAR_GET_MOONS, ProviderKey);
        //}     
    }
}
