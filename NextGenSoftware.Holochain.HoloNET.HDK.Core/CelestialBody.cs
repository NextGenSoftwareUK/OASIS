using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public abstract class CelestialBody : Holon, ICelestialBody
    {
        //  private const string PLANET_CORE_ZOME = "planet_core_zome"; //Equivilant to an anchor in hc rust... :)
        //  private string _coreProviderKey;
        // private string HOLON = "planet_holon";
        protected int _currentId = 0;
        protected string _hcinstance;
        protected TaskCompletionSource<string> _taskCompletionSourceGetInstance = new TaskCompletionSource<string>();

        public CelestialBodyCore CelestialBodyCore { get; set; } // This is the core zome of the planet (OAPP), which links to all the other planet zomes/holons...
                                                                 // public string RustHolonType { get; set; }
        public string RustCelestialBodyType { get; set; }
        public GenesisType GenesisType { get; set; }

        //TODO: Should these be in PlanetCore?
        public List<IZome> Zomes { get; set; }
        public List<IHolon> Holons
        {
            get
            {
                if (Zomes != null)
                {
                    List<IHolon> holons = new List<IHolon>();

                    foreach (IZome zome in Zomes)
                        holons.Add(zome);

                    return holons;
                }

                return null;
            }
        }

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

        //TODO: Not sure if we want to expose the HoloNETClient events at this level? They can subscribe to them through the HoloNETClient property below...
        public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        public event Disconnected OnDisconnected;

        public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        public event DataReceived OnDataReceived;

        public HoloNETClientBase HoloNETClient { get; private set; }

        public CelestialBody()
        {

        }

        //public CelestialBody(Guid id, GenesisType genesisType)
        //{
        //    this.GenesisType = genesisType;
        //    this.Id = id;
        //}

        public CelestialBody(GenesisType genesisType)
        {
            this.GenesisType = genesisType;
        }

        //TODO: Don't think we need to pass Id in if we are using ProviderKey?
        public CelestialBody(string providerKey, GenesisType genesisType)
        {
            this.GenesisType = genesisType;
            this.ProviderKey = providerKey;
        }

    
        /*
        public CelestialBody(HoloNETClientBase holoNETClient, Guid id, GenesisType genesisType)
        {
            this.GenesisType = genesisType;
            this.Id = id;
            this.HoloNETClient = holoNETClient;

            //Initialize(id, holoNETClient);
        }


        public CelestialBody(string holochainConductorURI, HoloNETClientType type, Guid id, GenesisType genesisType)
        {
            this.GenesisType = genesisType;
            this.Id = id;
            this.HolochainConductorURI = holochainConductorURI;

            Initialize(id, holochainConductorURI, type);
        }

        public CelestialBody(HoloNETClientBase holoNETClient, GenesisType genesisType)
        {
            this.GenesisType = genesisType;
            Initialize(holoNETClient);
        }

        public CelestialBody(string holochainConductorURI, HoloNETClientType type, GenesisType genesisType)
        {
            this.GenesisType = genesisType;
            Initialize(holochainConductorURI, type);
        }

        //TODO: Don't think we need to pass Id in if we are using ProviderKey?
        public CelestialBody(HoloNETClientBase holoNETClient, Guid id, string providerKey, GenesisType genesisType)
        {
            this.GenesisType = genesisType;
            this.ProviderKey = providerKey;
            Initialize(id, holoNETClient);
        }

        public CelestialBody(string holochainConductorURI, HoloNETClientType type, Guid id, string providerKey, GenesisType genesisType)
        {
            this.GenesisType = genesisType;
            this.ProviderKey = providerKey;
            Initialize(id, holochainConductorURI, type);
        }

        public CelestialBody(HoloNETClientBase holoNETClient, string providerKey, GenesisType genesisType)
        {
            this.GenesisType = genesisType;
            this.ProviderKey = providerKey;
            Initialize(holoNETClient);
        }

        public CelestialBody(string holochainConductorURI, HoloNETClientType type, string providerKey, GenesisType genesisType)
        {
            this.GenesisType = genesisType;
            this.ProviderKey = providerKey;
            Initialize(holochainConductorURI, type);
        }
        */

        /*
        public CelestialBody(HoloNETClientBase holoNETClient, Guid id, string rustHolonType)
        {
            this.RustHolonType = rustHolonType;
            Initialize(id, holoNETClient);
        }

        public CelestialBody(string holochainConductorURI, HoloNETClientType type, Guid id, string rustHolonType)
        {
            this.RustHolonType = rustHolonType;
            Initialize(id, holochainConductorURI, type);
        }

        public CelestialBody(HoloNETClientBase holoNETClient, string rustHolonType)
        {
            this.RustHolonType = rustHolonType;
            Initialize(holoNETClient);
        }

        public CelestialBody(string holochainConductorURI, HoloNETClientType type, string rustHolonType)
        {
            this.RustHolonType = rustHolonType;
            Initialize(holochainConductorURI, type);
        }

        //TODO: Don't think we need to pass Id in if we are using ProviderKey?
        public CelestialBody(HoloNETClientBase holoNETClient, Guid id, string providerKey, string rustHolonType)
        {
            this.ProviderKey = providerKey;
            this.RustHolonType = rustHolonType;
            Initialize(id, holoNETClient);
        }

        public CelestialBody(string holochainConductorURI, HoloNETClientType type, Guid id, string providerKey, string rustHolonType)
        {
            this.ProviderKey = providerKey;
            this.RustHolonType = rustHolonType;
            Initialize(id, holochainConductorURI, type);
        }

        public CelestialBody(HoloNETClientBase holoNETClient, string providerKey, string rustHolonType)
        {
            this.ProviderKey = providerKey;
            this.RustHolonType = rustHolonType;
            Initialize(holoNETClient);
        }

        public CelestialBody(string holochainConductorURI, HoloNETClientType type, string providerKey, string rustHolonType)
        {
            this.ProviderKey = providerKey;
            Initialize(holochainConductorURI, type);
        }
        */


        /*
        public CelestialBody(HoloNETClientBase holoNETClient, Guid id, string rustCelestialBodyType, string rustHolonType)
        {
            this.RustCelestialBodyType = rustCelestialBodyType;
            this.RustHolonType = rustHolonType;
            Initialize(id, holoNETClient);
        }

        public CelestialBody(string holochainConductorURI, HoloNETClientType type, Guid id, string rustCelestialBodyType,  string rustHolonType)
        {
            this.RustCelestialBodyType = rustCelestialBodyType;
            this.RustHolonType = rustHolonType;
            Initialize(id, holochainConductorURI, type);
        }

        public CelestialBody(HoloNETClientBase holoNETClient, string rustCelestialBodyType, string rustHolonType)
        {
            this.RustCelestialBodyType = rustCelestialBodyType;
            this.RustHolonType = rustHolonType;
            Initialize(holoNETClient);
        }

        public CelestialBody(string holochainConductorURI, HoloNETClientType type, string rustCelestialBodyType, string rustHolonType)
        {
            this.RustCelestialBodyType = rustCelestialBodyType;
            this.RustHolonType = rustHolonType;
            Initialize(holochainConductorURI, type);
        }

        //TODO: Don't think we need to pass Id in if we are using ProviderKey?
        public CelestialBody(HoloNETClientBase holoNETClient, Guid id, string providerKey, string rustCelestialBodyType, string rustHolonType)
        {
            this.RustCelestialBodyType = rustCelestialBodyType;
            this.ProviderKey = providerKey;
            this.RustHolonType = rustHolonType;
            Initialize(id, holoNETClient);
        }

        public CelestialBody(string holochainConductorURI, HoloNETClientType type, Guid id, string providerKey, string rustCelestialBodyType, string rustHolonType)
        {
            this.RustCelestialBodyType = rustCelestialBodyType;
            this.ProviderKey = providerKey;
            this.RustHolonType = rustHolonType;
            Initialize(id, holochainConductorURI, type);
        }

        public CelestialBody(HoloNETClientBase holoNETClient, string providerKey, string rustCelestialBodyType, string rustHolonType)
        {
            this.RustCelestialBodyType = rustCelestialBodyType;
            this.ProviderKey = providerKey;
            this.RustHolonType = rustHolonType;
            Initialize(holoNETClient);
        }

        public CelestialBody(string holochainConductorURI, HoloNETClientType type, string providerKey, string rustCelestialBodyType, string rustHolonType)
        {
            this.RustCelestialBodyType = rustCelestialBodyType;
            this.ProviderKey = providerKey;
            this.RustHolonType = rustHolonType;
            Initialize(holochainConductorURI, type);
        }
        */

        /*
        public PlanetBase(HoloNETClientBase holoNETClient, Guid id, string coreProviderKey)
        {
            this.ProviderKey = coreProviderKey;
            Initialize(id, holoNETClient);
        }

        public PlanetBase(string holochainConductorURI, HoloNETClientType type, Guid id, string coreProviderKey)
        {
            this.ProviderKey = coreProviderKey;
            Initialize(id, holochainConductorURI, type);
        }

        public PlanetBase(HoloNETClientBase holoNETClient, string coreProviderKey)
        {
            this.ProviderKey = coreProviderKey;
            Initialize(holoNETClient);
        }

        public PlanetBase(string holochainConductorURI, HoloNETClientType type, string coreProviderKey)
        {
            this.ProviderKey = coreProviderKey;
            Initialize(holochainConductorURI, type);
        }*/

        public void LoadAll()
        {
          //  LoadHolons();
            LoadZomes();
        }

        public async Task<bool> Save()
        {
            //TODO: Save Zomes/Holons added to collections here...
            //TODO: Better if we can pass in collections rather than saving one at a time...

            //TODO: Need to save the planet holon itself so we can get its anchor address (we can use later to load its collections of zomes/holons).

            if (Id == Guid.Empty)
                Id = Guid.NewGuid();

            //Just in case the zomes/holons have been added since the planet was last saved.
            foreach (Zome zome in Zomes)
            {
                zome.CelestialBody = this;
                zome.Parent = this;

                // TODO: Need to sort this.Holons collection too (this is a list of ALL holons that belong to ALL zomes for this planet.
                // So the same holon will be in both collections, just that this.Holons has been flatterned. Why it's Fractal Holonic! ;-)
                foreach (Holon holon in zome.Holons)
                {
                    holon.Parent = zome;
                    holon.CelestialBody = this;
                }
            }

            //await PlanetCore.SaveHolonAsync(new Holon() { Id = this.Id, Name = this.Name, Description = this.Description, HolonType = HolonType.Planet });
            await CelestialBodyCore.SaveCelestialBodyAsync(new Holon() { Id = this.Id, Name = this.Name, Description = this.Description, HolonType = HolonType.Planet });
            return true;
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
            return Star.Flare(this);
        }

        // Activate & Launch - Launch & activate the planet (OAPP) by shining the star's light upon it...
        public void Shine()
        {
            Star.Shine(this);
        }

        // Deactivate the planet (OAPP)
        public void Dim()
        {
            Star.Dim(this);
        }

        // Deploy the planet (OAPP)
        public void Seed()
        {
            Star.Seed(this);
        }

        // Run Tests
        public void Twinkle()
        {
            Star.Twinkle(this);
        }

        // Highlight the Planet (OAPP) in the OAPP Store (StarNET). *Admin Only*
        public void Radiate()
        {
            Star.Radiate(this);
        }

        // Show how much light the planet (OAPP) is emitting into the solar system (StarNET/HoloNET)
        public void Emit()
        {
            Star.Emit(this);
        }

        // Show stats of the Planet (OAPP).
        public void Reflect()
        {
            Star.Reflect(this);
        }

        // Upgrade/update a Planet (OAPP).
        public void Evolve()
        {
            Star.Evolve(this);
        }

        // Import/Export hApp, dApp & others.
        public void Mutate()
        {
            Star.Mutate(this);
        }

        // Send/Receive Love
        public void Love()
        {
            Star.Love(this);
        }

        // Reserved For Future Use...
        public void Super()
        {
            Star.Super(this);
        }

        private void PlanetCore_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private async void PlanetCore_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            foreach (ZomeBase zome in Zomes)
            {
                await zome.Initialize(zome.Name, this.HoloNETClient);
                zome.OnHolonLoaded += Zome_OnHolonLoaded;
                zome.OnHolonSaved += Zome_OnHolonSaved;
            }

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
            Zomes = CelestialBodyCore.LoadZomes();
        }

        /*
        public async void LoadHolons()
        {
            Holons = await CelestialBodyCore.LoadHolons();
        }*/

        public async Task Initialize(string holochainConductorURI, HoloNETClientType type)
        {
            switch (type)
            {
                case HoloNETClientType.Desktop:
                    this.HoloNETClient = new Client.Desktop.HoloNETClient(holochainConductorURI);
                    break;

                case HoloNETClientType.Unity:
                    this.HoloNETClient = new Client.Unity.HoloNETClient(holochainConductorURI);
                    break;
            }

            await Initialize(this.HoloNETClient);
        }

        //TODO: What use case is the Guid Id used for when we have Provider Key?
        //public async Task Initialize(Guid id, string holochainConductorURI, HoloNETClientType type)
        //{
        //   // this.Id = id;
        //    await Initialize(holochainConductorURI, type);
        //}

        //public async Task Initialize(Guid id, HoloNETClientBase holoNETClient)
        //{
        //   // this.Id = id;
        //    await Initialize(holoNETClient);
        //}

        public async Task Initialize(HoloNETClientBase holoNETClient)
        {
            HoloNETClient = holoNETClient;
            this.Zomes = new List<IZome>();
           // this.Holons = new List<IHolon>();

            switch (this.GenesisType)
            {
                case GenesisType.Planet:
                    CelestialBodyCore = new PlanetCore(holoNETClient);
                    break;

                case GenesisType.Moon:
                    CelestialBodyCore = new MoonCore(holoNETClient);
                    break;

                case GenesisType.Star:
                    CelestialBodyCore = new StarCore(holoNETClient);
                    break;
            }

            if (!string.IsNullOrEmpty(this.ProviderKey))
            {
                CelestialBodyCore.ProviderKey = this.ProviderKey; //_coreProviderKey = hc anchor.
                await LoadCelestialBody();

                //TODO: Load the planets Zome collection here? Or is it passed in from the sub-class implementation? Probably 2nd one... ;-)
                //No we load the holons and zomes linked to the planetcore zome via the coreProviderKey anchor...
                LoadZomes();
               // LoadHolons();
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
            HoloNETClient.OnConnected += HoloNETClient_OnConnected;
            HoloNETClient.OnDisconnected += HoloNETClient_OnDisconnected;
          //  HoloNETClient.OnError += HoloNETClient_OnError;
            HoloNETClient.OnDataReceived += HoloNETClient_OnDataReceived;
            HoloNETClient.OnGetInstancesCallBack += HoloNETClient_OnGetInstancesCallBack;
            HoloNETClient.OnSignalsCallBack += HoloNETClient_OnSignalsCallBack;
            HoloNETClient.OnZomeFunctionCallBack += HoloNETClient_OnZomeFunctionCallBack;

            CelestialBodyCore.OnHolonsLoaded += PlanetCore_OnHolonsLoaded;
            CelestialBodyCore.OnZomesLoaded += PlanetCore_OnZomesLoaded;
            CelestialBodyCore.OnHolonSaved += PlanetCore_OnHolonSaved;
            CelestialBodyCore.OnZomeError += PlanetCore_OnZomeError;
        }

        private async void PlanetCore_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            if (e.Holon.HolonType == HolonType.Planet)
            {
                // This is the hc Address of the planet (we can use this as the anchor/coreProviderKey to load all future zomes/holons belonging to this planet).
                this.ProviderKey = e.Holon.ProviderKey;

                //TODO: Does hc/rust save this address in the holon object? YES! :)

                //If we have just saved the planet then we need to now save it's zomes and holons.
                foreach (ZomeBase zome in Zomes)
                {
                    //TODO: Do we want to save all the planets zomes and holons everytime the planet is saved/updated? Should be option to specify SaveZomesAndHolons = true in Save method();
                    if (zome.Id == Guid.Empty)
                        zome.Id = Guid.NewGuid();

                    zome.Parent = e.Holon;
                    zome.CelestialBody = this;

                    await zome.SaveHolonAsync(this.RustHolonType, zome);
                }
            }
        }

        private void Zome_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            bool holonFound = false;

            foreach (ZomeBase zome in Zomes)
            {
                foreach (Holon holon in zome.Holons)
                {
                    if (holon.Name == e.Holon.Name)
                    {
                        holonFound = true;
                        break;
                    }
                }
            }

            // If the zome or holon is not stored in the cache yet then add it now...
            // Currently the collection will fill up as the individual loads each holon.
            // They can call the LoadAll function to load all Holons and Zomes linked to this Planet (OAPP).

            //TODO: Now all zomes and holons belonging to a planet (OAPP) are loaded in init method using hc anchor pattern.
            //Maybe it can be a setting to choose between lazy loading (loading only as needed) or to prefetch and load everything up front.
            //Pros and Cons to both methods, Lazy loading = quicker init load time and less memory but then if you start loading lots of zomes/holons after, that's a lot more network traffic, etc.
            //Loading up front- Longer init load time and uses more memory but then all data cached so no more loading or network traffic needed.

            if (!holonFound)
            {
                IZome zome = Zomes.FirstOrDefault(x => x.Parent.Name == e.Holon.Parent.Name);

                if (zome == null)
                    Zomes.Add(new Zome(HoloNETClient, e.Holon.Parent.Name));

                ((ZomeBase)zome).Holons.Add((Holon)e.Holon);
            }

            OnHolonLoaded?.Invoke(this, e);
        }

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
                    zome.Parent = e.Holon;
                    zome.CelestialBody = this;

                    foreach (Holon holon in GetHolonsThatBelongToZome(zome))
                        zome.SaveHolonAsync(this.RustHolonType, holon);
                    // }
                }
            }
            else
            {
                IHolon holon = Holons.FirstOrDefault(x => x.Id == e.Holon.Id);

                //TODO: Come back to this... Wouldn't parent already be set? Same for zomes? Need to check...
                if (holon != null)
                {
                    holon.ProviderKey = e.Holon.ProviderKey;
                    //holon.Parent = e.Holon;
                    holon.CelestialBody = this;
                }
            }

            OnHolonSaved?.Invoke(this, e);
        }

        private Zome GetZomeThatHolonBelongsTo(Holon holon)
        {
            return (Zome)Holons.FirstOrDefault(x => x.Id == holon.Id).Parent;
        }

        private List<IHolon> GetHolonsThatBelongToZome(IZome zome)
        {
            return Holons.Where(x => x.Parent.Id == zome.Id).ToList();
        }

        private IZome GetZomeByName(string name)
        {
            return Zomes.FirstOrDefault(x => x.Name == name);
        }

        private IZome GetZomeById(Guid id)
        {
            return Zomes.FirstOrDefault(x => x.Id == id);
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
        }
    }
}
