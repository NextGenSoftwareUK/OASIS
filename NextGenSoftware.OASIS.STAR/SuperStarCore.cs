using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR
{
    public class SuperStarCore : CelestialBodyCore, ISuperStarCore // TODO: Currently cannot inherit from StarCore because SuperStar is static (may change soon?)
    {
        public SuperStarCore(Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {

        }

        public SuperStarCore(Guid id) : base(id)
        {

        }

        public async Task<OASISResult<IStar>> AddStarAsync(IStar star)
        {
            //TODO: Do we want to add the new star to the main star? Can a Star have a collection of Stars?
            // Yes, I think we do, but that means if we can create Stars, then the first main star needs to be either SuperStar, BlueStar or GreatCentralSun! ;-)
            // Then SuperStar/BlueStar/GreatCentralSun is the only object that can contain a collection of other stars. Normal Stars only contain collections of planets.
            // I feel GreatCentralSun would be best because it then accurately models the Galaxy/Universe! ;-)

            //TODO: SO.... tomorrow need to rename the existing Star to GreatCentralSun and then create a normal Star...
            // Think StarBody can be renamed to Star and Star renamed to GreatCentralSun...

            SuperStar.Stars.Add((Star)star);
            return (IStar)await base.SaveHolonAsync((Star)star);
        }

        // DONT NEED BECAUSE INNERSTAR CONTAINS THIS.
        //public async Task<IPlanet> AddPlanetAsync(IPlanet planet)
        //{
        //    SuperStar.Planets.Add((Planet)planet);
        //    return (IPlanet)await base.SaveHolonAsync(planet);
        //}



        public async Task<OASISResult<IEnumerable<IStar>>> GetStars()
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<IHolon>> holonsResult = await base.LoadHolonsForParentAsync(ProviderKey, HolonType.Star);
            
            //TODO: Want to merge CopyResult/MapBaseHolonProperties tomorrow... :)
            OASISResultHelper<IEnumerable<IHolon>, IEnumerable<IStar>>.CopyResult(holonsResult, ref result);

            if (!result.IsError)
                result.Result = Mapper<IHolon, Star>.MapBaseHolonProperties(holonsResult.Result);

            return result;
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
