using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public abstract class CelestialBodyCore : ZomeBase, ICelestialBodyCore
    {
        public string ProviderKey { get; set; } //Anchor address in hc.

        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        public event HolonsLoaded OnHolonsLoaded;

        public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
        public event ZomesLoaded OnZomesLoaded;

        public string CoreZome { get; set; }
        public string HolonType { get; set; }
        public string HolonsType { get; set; }

        public CelestialBodyCore(HoloNETClientBase holoNETClient, string coreZome, string holonType, string holonsType, string providerKey) : base(holoNETClient, coreZome)
        {
            this.ProviderKey = providerKey;
            this.CoreZome = coreZome;
            this.HolonType = holonType;
            this.HolonsType = holonsType;
        }

        public CelestialBodyCore(string holochainConductorURI, HoloNETClientType type, string coreZome, string holonType, string holonsType, string providerKey) : base(holochainConductorURI, coreZome, type)
        {
            this.ProviderKey = providerKey;
            this.CoreZome = coreZome;
            this.HolonType = holonType;
            this.HolonsType = holonsType;
        }

        public CelestialBodyCore(HoloNETClientBase holoNETClient, string coreZome, string holonType, string holonsType) : base(holoNETClient, coreZome)
        {
            this.CoreZome = coreZome;
            this.HolonType = holonType;
            this.HolonsType = holonsType;
        }

        public CelestialBodyCore(string holochainConductorURI, HoloNETClientType type, string coreZome, string holonType, string holonsType) : base(holochainConductorURI, coreZome, type)
        {
            this.CoreZome = coreZome;
            this.HolonType = holonType;
            this.HolonsType = holonsType;
        }

        // Need to load list of zome names that belong to this Planet for PlanetBase to use...
        // Maybe load list of holons too?
        //public Task<List<IZome>> LoadZomes(string coreProviderKey)
        public List<IZome> LoadZomes()
        {
            if (string.IsNullOrEmpty(ProviderKey))
                throw new ArgumentNullException("ProviderKey", "The ProviderKey must be set before this method can be called.");

            //TODO: Check to see if the method awaits till the zomes(holons) are loaded before returning (if it doesn't need to refacoring to subscribe to events like LoadHolons does)
            List<IZome> zomes = new List<IZome>();
            foreach (IHolon holon in base.LoadHolonsAsync(HolonsType, ProviderKey).Result)
                zomes.Add((IZome)holon);

            OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Zomes = zomes });

            //TODO: Make this return a Task so is awaitable...
            return zomes;
        }

        public async Task<IHolon> SaveCelestialBodyAsync(IHolon savingHolon)
        {
            return await base.SaveHolonAsync(HolonType, savingHolon);
        }

        public async Task<IHolon> LoadCelestialBodyAsync()
        {
            if (string.IsNullOrEmpty(ProviderKey))
                throw new ArgumentNullException("ProviderKey", "The ProviderKey must be set before this method can be called.");

            return await base.LoadHolonAsync(HolonType, ProviderKey);
        }

        public async Task<List<IHolon>> LoadHolons()
        {
            if (string.IsNullOrEmpty(ProviderKey))
                throw new ArgumentNullException("ProviderKey", "The ProviderKey must be set before this method can be called.");

            return await base.LoadHolonsAsync(HolonsType, ProviderKey);
        }
    }
}
