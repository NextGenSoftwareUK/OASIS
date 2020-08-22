
using Newtonsoft.Json;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public abstract class HolochainBaseZome
    {
        protected int _currentId = 0;
        protected string _hcinstance;
        protected TaskCompletionSource<string> _taskCompletionSourceGetInstance = new TaskCompletionSource<string>();
        private Dictionary<string, IHolochainBaseDataObject> _savingHolochainBaseDataObjects = new Dictionary<string, IHolochainBaseDataObject>();
        private TaskCompletionSource<IHolochainBaseDataObject> _taskCompletionSourceLoadHolochainBaseDataObject = new TaskCompletionSource<IHolochainBaseDataObject>();
        private TaskCompletionSource<IHolochainBaseDataObject> _taskCompletionSourceSaveHolochainBaseDataObject = new TaskCompletionSource<IHolochainBaseDataObject>();

        public delegate void HolochainBaseDataObjectSaved(object sender, HolochainBaseDataObjectLoadedEventArgs e);
        public event HolochainBaseDataObjectSaved OnHolochainBaseDataObjectSaved;

        public delegate void HolochainBaseDataObjectLoaded(object sender, HolochainBaseDataObjectLoadedEventArgs e);
        public event HolochainBaseDataObjectLoaded OnHolochainBaseDataObjectLoaded;

        private List<string> _loadFuncNames = new List<string>();
        private List<string> _saveFuncNames = new List<string>();

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

        public string ZomeName { get; set; }

        public HoloNETClientBase HoloNETClient { get; private set; }

        public enum HoloNETClientType
        {
            Desktop,
            Unity
        }

        //TODO: Use only for Proxy classes (not sure to do this way?) Revisit later...
        //public HolochainBaseZome()
        //{
            
        //}

        public HolochainBaseZome(HoloNETClientBase holoNETClient, string zomeName, List<string> holochainDataObjectNames)
        {
            Initialize(zomeName, holoNETClient);

            for (int i = 0; i < holochainDataObjectNames.Count; i++)
            {
                _loadFuncNames[0] = string.Concat("create_", holochainDataObjectNames[i]);
                _saveFuncNames[1] = string.Concat("read_", holochainDataObjectNames[i]);
                _saveFuncNames[2] = string.Concat("update_", holochainDataObjectNames[i]);
                _saveFuncNames[3] = string.Concat("delete_", holochainDataObjectNames[i]);
            }
        }

        public HolochainBaseZome(string holochainConductorURI, string zomeName, HoloNETClientType type, List<string> holochainDataObjectNames)
        {
            Initialize(zomeName, holochainConductorURI, type);

            for (int i = 0; i < holochainDataObjectNames.Count; i++)
            {
                _loadFuncNames[0] = string.Concat("create_", holochainDataObjectNames[i]);
                _saveFuncNames[1] = string.Concat("read_", holochainDataObjectNames[i]);
                _saveFuncNames[2] = string.Concat("update_", holochainDataObjectNames[i]);
                _saveFuncNames[3] = string.Concat("delete_", holochainDataObjectNames[i]);
            }
        }

        public HolochainBaseZome(HoloNETClientBase holoNETClient, string zomeName, string loadFuncName, string saveFuncName)
        {
            Initialize(zomeName, holoNETClient);

            _loadFuncNames[0] = loadFuncName;
            _saveFuncNames[0] = saveFuncName;
        }

        public HolochainBaseZome(HoloNETClientBase holoNETClient, string zomeName, List<string> loadFuncNames, List<string> saveFuncNames)
        {
            Initialize(zomeName, holoNETClient);

            _loadFuncNames = loadFuncNames;
            _saveFuncNames = saveFuncNames;
        }

        public HolochainBaseZome(string holochainConductorURI, HoloNETClientType type, string zomeName, string loadFuncName, string saveFuncName)
        {
            Initialize(zomeName, holochainConductorURI, type);

            _loadFuncNames[0] = loadFuncName;
            _saveFuncNames[0] = saveFuncName;
        }

        public HolochainBaseZome(string holochainConductorURI, HoloNETClientType type, string zomeName, List<string> loadFuncNames, List<string> saveFuncNames)
        {
            Initialize(zomeName, holochainConductorURI, type);

            _loadFuncNames = loadFuncNames;
            _saveFuncNames = saveFuncNames;
        }

        public async Task Initialize(string zomeName, HoloNETClientBase holoNETClient)
        {
            ZomeName = zomeName;
            HoloNETClient = holoNETClient;
            await WireUpEvents();
        }

        public async Task Initialize(string zomeName, string holochainConductorURI, HoloNETClientType type)
        {
            ZomeName = zomeName;

            switch (type)
            {
                case HoloNETClientType.Desktop:
                    this.HoloNETClient = new Client.Desktop.HoloNETClient(holochainConductorURI);
                    break;

                case HoloNETClientType.Unity:
                    this.HoloNETClient = new Client.Unity.HoloNETClient(holochainConductorURI);
                    break;
            }

            await WireUpEvents();
 
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
                for (int i = 0; i < _loadFuncNames.Count; i++)
                {
                    if (e.ZomeFunction == _loadFuncNames[i])
                    {
                        IHolochainBaseDataObject hcObject = (IHolochainBaseDataObject)JsonConvert.DeserializeObject<HolochainBaseDataObject>(string.Concat("{", e.ZomeReturnData, "}"));
                        OnHolochainBaseDataObjectLoaded?.Invoke(this, new HolochainBaseDataObjectLoadedEventArgs { HolochainBaseDataObject = hcObject });
                        _taskCompletionSourceLoadHolochainBaseDataObject.SetResult(hcObject);
                    }
                    else if (e.ZomeFunction == _saveFuncNames[i])
                    {
                        _savingHolochainBaseDataObjects[e.Id].HcAddressHash = e.ZomeReturnData;

                        OnHolochainBaseDataObjectSaved?.Invoke(this, new HolochainBaseDataObjectLoadedEventArgs { HolochainBaseDataObject = _savingHolochainBaseDataObjects[e.Id] });
                        _taskCompletionSourceSaveHolochainBaseDataObject.SetResult(_savingHolochainBaseDataObjects[e.Id]);
                        _savingHolochainBaseDataObjects.Remove(e.Id);
                    }
                }

                /*
                switch (e.ZomeFunction)
                {
                    case LoadFuncName:
                    {
                        IHolochainBaseDataObject hcObject = (IHolochainBaseDataObject)JsonConvert.DeserializeObject<HolochainBaseDataObject>(string.Concat("{", e.ZomeReturnData, "}"));
                        OnHolochainBaseDataObjectLoaded?.Invoke(this, new HolochainBaseDataObjectLoadedEventArgs { HolochainBaseDataObject = hcObject });
                        _taskCompletionSourceLoadHolochainBaseDataObject.SetResult(hcObject);
                    }
                    break;

                    case SAVE_HOLOCHAINDATAOBJECT_FUNC:
                    {
                        _savingHolochainBaseDataObjects[e.Id].HcAddressHash = e.ZomeReturnData;

                        OnHolochainBaseDataObjectSaved?.Invoke(this, new HolochainBaseDataObjectLoadedEventArgs { HolochainBaseDataObject = _savingHolochainBaseDataObjects[e.Id] });
                        _taskCompletionSourceSaveHolochainBaseDataObject.SetResult(_savingHolochainBaseDataObjects[e.Id]);
                        _savingHolochainBaseDataObjects.Remove(e.Id);
                    }
                    break;
                }*/
            }
        }

        public async Task<IHolochainBaseDataObject> LoadHolochainBaseDataObjectAsync(string hcEntryAddressHash)
        {
            await _taskCompletionSourceGetInstance.Task;

            for (int i= 0; i < _loadFuncNames.Count; i++)
            {
                if (_loadFuncNames[i].Contains("load"))
                {
                    if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
                    {
                        await HoloNETClient.CallZomeFunctionAsync(_hcinstance, ZomeName, _loadFuncNames[i], new { address = hcEntryAddressHash });
                        return await _taskCompletionSourceLoadHolochainBaseDataObject.Task;
                    }
                }
            }

            return null;
        }

        public async Task<IHolochainBaseDataObject> LoadHolochainBaseDataObjectAsync(string loadFuncName, string hcEntryAddressHash)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, ZomeName, loadFuncName, new { address = hcEntryAddressHash });
                return await _taskCompletionSourceLoadHolochainBaseDataObject.Task;
            }

            return null;
        }

        //public async Task<IHolochainBaseDataObject> LoadMyClassAsync(Guid id)
        //{
        //    await _taskCompletionSourceGetInstance.Task;

        //    if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
        //    {
        //        //TODO: Implement in HC/Rust
        //        await HoloNETClient.CallZomeFunctionAsync(_hcinstance, MYZOME_ZOME, LOAD_MYCLASS_FUNC, new { id });
        //        return await _taskCompletionSourceLoadHolochainBaseDataObject.Task;
        //    }

        //    return null;
        //}

        //public async Task<IHolochainBaseDataObject> LoadMyClassAsync(string username, string password)
        //{
        //    await _taskCompletionSourceGetInstance.Task;

        //    if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
        //    {
        //        //TODO: Implement in HC/Rust
        //        //await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_MyClass_FUNC, new { username, password });

        //        //TODO: TEMP HARDCODED JUST TO TEST WITH!
        //        await HoloNETClient.CallZomeFunctionAsync(_hcinstance, MYZOME_ZOME, LOAD_MYCLASS_FUNC, new { address = "QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg" });
        //        return await _taskCompletionSourceLoadHolochainBaseDataObject.Task;
        //    }

        //    return null;
        //}

        public async Task<IHolochainBaseDataObject> SaveMyClassAsync(string saveFuncName, IHolochainBaseDataObject hcObject)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                // Rust/HC does not like null strings so need to set to empty string.
                if (hcObject.HcAddressHash == null)
                    hcObject.HcAddressHash = string.Empty;

                _currentId++;
                _savingHolochainBaseDataObjects[_currentId.ToString()] = hcObject;

                await HoloNETClient.CallZomeFunctionAsync(_currentId.ToString(), _hcinstance, ZomeName, saveFuncName, new { entry = hcObject });
                return await _taskCompletionSourceSaveHolochainBaseDataObject.Task;
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
