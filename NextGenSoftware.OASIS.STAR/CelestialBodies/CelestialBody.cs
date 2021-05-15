using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.STAR.Zomes;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public abstract class CelestialBody : Holon, ICelestialBody
    {
        protected int _currentId = 0;
        protected string _hcinstance;
        protected TaskCompletionSource<string> _taskCompletionSourceGetInstance = new TaskCompletionSource<string>();

        //public CelestialBodyCore CelestialBodyCore { get; set; } // This is the core zome of the planet (OAPP), which links to all the other planet zomes/holons...
        public ICelestialBodyCore CelestialBodyCore { get; set; } // This is the core zome of the planet (OAPP), which links to all the other planet zomes/holons...

        //public OASISAPIManager OASISAPI = new OASISAPIManager(new List<IOASISProvider>() { new SEEDSOASIS() });

        // public string RustHolonType { get; set; }
        //  public string RustCelestialBodyType { get; set; }
        public GenesisType GenesisType { get; set; }
       // ICelestialBodyCore ICelestialBody.CelestialBodyCore { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsInitialized
        {
            get
            {
                return CelestialBodyCore != null;
            }
        }

        //TODO: Should these be in PlanetCore?
        //public List<IZome> Zomes { get; set; }

        //public List<IHolon> Holons
        //{
        //    get
        //    {
        //        if (Zomes != null)
        //        {
        //            List<IHolon> holons = new List<IHolon>();

        //            foreach (IZome zome in Zomes)
        //                holons.Add(zome);

        //            return holons;
        //        }

        //        return null;
        //    }
        //}

        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        public event HolonsLoaded OnHolonsLoaded;

        public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
        public event ZomesLoaded OnZomesLoaded;

        public delegate void HolonSaved(object sender, HolonSavedEventArgs e);
        public event HolonSaved OnHolonSaved;

        public delegate void HolonLoaded(object sender, HolonLoadedEventArgs e);
        public event HolonLoaded OnHolonLoaded;

        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        public delegate void ZomeError(object sender, ZomeErrorEventArgs e);
        public event ZomeError OnZomeError;

        //TODO: Move these two events to IOASISStorage so can be shared...
        public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        public event Disconnected OnDisconnected;

        public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        public event DataReceived OnDataReceived;

       // public HoloNETClientBase HoloNETClient { get; private set; }

        public CelestialBody()
        {
            Initialize(); //TODO: It never called this from the constructor before, was there a good reason? Will soon find out! ;-)
        }

        public CelestialBody(GenesisType genesisType)
        {
            this.GenesisType = genesisType;
            Initialize();  //TODO: It never called this from the constructor before, was there a good reason? Will soon find out! ;-)
        }

        public CelestialBody(Guid id, GenesisType genesisType)
        {
            this.GenesisType = genesisType;
            this.Id = id;
            Initialize();
        }

        public CelestialBody(Dictionary<ProviderType, string> providerKey, GenesisType genesisType)
        {
            this.GenesisType = genesisType;
            this.ProviderKey = providerKey;
            Initialize();  //TODO: It never called this from the constructor before, was there a good reason? Will soon find out! ;-)
        }

        public void LoadAll()
        {
          //  LoadHolons();
            LoadZomes();
        }

       //public OASISResult<ICelestialBody> Save()
       //{

       //}

     //  private OASISResult<ICelestialBody> 

        public async Task<OASISResult<ICelestialBody>> SaveAsync()
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>(this);
            OASISResult<IHolon> celestialBodyHolonResult = new OASISResult<IHolon>();

            if (this.Children == null)
                this.Children = new List<Holon>();

            // Only save if the holon has any changes.
            if (!HasHolonChanged())
            {
                result.Result = this;
                result.IsSaved = false;
                result.Message = "No changes need saving";
                return result;
            }

            // If the celestiablBody has not been saved yet then save now so its children can set the parentId's.
           // if (this.Id == Guid.Empty)
           // {
                celestialBodyHolonResult = await CelestialBodyCore.SaveCelestialBodyAsync(this);
                result.Message = celestialBodyHolonResult.Message;
                result.IsSaved = celestialBodyHolonResult.IsSaved;

                if (celestialBodyHolonResult.IsError || !celestialBodyHolonResult.IsSaved)
                {
                    result.IsError = celestialBodyHolonResult.IsError;
                    return result;
                }
                else
                {
                    this.Id = celestialBodyHolonResult.Result.Id;
                    this.ProviderKey = celestialBodyHolonResult.Result.ProviderKey;
                    this.CelestialBodyCore.ProviderKey = celestialBodyHolonResult.Result.ProviderKey;
                    this.CreatedByAvatar = celestialBodyHolonResult.Result.CreatedByAvatar;
                    this.CreatedByAvatarId = celestialBodyHolonResult.Result.CreatedByAvatarId;
                    this.CreatedDate = celestialBodyHolonResult.Result.CreatedDate;
                    this.ModifiedByAvatar = celestialBodyHolonResult.Result.ModifiedByAvatar;
                    this.ModifiedByAvatarId = celestialBodyHolonResult.Result.ModifiedByAvatarId;
                    this.ModifiedDate = celestialBodyHolonResult.Result.ModifiedDate;
                    this.Children = celestialBodyHolonResult.Result.Children;

                    // TODO: Not sure if ParentStar and ParentPlanet will be set?
                    switch (this.HolonType)
                    {
                        case HolonType.SuperStar:
                            SetParentIdsForSuperStar(); 
                            break;

                        case HolonType.Star:
                            SetParentIdsForStar((IStar)this);
                            break;

                        case HolonType.Planet:
                            SetParentIdsForPlanet(this.ParentStar, (IPlanet)this);
                            break;

                        case HolonType.Moon:
                            SetParentIdsForMoon(this.ParentStar, this.ParentPlanet, (IMoon)this);
                            break;
                    }
                }
           // }

            if (this.HolonType == HolonType.SuperStar)
            {
                //TODO: Eventually this will use "this" rather than static SuperStar when SuperStar inherits from Star and a new static GrandSuperStar is created... :)
                foreach (Star star in SuperStar.Stars) 
                {
                    result = await star.SaveAsync();

                    if (result.IsError)
                        return result;
                }
            }

            //TODO: We need to indivudally save each planet/zome/holon first so we get their unique id's. We can then set the parentId's etc.
            if (this.HolonType == HolonType.Star)
            {
                foreach (Planet planet in ((IStar)this).Planets)
                {
                    result = await planet.SaveAsync(); // TODO: Think we need to save again even if id is not null just in case its children have changed since last time it was saved?

                    if (result.IsError)
                        return result;

                   // ((List<Holon>)this.Children).Add(planet);
                }
            }
            else
            {
                OASISResult<IZome> zomeResult = new OASISResult<IZome>(); 

                if (this.HolonType == HolonType.Planet)
                {
                    foreach (Moon moon in ((IPlanet)this).Moons)
                    {
                        result = await moon.SaveAsync();

                        if (result.IsError)
                            return result;
                    }
                }

                foreach (Zome zome in this.CelestialBodyCore.Zomes)
                {
                    // if (zome.Id == Guid.Empty)
                    zomeResult = await zome.Save(); // TODO: Think we need to save again even if id is not null just in case its children have changed since last time it was saved?

                    if (zomeResult.IsError)
                    {
                        result.IsError = true;
                        result.Message = zomeResult.Message;
                        return result;
                    }

                   // ((List<Holon>)this.Children).Add(zome);
                }
            }

            // TODO: Dont think we need to save again now? Check...
            /*
            // If any changes have been made (child objects added/changed) then need to save again.
            if (!HasHolonChanged())
            {
                OASISResult<IHolon> holonResult = await CelestialBodyCore.SaveCelestialBodyAsync(this);

                if (holonResult.Result != null)
                {
                    holonResult.IsSaved = celestialBodyHolonResult.IsSaved;
                    this.Id = holonResult.Result.Id;
                    this.ProviderKey = holonResult.Result.ProviderKey;
                    this.CelestialBodyCore.ProviderKey = holonResult.Result.ProviderKey;
                    this.CreatedByAvatar = holonResult.Result.CreatedByAvatar;
                    this.CreatedByAvatarId = holonResult.Result.CreatedByAvatarId;
                    this.CreatedDate = holonResult.Result.CreatedDate;
                    this.ModifiedByAvatar = holonResult.Result.ModifiedByAvatar;
                    this.ModifiedByAvatarId = holonResult.Result.ModifiedByAvatarId;
                    this.ModifiedDate = holonResult.Result.ModifiedDate;
                    this.Children = holonResult.Result.Children;
                }

                result.Result = this;
                result.Message = holonResult.Message;
                result.IsError = holonResult.IsError;

                //return new OASISResult<ICelestialBody>() { Result = this, Message = holonResult.Message, IsError = holonResult.IsError };
            }*/

            return result;
        }

        private void SetParentIdsForHolon(IStar star, IPlanet planet, IMoon moon, IZome zome, IHolon holon)
        {
            foreach (Holon innerHolon in holon.Children)
            {
                innerHolon.ParentHolonId = holon.Id;
                innerHolon.ParentHolon = holon;
                innerHolon.ParentStar = star;
                innerHolon.ParentStarId = star.Id;
                innerHolon.ParentPlanet = planet;
                innerHolon.ParentPlanetId = planet.Id;

                if (moon != null)
                {
                    holon.ParentMoon = moon;
                    holon.ParentMoonId = moon.Id;
                }

                innerHolon.ParentZome = zome;
                innerHolon.ParentZomeId = zome.Id;

                foreach (Holon childHolon in innerHolon.Children)
                    SetParentIdsForHolon(star, planet, moon, zome, childHolon);
            }
        }

        private void SetParentIdsForZome(IStar star, IPlanet planet, IMoon moon, IZome zome)
        {
            foreach (Holon holon in zome.Holons)
            {
                holon.ParentHolonId = zome.Id;
                holon.ParentHolon = zome;
                holon.ParentStar = star;
                holon.ParentStarId = star.Id;
                holon.ParentPlanet = planet;
                holon.ParentPlanetId = planet.Id;

                if (moon != null)
                { 
                    holon.ParentMoon = moon;
                    holon.ParentMoonId = moon.Id;
                }

                holon.ParentZome = zome;
                holon.ParentZomeId = zome.Id;

                foreach (Holon childHolon in holon.Children)
                    SetParentIdsForHolon(star, planet, moon, zome, childHolon);
            }
        }

        private void SetParentIdsForMoon(IStar star, IPlanet planet, IMoon moon)
        {
            foreach (Zome zome in moon.CelestialBodyCore.Zomes)
                SetParentIdsForZome(star, planet, moon, (IZome)zome);
        }

        private void SetParentIdsForPlanet(IStar star, IPlanet planet)
        {
            foreach (IZome zome in planet.CelestialBodyCore.Zomes)
                SetParentIdsForZome(star, planet, null, zome);

            foreach (IMoon moon in planet.Moons)
                SetParentIdsForMoon(star, planet, moon);
        }

        private void SetParentIdsForStar(IStar star)
        {
            foreach (IPlanet planet in star.Planets)
                SetParentIdsForPlanet(star, planet);

            //TODO: Do we want to add Zomes to a Star? Maybe?
        }

        //TODO: We may one day pass in a SuperStar here? Currently its static when we thought there would be only one SuperStar but now think we will have more than one so will likely make SuperStar inherit from Star, etc. And need to change existing static SuperStar class to GrandSuperStar or something! ;-)
        private void SetParentIdsForSuperStar()
        {
            foreach (IStar star in SuperStar.Stars)
                SetParentIdsForStar(star);

           // foreach (IPlanet planet in SuperStar.Planets)
           //     SetParentIdsForPlanet(SuperStar.InnerStar, planet);
        }



        /*
        private async Task<bool> SaveZomesAndHolons()
        {
            foreach (ZomeBase zome in Zomes)
            {
                //TODO: Need to check if any state has changed and only save if it has...
                //await zome.SaveHolonAsync(zome);
                await zome.SaveHolonAsync(this.RustHolonType, zome); //TODO: FIX ASAP!

                foreach (Holon holon in zome.Holons)
                {
                    //TODO: Need to check if any state has changed and only save if it has...
                    await zome.SaveHolonAsync(this.RustHolonType, holon);
                }
            }

            return true;
        }
        */

        // Build
        public CoronalEjection Flare()
        {
            return SuperStar.Flare(this);
        }

        // Activate & Launch - Launch & activate the planet (OAPP) by shining the star's light upon it...
        public void Shine()
        {
            SuperStar.Shine(this);
        }

        // Deactivate the planet (OAPP)
        public void Dim()
        {
            SuperStar.Dim(this);
        }

        // Deploy the planet (OAPP)
        public void Seed()
        {
            SuperStar.Seed(this);
        }

        // Run Tests
        public void Twinkle()
        {
            SuperStar.Twinkle(this);
        }

        // Highlight the Planet (OAPP) in the OAPP Store (StarNET). *Admin Only*
        public void Radiate()
        {
            SuperStar.Radiate(this);
        }

        // Show how much light the planet (OAPP) is emitting into the solar system (StarNET/HoloNET)
        public void Emit()
        {
            SuperStar.Emit(this);
        }

        // Show stats of the Planet (OAPP).
        public void Reflect()
        {
            SuperStar.Reflect(this);
        }

        // Upgrade/update a Planet (OAPP).
        public void Evolve()
        {
            SuperStar.Evolve(this);
        }

        // Import/Export hApp, dApp & others.
        public void Mutate()
        {
            SuperStar.Mutate(this);
        }

        // Send/Receive Love
        public void Love()
        {
            SuperStar.Love(this);
        }

        // Reserved For Future Use...
        public void Super()
        {
            SuperStar.Super(this);
        }

        private void PlanetCore_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private async void PlanetCore_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            // TODO: Dont think this is needed now?
            // This was going to load each of the zomes holons once the zomes were loaded for this Planet. 
            // But maybe it is better to allow them be lazy loaded as and when they are needed rather than pulling them all back in one go?
            // Trade offs between the 2 approaches... for now we leave as lazy loading so will only load when they are needed...

            /*
            foreach (ZomeBase zome in CelestialBodyCore.Zomes)
            {
                await zome.Initialize(zome.Name, this.HoloNETClient);
                zome.OnHolonLoaded += Zome_OnHolonLoaded;
                zome.OnHolonSaved += Zome_OnHolonSaved;
            }*/

            //TODO: Not sure whether to delegate holons being loaded by zomes if can just load direct from PlanetCore?
            //Nice for Zomes to manage their own collections of holons (good practice) so will see... :)
        }

        private void PlanetCore_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {

        }

        //TODO: Make LoadZomes async once PlanetCore.LoadZomes() refactored to return a Task...
        //public async void LoadZomes()
        //{
        //    Zomes = await PlanetCore.LoadZomes();
        //}
        public void LoadZomes()
        {
            //Zomes = CelestialBodyCore.LoadZomes();
            CelestialBodyCore.LoadZomes();
        }

        public async Task Initialize()
        {
            switch (this.GenesisType)
            {
                case GenesisType.Planet:
                    CelestialBodyCore = new PlanetCore(this.Id, (IPlanet)this);
                    break;

                case GenesisType.Moon:
                    CelestialBodyCore = new MoonCore(this.Id, (IMoon)this);
                    break;

                case GenesisType.Star:
                    CelestialBodyCore = new StarCore(this.Id, (IStar)this);
                    break;
            }
           
            //TODO: Not even sure if we need to bother with providerKey at all when we have the id guid?
            if (ProviderKey != null && ProviderKey.ContainsKey(ProviderManager.CurrentStorageProviderType.Value) && !string.IsNullOrEmpty(ProviderKey[ProviderManager.CurrentStorageProviderType.Value]))
            {
               // CelestialBodyCore.ProviderKey = this.ProviderKey[ProviderManager.CurrentStorageProviderType.Value]; //_coreProviderKey = hc anchor.
                await LoadCelestialBody();

                //TODO: Load the planets Zome collection here? Or is it passed in from the sub-class implementation? Probably 2nd one... ;-)
                //No we load the holons and zomes linked to the planetcore zome via the coreProviderKey anchor...
                LoadZomes();
               // LoadHolons();
            }
            else if (Id != Guid.Empty)
            {
                await LoadCelestialBody();
                LoadZomes();
            }

            await WireUpEvents();
        }

        private async Task LoadCelestialBody()
        {
            //await PlanetCore.LoadHolonAsync(PLANET_HOLON, this.ProviderKey);
            //await PlanetCore.LoadHolonAsync(string.Concat(this.RustHolonType, "_holon"), this.ProviderKey);

            await CelestialBodyCore.LoadCelestialBodyAsync();
        }

        private async Task WireUpEvents()
        {
            /*
            HoloNETClient.OnConnected += HoloNETClient_OnConnected;
            HoloNETClient.OnDisconnected += HoloNETClient_OnDisconnected;
          //  HoloNETClient.OnError += HoloNETClient_OnError;
            HoloNETClient.OnDataReceived += HoloNETClient_OnDataReceived;
            HoloNETClient.OnGetInstancesCallBack += HoloNETClient_OnGetInstancesCallBack;
            HoloNETClient.OnSignalsCallBack += HoloNETClient_OnSignalsCallBack;
            HoloNETClient.OnZomeFunctionCallBack += HoloNETClient_OnZomeFunctionCallBack;
            */

            ((CelestialBodyCore)CelestialBodyCore).OnHolonsLoaded += PlanetCore_OnHolonsLoaded;
            ((CelestialBodyCore)CelestialBodyCore).OnZomesLoaded += PlanetCore_OnZomesLoaded;
            ((CelestialBodyCore)CelestialBodyCore).OnHolonSaved += PlanetCore_OnHolonSaved;
            ((CelestialBodyCore)CelestialBodyCore).OnZomeError += PlanetCore_OnZomeError;
        }

        private async void PlanetCore_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            if (e.Holon.HolonType == HolonType.Planet)
            {
                // This is the hc Address of the planet (we can use this as the anchor/coreProviderKey to load all future zomes/holons belonging to this planet).
                this.ProviderKey = e.Holon.ProviderKey;

                //Just in case the zomes/holons have been added since the planet was last saved.
                foreach (Zome zome in CelestialBodyCore.Zomes)
                {
                    switch (HolonType)
                    {
                        case HolonType.Star:
                            zome.ParentStar = (IStar)this;
                            zome.ParentStarId = this.Id;
                            break;

                        case HolonType.Planet:
                            zome.ParentPlanet = (IPlanet)this;
                            zome.ParentPlanetId = this.Id;
                            break;

                        case HolonType.Moon:
                            zome.ParentMoon = (IMoon)this;
                            zome.ParentMoonId = this.Id;
                            break;
                    }

                    zome.ParentHolonId = this.Id;
                    zome.ParentHolon = this;

                    // TODO: Need to sort this.Holons collection too (this is a list of ALL holons that belong to ALL zomes for this planet.
                    // So the same holon will be in both collections, just that this.Holons has been flatterned. Why it's Fractal Holonic! ;-)
                    foreach (Holon holon in zome.Holons)
                    {
                        holon.ParentHolon = zome;
                        holon.ParentHolonId = zome.Id;

                        switch (HolonType)
                        {
                            case HolonType.Star:
                                zome.ParentStar = (IStar)this;
                                zome.ParentStarId = this.Id;
                                break;

                            case HolonType.Planet:
                                zome.ParentPlanet = (IPlanet)this;
                                zome.ParentPlanetId = this.Id;
                                break;

                            case HolonType.Moon:
                                zome.ParentMoon = (IMoon)this;
                                zome.ParentMoonId = this.Id;
                                break;
                        }
                    }

                    await zome.SaveHolonsAsync(zome.Holons);
                }
            }
        }

        //TODO: Come back to this, this is what is fired when each zome is loaded once the celestialbody is loaded but I think for now we will lazy load them later...
        private void Zome_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            /*
            bool holonFound = false;

            foreach (ZomeBase zome in CelestialBodyCore.Zomes)
            {
                foreach (Holon holon in zome.Holons)
                {
                    if (holon.Id == e.Holon.Id)
                    {
                        holonFound = true;
                        break;
                    }
                }
            }

            // If the zome or holon is not stored in the cache yet then add it now...
            // Currently the collection will fill up as the individual zome loads each holon.
            // They can call the LoadAll function to load all Holons and Zomes linked to this Planet (OAPP).

            //TODO: Now all zomes and holons belonging to a planet (OAPP) are loaded in init method using hc anchor pattern.
            //Maybe it can be a setting to choose between lazy loading (loading only as needed) or to prefetch and load everything up front.
            //Pros and Cons to both methods, Lazy loading = quicker init load time and less memory but then if you start loading lots of zomes/holons after, that's a lot more network traffic, etc.
            //Loading up front- Longer init load time and uses more memory but then all data cached so no more loading or network traffic needed.

            if (!holonFound)
            {
                //IZome zome = CelestialBodyCore.Zomes.FirstOrDefault(x => x.Parent.Name == e.Holon.Parent.Name);
                IZome zome = CelestialBodyCore.Zomes.FirstOrDefault(x => x.Parent.Id == e.Holon.Parent.Id);

                if (zome == null)
                {
                    zome = new Zome(e.Holon.Parent.Id);
                    zome.Holons.Add(e.Holon);
                    CelestialBodyCore.Zomes.Add(zome);
                    //CelestialBodyCore.Zomes.Add(new Zome(HoloNETClient, e.Holon.Parent.Name));
                }

                ((ZomeBase)zome).Holons.Add((Holon)e.Holon);
            }

            OnHolonLoaded?.Invoke(this, e);
            */
        }


        //TODO: COME BACK TO THIS!!! 
        private void Zome_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            if (e.Holon.HolonType == HolonType.Zome)
            {
                IZome zome = GetZomeById(e.Holon.Id);

                //Update the providerKey (address hash returned from hc)
                if (zome != null)
                {
                    //If the ProviderKey is empty then this is the first time the zome has been saved so we now need to save the zomes holons.
                    //if (string.IsNullOrEmpty(zome.ProviderKey))
                    // {
                    zome.ProviderKey = e.Holon.ProviderKey;
                    zome.ParentHolon = e.Holon;
                    
                    switch (HolonType)
                    {
                        case HolonType.Star:
                            zome.ParentStar = (IStar)this;
                            zome.ParentStarId = this.Id;
                            break;

                        case HolonType.Planet:
                            zome.ParentPlanet = (IPlanet)this;
                            zome.ParentPlanetId = this.Id;
                            break;

                        case HolonType.Moon:
                            zome.ParentMoon = (IMoon)this;
                            zome.ParentMoonId = this.Id;
                            break;
                    }

                    foreach (Holon holon in GetHolonsThatBelongToZome(zome))
                        zome.SaveHolonAsync(holon);
                    //zome.SaveHolonAsync(this.RustHolonType, holon);
                    // }
                }
            }
            else
            {
                IHolon holon = CelestialBodyCore.Holons.FirstOrDefault(x => x.Id == e.Holon.Id);

                //TODO: Come back to this... Wouldn't parent already be set? Same for zomes? Need to check...
                if (holon != null)
                {
                    holon.ProviderKey = e.Holon.ProviderKey;
                    //holon.Parent = e.Holon;
                    //holon.ParentCelestialBody = this;

                    switch (HolonType)
                    {
                        case HolonType.Star:
                            holon.ParentStar = (IStar)this;
                            holon.ParentStarId = this.Id;
                            break;

                        case HolonType.Planet:
                            holon.ParentPlanet = (IPlanet)this;
                            holon.ParentPlanetId = this.Id;
                            break;

                        case HolonType.Moon:
                            holon.ParentMoon = (IMoon)this;
                            holon.ParentMoonId = this.Id;
                            break;
                    }
                }
            }

            OnHolonSaved?.Invoke(this, e);
        }

        private Zome GetZomeThatHolonBelongsTo(Holon holon)
        {
            return (Zome)CelestialBodyCore.Holons.FirstOrDefault(x => x.Id == holon.Id).ParentHolon;
        }

        private List<IHolon> GetHolonsThatBelongToZome(IZome zome)
        {
            return CelestialBodyCore.Holons.Where(x => x.ParentHolon.Id == zome.Id).ToList();
        }

        private IZome GetZomeByName(string name)
        {
            return CelestialBodyCore.Zomes.FirstOrDefault(x => x.Name == name);
        }

        private IZome GetZomeById(Guid id)
        {
            return CelestialBodyCore.Zomes.FirstOrDefault(x => x.Id == id);
        }

        //private void PlanetBase_OnHolonSaved(object sender, HolonLoadedEventArgs e)
        //{

        //}

        //private void PlanetBase_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        //{

        //}

        private void HoloNETClient_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
        {
            //if (!e.IsCallSuccessful)
            //    HandleError(string.Concat("Zome function ", e.ZomeFunction, " on zome ", e.Zome, " returned an error. Error Details: ", e.ZomeReturnData), null, null);
            //else
            //{
            //    for (int i = 0; i < _loadFuncNames.Count; i++)
            //    {
            //        if (e.ZomeFunction == _loadFuncNames[i])
            //        {
            //            IHolon holon = (IHolon)JsonConvert.DeserializeObject<IHolon>(string.Concat("{", e.ZomeReturnData, "}"));
            //            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs { Holon = holon });
            //            _taskCompletionSourceLoadHolon.SetResult(holon);
            //        }
            //        else if (e.ZomeFunction == _saveFuncNames[i])
            //        {
            //            _savingHolons[e.Id].HcAddressHash = e.ZomeReturnData;

            //            OnHolonSaved?.Invoke(this, new HolonLoadedEventArgs { Holon = _savingHolons[e.Id] });
            //            _taskCompletionSourceSaveHolon.SetResult(_savingHolons[e.Id]);
            //            _savingHolons.Remove(e.Id);
            //        }
            //    }
            //}
        }

        /*
        public virtual async Task<IHolon> LoadHolonAsync(string hcEntryAddressHash)
        {
            return await LoadHolonAsync(this.RustHolonType, hcEntryAddressHash);

            // Find the zome that the holon belongs to and then load it...
            //TODO: May be more efficient way of doing this by loading it directly? But nice each zome manages its own collection of holons...
            //foreach (ZomeBase zome in Zomes)
            //{
            //    foreach (Holon holon in zome.Holons)
            //    {
            //        //if (holon.Name == holonName)
            //        if (holon.RustHolonType == this.RustHolonType)
            //            return await zome.LoadHolonAsync(holon.RustHolonType, hcEntryAddressHash);
            //    }
            //}

            //return null;
        }
        */

        //TODO: Should this be in PlanetCore?
      /*
        public virtual async Task<IHolon> LoadHolonAsync(string rustHolonType, string hcEntryAddressHash)
        {
            // Find the zome that the holon belongs to and then load it...
            //TODO: May be more efficient way of doing this by loading it directly? But nice each zome manages its own collection of holons...
            foreach (ZomeBase zome in Zomes)
            {
                foreach (Holon holon in zome.Holons)
                {
                    //if (holon.Name == holonName)
                    if (holon.RustHolonType == rustHolonType)
                        return await zome.LoadHolonAsync(rustHolonType, hcEntryAddressHash);
                }
            }

            return null;
        }*/

        /*
        //TODO: Should this be in PlanetCore?
        public virtual async Task<IHolon> SaveHolonAsync(string rustHolonType, IHolon savingHolon)
        {
            // Find the zome that the holon belongs to and then save it...
            foreach (ZomeBase zome in Zomes)
            {
                foreach (Holon holon in zome.Holons)
                {
                    //if (holon.Name == savingHolon.Name)
                    if (holon.RustHolonType == rustHolonType)
                        return await zome.SaveHolonAsync(rustHolonType, savingHolon);
                    //return await zome.SaveHolonAsync(this.RustHolonType, savingHolon);
                }
            }

            return null;
        }*/

        /*
        public virtual async Task<IHolon> SaveHolonAsync(IHolon savingHolon)
        {
            // Find the zome that the holon belongs to and then save it...
            foreach (ZomeBase zome in Zomes)
            {
                foreach (Holon holon in zome.Holons)
                {
                    //if (holon.Name == savingHolon.Name)
                    if (holon.RustHolonType == this.RustHolonType)
                        return await zome.SaveHolonAsync(this.RustHolonType, savingHolon);
                    //return await zome.SaveHolonAsync(this.RustHolonType, savingHolon);
                }
            }

            return null;
        }
        */


        /*
        private void HoloNETClient_OnSignalsCallBack(object sender, SignalsCallBackEventArgs e)
        {

        }

        private void HoloNETClient_OnGetInstancesCallBack(object sender, GetInstancesCallBackEventArgs e)
        {
            _hcinstance = e.Instances[0];
            OnInitialized?.Invoke(this, new EventArgs());
            _taskCompletionSourceGetInstance.SetResult(_hcinstance);
        }

        private void HoloNETClient_OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            OnDataReceived?.Invoke(this, e);
        }

        private void HoloNETClient_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            OnDisconnected?.Invoke(this, e);
        }

        private void HoloNETClient_OnConnected(object sender, ConnectedEventArgs e)
        {
            HoloNETClient.GetHolochainInstancesAsync();
        }

        private void HoloNETClient_OnError(object sender, HoloNETErrorEventArgs e)
        {
            HandleError("Error occured in HoloNET. See ErrorDetial for reason.", null, e);
        }


        /// <summary>
        /// Handles any errors thrown by HoloNET or HolochainBaseZome. It fires the OnZomeError error handler if there are any 
        /// subscriptions.
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="errorDetails"></param>
        /// <param name="holoNETEventArgs"></param>
        protected void HandleError(string reason, Exception errorDetails, HoloNETErrorEventArgs holoNETEventArgs)
        {
            OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { EndPoint = HoloNETClient.EndPoint, Reason = reason, ErrorDetails = errorDetails, HoloNETErrorDetails = holoNETEventArgs });
        }*/
    }
}
