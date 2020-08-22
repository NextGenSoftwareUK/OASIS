using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.Holochain.HoloNET.HDK.Core;
using System.Collections.Generic;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core.CSharpTemplates
{
    // TODO: Move generic parts like HoloNETClient, error handling, etc into HolochainBaseZome
    // Update NETHDK to generate serperate partial classes for each class defined in proxyClasses.
    // Nearly there! ;-)

    public partial class MyZome : HolochainBaseZome
    {
        public MyZome(HoloNETClientBase holoNETClient) : base(holoNETClient, "my_zome", new List<string> { "class_list" })
        {

        }

        public MyZome(string holochainConductorURI, HoloNETClientType type) : base(holochainConductorURI, "my_zome", type, new List<string> { "class_list" })
        {

        }

        /*
        private const string LOAD_MYCLASS_FUNC = "load_my_class";
        private const string SAVE_MYCLASS_FUNC = "save_my_class";

        private Dictionary<string, MyClass> _savingMyClasss = new Dictionary<string, MyClass>();
        private TaskCompletionSource<MyClass> _taskCompletionSourceLoadMyClass = new TaskCompletionSource<MyClass>();
        private TaskCompletionSource<MyClass> _taskCompletionSourceSaveMyClass = new TaskCompletionSource<MyClass>();

        public delegate void MyClassSaved(object sender, HolochainBaseDataObjectLoadedEventArgs e);
        public event MyClassSaved OnMyClassSaved;

        public delegate void MyClassLoaded(object sender, HolochainBaseDataObjectLoadedEventArgs e);
        public event MyClassLoaded OnMyClassLoaded;

        private void MyZome_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
        {
            if (!e.IsCallSuccessful)
                HandleError(string.Concat("Zome function ", e.ZomeFunction, " on zome ", e.Zome, " returned an error. Error Details: ", e.ZomeReturnData), null, null);
            else
            {
                switch (e.ZomeFunction)
                {
                    case LOAD_MYCLASS_FUNC:
                    {
                        MyClass myClass = JsonConvert.DeserializeObject<MyClass>(string.Concat("{", e.ZomeReturnData, "}"));
                        OnMyClassLoaded?.Invoke(this, new HolochainBaseDataObjectLoadedEventArgs { HolochainBaseDataObject = myClass });
                        _taskCompletionSourceLoadMyClass.SetResult(myClass);
                    }
                    break;

                    case SAVE_MYCLASS_FUNC:
                    {
                        _savingMyClasss[e.Id].HcAddressHash = e.ZomeReturnData;

                        OnMyClassSaved?.Invoke(this, new HolochainBaseDataObjectLoadedEventArgs { HolochainBaseDataObject = _savingMyClasss[e.Id] });
                        _taskCompletionSourceSaveMyClass.SetResult(_savingMyClasss[e.Id]);
                        _savingMyClasss.Remove(e.Id);
                    }
                    break;
                }
            }
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
        */
    }
}
