using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    //public class PlanetCore : PlanetBase, IPlanet
    public class PlanetCore : ZomeBase, IZome
    {
        private const string PLANET_CORE_ZOME = "planet_core_zome"; //Name of the core zome in rust hc.
        private const string PLANET_HOLON_TYPE = "planet_holon";
        private const string PLANET_HOLONS_TYPE = "planet_holons";
        private string _providerKey; //Anchor address in hc.

        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        public event HolonsLoaded OnHolonsLoaded;

        public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
        public event ZomesLoaded OnZomesLoaded;

        public PlanetCore(HoloNETClientBase holoNETClient, string providerKey) : base(holoNETClient, PLANET_CORE_ZOME)
        {
            _providerKey = providerKey;
        }

        public PlanetCore(string holochainConductorURI, HoloNETClientType type, string providerKey) : base(holochainConductorURI, PLANET_CORE_ZOME, type)
        {
            _providerKey = providerKey;
        }

        // Need to load list of zome names that belong to this Planet for PlanetBase to use...
        // Maybe load list of holons too?
        //public Task<List<IZome>> LoadZomes(string coreProviderKey)
        public List<IZome> LoadZomes()
        {
            //TODO: Check to see if the method awaits till the zomes(holons) are loaded before returning (if it doesn't need to refacoring to subscribe to events like LoadHolons does)
            List<IZome> zomes = new List<IZome>();
            foreach(IHolon holon in base.LoadHolonsAsync(PLANET_HOLONS_TYPE, _providerKey).Result)
                zomes.Add((IZome)holon);

            OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Zomes = zomes });

            //TODO: Make this return a Task so is awaitable...
            return zomes;
        }

        //public async Task<IHolon> SaveHolonAsync(IHolon savingHolon)
        public async Task<IHolon> SavePlanetAsync(IHolon savingHolon)
        {
            return await base.SaveHolonAsync(PLANET_HOLON_TYPE, savingHolon);
        }

        public async Task<IHolon> LoadPlanetAsync()
        {
            return await base.LoadHolonAsync(PLANET_HOLON_TYPE, _providerKey);
        }

        public async Task<List<IHolon>> LoadHolons()
        {
            return await base.LoadHolonsAsync(PLANET_HOLONS_TYPE, _providerKey);
        }

    }
}
