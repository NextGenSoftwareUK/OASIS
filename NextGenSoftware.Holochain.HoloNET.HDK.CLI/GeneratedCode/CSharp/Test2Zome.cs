using Newtonsoft.Json;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core.CSharpTemplates
{
    public class Test2Zome : HolochainBaseZome
    {
        private const string TEST2ZOME_ZOME = "test2_zome_core";
        private const string LOAD_TEST2_FUNC = "load_test2";
        private const string SAVE_TEST2_FUNC = "save_test2";

        private int _currentId = 0;
        private Dictionary<string, Test2> _savingTest2s = new Dictionary<string, Test2>();
        private string _hcinstance;
        private TaskCompletionSource<Test2> _taskCompletionSourceLoadTest2 = new TaskCompletionSource<Test2>();
        private TaskCompletionSource<Test2> _taskCompletionSourceSaveTest2 = new TaskCompletionSource<Test2>();

        private TaskCompletionSource<string> _taskCompletionSourceGetInstance = new TaskCompletionSource<string>();

        // public event Test2Manager.StorageProviderError OnStorageProviderError;

        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        public delegate void Test2Saved(object sender, Test2SavedEventArgs e);
        public event Test2Saved OnTest2Saved;

        public delegate void Test2Loaded(object sender, Test2LoadedEventArgs e);
        public event Test2Loaded OnTest2Loaded;

        public delegate void Test2ZomeError(object sender, Test2ZomeErrorEventArgs e);
        public event Test2ZomeError OnTest2ZomeError;

        public HoloNETClientBase HoloNETClient { get; private set; }


        public Test2Zome(HoloNETClientBase holoNETClient)
        {
            this.HoloNETClient = holoNETClient;
            Initialize();
        }

        public async Task Initialize()
        {
            HoloNETClient.OnConnected += Test2Zome_OnConnected;
            HoloNETClient.OnDisconnected += Test2Zome_OnDisconnected;
            HoloNETClient.OnError += HoloNETClient_OnError;
            HoloNETClient.OnDataReceived += Test2Zome_OnDataReceived;
            HoloNETClient.OnGetInstancesCallBack += Test2Zome_OnGetInstancesCallBack;
            HoloNETClient.OnSignalsCallBack += Test2Zome_OnSignalsCallBack;
            HoloNETClient.OnZomeFunctionCallBack += Test2Zome_OnZomeFunctionCallBack;

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

        private void Test2Zome_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
        {
            if (!e.IsCallSuccessful)
                HandleError(string.Concat("Zome function ", e.ZomeFunction, " on zome ", e.Zome, " returned an error. Error Details: ", e.ZomeReturnData), null, null);
            //OnHoloOASISError?.Invoke(this, new ErrorEventArgs() { EndPoint = HoloNETClient.EndPoint, Reason = string.Concat("Zome function ", e.ZomeFunction, " on zome ", e.Zome, " returned an error. Error Details: ", e.ZomeReturnData) });
            else
            {
                switch (e.ZomeFunction)
                {
                    case LOAD_TEST2_FUNC:
                        //HcTest2 hcTest2 = JsonConvert.DeserializeObject<HcTest2>(string.Concat("{", e.ZomeReturnData, "}"));
                        //OnPlayerTest2Loaded?.Invoke(this, new Test2LoadedEventArgs { HcTest2 = hcTest2, Test2 = ConvertHcTest2ToTest2(hcTest2) });
                        //_taskCompletionSourceLoadTest2.SetResult(ConvertHcTest2ToTest2(JsonConvert.DeserializeObject<HcTest2>(string.Concat("{", e.ZomeReturnData, "}"))));

                        Test2 test2 = JsonConvert.DeserializeObject<Test2>(string.Concat("{", e.ZomeReturnData, "}"));
                        OnTest2Loaded?.Invoke(this, new Test2LoadedEventArgs { Test2 = test2 });
                        _taskCompletionSourceLoadTest2.SetResult(test2);
                        break;

                    case SAVE_TEST2_FUNC:
                        // TODO: Eventually want to return the Test2 object from HC with the HCAddressHash set when I work out how! ;-)
                        // The dictonary below can then be removed.

                        //if (string.IsNullOrEmpty(_savingTest2s[e.Id].HcAddressHash))
                        //{
                        //    //TODO: Forced to re-save the object with the address (wouldn't that create a new hash entry?!)
                        _savingTest2s[e.Id].HcAddressHash = e.ZomeReturnData;

                        //    SaveTest2Async(_savingTest2s[e.Id]);
                        //}
                        //else
                        //{

                        //Test2 myclass = ConvertHcTest2ToTest2(_savingTest2s[e.Id]);
                        //OnPlayerTest2Saved?.Invoke(this, new Test2SavedEventArgs { Test2 = myclass, HcTest2 = _savingTest2s[e.Id] });
                        //_taskCompletionSourceSaveTest2.SetResult(Test2);
                        //_savingTest2s.Remove(e.Id);

                        OnTest2Saved?.Invoke(this, new Test2SavedEventArgs { Test2 = _savingTest2s[e.Id] });
                        _taskCompletionSourceSaveTest2.SetResult(_savingTest2s[e.Id]);
                        _savingTest2s.Remove(e.Id);

                        //}

                        //TODO: Want to use these eventually so the async methods can return the results without having to use events/callbacks!
                        // _taskCompletionSourceITest2.SetResult(JsonConvert.DeserializeObject<ITest2>(e.ZomeReturnData));
                        break;
                }
            }
        }

        private void Test2Zome_OnSignalsCallBack(object sender, SignalsCallBackEventArgs e)
        {

        }

        private void Test2Zome_OnGetInstancesCallBack(object sender, GetInstancesCallBackEventArgs e)
        {
            _hcinstance = e.Instances[0];
            OnInitialized?.Invoke(this, new EventArgs());
            _taskCompletionSourceGetInstance.SetResult(_hcinstance);
        }

        private void Test2Zome_OnDisconnected(object sender, DisconnectedEventArgs e)
        {

        }

        private void Test2Zome_OnDataReceived(object sender, DataReceivedEventArgs e)
        {

        }

        private void Test2Zome_OnConnected(object sender, ConnectedEventArgs e)
        {
            HoloNETClient.GetHolochainInstancesAsync();
        }

        public async Task<ITest2> LoadTest2Async(string Test2EntryHash)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, TEST2ZOME_ZOME, LOAD_TEST2_FUNC, new { address = Test2EntryHash });
                return await _taskCompletionSourceLoadTest2.Task;
            }

            return null;
        }

        public async Task<ITest2> LoadTest2Async(Guid id)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Implement in HC/Rust
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, TEST2ZOME_ZOME, LOAD_TEST2_FUNC, new { id });
                return await _taskCompletionSourceLoadTest2.Task;
            }

            return null;
        }

        public async Task<ITest2> LoadTest2Async(string username, string password)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Implement in HC/Rust
                //await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_Test2_FUNC, new { username, password });

                //TODO: TEMP HARDCODED JUST TO TEST WITH!
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, TEST2ZOME_ZOME, LOAD_TEST2_FUNC, new { address = "QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg" });
                return await _taskCompletionSourceLoadTest2.Task;
            }

            return null;
        }

        public async Task<ITest2> SaveTest2Async(ITest2 test2)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                // Rust/HC does not like null strings so need to set to empty string.
                if (test2.HcAddressHash == null)
                    test2.HcAddressHash = string.Empty;               

                _currentId++;
                _savingTest2s[_currentId.ToString()] = (Test2)test2;

                await HoloNETClient.CallZomeFunctionAsync(_currentId.ToString(), _hcinstance, TEST2ZOME_ZOME, SAVE_TEST2_FUNC, new { entry = test2 });
                return await _taskCompletionSourceSaveTest2.Task;
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
            OnTest2ZomeError?.Invoke(this, new Test2ZomeErrorEventArgs() { EndPoint = HoloNETClient.EndPoint, Reason = reason, ErrorDetails = errorDetails, HoloNETErrorDetails = holoNETEventArgs });
        }
    }
}
