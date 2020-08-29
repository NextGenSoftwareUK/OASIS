
using Newtonsoft.Json;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public abstract class ZomeBase: Holon, IZome
    {
        protected int _currentId = 0;
        protected string _hcinstance;
        protected TaskCompletionSource<string> _taskCompletionSourceGetInstance = new TaskCompletionSource<string>();
        private Dictionary<string, IHolon> _savingHolons = new Dictionary<string, IHolon>();
        private TaskCompletionSource<IHolon> _taskCompletionSourceLoadHolon = new TaskCompletionSource<IHolon>();
        private TaskCompletionSource<List<IHolon>> _taskCompletionSourceLoadHolons = new TaskCompletionSource<List<IHolon>>();
        private TaskCompletionSource<IHolon> _taskCompletionSourceSaveHolon = new TaskCompletionSource<IHolon>();

        //public List<HolonBase> Holons = new List<HolonBase>();
        public List<Holon> Holons = new List<Holon>();
        

        public delegate void HolonSaved(object sender, HolonLoadedEventArgs e);
        public event HolonSaved OnHolonSaved;

        public delegate void HolonLoaded(object sender, HolonLoadedEventArgs e);
        public event HolonLoaded OnHolonLoaded;

        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        public event HolonsLoaded OnHolonsLoaded;

        // private List<string> _loadFuncNames = new List<string>();
        //  private List<string> _saveFuncNames = new List<string>();

        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        public delegate void ZomeError(object sender, ZomeErrorEventArgs e);
        public event ZomeError OnZomeError;

        //TODO: Not sure if we want to expose the HoloNETClient events at this level? They can subscribe to them through the HoloNETClient property below...
        public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        public event Disconnected OnDisconnected;

        public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        public event DataReceived OnDataReceived;

        //TODO: If decide yes to above, finish passing through HoloNETClient events here...

      //  public string ZomeName { get; set; }

        public HoloNETClientBase HoloNETClient { get; private set; }

        //TODO: Use only for Proxy classes (not sure to do this way?) Revisit later...
        //public HolochainBaseZome()
        //{

        //}

      //  public ZomeBase(HoloNETClientBase holoNETClient, string zomeName, List<string> holochainDataObjectNames)
        public ZomeBase(HoloNETClientBase holoNETClient, string zomeName)
        {
            Initialize(zomeName, holoNETClient);

            //for (int i = 0; i < holochainDataObjectNames.Count; i++)
            //{
            //    _loadFuncNames[0] = string.Concat("create_", holochainDataObjectNames[i]);
            //    _saveFuncNames[1] = string.Concat("read_", holochainDataObjectNames[i]);
            //    _saveFuncNames[2] = string.Concat("update_", holochainDataObjectNames[i]);
            //    _saveFuncNames[3] = string.Concat("delete_", holochainDataObjectNames[i]);
            //}
        }

        //public ZomeBase(string holochainConductorURI, string zomeName, HoloNETClientType type, List<string> holochainDataObjectNames)
        public ZomeBase(string holochainConductorURI, string zomeName, HoloNETClientType type)
        {
            Initialize(zomeName, holochainConductorURI, type);

            //for (int i = 0; i < holochainDataObjectNames.Count; i++)
            //{
            //    _loadFuncNames[0] = string.Concat("create_", holochainDataObjectNames[i]);
            //    _saveFuncNames[1] = string.Concat("read_", holochainDataObjectNames[i]);
            //    _saveFuncNames[2] = string.Concat("update_", holochainDataObjectNames[i]);
            //    _saveFuncNames[3] = string.Concat("delete_", holochainDataObjectNames[i]);
            //}
        }


        //public ZomeBase(HoloNETClientBase holoNETClient, string zomeName, List<string> loadFuncNames, List<string> saveFuncNames)
        //public ZomeBase(HoloNETClientBase holoNETClient, string zomeName)
        //{
        //    Initialize(zomeName, holoNETClient);

        //    //_loadFuncNames = loadFuncNames;
        //    //_saveFuncNames = saveFuncNames;
        //}

        ////public ZomeBase(string holochainConductorURI, HoloNETClientType type, string zomeName, string loadFuncName, string saveFuncName)
        //public ZomeBase(string holochainConductorURI, HoloNETClientType type, string zomeName)
        //{
        //    Initialize(zomeName, holochainConductorURI, type);

        //    //_loadFuncNames[0] = loadFuncName;
        //    //_saveFuncNames[0] = saveFuncName;
        //}

        //public ZomeBase(string holochainConductorURI, HoloNETClientType type, string zomeName, List<string> loadFuncNames, List<string> saveFuncNames)
        //public ZomeBase(string holochainConductorURI, HoloNETClientType type, string zomeName)
        //{
        //    Initialize(zomeName, holochainConductorURI, type);

        //    //_loadFuncNames = loadFuncNames;
        //    //_saveFuncNames = saveFuncNames;
        //}

        public async Task Initialize(string zomeName, HoloNETClientBase holoNETClient)
        {
            this.Name = zomeName;
            this.HolonType = HolonType.Zome;

            //ZomeName = zomeName;
            HoloNETClient = holoNETClient;
            await WireUpEvents();
        }

        public async Task Initialize(string zomeName, string holochainConductorURI, HoloNETClientType type)
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

            await Initialize(zomeName, this.HoloNETClient);
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

            // HoloNETClient.Config.AutoStartConductor = true;
            //  HoloNETClient.Config.AutoShutdownConductor = true;
            //  HoloNETClient.Config.FullPathToExternalHolochainConductor = string.Concat(Directory.GetCurrentDirectory(), "\\hc.exe");
            //   HoloNETClient.Config.FullPathToHolochainAppDNA = string.Concat(Directory.GetCurrentDirectory(), "\\our_world\\dist\\our_world.dna.json"); 

            //await HoloNETClient.Connect();
        }

        private void HoloNETClient_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
        {
            if (!e.IsCallSuccessful)
                HandleError(string.Concat("Zome function ", e.ZomeFunction, " on zome ", e.Zome, " returned an error. Error Details: ", e.ZomeReturnData), null, null);
            else
            {
                /*
                for (int i = 0; i < _loadFuncNames.Count; i++)
                {
                    if (e.ZomeFunction == _loadFuncNames[i])
                    {
                        IHolon holon = (IHolon)JsonConvert.DeserializeObject<IHolon>(string.Concat("{", e.ZomeReturnData, "}"));
                        OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs { Holon = holon });
                        _taskCompletionSourceLoadHolon.SetResult(holon);
                    }
                    else if (e.ZomeFunction == _saveFuncNames[i])
                    {
                        _savingHolons[e.Id].ProviderKey = e.ZomeReturnData;

                        OnHolonSaved?.Invoke(this, new HolonLoadedEventArgs { Holon = _savingHolons[e.Id] });
                        _taskCompletionSourceSaveHolon.SetResult(_savingHolons[e.Id]);
                        _savingHolons.Remove(e.Id);
                    }
                }*/

                if (e.ZomeFunction.Contains("loadall"))
                {
                    List<IHolon> holons = (List<IHolon>)JsonConvert.DeserializeObject<List<IHolon>>(string.Concat("{", e.ZomeReturnData, "}"));
                    OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs { Holons = holons });
                    _taskCompletionSourceLoadHolons.SetResult(holons);
                }
                else if (e.ZomeFunction.Contains("load"))
                {
                    IHolon holon = (IHolon)JsonConvert.DeserializeObject<IHolon>(string.Concat("{", e.ZomeReturnData, "}"));
                    OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs { Holon = holon });
                    _taskCompletionSourceLoadHolon.SetResult(holon);
                }
                else if (e.ZomeFunction.Contains("save"))
                {
                    _savingHolons[e.Id].ProviderKey = e.ZomeReturnData;

                    OnHolonSaved?.Invoke(this, new HolonLoadedEventArgs { Holon = _savingHolons[e.Id] });
                    _taskCompletionSourceSaveHolon.SetResult(_savingHolons[e.Id]);
                    _savingHolons.Remove(e.Id);
                }
                
                /*
                switch (e.ZomeFunction)
                {
                    case LoadFuncName:
                    {
                        iHolon hcObject = (iHolon)JsonConvert.DeserializeObject<Holon>(string.Concat("{", e.ZomeReturnData, "}"));
                        OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs { Holon = hcObject });
                        _taskCompletionSourceLoadHolon.SetResult(hcObject);
                    }
                    break;

                    case SAVE_HOLOCHAINDATAOBJECT_FUNC:
                    {
                        _savingHolons[e.Id].HcAddressHash = e.ZomeReturnData;

                        OnHolonSaved?.Invoke(this, new HolonLoadedEventArgs { Holon = _savingHolons[e.Id] });
                        _taskCompletionSourceSaveHolon.SetResult(_savingHolons[e.Id]);
                        _savingHolons.Remove(e.Id);
                    }
                    break;
                }*/
            }
        }

        public virtual async Task<IHolon> LoadHolonAsync(string holonType, string hcEntryAddressHash)
        {
            await CallZomeFunctionAsync(string.Concat(holonType, "_load"), hcEntryAddressHash);
            return await _taskCompletionSourceLoadHolon.Task; //TODO: Look into this...
        }

        public virtual async Task<List<IHolon>> LoadHolonsAsync(string holonType, string hcAnchorAddressHash)
        {
            await CallZomeFunctionAsync(string.Concat(holonType, "_loadall"), hcAnchorAddressHash);
            return await _taskCompletionSourceLoadHolons.Task; //TODO: Look into this...
        }

        public virtual async Task<IHolon> SaveHolonAsync(string holonType, IHolon savingHolon)
        {
            CallZomeFunctionAsync(string.Concat(holonType, string.IsNullOrEmpty(savingHolon.ProviderKey) ? "_create" : "_update"), savingHolon);
            return await _taskCompletionSourceSaveHolon.Task; //TODO: Look into this?
        }

        public virtual async Task<IHolon> CallZomeFunctionAsync(string zomeFunctionName, IHolon holon)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                // Rust/HC does not like null strings so need to set to empty string.
                if (holon.ProviderKey == null)
                    holon.ProviderKey = string.Empty;

                //TODO: Not sure we need this anymore? Need to look into...
                _currentId++;
               // _savingHolons[_currentId.ToString()] = savingHolon;

                await HoloNETClient.CallZomeFunctionAsync(_currentId.ToString(), _hcinstance, zomeFunctionName, new { entry = holon });

                //TODO: Fix this
                // return await _taskCompletionSourceSaveHolon.Task;
            }

            return null;
        }

        public virtual async Task<IHolon> CallZomeFunctionAsync(string zomeFunctionName, string hcAnchorAddressHash)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Think may change loadall to list (to match rust conventions...) :)
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, this.Name, zomeFunctionName, new { address = hcAnchorAddressHash });
               
                //TODO: Fix this
                // return await _taskCompletionSourceLoadHolons.Task;
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
