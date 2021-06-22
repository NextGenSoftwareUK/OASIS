using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    // At the centre of each Solar System
    public class StarCore : CelestialBodyCore, IStarCore
    {
        public IStar Star { get; set; }

        public StarCore(IStar star) : base()
        {
            this.Star = star;
        }

        public StarCore(IStar star, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {
            this.Star = star;
        }

        public StarCore(IStar star, Guid id) : base(id)
        {
            this.Star = star;
        }

        public async Task<OASISResult<IPlanet>> AddPlanetAsync(IPlanet planet)
        {
            return OASISResultHolonToHolonHelper<IHolon, IPlanet>.CopyResult(
                await AddHolonToCollectionAsync(Star, planet, (List<IHolon>)Mapper<IPlanet, Holon>.MapBaseHolonProperties(
                    Star.ParentSolarSystem.Planets)), new OASISResult<IPlanet>());
        }

        //public async Task<OASISResult<IPlanet>> AddPlanetAsync(IPlanet planet)
        //{
        //    OASISResult<IPlanet> result = new OASISResult<IPlanet>();

        //    if (this.Star.ParentSolarSystem.Planets == null)
        //        this.Star.ParentSolarSystem.Planets = new List<IPlanet>();

        //    planet.ParentStarId = this.Star.Id;
        //    planet.ParentSolarSystemId = this.Star.ParentSolarSystemId;

        //    this.Star.ParentSolarSystem.Planets.Add(planet);

        //    /*
        //    OASISResult<ICelestialBody> starResult =  await this.Star.SaveAsync();

        //    if (starResult.IsError)
        //    {
        //        OASISResultHelper<ICelestialBody, IPlanet>.CopyResult(starResult, ref result);
        //        return result;
        //    }*/


        //    // TODO: This is more efficient than calling SaveAsync above, which will save all nested children, but we may need to do that?
        //    OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(this.Star.ParentSolarSystem.Planets);
        //    OASISResultCollectionToHolonHelper<IEnumerable<IHolon>, IPlanet>.CopyResult(holonsResult, ref result);

        //    // TODO: This will only work if the planet names are unique (which we want to enforce anyway!) - need to add this soon!
        //    if (!holonsResult.IsError)
        //    {
        //        IPlanet savedPlanet = this.Star.ParentSolarSystem.Planets.FirstOrDefault(x => x.Name == planet.Name);
        //        result.Result = savedPlanet;
        //    }

        //    return result;



        //    // Alternative way is to save the planet first and then then the star (as code below does):

        //    /*
        //    // If the planet has now been saved yet then save it now.
        //    if (planet.Id == Guid.Empty)
        //        result = await planet.Save();

        //    if (result.IsError)
        //        return new OASISResult<IPlanet>() { Result = (IPlanet)result.Result, ErrorMessage = result.ErrorMessage, IsError = result.IsError };
        //    else
        //    {
        //        planet = (IPlanet)result.Result;

        //        if (this.Star.Planets == null)
        //            this.Star.Planets = new List<IPlanet>();

        //        this.Star.Planets.Add(planet);
        //        result = await this.Star.Save(); //TODO: Even though this will save the planet and all its child zomes and holons, we want to save the planet as its own indiviudal holon (and not just be embedded as a child object). This also applies to saving each child zome/holon indivudally so we can get a unique id for each. But this depends on how each OASIS Provider implements this. This is where graphDB (neo4j) would be better?
        //        return new OASISResult<IPlanet>() { Result = planet, ErrorMessage = result.ErrorMessage, IsError = result.IsError };
        //    }*/
        //}

        public OASISResult<IPlanet> AddPlanet(IPlanet planet)
        {
            return AddPlanetAsync(planet).Result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForSolarSystemAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(Star.ParentSolarSystem.Planets, HolonType.Planet, refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IPlanet>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, Planet>.MapBaseHolonProperties(holonResult.Result);
            return result;
        }

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForSolarSystem(bool refresh = true)
        {
            return GetAllPlanetsForSolarSystemAsync(refresh).Result;
        }  
    }
}
