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
using static NextGenSoftware.OASIS.API.Core.Events.Events;
using Mapster;
using System.Collections.ObjectModel;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public abstract class CelestialBody : Holon, ICelestialBody
    {
        //protected int _currentId = 0;
       // protected string _hcinstance;
      //  protected TaskCompletionSource<string> _taskCompletionSourceGetInstance = new TaskCompletionSource<string>();

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

        public event HolonsLoaded OnHolonsLoaded;
        public event ZomesLoaded OnZomesLoaded;
        public event HolonSaved OnHolonSaved;
        public event HolonLoaded OnHolonLoaded;
        public event Initialized OnInitialized;
        public event ZomeError OnZomeError;

        //TODO: Think will remove these soon, these were only used for HoloNET...
       // public event Disconnected OnDisconnected;
       // public delegate void DataReceived(object sender, DataReceivedEventArgs e);
       // public event DataReceived OnDataReceived;
        // public HoloNETClientBase HoloNETClient { get; private set; }

        public CelestialBody()
        {
            //Initialize(); //TODO: It never called this from the constructor before, was there a good reason? Will soon find out! ;-)
        }

        public CelestialBody(GenesisType genesisType)
        {
            this.GenesisType = genesisType;
            //Initialize();  //TODO: It never called this from the constructor before, was there a good reason? Will soon find out! ;-)
        }

        public CelestialBody(Guid id, GenesisType genesisType)
        {
            this.GenesisType = genesisType;
            this.Id = id;
            //Initialize();
        }

        public CelestialBody(Dictionary<ProviderType, string> providerKey, GenesisType genesisType)
        {
            this.GenesisType = genesisType;
            this.ProviderKey = providerKey;
            //Initialize();  //TODO: It never called this from the constructor before, was there a good reason? Will soon find out! ;-)
        }

        public async Task<OASISResult<ICelestialBody>> SaveAsync()
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>(this);
            OASISResult<IHolon> celestialBodyHolonResult = new OASISResult<IHolon>();

            if (this.Children == null)
                this.Children = new ObservableCollection<IHolon>();

            // Only save if the holon has any changes.
            if (!HasHolonChanged())
            {
                result.Result = this;
                result.IsSaved = false;
                result.Message = "No changes need saving";
                return result;
            }

            //TODO: Don't think we need to save here to get the Id (saves having to save it twice!), we can generate it here instead and set the IsNewHolon property so the providers know it is a new holon (they use to check if the Id had been set).
            if (this.Id == Guid.Empty)
            {
                Id = Guid.NewGuid();
                IsNewHolon = true;
            }

            /*
            celestialBodyHolonResult = await CelestialBodyCore.SaveCelestialBodyAsync(this);
            result.Message = celestialBodyHolonResult.Message;
            result.IsSaved = celestialBodyHolonResult.IsSaved;

            if (celestialBodyHolonResult.IsError || !celestialBodyHolonResult.IsSaved || celestialBodyHolonResult.Result == null)
            {
                result.IsError = celestialBodyHolonResult.IsError;
                return result;
            }

            SetProperties(celestialBodyHolonResult.Result);
            */

            //celestialBodyHolonResult.Result.Adapt(this);
            // SuperStar.Mapper.Map()
            //this = SuperStar.Mapper.Map<CelestialBody>(celestialBodyHolonResult.Result);

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

            if (this.HolonType == HolonType.SuperStar)
            {
                //TODO: Eventually this will use "this" rather than static SuperStar when SuperStar inherits from Star and a new static GrandSuperStar is created... :)
                if (SuperStar.Stars != null)
                {
                    foreach (Star star in SuperStar.Stars)
                    {
                        if (star.HasHolonChanged())
                            result = await star.SaveAsync();

                        if (result.IsError)
                            return result;
                    }
                }
            }

            //TODO: We need to indivudally save each planet/zome/holon first so we get their unique id's. We can then set the parentId's etc.
            if (this.HolonType == HolonType.Star)
            {
                if (((IStar)this).ParentSolarSystem.Planets != null)
                {
                    foreach (Planet planet in ((IStar)this).ParentSolarSystem.Planets)
                    {
                        if (planet.HasHolonChanged())
                            result = await planet.SaveAsync(); 

                        if (result.IsError)
                            return result;
                    }
                }
            }
            else
            {
                OASISResult<IZome> zomeResult = new OASISResult<IZome>();

                if (this.HolonType == HolonType.Planet)
                {
                    if (((IPlanet)this).Moons != null)
                    {
                        foreach (Moon moon in ((IPlanet)this).Moons)
                        {
                            if (moon.HasHolonChanged())
                                result = await moon.SaveAsync();

                            if (result.IsError)
                                return result;
                        }
                    }
                }

                if (CelestialBodyCore.Zomes != null)
                {
                    foreach (Zome zome in this.CelestialBodyCore.Zomes)
                    {
                        if (zome.HasHolonChanged())
                            zomeResult = await zome.SaveAsync(); 

                        if (zomeResult.IsError)
                        {
                            result.IsError = true;
                            result.Message = zomeResult.Message;
                            return result;
                        }
                    }
                }
            }

            // Finally we need to save again so the child holon ids's are stored in the graph...
            // TODO: We may not need to do this save again in future since when we load the zome we could lazy load its child holons seperatley from their parentIds.
            // But loading the celestialbody with all its child holons will be faster than loading them seperatley (but only if the current OASIS Provider supports this, so far MongoDBOASIS does).

            celestialBodyHolonResult = await CelestialBodyCore.SaveCelestialBodyAsync(this);
            result.Message = celestialBodyHolonResult.Message;
            result.IsSaved = celestialBodyHolonResult.IsSaved;
            result.IsError = celestialBodyHolonResult.IsError;

            if (!celestialBodyHolonResult.IsError && celestialBodyHolonResult.Result != null)
                //celestialBodyHolonResult.Result.Adapt(this);
                SetProperties(celestialBodyHolonResult.Result);

            return result;
        }

        public OASISResult<ICelestialBody> Save()
        {
            return SaveAsync().Result; //TODO: Best way of doing this?
        }



        private void SetProperties(IHolon holon)
        {
            this.Id = holon.Id;
            this.Name = holon.Name;
            this.Description = holon.Description;
            this.HolonType = holon.HolonType;
            this.ParentHolon = holon.ParentHolon;
            this.ParentHolonId = holon.ParentHolonId;
            this.ParentMoon = holon.ParentMoon;
            this.ParentMoonId = holon.ParentMoonId;
            this.ParentPlanet = holon.ParentPlanet;
            this.ParentPlanetId = holon.ParentPlanetId;
            this.ParentStar = holon.ParentStar;
            this.ParentStarId = holon.ParentStarId;
            this.ProviderKey = holon.ProviderKey;
            this.CelestialBodyCore.Id = holon.Id;
            this.CelestialBodyCore.ProviderKey = holon.ProviderKey;
            this.CreatedByAvatar = holon.CreatedByAvatar;
            this.CreatedByAvatarId = holon.CreatedByAvatarId;
            this.CreatedDate = holon.CreatedDate;
            this.ModifiedByAvatar = holon.ModifiedByAvatar;
            this.ModifiedByAvatarId = holon.ModifiedByAvatarId;
            this.ModifiedDate = holon.ModifiedDate;
            this.Children = holon.Children;
        }

        private void SetParentIdsForMoon(IStar star, IPlanet planet, IMoon moon)
        {
            if (moon.CelestialBodyCore.Zomes != null)
            {
                foreach (Zome zome in moon.CelestialBodyCore.Zomes)
                    ZomeHelper.SetParentIdsForZome(star, planet, moon, zome);
            }
        }

        private void SetParentIdsForPlanet(IStar star, IPlanet planet)
        {
            if (planet.CelestialBodyCore.Zomes != null)
            {
                foreach (IZome zome in planet.CelestialBodyCore.Zomes)
                    ZomeHelper.SetParentIdsForZome(star, planet, null, zome);
            }

            if (planet.Moons != null)
            {
                foreach (IMoon moon in planet.Moons)
                    SetParentIdsForMoon(star, planet, moon);
            }
        }

        private void SetParentIdsForStar(IStar star)
        {
            if (star.ParentSolarSystem.Planets != null)
            {
                foreach (IPlanet planet in star.ParentSolarSystem.Planets)
                    SetParentIdsForPlanet(star, planet);
            }

            //TODO: Do we want to add Zomes to a Star? Maybe?
        }

        //TODO: We may one day pass in a SuperStar here? Currently its static when we thought there would be only one SuperStar but now think we will have more than one so will likely make SuperStar inherit from Star, etc. And need to change existing static SuperStar class to GrandSuperStar or something! ;-)
        private void SetParentIdsForSuperStar()
        {
            if (SuperStar.Stars != null)
            {
                foreach (IStar star in SuperStar.Stars)
                    SetParentIdsForStar(star);
            }

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

        public async Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync()
        {
            return await CelestialBodyCore.LoadZomesAsync(); 
        }

        public OASISResult<IEnumerable<IZome>> LoadZomes()
        {
            return CelestialBodyCore.LoadZomes();
        } 

        public async Task<OASISResult<ICelestialBody>> LoadCelestialBodyAsync()
        {
            OASISResult<ICelestialBody> result = await CelestialBodyCore.LoadCelestialBodyAsync();

            if (!result.IsError)
                SetProperties(result.Result);

            return result;

            //(await CelestialBodyCore.LoadCelestialBodyAsync()).Adapt(this);
            //IHolon holon = await CelestialBodyCore.LoadCelestialBodyAsync();
           // holon.Adapt(this);
        }

        public OASISResult<ICelestialBody> LoadCelestialBody()
        {
            OASISResult<ICelestialBody> result = CelestialBodyCore.LoadCelestialBody();

            if (!result.IsError)
                SetProperties(result.Result);

            return result;
            //CelestialBodyCore.LoadCelestialBody().Adapt(this);
        }

        public async Task InitializeAsync()
        {
            InitCelestialBodyCore();

            //TODO: Not even sure if we need to bother with providerKey at all when we have the id guid?
            if (ProviderKey != null && ProviderKey.ContainsKey(ProviderManager.CurrentStorageProviderType.Value) && !string.IsNullOrEmpty(ProviderKey[ProviderManager.CurrentStorageProviderType.Value]))
            {
                await LoadCelestialBodyAsync();
                await LoadZomesAsync();
                // LoadHolons();
            }
            else if (Id != Guid.Empty)
            {
                await LoadCelestialBodyAsync();
                await LoadZomesAsync();
                // LoadHolons();
            }

            WireUpEvents();
        }

        public void Initialize()
        {
            InitCelestialBodyCore();

            //TODO: Not even sure if we need to bother with providerKey at all when we have the id guid?
            if (ProviderKey != null && ProviderKey.ContainsKey(ProviderManager.CurrentStorageProviderType.Value) && !string.IsNullOrEmpty(ProviderKey[ProviderManager.CurrentStorageProviderType.Value]))
            {
                LoadCelestialBody();
                LoadZomes();
                // LoadHolons();
            }
            else if (Id != Guid.Empty)
            {
                LoadCelestialBody();
                LoadZomes();
                // LoadHolons();
            }

            WireUpEvents();
        }

        private void InitCelestialBodyCore()
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
        }

        private void WireUpEvents()
        {
            ((CelestialBodyCore)CelestialBodyCore).OnHolonsLoaded += PlanetCore_OnHolonsLoaded;
            ((CelestialBodyCore)CelestialBodyCore).OnZomesLoaded += PlanetCore_OnZomesLoaded;
            ((CelestialBodyCore)CelestialBodyCore).OnHolonSaved += PlanetCore_OnHolonSaved;
            ((CelestialBodyCore)CelestialBodyCore).OnZomeError += PlanetCore_OnZomeError;
        }

        private async void PlanetCore_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            //TODO: Come back to this...
            return;

            //TODO: Handle error.
            if (!e.Result.IsError)
            {
                if (e.Result.Result.HolonType == HolonType.Planet)
                {
                    // This is the hc Address of the planet (we can use this as the anchor/coreProviderKey to load all future zomes/holons belonging to this planet).
                    this.ProviderKey = e.Result.Result.ProviderKey;

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
            //TODO: Handle Error.
            if (!e.Result.IsError)
            {
                if (e.Result.Result.HolonType == HolonType.Zome)
                {
                    IZome zome = GetZomeById(e.Result.Result.Id);

                    //Update the providerKey (address hash returned from hc)
                    if (zome != null)
                    {
                        //If the ProviderKey is empty then this is the first time the zome has been saved so we now need to save the zomes holons.
                        //if (string.IsNullOrEmpty(zome.ProviderKey))
                        // {
                        zome.ProviderKey = e.Result.Result.ProviderKey;
                        zome.ParentHolon = e.Result.Result;

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
                    IHolon holon = CelestialBodyCore.Holons.FirstOrDefault(x => x.Id == e.Result.Result.Id);

                    //TODO: Come back to this... Wouldn't parent already be set? Same for zomes? Need to check...
                    if (holon != null)
                    {
                        holon.ProviderKey = e.Result.Result.ProviderKey;
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
