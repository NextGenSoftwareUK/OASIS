
using Newtonsoft.Json;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public abstract class HolochainBaseZomeSingleDataObject : HolochainBaseZome
    {
        private Dictionary<string, IHolochainBaseDataObject> _savingHolochainBaseDataObjects = new Dictionary<string, IHolochainBaseDataObject>();
        private TaskCompletionSource<IHolochainBaseDataObject> _taskCompletionSourceLoadHolochainBaseDataObject = new TaskCompletionSource<IHolochainBaseDataObject>();
        private TaskCompletionSource<IHolochainBaseDataObject> _taskCompletionSourceSaveHolochainBaseDataObject = new TaskCompletionSource<IHolochainBaseDataObject>();

        public delegate void HolochainBaseDataObjectSaved(object sender, HolochainBaseDataObjectLoadedEventArgs e);
        public event HolochainBaseDataObjectSaved OnHolochainBaseDataObjectSaved;

        public delegate void HolochainBaseDataObjectLoaded(object sender, HolochainBaseDataObjectLoadedEventArgs e);
        public event HolochainBaseDataObjectLoaded OnHolochainBaseDataObjectLoaded;

        public string SaveFuncName { get; set; }
        public string LoadFuncName { get; set; }

        public HolochainBaseZomeSingleDataObject(HoloNETClientBase holoNETClient, string zomeName, string loadFuncName, string saveFuncName) : base(holoNETClient, zomeName)
        {
            this.LoadFuncName = loadFuncName;
            this.SaveFuncName = saveFuncName;
        }

        public HolochainBaseZomeSingleDataObject(string holochainConductorURI, HoloNETType type, string zomeName, string loadFuncName, string saveFuncName) : base(holochainConductorURI, type, zomeName)
        {
            this.LoadFuncName = loadFuncName;
            this.SaveFuncName = saveFuncName;
        }

        private void HoloNETClient_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
        {
            if (!e.IsCallSuccessful)
                HandleError(string.Concat("Zome function ", e.ZomeFunction, " on zome ", e.Zome, " returned an error. Error Details: ", e.ZomeReturnData), null, null);
            else
            {
                if (e.ZomeFunction == LoadFuncName)
                {
                    IHolochainBaseDataObject hcObject = (IHolochainBaseDataObject)JsonConvert.DeserializeObject<HolochainBaseDataObject>(string.Concat("{", e.ZomeReturnData, "}"));
                    OnHolochainBaseDataObjectLoaded?.Invoke(this, new HolochainBaseDataObjectLoadedEventArgs { HolochainBaseDataObject = hcObject });
                    _taskCompletionSourceLoadHolochainBaseDataObject.SetResult(hcObject);
                }
                else if (e.ZomeFunction == SaveFuncName)
                {
                    _savingHolochainBaseDataObjects[e.Id].HcAddressHash = e.ZomeReturnData;

                    OnHolochainBaseDataObjectSaved?.Invoke(this, new HolochainBaseDataObjectLoadedEventArgs { HolochainBaseDataObject = _savingHolochainBaseDataObjects[e.Id] });
                    _taskCompletionSourceSaveHolochainBaseDataObject.SetResult(_savingHolochainBaseDataObjects[e.Id]);
                    _savingHolochainBaseDataObjects.Remove(e.Id);
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

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, ZomeName, LoadFuncName, new { address = hcEntryAddressHash });
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

        public async Task<IHolochainBaseDataObject> SaveMyClassAsync(IHolochainBaseDataObject hcObject)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                // Rust/HC does not like null strings so need to set to empty string.
                if (hcObject.HcAddressHash == null)
                    hcObject.HcAddressHash = string.Empty;

                _currentId++;
                _savingHolochainBaseDataObjects[_currentId.ToString()] = hcObject;

                await HoloNETClient.CallZomeFunctionAsync(_currentId.ToString(), _hcinstance, ZomeName, SaveFuncName, new { entry = hcObject });
                return await _taskCompletionSourceSaveHolochainBaseDataObject.Task;
            }

            return null;
        }
    }
}
