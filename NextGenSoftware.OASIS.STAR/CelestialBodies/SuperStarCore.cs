using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

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

        public OASISResult<ISolarSystem> AddSolarSystem(ISolarSystem solarSystem)
        {
            
        }

        public Task<OASISResult<ISolarSystem>> AddSolarSystemAsync(ISolarSystem solarSystem)
        {
            
        }

        
        public async Task<OASISResult<IStar>> AddStarAsync(IStar star)
        {
            SuperStar.ParentGalaxy.Stars.Add((Star)star);
            return (IStar)await base.SaveHolonAsync((Star)star);
        }

        public OASISResult<IStar> AddStar(IStar star)
        {
            return AddStarAsync(star).Result;
        }

        public OASISResult<IEnumerable<ISolarSystem>> GetSolarSystems(bool refresh = true)
        {
            return GetSolarSystemsAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetSolarSystemsAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();

            if (this.SuperStar.ParentGalaxy.SolarSystems == null || refresh)
            {
                OASISResult<IEnumerable<IHolon>> holonsResult = null;

                if (this.Id != Guid.Empty)
                    holonsResult = await base.LoadHolonsForParentAsync(Id, HolonType.SoloarSystem);

                else if (this.ProviderKey != null)
                    holonsResult = await base.LoadHolonsForParentAsync(ProviderKey, HolonType.SoloarSystem);
                else
                {
                    result.IsError = true;
                    result.Message = "Both Id and ProviderKey are null, one of these need to be set before calling this method.";
                }

                if (!result.IsError)
                {
                    OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<ISolarSystem>>.CopyResult(holonsResult, ref result);
                    result.Result = (IEnumerable<ISolarSystem>)holonsResult.Result; //TODO: Not sure if this cast will work? Probably not... Need to map...
                    //result.Result = Mapper<IHolon, SolarSystem>.MapBaseHolonProperties(holonsResult.Result); //TODO: Use this if cast does not work... ;-)
                    this.SuperStar.ParentGalaxy.SolarSystems = result.Result.ToList();
                }
            }
            else
            {
                result.Message = "Refresh not required";
                result.Result = this.SuperStar.ParentGalaxy.SolarSystems;
            }

            return result;
        }

        // DONT NEED BECAUSE INNERSTAR CONTAINS THIS.
        //public async Task<IPlanet> AddPlanetAsync(IPlanet planet)
        //{
        //    SuperStar.Planets.Add((Planet)planet);
        //    return (IPlanet)await base.SaveHolonAsync(planet);
        //}

        
        public OASISResult<IEnumerable<IStar>> GetStars(bool refresh = true)
        {
            return GetStarsAsync(refresh).Result;
        }


        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();

            if (this.SuperStar.ParentGalaxy.Stars == null || refresh)
            {
                OASISResult<IEnumerable<IHolon>> holonsResult = null;

                if (this.Id != Guid.Empty)
                    holonsResult = await base.LoadHolonsForParentAsync(Id, HolonType.Star);

                else if (this.ProviderKey != null)
                    holonsResult = await base.LoadHolonsForParentAsync(ProviderKey, HolonType.Star);
                else
                {
                    result.IsError = true;
                    result.Message = "Both Id and ProviderKey are null, one of these need to be set before calling this method.";
                }

                if (!result.IsError)
                {
                    OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IStar>>.CopyResult(holonsResult, ref result);
                    result.Result = (IEnumerable<IStar>)holonsResult.Result; //TODO: Not sure if this cast will work? Probably not... Need to map...
                    //result.Result = Mapper<IHolon, Star>.MapBaseHolonProperties(holonsResult.Result); //TODO: Use this if cast does not work... ;-)
                    this.SuperStar.ParentGalaxy.Stars = result.Result.ToList();
                }
            }
            else
            {
                result.Message = "Refresh not required";
                result.Result = this.SuperStar.ParentGalaxy.Stars;
            }

            return result;
        }

        //TODO: TEST METHOD, REMOVE AFTER...
        /*
        public async Task<OASISResult<IStar>> GetStar()
        {
            OASISResult<IStar> result = new OASISResult<IStar>();
            OASISResult<IHolon> holonResult = await base.LoadHolonAsync(ProviderKey);

            //TODO: Want to merge CopyResult/MapBaseHolonProperties tomorrow... :)
            OASISResultHelper<IHolon, Star>.CopyResult(holonResult, ref result);

            return result;
        }*/

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
