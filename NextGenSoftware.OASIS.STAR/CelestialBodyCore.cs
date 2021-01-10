
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.STAR
{
    public abstract class CelestialBodyCore : ZomeBase, ICelestialBodyCore
    {
        private const string ZOMES_LOAD_ALL = "_zomes_loadall";
        private const string ZOMES_ADD = "_zomes_add";
        private const string ZOMES_REMOVE = "_zomes_remove";

       
        //private const string ZOMES_ADD = "planet_holons_add";  //Holons Collection on CelestialBody only loads all holons in all zomes belonging to that body (like a shortcut). So a body does not store holons directly, only zomes (containing holons).
        //private const string ZOMES_REMOVE = "planet_holons_remove";
        //private const string HOLONS_LOAD_ALL = "_holons_loadall";



        public string ProviderKey { get; set; } //Anchor address in hc.

        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        public event HolonsLoaded OnHolonsLoaded;

        public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
        public event ZomesLoaded OnZomesLoaded;

        public List<Zome> Zomes { get; set; }

        public string CoreZomeName { get; set; }
        public string CoreHolonType { get; set; }

        public CelestialBodyCore(string coreZomeName, string coreHolonBase, string providerKey) : base(coreZomeName)
        {
            this.ProviderKey = providerKey;
            this.CoreZomeName = coreZomeName;
            this.CoreHolonType = coreHolonBase;
        }

        public CelestialBodyCore(string coreZomeName, string coreHolonBase) : base(coreZomeName)
        {
            this.CoreZomeName = coreZomeName;
            this.CoreHolonType = coreHolonBase;
        }

        /*
        public CelestialBodyCore(HoloNETClientBase holoNETClient, string coreZomeName, string coreHolonBase, string providerKey) : base(holoNETClient, coreZomeName)
        {
            this.ProviderKey = providerKey;
            this.CoreZomeName = coreZomeName;
            this.CoreHolonType = coreHolonBase;
            //this.HolonType = holonType;
            //this.HolonsType = holonsType;
        }


        public CelestialBodyCore(string holochainConductorURI, HoloNETClientType type, string coreZomeName, string coreHolonBase, string providerKey) : base(holochainConductorURI, coreZomeName, type)
        {
            this.ProviderKey = providerKey;
            this.CoreZomeName = coreZomeName;
            this.CoreHolonType = coreHolonBase;
            //this.HolonType = holonType;
            //this.HolonsType = holonsType;
        }

        public CelestialBodyCore(HoloNETClientBase holoNETClient, string coreZomeName, string coreHolonBase) : base(holoNETClient, coreZomeName)
        {
            this.CoreZomeName = coreZomeName;
            this.CoreHolonType = coreHolonBase;
            //this.HolonType = holonType;
            //this.HolonsType = holonsType;
        }

        public CelestialBodyCore(string holochainConductorURI, HoloNETClientType type, string coreZomeName, string coreHolonBase) : base(holochainConductorURI, coreZomeName, type)
        {
            this.CoreZomeName = coreZomeName;
            this.CoreHolonType = coreHolonBase;
            //this.HolonType = holonType;
            //this.HolonsType = holonsType;
        }
        */

        // Need to load list of zome names that belong to this Planet for PlanetBase to use...
        // Maybe load list of holons too?
        public List<IZome> LoadZomes()
        {
            if (string.IsNullOrEmpty(ProviderKey))
                throw new ArgumentNullException("ProviderKey", "The ProviderKey must be set before this method can be called.");

            //TODO: Check to see if the method awaits till the zomes(holons) are loaded before returning (if it doesn't need to refacoring to subscribe to events like LoadHolons does)
            List<IZome> zomes = new List<IZome>();
            List<OASIS.API.Core.IZome> coreZomes = new List<OASIS.API.Core.IZome>();

            //TODO: Come back to this, must be better way of doing this?
            foreach (IHolon holon in base.LoadHolonsAsync(string.Concat(this.CoreHolonType, ZOMES_LOAD_ALL), ProviderKey).Result)
            {
                zomes.Add((IZome)holon);
                coreZomes.Add((OASIS.API.Core.IZome)holon);
            }

            OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Zomes = coreZomes });

            //TODO: Make this return a Task so is awaitable...
            return zomes;
        }

        public async Task<IHolon> AddZome(IZome zome)
        {
            return await base.SaveHolonAsync(string.Concat(this.CoreHolonType, ZOMES_ADD), zome);
        }

        public async Task<IHolon> RemoveZome(IZome zome)
        {
            //TODO: Finish
            return await base.SaveHolonAsync(string.Concat(this.CoreHolonType, ZOMES_REMOVE), zome);
        }

        public async Task<IHolon> SaveCelestialBodyAsync(IHolon savingHolon)
        {
            return await base.SaveHolonAsync(savingHolon);
        }

        public async Task<IHolon> LoadCelestialBodyAsync()
        {
            if (string.IsNullOrEmpty(ProviderKey))
                throw new ArgumentNullException("ProviderKey", "The ProviderKey must be set before this method can be called.");

            return await base.LoadHolonAsync(ProviderKey);
        }
    }
}
