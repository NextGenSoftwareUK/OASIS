using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public class StarCore : CelestialBodyCore, IPlanetCore
    {
      //  private string _providerKey = "";
        private const string STAR_CORE_ZOME = "star_core_zome"; //Name of the core zome in rust hc.
        private const string STAR_HOLON_TYPE = "star_holon";
        private const string STAR_HOLONS_TYPE = "star_holons";

        private const string STAR_ADD_STAR = "star_add_star";
        private const string STAR_ADD_PLANET = "star_add_planet";
        private const string STAR_ADD_MOON = "star_add_moon";
        private const string STAR_GET_STARS = "star_get_stars";
        private const string STAR_GET_PLANETS = "star_get_planets";
        private const string STAR_GET_MOONS = "star_get_moons";

        public string ProviderKey { get; set; }

        public StarCore(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, STAR_CORE_ZOME, STAR_HOLON_TYPE, STAR_HOLONS_TYPE, providerKey)
        {
            ProviderKey = providerKey;
        }

        public StarCore(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, type, STAR_CORE_ZOME, STAR_HOLON_TYPE, STAR_HOLONS_TYPE, providerKey)
        {
            ProviderKey = providerKey;
        }

        public StarCore(HoloNETClientBase holoNETClient) : base(holoNETClient, STAR_CORE_ZOME, STAR_HOLON_TYPE, STAR_HOLONS_TYPE)
        {

        }

        public StarCore(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, type, STAR_CORE_ZOME, STAR_HOLON_TYPE, STAR_HOLONS_TYPE)
        {

        }
        public async Task<IPlanet> AddStarAsync(IStar planet)
        {
            return (IPlanet)await base.CallZomeFunctionAsync(STAR_ADD_STAR, planet);
        }

        public async Task<IPlanet> AddPlanetAsync(IPlanet planet)
        {
            return (IPlanet)await base.CallZomeFunctionAsync(STAR_ADD_PLANET, planet);
        }

        public async Task<IMoon> AddMoonAsync(IMoon moon)
        {
            return (IMoon)await base.CallZomeFunctionAsync(STAR_ADD_MOON, moon);
        }

        public async Task<List<IMoon>> GetStars()
        {
            if (string.IsNullOrEmpty(ProviderKey))
                throw new System.ArgumentException("ERROR: ProviderKey is null, please set this before calling this method.", "ProviderKey");

            return (List<IMoon>)await base.CallZomeFunctionAsync(STAR_GET_STARS, ProviderKey);
        }

        public async Task<List<IPlanet>> GetPlanets()
        {
            if (string.IsNullOrEmpty(ProviderKey))
                throw new System.ArgumentException("ERROR: ProviderKey is null, please set this before calling this method.", "ProviderKey");

            return (List<IPlanet>)await base.CallZomeFunctionAsync(STAR_GET_PLANETS, ProviderKey);
        }

        public async Task<List<IMoon>> GetMoons()
        {
            if (string.IsNullOrEmpty(ProviderKey))
                throw new System.ArgumentException("ERROR: ProviderKey is null, please set this before calling this method.", "ProviderKey");

            return (List<IMoon>)await base.CallZomeFunctionAsync(STAR_GET_MOONS, ProviderKey);
        }     
    }
}
