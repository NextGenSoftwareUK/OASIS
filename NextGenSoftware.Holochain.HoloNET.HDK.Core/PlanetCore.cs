using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    //public class PlanetCore : PlanetBase, IPlanet
    public class PlanetCore : ZomeBase, IZome
    {
        private const string PLANET_CORE = "planet_core"; //Name of the core zome in rust hc.
        private string _coreProviderKey; //Anchor address in hc.

        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        public event HolonsLoaded OnHolonsLoaded;

        public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
        public event ZomesLoaded OnZomesLoaded;

        public PlanetCore(HoloNETClientBase holoNETClient, string coreProviderKey) : base(holoNETClient, PLANET_CORE)
        {
            _coreProviderKey = coreProviderKey;
        }

        public PlanetCore(string holochainConductorURI, HoloNETClientType type, string coreProviderKey) : base(holochainConductorURI, PLANET_CORE, type)
        {
            _coreProviderKey = coreProviderKey;
        }

        // Need to load list of zome names that belong to this Planet for PlanetBase to use...
        // Maybe load list of holons too?
        //public Task<List<IZome>> LoadZomes(string coreProviderKey)
        public List<IZome> LoadZomes()
        {
            //TODO: Check to see if the method awaits till the zomes(holons) are loaded before returning (if it doesn't need to refacoring to subscribe to events like LoadHolons does)
            List<IZome> zomes = new List<IZome>();
            foreach(IHolon holon in base.LoadHolonsAsync("zome", _coreProviderKey).Result)
                zomes.Add((IZome)holon);

            OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Zomes = zomes });

            //TODO: Make this return a Task so is awaitable...
            return zomes;
        }

        public Task<List<IHolon>> LoadHolons()
        {
            return base.LoadHolonsAsync("holon", _coreProviderKey);
        }
    }
}
