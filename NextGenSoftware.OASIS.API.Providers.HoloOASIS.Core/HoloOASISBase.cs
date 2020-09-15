using Newtonsoft.Json;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core
{
    public abstract class HoloOASISBase : OASISStorageBase, IOASISNET, IOASISStorage
    {
        private const string OURWORLD_ZOME = "our_world_core";
        private const string LOAD_Avatar_FUNC = "load_Avatar";
        private const string SAVE_Avatar_FUNC = "save_Avatar";

        private int _currentId = 0;
        private Dictionary<string, HcAvatar> _savingAvatars = new Dictionary<string, HcAvatar>();
        private string _hcinstance;
        //private TaskCompletionSource<NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core.IAvatar> _taskCompletionSourceLoadAvatar = new TaskCompletionSource<NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core.IAvatar>();
        //private TaskCompletionSource<NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core.IAvatar> _taskCompletionSourceSaveAvatar = new TaskCompletionSource<NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core.IAvatar>();

        private TaskCompletionSource<Avatar> _taskCompletionSourceLoadAvatar = new TaskCompletionSource<Avatar>();
        private TaskCompletionSource<Avatar> _taskCompletionSourceSaveAvatar = new TaskCompletionSource<Avatar>();

        private TaskCompletionSource<string> _taskCompletionSourceGetInstance = new TaskCompletionSource<string>();

       // public event AvatarManager.StorageProviderError OnStorageProviderError;

        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        public delegate void AvatarSaved(object sender, AvatarSavedEventArgs e);
        public event AvatarSaved OnPlayerAvatarSaved;

        public delegate void AvatarLoaded(object sender, AvatarLoadedEventArgs e);
        public event AvatarLoaded OnPlayerAvatarLoaded;

        public delegate void HoloOASISError(object sender, HoloOASISErrorEventArgs e);
        public event HoloOASISError OnHoloOASISError;

        public HoloNETClientBase HoloNETClient { get; private set; }
       

        public HoloOASISBase(HoloNETClientBase holoNETClient)
        {
            this.ProviderName = "HoloOASIS";
            this.ProviderDescription = "Holochain Provider";
            this.ProviderType = ProviderType.HoloOASIS;
            this.ProviderCategory = ProviderCategory.StorageAndNetwork;
            this.HoloNETClient = holoNETClient;
          //  _holochainURI = holochainURI;
            Initialize();
        }

        public async Task Initialize()
        {
            HoloNETClient.OnConnected += HoloOASIS_OnConnected;
            HoloNETClient.OnDisconnected += HoloOASIS_OnDisconnected;
            HoloNETClient.OnError += HoloNETClient_OnError;
            HoloNETClient.OnDataReceived += HoloOASIS_OnDataReceived;
            HoloNETClient.OnGetInstancesCallBack += HoloOASIS_OnGetInstancesCallBack;
            HoloNETClient.OnSignalsCallBack += HoloOASIS_OnSignalsCallBack;
            HoloNETClient.OnZomeFunctionCallBack += HoloOASIS_OnZomeFunctionCallBack;

           // HoloNETClient.Config.AutoStartConductor = true;
          //  HoloNETClient.Config.AutoShutdownConductor = true;
          //  HoloNETClient.Config.FullPathToExternalHolochainConductor = string.Concat(Directory.GetCurrentDirectory(), "\\hc.exe");
         //   HoloNETClient.Config.FullPathToHolochainAppDNA = string.Concat(Directory.GetCurrentDirectory(), "\\our_world\\dist\\our_world.dna.json"); 
            
            //await HoloNETClient.Connect();
        }

        public override IEnumerable<IAvatar> LoadAllAvatars()
        {
            //TODO: Implement
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(Guid Id)
        {
            //TODO: Implement
            throw new NotImplementedException();
        }

        private void HoloNETClient_OnError(object sender, HoloNETErrorEventArgs e)
        {
            HandleError("Error occured in HoloNET. See ErrorDetial for reason.", null, e);
            //OnHoloOASISError?.Invoke(this, new ErrorEventArgs { EndPoint = HoloNETClient.EndPoint, Reason = "Error occured in HoloNET. See ErrorDetial for reason.", HoloNETErrorDetails = e });
        }

        private void HoloOASIS_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
        {
            if (!e.IsCallSuccessful)
                HandleError(string.Concat("Zome function ", e.ZomeFunction, " on zome ", e.Zome, " returned an error. Error Details: ", e.ZomeReturnData), null, null);
                //OnHoloOASISError?.Invoke(this, new ErrorEventArgs() { EndPoint = HoloNETClient.EndPoint, Reason = string.Concat("Zome function ", e.ZomeFunction, " on zome ", e.Zome, " returned an error. Error Details: ", e.ZomeReturnData) });
            else
            {
                switch (e.ZomeFunction)
                {
                    case LOAD_Avatar_FUNC:
                        HcAvatar hcAvatar = JsonConvert.DeserializeObject<HcAvatar>(string.Concat("{", e.ZomeReturnData, "}"));
                        OnPlayerAvatarLoaded?.Invoke(this, new AvatarLoadedEventArgs {HcAvatar = hcAvatar, Avatar = ConvertHcAvatarToAvatar(hcAvatar) });

                        //TODO: Want to use these eventually so the async methods can return the results without having to use events/callbacks!
                         _taskCompletionSourceLoadAvatar.SetResult(ConvertHcAvatarToAvatar(JsonConvert.DeserializeObject<HcAvatar>(string.Concat("{", e.ZomeReturnData, "}"))));
                        break;

                    case SAVE_Avatar_FUNC:
                        // TODO: Eventually want to return the Avatar object from HC with the HCAddressHash set when I work out how! ;-)
                        // The dictonary below can then be removed.

                        //if (string.IsNullOrEmpty(_savingAvatars[e.Id].HcAddressHash))
                        //{
                        //    //TODO: Forced to re-save the object with the address (wouldn't that create a new hash entry?!)
                        _savingAvatars[e.Id].hc_address_hash = e.ZomeReturnData;
                        _savingAvatars[e.Id].provider_key = e.ZomeReturnData; //Generic field for providers to store their key (in this case the address hash)

                        //    SaveAvatarAsync(_savingAvatars[e.Id]);
                        //}
                        //else
                        //{

                        Avatar Avatar = ConvertHcAvatarToAvatar(_savingAvatars[e.Id]);
                        OnPlayerAvatarSaved?.Invoke(this, new AvatarSavedEventArgs { Avatar = Avatar, HcAvatar = _savingAvatars[e.Id] });
                        _taskCompletionSourceSaveAvatar.SetResult(Avatar);
                        _savingAvatars.Remove(e.Id);
                        //}

                        //TODO: Want to use these eventually so the async methods can return the results without having to use events/callbacks!
                        // _taskCompletionSourceIAvatar.SetResult(JsonConvert.DeserializeObject<IAvatar>(e.ZomeReturnData));
                        break;
                }
            }
        }

        private void HoloOASIS_OnSignalsCallBack(object sender, SignalsCallBackEventArgs e)
        {
            
        }

        private void HoloOASIS_OnGetInstancesCallBack(object sender, GetInstancesCallBackEventArgs e)
        {
            _hcinstance = e.Instances[0];
            OnInitialized?.Invoke(this, new EventArgs());
            _taskCompletionSourceGetInstance.SetResult(_hcinstance);
        }

        private void HoloOASIS_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            
        }

        private void HoloOASIS_OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            
        }

        private void HoloOASIS_OnConnected(object sender, ConnectedEventArgs e)
        {
            HoloNETClient.GetHolochainInstancesAsync();
        }

        #region IOASISStorage Implementation

        public override async Task<API.Core.IAvatar> LoadAvatarAsync(string AvatarEntryHash)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_Avatar_FUNC, new { address = AvatarEntryHash });
                return await _taskCompletionSourceLoadAvatar.Task;
            }

            return null;
        }

        public override async Task<API.Core.IAvatar> LoadAvatarAsync(Guid id)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Implement in HC/Rust
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_Avatar_FUNC, new { id });
                return await _taskCompletionSourceLoadAvatar.Task;
            }

            return null;
        }

        public override async Task<API.Core.IAvatar> LoadAvatarAsync(string username, string password)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Implement in HC/Rust
                //await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_Avatar_FUNC, new { username, password });

                //TODO: TEMP HARDCODED JUST TO TEST WITH!
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_Avatar_FUNC, new { address = "QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg" });
                return await _taskCompletionSourceLoadAvatar.Task;
            }

            return null;
        }

        public override IAvatar LoadAvatar(string username, string password)
        {
            // TODO: {URGENT} FIX THIS ASAP! Need to wait for GetInstance to complete, ideally this method should call the async method above..
            // Even better is we ONLY use the async method, need to fix the bug in WebAPI so it can call async methods from the controllers ASAP...
            // _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State != System.Net.WebSockets.WebSocketState.Open && HoloNETClient.State != System.Net.WebSockets.WebSocketState.Connecting)
                HoloNETClient.Connect();

            //TODO: Come back to this... (Need to wait for it to connect...)
            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Implement in HC/Rust
                //await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_Avatar_FUNC, new { username, password });

                //TODO: TEMP HARDCODED JUST TO TEST WITH!
                HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_Avatar_FUNC, new { address = "QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg" });
              //  return await _taskCompletionSourceLoadAvatar.Task;
            }

            return null;
        }

        public override Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
        {
            //TODO: {URGENT} FIX ASAP!
            //return new (IEnumerable<IAvatar>)List<IAvatar>();

            throw new System.NotImplementedException();
        }


        public override Task<ISearchResults> SearchAsync(string searchTerm)
        {
            throw new System.NotImplementedException();
        }

        /*
        public override async Task<API.Core.IAvatar> SaveAvatarAsync(API.Core.IAvatar Avatar)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                if (Avatar.Id == Guid.Empty)
                    Avatar.Id = Guid.NewGuid();

                Avatar hcAvatar = Avatar as Avatar;

                if (hcAvatar == null)
                    hcAvatar = ConvertAvatarToHoloOASISAvatar(Avatar);
                else
                {
                    // Rust/HC does not like null strings so need to set to empty string.
                    if (hcAvatar.HcAddressHash == null)
                        hcAvatar.HcAddressHash = string.Empty;

                    if (hcAvatar.ProviderKey == null)
                        hcAvatar.ProviderKey = string.Empty;
                }

                _currentId++;
                _savingAvatars[_currentId.ToString()] = ConvertAvatarToHoloOASISAvatar(hcAvatar);
                await HoloNETClient.CallZomeFunctionAsync(_currentId.ToString(), _hcinstance, OURWORLD_ZOME, SAVE_Avatar_FUNC, new { entry = hcAvatar });
                return await _taskCompletionSourceSaveAvatar.Task;
            }

            return null;
        }
        */

        public override async Task<IAvatar> SaveAvatarAsync(API.Core.IAvatar Avatar)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                if (Avatar.Id == Guid.Empty)
                    Avatar.Id = Guid.NewGuid();

                HcAvatar hcAvatar = Avatar as HcAvatar;

                if (hcAvatar == null)
                    hcAvatar = ConvertAvatarToHoloOASISAvatar(Avatar);
                else
                {
                    // Rust/HC does not like null strings so need to set to empty string.
                    if (hcAvatar.hc_address_hash == null)
                        hcAvatar.hc_address_hash = string.Empty;

                    if (hcAvatar.provider_key == null)
                        hcAvatar.provider_key = string.Empty;
                }

                _currentId++;
                //_savingAvatars[_currentId.ToString()] = ConvertAvatarToHoloOASISAvatar(hcAvatar);
                _savingAvatars[_currentId.ToString()] = hcAvatar;
                await HoloNETClient.CallZomeFunctionAsync(_currentId.ToString(), _hcinstance, OURWORLD_ZOME, SAVE_Avatar_FUNC, new { entry = hcAvatar });
                return await _taskCompletionSourceSaveAvatar.Task;
            }

            return null;
        }


            #endregion

            #region IOASISNET Implementation

        public List<IHolon> GetHolonsNearMe(HolonType type)
        {
            throw new NotImplementedException();
        }

        public List<IPlayer> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Avatar"></param>
        /// <returns></returns>
        //private Avatar ConvertAvatarToHoloOASISAvatar(API.Core.IAvatar Avatar)
        //{
        //    return new Avatar
        //    {
        //        DOB = Avatar.DOB,
        //        Email = Avatar.Email,
        //        FirstName = Avatar.FirstName,
        //        HcAddressHash = string.Empty,
        //        HolonType = Avatar.HolonType,
        //        Id = Avatar.Id,
        //        Karma = Avatar.Karma,
        //        LastName = Avatar.LastName,
        //        Password = Avatar.Password,
        //        PlayerAddress = Avatar.PlayerAddress,
        //        ProviderKey = Avatar.ProviderKey == null ? string.Empty : Avatar.ProviderKey,
        //        Title = Avatar.Title,
        //        Username = Avatar.Username
        //    };
        //}

        private HcAvatar ConvertAvatarToHoloOASISAvatar(API.Core.IAvatar Avatar)
        {
            return new HcAvatar
            {
                dob = Avatar.DOB.ToString(),
                email = Avatar.Email,
                first_name = Avatar.FirstName,
                hc_address_hash = string.Empty,
                holon_type = Avatar.HolonType,
                id = Avatar.Id,
                karma = Avatar.Karma,
                last_name = Avatar.LastName,
                password = Avatar.Password,
                address = Avatar.Address,
                provider_key = Avatar.ProviderKey == null ? string.Empty : Avatar.ProviderKey,
                title = Avatar.Title,
                username = Avatar.Username
            };
        }

        private Avatar ConvertHcAvatarToAvatar(HcAvatar Avatar)
        {
            return new Avatar
            {
                DOB = Convert.ToDateTime(Avatar.dob),
                Email = Avatar.email,
                FirstName = Avatar.first_name,
                HolonType = Avatar.holon_type,
                Id = Avatar.id,
                Karma = Avatar.karma,
                LastName = Avatar.last_name,
                Password = Avatar.password,
                Address = Avatar.address,
                ProviderKey = Avatar.provider_key,
                Title = Avatar.title,
                Username = Avatar.username
            };
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
            //OnStorageProviderError?.Invoke(this, new AvatarManagerErrorEventArgs { EndPoint = this.HoloNETClient.EndPoint, Reason = string.Concat(reason, holoNETEventArgs != null ? string.Concat(" - HoloNET Error: ", holoNETEventArgs.Reason, " - ", holoNETEventArgs.ErrorDetails.ToString()) : ""), ErrorDetails = errorDetails });
            OnStorageProviderError(HoloNETClient.EndPoint, string.Concat(reason, holoNETEventArgs != null ? string.Concat(" - HoloNET Error: ", holoNETEventArgs.Reason, " - ", holoNETEventArgs.ErrorDetails.ToString()) : ""), errorDetails);
            OnHoloOASISError?.Invoke(this, new HoloOASISErrorEventArgs() { EndPoint = HoloNETClient.EndPoint, Reason = reason, ErrorDetails = errorDetails, HoloNETErrorDetails = holoNETEventArgs });
        }

        public async override void ActivateProvider()
        {
            if (HoloNETClient.State != System.Net.WebSockets.WebSocketState.Open && HoloNETClient.State != System.Net.WebSockets.WebSocketState.Connecting)
                await HoloNETClient.Connect();
            
            base.ActivateProvider();
        }

        public async override void DeActivateProvider()
        {
            await HoloNETClient.Disconnect();
            base.DeActivateProvider();
        }
    }
}
