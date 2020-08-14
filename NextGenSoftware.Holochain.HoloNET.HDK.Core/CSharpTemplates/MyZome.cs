using Newtonsoft.Json;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core.CSharpTemplates
{
    public class MyZome : HolochainBaseZome
    {
        private const string MYZOME_ZOME = "my_zome_core";
        private const string LOAD_MYCLASS_FUNC = "load_my_class";
        private const string SAVE_MYCLASS_FUNC = "save_my_class";

        private int _currentId = 0;
        private Dictionary<string, MyClass> _savingMyClasss = new Dictionary<string, MyClass>();
        private string _hcinstance;
        private TaskCompletionSource<MyClass> _taskCompletionSourceLoadMyClass = new TaskCompletionSource<MyClass>();
        private TaskCompletionSource<MyClass> _taskCompletionSourceSaveMyClass = new TaskCompletionSource<MyClass>();

        private TaskCompletionSource<string> _taskCompletionSourceGetInstance = new TaskCompletionSource<string>();

        // public event MyClassManager.StorageProviderError OnStorageProviderError;

        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        public delegate void MyClassSaved(object sender, MyClassSavedEventArgs e);
        public event MyClassSaved OnMyClassSaved;

        public delegate void MyClassLoaded(object sender, MyClassLoadedEventArgs e);
        public event MyClassLoaded OnMyClassLoaded;

        public delegate void MyZomeError(object sender, MyZomeErrorEventArgs e);
        public event MyZomeError OnMyZomeError;

        public HoloNETClientBase HoloNETClient { get; private set; }


        public MyZome(HoloNETClientBase holoNETClient)
        {
            this.HoloNETClient = holoNETClient;
            Initialize();
        }

        public async Task Initialize()
        {
            HoloNETClient.OnConnected += MyZome_OnConnected;
            HoloNETClient.OnDisconnected += MyZome_OnDisconnected;
            HoloNETClient.OnError += HoloNETClient_OnError;
            HoloNETClient.OnDataReceived += MyZome_OnDataReceived;
            HoloNETClient.OnGetInstancesCallBack += MyZome_OnGetInstancesCallBack;
            HoloNETClient.OnSignalsCallBack += MyZome_OnSignalsCallBack;
            HoloNETClient.OnZomeFunctionCallBack += MyZome_OnZomeFunctionCallBack;

            // HoloNETClient.Config.AutoStartConductor = true;
            //  HoloNETClient.Config.AutoShutdownConductor = true;
            //  HoloNETClient.Config.FullPathToExternalHolochainConductor = string.Concat(Directory.GetCurrentDirectory(), "\\hc.exe");
            //   HoloNETClient.Config.FullPathToHolochainAppDNA = string.Concat(Directory.GetCurrentDirectory(), "\\our_world\\dist\\our_world.dna.json"); 

            //await HoloNETClient.Connect();
        }

        private void HoloNETClient_OnError(object sender, HoloNETErrorEventArgs e)
        {
            HandleError("Error occured in HoloNET. See ErrorDetial for reason.", null, e);
            //OnHoloOASISError?.Invoke(this, new ErrorEventArgs { EndPoint = HoloNETClient.EndPoint, Reason = "Error occured in HoloNET. See ErrorDetial for reason.", HoloNETErrorDetails = e });
        }

        private void MyZome_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
        {
            if (!e.IsCallSuccessful)
                HandleError(string.Concat("Zome function ", e.ZomeFunction, " on zome ", e.Zome, " returned an error. Error Details: ", e.ZomeReturnData), null, null);
            //OnHoloOASISError?.Invoke(this, new ErrorEventArgs() { EndPoint = HoloNETClient.EndPoint, Reason = string.Concat("Zome function ", e.ZomeFunction, " on zome ", e.Zome, " returned an error. Error Details: ", e.ZomeReturnData) });
            else
            {
                switch (e.ZomeFunction)
                {
                    case LOAD_MYCLASS_FUNC:
                        //HcMyClass hcMyClass = JsonConvert.DeserializeObject<HcMyClass>(string.Concat("{", e.ZomeReturnData, "}"));
                        //OnPlayerMyClassLoaded?.Invoke(this, new MyClassLoadedEventArgs { HcMyClass = hcMyClass, MyClass = ConvertHcMyClassToMyClass(hcMyClass) });
                        //_taskCompletionSourceLoadMyClass.SetResult(ConvertHcMyClassToMyClass(JsonConvert.DeserializeObject<HcMyClass>(string.Concat("{", e.ZomeReturnData, "}"))));

                        MyClass myClass = JsonConvert.DeserializeObject<MyClass>(string.Concat("{", e.ZomeReturnData, "}"));
                        OnMyClassLoaded?.Invoke(this, new MyClassLoadedEventArgs { MyClass = myClass });
                        _taskCompletionSourceLoadMyClass.SetResult(myClass);
                        break;

                    case SAVE_MYCLASS_FUNC:
                        // TODO: Eventually want to return the MyClass object from HC with the HCAddressHash set when I work out how! ;-)
                        // The dictonary below can then be removed.

                        //if (string.IsNullOrEmpty(_savingMyClasss[e.Id].HcAddressHash))
                        //{
                        //    //TODO: Forced to re-save the object with the address (wouldn't that create a new hash entry?!)
                        _savingMyClasss[e.Id].HcAddressHash = e.ZomeReturnData;

                        //    SaveMyClassAsync(_savingMyClasss[e.Id]);
                        //}
                        //else
                        //{

                        //MyClass myclass = ConvertHcMyClassToMyClass(_savingMyClasss[e.Id]);
                        //OnPlayerMyClassSaved?.Invoke(this, new MyClassSavedEventArgs { MyClass = myclass, HcMyClass = _savingMyClasss[e.Id] });
                        //_taskCompletionSourceSaveMyClass.SetResult(MyClass);
                        //_savingMyClasss.Remove(e.Id);

                        OnMyClassSaved?.Invoke(this, new MyClassSavedEventArgs { MyClass = _savingMyClasss[e.Id] });
                        _taskCompletionSourceSaveMyClass.SetResult(_savingMyClasss[e.Id]);
                        _savingMyClasss.Remove(e.Id);

                        //}

                        //TODO: Want to use these eventually so the async methods can return the results without having to use events/callbacks!
                        // _taskCompletionSourceIMyClass.SetResult(JsonConvert.DeserializeObject<IMyClass>(e.ZomeReturnData));
                        break;
                }
            }
        }

        private void MyZome_OnSignalsCallBack(object sender, SignalsCallBackEventArgs e)
        {

        }

        private void MyZome_OnGetInstancesCallBack(object sender, GetInstancesCallBackEventArgs e)
        {
            _hcinstance = e.Instances[0];
            OnInitialized?.Invoke(this, new EventArgs());
            _taskCompletionSourceGetInstance.SetResult(_hcinstance);
        }

        private void MyZome_OnDisconnected(object sender, DisconnectedEventArgs e)
        {

        }

        private void MyZome_OnDataReceived(object sender, DataReceivedEventArgs e)
        {

        }

        private void MyZome_OnConnected(object sender, ConnectedEventArgs e)
        {
            HoloNETClient.GetHolochainInstancesAsync();
        }

        public async Task<IMyClass> LoadMyClassAsync(string MyClassEntryHash)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, MYZOME_ZOME, LOAD_MYCLASS_FUNC, new { address = MyClassEntryHash });
                return await _taskCompletionSourceLoadMyClass.Task;
            }

            return null;
        }

        public async Task<IMyClass> LoadMyClassAsync(Guid id)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Implement in HC/Rust
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, MYZOME_ZOME, LOAD_MYCLASS_FUNC, new { id });
                return await _taskCompletionSourceLoadMyClass.Task;
            }

            return null;
        }

        public async Task<IMyClass> LoadMyClassAsync(string username, string password)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Implement in HC/Rust
                //await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_MyClass_FUNC, new { username, password });

                //TODO: TEMP HARDCODED JUST TO TEST WITH!
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, MYZOME_ZOME, LOAD_MYCLASS_FUNC, new { address = "QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg" });
                return await _taskCompletionSourceLoadMyClass.Task;
            }

            return null;
        }

        public async Task<IMyClass> SaveMyClassAsync(IMyClass myClass)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                // Rust/HC does not like null strings so need to set to empty string.
                if (myClass.HcAddressHash == null)
                    myClass.HcAddressHash = string.Empty;               

                _currentId++;
                _savingMyClasss[_currentId.ToString()] = (MyClass)myClass;

                await HoloNETClient.CallZomeFunctionAsync(_currentId.ToString(), _hcinstance, MYZOME_ZOME, SAVE_MYCLASS_FUNC, new { entry = myClass });
                return await _taskCompletionSourceSaveMyClass.Task;
            }

            return null;
        }

        /// <summary>
        /// Handles any errors thrown by HoloNET or HoloOASIS. It fires the OnHoloOASISError error handler if there are any 
        /// subscriptions. The same applies to the OnStorageProviderError event implemented as part of the IOASISStorage interface.
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="errorDetails"></param>
        /// <param name="holoNETEventArgs"></param>
        private void HandleError(string reason, Exception errorDetails, HoloNETErrorEventArgs holoNETEventArgs)
        {
            OnMyZomeError?.Invoke(this, new MyZomeErrorEventArgs() { EndPoint = HoloNETClient.EndPoint, Reason = reason, ErrorDetails = errorDetails, HoloNETErrorDetails = holoNETEventArgs });
        }
    }
}
