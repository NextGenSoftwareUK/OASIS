using Newtonsoft.Json;
using NextGenSoftware.Holochain.HDK.HoloNET.Client.Core;
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
        private const string LOAD_PROFILE_FUNC = "load_profile";
        private const string SAVE_PROFILE_FUNC = "save_profile";

        private int _currentId = 0;
        private Dictionary<string, HcProfile> _savingProfiles = new Dictionary<string, HcProfile>();
        private string _hcinstance;
        //private TaskCompletionSource<NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core.IProfile> _taskCompletionSourceLoadProfile = new TaskCompletionSource<NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core.IProfile>();
        //private TaskCompletionSource<NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core.IProfile> _taskCompletionSourceSaveProfile = new TaskCompletionSource<NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core.IProfile>();

        private TaskCompletionSource<Profile> _taskCompletionSourceLoadProfile = new TaskCompletionSource<Profile>();
        private TaskCompletionSource<Profile> _taskCompletionSourceSaveProfile = new TaskCompletionSource<Profile>();

        private TaskCompletionSource<string> _taskCompletionSourceGetInstance = new TaskCompletionSource<string>();

       // public event ProfileManager.StorageProviderError OnStorageProviderError;

        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        public delegate void ProfileSaved(object sender, ProfileSavedEventArgs e);
        public event ProfileSaved OnPlayerProfileSaved;

        public delegate void ProfileLoaded(object sender, ProfileLoadedEventArgs e);
        public event ProfileLoaded OnPlayerProfileLoaded;

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
                    case LOAD_PROFILE_FUNC:
                        HcProfile hcProfile = JsonConvert.DeserializeObject<HcProfile>(string.Concat("{", e.ZomeReturnData, "}"));
                        OnPlayerProfileLoaded?.Invoke(this, new ProfileLoadedEventArgs {HcProfile = hcProfile, Profile = ConvertHcProfileToProfile(hcProfile) });

                        //TODO: Want to use these eventually so the async methods can return the results without having to use events/callbacks!
                         _taskCompletionSourceLoadProfile.SetResult(ConvertHcProfileToProfile(JsonConvert.DeserializeObject<HcProfile>(string.Concat("{", e.ZomeReturnData, "}"))));
                        break;

                    case SAVE_PROFILE_FUNC:
                        // TODO: Eventually want to return the Profile object from HC with the HCAddressHash set when I work out how! ;-)
                        // The dictonary below can then be removed.

                        //if (string.IsNullOrEmpty(_savingProfiles[e.Id].HcAddressHash))
                        //{
                        //    //TODO: Forced to re-save the object with the address (wouldn't that create a new hash entry?!)
                        _savingProfiles[e.Id].hc_address_hash = e.ZomeReturnData;
                        _savingProfiles[e.Id].provider_key = e.ZomeReturnData; //Generic field for providers to store their key (in this case the address hash)

                        //    SaveProfileAsync(_savingProfiles[e.Id]);
                        //}
                        //else
                        //{

                        Profile profile = ConvertHcProfileToProfile(_savingProfiles[e.Id]);
                        OnPlayerProfileSaved?.Invoke(this, new ProfileSavedEventArgs { Profile = profile, HcProfile = _savingProfiles[e.Id] });
                        _taskCompletionSourceSaveProfile.SetResult(profile);
                        _savingProfiles.Remove(e.Id);
                        //}

                        //TODO: Want to use these eventually so the async methods can return the results without having to use events/callbacks!
                        // _taskCompletionSourceIProfile.SetResult(JsonConvert.DeserializeObject<IProfile>(e.ZomeReturnData));
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

        public override async Task<API.Core.IProfile> LoadProfileAsync(string profileEntryHash)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_PROFILE_FUNC, new { address = profileEntryHash });
                return await _taskCompletionSourceLoadProfile.Task;
            }

            return null;
        }

        public override async Task<API.Core.IProfile> LoadProfileAsync(Guid id)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Implement in HC/Rust
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_PROFILE_FUNC, new { id });
                return await _taskCompletionSourceLoadProfile.Task;
            }

            return null;
        }

        public override async Task<API.Core.IProfile> LoadProfileAsync(string username, string password)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Implement in HC/Rust
                //await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_PROFILE_FUNC, new { username, password });

                //TODO: TEMP HARDCODED JUST TO TEST WITH!
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_PROFILE_FUNC, new { address = "QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg" });
                return await _taskCompletionSourceLoadProfile.Task;
            }

            return null;
        }

        /*
        public override async Task<API.Core.IProfile> SaveProfileAsync(API.Core.IProfile profile)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                if (profile.Id == Guid.Empty)
                    profile.Id = Guid.NewGuid();

                Profile hcProfile = profile as Profile;

                if (hcProfile == null)
                    hcProfile = ConvertProfileToHoloOASISProfile(profile);
                else
                {
                    // Rust/HC does not like null strings so need to set to empty string.
                    if (hcProfile.HcAddressHash == null)
                        hcProfile.HcAddressHash = string.Empty;

                    if (hcProfile.ProviderKey == null)
                        hcProfile.ProviderKey = string.Empty;
                }

                _currentId++;
                _savingProfiles[_currentId.ToString()] = ConvertProfileToHoloOASISProfile(hcProfile);
                await HoloNETClient.CallZomeFunctionAsync(_currentId.ToString(), _hcinstance, OURWORLD_ZOME, SAVE_PROFILE_FUNC, new { entry = hcProfile });
                return await _taskCompletionSourceSaveProfile.Task;
            }

            return null;
        }
        */

        public override async Task<IProfile> SaveProfileAsync(API.Core.IProfile profile)
        {
            await _taskCompletionSourceGetInstance.Task;

            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                if (profile.Id == Guid.Empty)
                    profile.Id = Guid.NewGuid();

                HcProfile hcProfile = profile as HcProfile;

                if (hcProfile == null)
                    hcProfile = ConvertProfileToHoloOASISProfile(profile);
                else
                {
                    // Rust/HC does not like null strings so need to set to empty string.
                    if (hcProfile.hc_address_hash == null)
                        hcProfile.hc_address_hash = string.Empty;

                    if (hcProfile.provider_key == null)
                        hcProfile.provider_key = string.Empty;
                }

                _currentId++;
                //_savingProfiles[_currentId.ToString()] = ConvertProfileToHoloOASISProfile(hcProfile);
                _savingProfiles[_currentId.ToString()] = hcProfile;
                await HoloNETClient.CallZomeFunctionAsync(_currentId.ToString(), _hcinstance, OURWORLD_ZOME, SAVE_PROFILE_FUNC, new { entry = hcProfile });
                return await _taskCompletionSourceSaveProfile.Task;
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
        /// <param name="profile"></param>
        /// <returns></returns>
        //private Profile ConvertProfileToHoloOASISProfile(API.Core.IProfile profile)
        //{
        //    return new Profile
        //    {
        //        DOB = profile.DOB,
        //        Email = profile.Email,
        //        FirstName = profile.FirstName,
        //        HcAddressHash = string.Empty,
        //        HolonType = profile.HolonType,
        //        Id = profile.Id,
        //        Karma = profile.Karma,
        //        LastName = profile.LastName,
        //        Password = profile.Password,
        //        PlayerAddress = profile.PlayerAddress,
        //        ProviderKey = profile.ProviderKey == null ? string.Empty : profile.ProviderKey,
        //        Title = profile.Title,
        //        Username = profile.Username
        //    };
        //}

        private HcProfile ConvertProfileToHoloOASISProfile(API.Core.IProfile profile)
        {
            return new HcProfile
            {
                dob = profile.DOB,
                email = profile.Email,
                first_name = profile.FirstName,
                hc_address_hash = string.Empty,
                holon_type = profile.HolonType,
                id = profile.Id,
                karma = profile.Karma,
                last_name = profile.LastName,
                password = profile.Password,
                player_address = profile.PlayerAddress,
                provider_key = profile.ProviderKey == null ? string.Empty : profile.ProviderKey,
                title = profile.Title,
                username = profile.Username
            };
        }

        private Profile ConvertHcProfileToProfile(HcProfile profile)
        {
            return new Profile
            {
                DOB = profile.dob,
                Email = profile.email,
                FirstName = profile.first_name,
                HolonType = profile.holon_type,
                Id = profile.id,
                Karma = profile.karma,
                LastName = profile.last_name,
                Password = profile.password,
                PlayerAddress = profile.player_address,
                ProviderKey = profile.provider_key,
                Title = profile.title,
                Username = profile.username
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
            //OnStorageProviderError?.Invoke(this, new ProfileManagerErrorEventArgs { EndPoint = this.HoloNETClient.EndPoint, Reason = string.Concat(reason, holoNETEventArgs != null ? string.Concat(" - HoloNET Error: ", holoNETEventArgs.Reason, " - ", holoNETEventArgs.ErrorDetails.ToString()) : ""), ErrorDetails = errorDetails });
            OnStorageProviderError(HoloNETClient.EndPoint, string.Concat(reason, holoNETEventArgs != null ? string.Concat(" - HoloNET Error: ", holoNETEventArgs.Reason, " - ", holoNETEventArgs.ErrorDetails.ToString()) : ""), errorDetails);
            OnHoloOASISError?.Invoke(this, new HoloOASISErrorEventArgs() { EndPoint = HoloNETClient.EndPoint, Reason = reason, ErrorDetails = errorDetails, HoloNETErrorDetails = holoNETEventArgs });
        }

        public async override void ActivateProvider()
        {
            await HoloNETClient.Connect();
            base.ActivateProvider();
        }

        public async override void DeActivateProvider()
        {
            await HoloNETClient.Disconnect();
            base.DeActivateProvider();
        }

        public override Task<ISearchResults> SearchAsync(string searchTerm)
        {
            throw new System.NotImplementedException();
        }
    }
}
