using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public abstract class PlanetBase : Holon, IPlanet
    {
        protected int _currentId = 0;
        protected string _hcinstance;
        protected TaskCompletionSource<string> _taskCompletionSourceGetInstance = new TaskCompletionSource<string>();

        public PlanetBase()
        {

        }

        public PlanetBase(HoloNETClientBase holoNETClient, Guid id)
        {
            Initialize(id, holoNETClient);
        }

        public PlanetBase(string holochainConductorURI, HoloNETClientType type, Guid id)
        {
            Initialize(id, holochainConductorURI, type);
        }

        public PlanetBase(HoloNETClientBase holoNETClient)
        {
            Initialize(holoNETClient);
        }

        public PlanetBase(string holochainConductorURI, HoloNETClientType type)
        {
            Initialize(holochainConductorURI, type);
        }

        public delegate void HolonSaved(object sender, HolonLoadedEventArgs e);
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

        public enum HoloNETClientType
        {
            Desktop,
            Unity
        }

        public List<ZomeBase> Zomes = new List<ZomeBase>();

        public void Load()
        {
            //TODO: Load here using Anchor/Catalog pattern.

            //foreach (ZomeBase zome in Zomes)
            //{
            //    //TODO: Need to check if any state has changed and only save if it has...
            //    zome.LoadHolonAsync(zome,);

            //    foreach (Holon holon in zome.Holons)
            //    {
            //        //TODO: Need to check if any state has changed and only save if it has...
            //        zome.LoadHolonAsync(holon);
            //    }
            //}

            //return true;
        }

        public bool Save()
        {
            //TODO: Save Zomes/Holons added to collections here...
            //TODO: Better if we can pass in collections rather than saving one at a time...

            foreach (ZomeBase zome in Zomes)
            {
                //TODO: Need to check if any state has changed and only save if it has...
                zome.SaveHolonAsync(zome);

                foreach (Holon holon in zome.Holons)
                {
                    //TODO: Need to check if any state has changed and only save if it has...
                    zome.SaveHolonAsync(holon);
                }
            }
            
            return true;
        }

        // Build
        public void Light()
        {
            Star.Light(this);
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

        public async Task Initialize(HoloNETClientBase holoNETClient)
        {
            this.HolonType = HolonType.Planet;
            HoloNETClient = holoNETClient;

            //TODO: Load the planets Zome collection here? Or is it passed in from the sub-class implementation? Probably 2nd one... ;-)
            LoadZomes();

            foreach (ZomeBase zome in Zomes)
                await zome.Initialize(zome.Name, holoNETClient);

            await WireUpEvents();
        }

        private void LoadZomes()
        {

        }

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

        public async Task Initialize(Guid id, string holochainConductorURI, HoloNETClientType type)
        {
            this.Id = id;
            await Initialize(holochainConductorURI, type);
        }

        public async Task Initialize(Guid id, HoloNETClientBase holoNETClient)
        {
            this.Id = id;
            await Initialize(holoNETClient);
        }

        private async Task WireUpEvents()
        {
            HoloNETClient.OnConnected += HoloNETClient_OnConnected;
            HoloNETClient.OnDisconnected += HoloNETClient_OnDisconnected;
            HoloNETClient.OnError += HoloNETClient_OnError;
            HoloNETClient.OnDataReceived += HoloNETClient_OnDataReceived;
            HoloNETClient.OnGetInstancesCallBack += HoloNETClient_OnGetInstancesCallBack;
            HoloNETClient.OnSignalsCallBack += HoloNETClient_OnSignalsCallBack;
            HoloNETClient.OnZomeFunctionCallBack += HoloNETClient_OnZomeFunctionCallBack;

            foreach (ZomeBase zome in this.Zomes)
            {
                zome.OnHolonLoaded += Zome_OnHolonLoaded;
                zome.OnHolonSaved += Zome_OnHolonSaved;
            }

          //  OnHolonLoaded += PlanetBase_OnHolonLoaded;
           // OnHolonSaved += PlanetBase_OnHolonSaved;
            
            // HoloNETClient.Config.AutoStartConductor = true;
            //  HoloNETClient.Config.AutoShutdownConductor = true;
            //  HoloNETClient.Config.FullPathToExternalHolochainConductor = string.Concat(Directory.GetCurrentDirectory(), "\\hc.exe");
            //   HoloNETClient.Config.FullPathToHolochainAppDNA = string.Concat(Directory.GetCurrentDirectory(), "\\our_world\\dist\\our_world.dna.json"); 

            //await HoloNETClient.Connect();
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
            if (!holonFound)
            {
                ZomeBase zome = Zomes.FirstOrDefault(x => x.Parent.Name == e.Holon.Parent.Name);

                if (zome == null)
                    Zomes.Add(new Zome(HoloNETClient, e.Holon.Parent.Name));

                zome.Holons.Add((Holon)e.Holon);
            }

            OnHolonLoaded?.Invoke(this, e);
        }

        private void Zome_OnHolonSaved(object sender, HolonLoadedEventArgs e)
        {
            OnHolonSaved?.Invoke(this, e);
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

        public virtual async Task<IHolon> LoadHolonAsync(string holonName, string hcEntryAddressHash)
        {
            foreach (ZomeBase zome in Zomes)
            {
                foreach (Holon holon in zome.Holons)
                {
                    if (holon.Name == holonName)
                        return await zome.LoadHolonAsync(holonName, hcEntryAddressHash);
                }
            }

            return null;
        }

        public virtual async Task<IHolon> SaveHolonAsync(IHolon savingHolon)
        {
            foreach (ZomeBase zome in Zomes)
            {
                foreach (Holon holon in zome.Holons)
                {
                    if (holon.Name == savingHolon.Name)
                        return await zome.SaveHolonAsync(savingHolon);
                }
            }

            return null;
        }

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
