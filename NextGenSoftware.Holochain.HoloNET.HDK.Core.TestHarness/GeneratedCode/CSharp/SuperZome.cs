using Newtonsoft.Json;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.Holochain.HoloNET.HDK.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.CLI.ProxyClasses
{
    // TODO: Move generic parts like HoloNETClient, error handling, etc into HolochainBaseZome
    // Update NETHDK to generate serperate partial classes for each class defined in proxyClasses.
    // Nearly there! ;-)

    public partial class SuperZome : HolochainBaseZome
    {
        public SuperZome(HoloNETClientBase holoNETClient) : base(holoNETClient, "super_zome", new List<string> { "SuperTest, SuperClass" })
        {

        }

        public SuperZome(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, "super_zome", type, new List<string> { "SuperTest, SuperClass" })
        {

        }

        /*
        private const string LOAD_SUPERTEST_FUNC = "load_super_test";
        private const string SAVE_SUPERTEST_FUNC = "save_super_test";

        private Dictionary<string, SuperTest> _savingSuperTests = new Dictionary<string, SuperTest>();
        private TaskCompletionSource<SuperTest> _taskCompletionSourceLoadSuperTest = new TaskCompletionSource<SuperTest>();
        private TaskCompletionSource<SuperTest> _taskCompletionSourceSaveSuperTest = new TaskCompletionSource<SuperTest>();

        public delegate void SuperTestSaved(object sender, HolochainBaseDataObjectLoadedEventArgs e);
        public event SuperTestSaved OnSuperTestSaved;

        public delegate void SuperTestLoaded(object sender, HolochainBaseDataObjectLoadedEventArgs e);
        public event SuperTestLoaded OnSuperTestLoaded;

        private void SuperZome_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
        {
            if (!e.IsCallSuccessful)
                HandleError(string.Concat("Zome function ", e.ZomeFunction, " on zome ", e.Zome, " returned an error. Error Details: ", e.ZomeReturnData), null, null);
            else
            {
                switch (e.ZomeFunction)
                {
                    case LOAD_SUPERTEST_FUNC:
                    {
                        SuperTest superTest = JsonConvert.DeserializeObject<SuperTest>(string.Concat("{", e.ZomeReturnData, "}"));
                        OnSuperTestLoaded?.Invoke(this, new HolochainBaseDataObjectLoadedEventArgs { HolochainBaseDataObject = superTest });
                        _taskCompletionSourceLoadSuperTest.SetResult(superTest);
                    }
                    break;

                    case SAVE_SUPERTEST_FUNC:
                    {
                        _savingSuperTests[e.Id].HcAddressHash = e.ZomeReturnData;

                        OnSuperTestSaved?.Invoke(this, new HolochainBaseDataObjectLoadedEventArgs { HolochainBaseDataObject = _savingSuperTests[e.Id] });
                        _taskCompletionSourceSaveSuperTest.SetResult(_savingSuperTests[e.Id]);
                        _savingSuperTests.Remove(e.Id);
                    }
                    break;
                }
            }
        }

        public async Task<ISuperTest> LoadSuperTestAsync(string SuperTestEntryHash)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, SUPERZOME_ZOME, LOAD_SUPERTEST_FUNC, new { address = SuperTestEntryHash });
                return await _taskCompletionSourceLoadSuperTest.Task;
            }

            return null;
        }

        public async Task<ISuperTest> LoadSuperTestAsync(Guid id)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Implement in HC/Rust
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, SUPERZOME_ZOME, LOAD_SUPERTEST_FUNC, new { id });
                return await _taskCompletionSourceLoadSuperTest.Task;
            }

            return null;
        }

        public async Task<ISuperTest> LoadSuperTestAsync(string username, string password)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Implement in HC/Rust
                //await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_SuperTest_FUNC, new { username, password });

                //TODO: TEMP HARDCODED JUST TO TEST WITH!
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, SUPERZOME_ZOME, LOAD_SUPERTEST_FUNC, new { address = "QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg" });
                return await _taskCompletionSourceLoadSuperTest.Task;
            }

            return null;
        }

        public async Task<ISuperTest> SaveSuperTestAsync(ISuperTest superTest)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                // Rust/HC does not like null strings so need to set to empty string.
                if (superTest.HcAddressHash == null)
                    superTest.HcAddressHash = string.Empty;               

                _currentId++;
                _savingSuperTests[_currentId.ToString()] = (SuperTest)superTest;

                await HoloNETClient.CallZomeFunctionAsync(_currentId.ToString(), _hcinstance, SUPERZOME_ZOME, SAVE_SUPERTEST_FUNC, new { entry = superTest });
                return await _taskCompletionSourceSaveSuperTest.Task;
            }

            return null;
        }
        */
    }
}
