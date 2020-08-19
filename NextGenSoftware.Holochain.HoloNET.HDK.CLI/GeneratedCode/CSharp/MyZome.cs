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
        private const string LOAD_TEST_FUNC = "load_test";
        private const string SAVE_TEST_FUNC = "save_test";

        private int _currentId = 0;
        private Dictionary<string, Test> _savingTests = new Dictionary<string, Test>();
        private string _hcinstance;
        private TaskCompletionSource<Test> _taskCompletionSourceLoadTest = new TaskCompletionSource<Test>();
        private TaskCompletionSource<Test> _taskCompletionSourceSaveTest = new TaskCompletionSource<Test>();

        private TaskCompletionSource<string> _taskCompletionSourceGetInstance = new TaskCompletionSource<string>();

        // public event TestManager.StorageProviderError OnStorageProviderError;

        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        public delegate void TestSaved(object sender, TestSavedEventArgs e);
        public event TestSaved OnTestSaved;

        public delegate void TestLoaded(object sender, TestLoadedEventArgs e);
        public event TestLoaded OnTestLoaded;

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
                    case LOAD_TEST_FUNC:
                        //HcTest hcTest = JsonConvert.DeserializeObject<HcTest>(string.Concat("{", e.ZomeReturnData, "}"));
                        //OnPlayerTestLoaded?.Invoke(this, new TestLoadedEventArgs { HcTest = hcTest, Test = ConvertHcTestToTest(hcTest) });
                        //_taskCompletionSourceLoadTest.SetResult(ConvertHcTestToTest(JsonConvert.DeserializeObject<HcTest>(string.Concat("{", e.ZomeReturnData, "}"))));

                        Test test = JsonConvert.DeserializeObject<Test>(string.Concat("{", e.ZomeReturnData, "}"));
                        OnTestLoaded?.Invoke(this, new TestLoadedEventArgs { Test = test });
                        _taskCompletionSourceLoadTest.SetResult(test);
                        break;

                    case SAVE_TEST_FUNC:
                        // TODO: Eventually want to return the Test object from HC with the HCAddressHash set when I work out how! ;-)
                        // The dictonary below can then be removed.

                        //if (string.IsNullOrEmpty(_savingTests[e.Id].HcAddressHash))
                        //{
                        //    //TODO: Forced to re-save the object with the address (wouldn't that create a new hash entry?!)
                        _savingTests[e.Id].HcAddressHash = e.ZomeReturnData;

                        //    SaveTestAsync(_savingTests[e.Id]);
                        //}
                        //else
                        //{

                        //Test myclass = ConvertHcTestToTest(_savingTests[e.Id]);
                        //OnPlayerTestSaved?.Invoke(this, new TestSavedEventArgs { Test = myclass, HcTest = _savingTests[e.Id] });
                        //_taskCompletionSourceSaveTest.SetResult(Test);
                        //_savingTests.Remove(e.Id);

                        OnTestSaved?.Invoke(this, new TestSavedEventArgs { Test = _savingTests[e.Id] });
                        _taskCompletionSourceSaveTest.SetResult(_savingTests[e.Id]);
                        _savingTests.Remove(e.Id);

                        //}

                        //TODO: Want to use these eventually so the async methods can return the results without having to use events/callbacks!
                        // _taskCompletionSourceITest.SetResult(JsonConvert.DeserializeObject<ITest>(e.ZomeReturnData));
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

        public async Task<ITest> LoadTestAsync(string TestEntryHash)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, MYZOME_ZOME, LOAD_TEST_FUNC, new { address = TestEntryHash });
                return await _taskCompletionSourceLoadTest.Task;
            }

            return null;
        }

        public async Task<ITest> LoadTestAsync(Guid id)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Implement in HC/Rust
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, MYZOME_ZOME, LOAD_TEST_FUNC, new { id });
                return await _taskCompletionSourceLoadTest.Task;
            }

            return null;
        }

        public async Task<ITest> LoadTestAsync(string username, string password)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Implement in HC/Rust
                //await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_Test_FUNC, new { username, password });

                //TODO: TEMP HARDCODED JUST TO TEST WITH!
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, MYZOME_ZOME, LOAD_TEST_FUNC, new { address = "QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg" });
                return await _taskCompletionSourceLoadTest.Task;
            }

            return null;
        }

        public async Task<ITest> SaveTestAsync(ITest test)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                // Rust/HC does not like null strings so need to set to empty string.
                if (test.HcAddressHash == null)
                    test.HcAddressHash = string.Empty;               

                _currentId++;
                _savingTests[_currentId.ToString()] = (Test)test;

                await HoloNETClient.CallZomeFunctionAsync(_currentId.ToString(), _hcinstance, MYZOME_ZOME, SAVE_TEST_FUNC, new { entry = test });
                return await _taskCompletionSourceSaveTest.Task;
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
