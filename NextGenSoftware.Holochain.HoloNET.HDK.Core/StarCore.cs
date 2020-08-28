using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public class StarCore : ZomeBase, IZome
    {
        private const string STAR_CORE_ZOME = "star_core_zome"; //Name of the core zome in rust hc.
        private const string STAR_ADD_PLANET = "star_add_planet";
        private const string STAR_ADD_MOON = "star_add_moon";
        private const string STAR_GET_PLANETS = "star_get_planets";
        private const string STAR_GET_MOONS = "star_get_moons";

        //private const string STAR_HOLON_TYPE = "star_holon";
        //private const string STAR_HOLONS_TYPE = "star_holons";
        private string _providerKey; //Anchor address in hc.

        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        public event HolonsLoaded OnHolonsLoaded;

        public StarCore(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, STAR_CORE_ZOME)
        {
            _providerKey = providerKey;
        }

        public StarCore(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, STAR_CORE_ZOME, type)
        {
            _providerKey = providerKey;
        }

        public async Task<IPlanet> AddPlanetAsync(IPlanet planet)
        {
            return (IPlanet)await base.CallZomeFunctionAsync(STAR_ADD_PLANET, planet);
        }

        public async Task<List<IPlanet>> GetPlanets()
        {
            return (List<IPlanet>)await base.CallZomeFunctionAsync(STAR_GET_PLANETS, _providerKey);
        }

        public async Task<IMoon> AddMoonAsync(IMoon moon)
        {
            return (IMoon)await base.CallZomeFunctionAsync(STAR_ADD_PLANET, moon);
        }

        public async Task<List<IMoon>> GetMoons()
        {
            return (List<IMoon>)await base.CallZomeFunctionAsync(STAR_GET_MOONS, _providerKey);
        }

        /*
        public async Task<ICelestialBodyBase> AddPlanetAsync(ICelestialBodyBase body)
        {
            switch (body.HolonType)
            {
                case HolonType.Planet:
                    return (ICelestialBodyBase)await base.CallZomeFunctionAsync(STAR_ADD_PLANET, body);

                case HolonType.Moon:
                    return (ICelestialBodyBase)await base.CallZomeFunctionAsync(STAR_ADD_MOON, body);
            }
        }

        public async Task<List<ICelestialBodyBase>> GetPlanets(HolonType type)
        {
            switch (type)
            {
                case HolonType.Planet:
                    return (List<ICelestialBodyBase>)await base.CallZomeFunctionAsync(STAR_GET_PLANETS, _providerKey);

                case HolonType.Moon:
                    return (ICelestialBodyBase)await base.CallZomeFunctionAsync(STAR_GET_MOONS, _providerKey);
            }
        }*/

        //public async Task<IHolon> SaveHolonAsync(IHolon savingHolon)
        //{
        //    return await base.SaveHolonAsync(STAR_HOLON_TYPE, savingHolon);
        //}

        //public async Task<List<IHolon>> LoadHolons()
        //{
        //    return await base.LoadHolonsAsync(STAR_HOLONS_TYPE, _providerKey);
        //}
    }
}
