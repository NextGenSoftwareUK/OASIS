using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.STAR.Zomes;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public abstract class CelestialBodyCore : ZomeBase, ICelestialBodyCore
    {
        //private const string ZOMES_LOAD_ALL = "_zomes_loadall";
        //private const string ZOMES_ADD = "_zomes_add";
        //private const string ZOMES_REMOVE = "_zomes_remove";


        //private const string ZOMES_ADD = "planet_holons_add";  //Holons Collection on CelestialBody only loads all holons in all zomes belonging to that body (like a shortcut). So a body does not store holons directly, only zomes (containing holons).
        //private const string ZOMES_REMOVE = "planet_holons_remove";
        //private const string HOLONS_LOAD_ALL = "_holons_loadall";



        public string ProviderKey { get; set; } //Anchor address in hc.

        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        //  public event HolonsLoaded OnHolonsLoaded;
        public event Events.HolonsLoaded OnHolonsLoaded;

        public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
        //public event ZomesLoaded OnZomesLoaded;
        public event Events.ZomesLoaded OnZomesLoaded;

        public List<IZome> Zomes { get; set; } = new List<IZome>();

        public new List<IHolon> Holons
        {
            get
            {
                if (Zomes != null)
                {
                    List<IHolon> holons = new List<IHolon>();

                    foreach (IZome zome in Zomes)
                        holons.Add(zome);

                    //Now we need to add the base holons that are linked directly to the celestialbody.
                    holons.AddRange(base.Holons);

                    return holons;
                }

                return null;
            }
        }

        //TODO: FIX THIS TOMORROW!
        //string ICelestialBodyCore.ProviderKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        //List<IZome> ICelestialBodyCore.Zomes { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        List<IHolon> ICelestialBodyCore.Holons => throw new NotImplementedException();

        //public string CoreZomeName { get; set; }
        //public string CoreHolonType { get; set; }


        public CelestialBodyCore(string providerKey) : base()
        {
            this.ProviderKey = providerKey;
        }

        public CelestialBodyCore() : base()
        {
        }

        //event Events.HolonsLoaded ICelestialBodyCore.OnHolonsLoaded
        //{
        //    add
        //    {
        //        throw new NotImplementedException();
        //    }

        //    remove
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        //event Events.ZomesLoaded ICelestialBodyCore.OnZomesLoaded
        //{
        //    add
        //    {
        //        throw new NotImplementedException();
        //    }

        //    remove
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        /*
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
        }*/

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
            // List<IZome> coreZomes = new List<IZome>();

            if (Zomes == null)
                Zomes = new List<IZome>();

            foreach (IHolon holon in base.LoadHolonsAsync(ProviderKey).Result)
            {
                //TODO: Why we we need two versions of IZome? Can't remember why now?!
                Zomes.Add((IZome)holon);
                // coreZomes.Add((IZome)holon);
            }

            //OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Zomes = coreZomes });
            OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Zomes = Zomes });

            //TODO: Make this return a Task so is awaitable...
            return Zomes;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> AddZome(IZome zome)
        {
            this.Zomes.Add(zome);
            return await base.SaveHolonsAsync(this.Zomes);
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> RemoveZome(IZome zome)
        {
            this.Zomes.Remove(zome);
            return await base.SaveHolonsAsync(this.Zomes);
        }

        public async Task<OASISResult<IHolon>> SaveCelestialBodyAsync(IHolon savingHolon)
        {
            return await base.SaveHolonAsync(savingHolon);
        }

        public async Task<IHolon> LoadCelestialBodyAsync()
        {
            if (string.IsNullOrEmpty(ProviderKey))
                throw new ArgumentNullException("ProviderKey", "The ProviderKey must be set before this method can be called.");

            return await base.LoadHolonAsync(ProviderKey);
        }

        Task<IHolon> ICelestialBodyCore.LoadCelestialBodyAsync()
        {
            throw new NotImplementedException();
        }

        List<IZome> ICelestialBodyCore.LoadZomes()
        {
            throw new NotImplementedException();
        }

        Task<OASISResult<IHolon>> ICelestialBodyCore.SaveCelestialBodyAsync(IHolon savingHolon)
        {
            throw new NotImplementedException();
        }
    }
}
