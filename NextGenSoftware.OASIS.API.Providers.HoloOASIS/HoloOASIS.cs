using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.Holochain.HoloNET.Client.Desktop;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public class HoloOASIS : IOASISNET, IOASISSTORAGE
    {
        private const string OURWORLD_ZOME = "our_world_core";
        private const string LOAD_PROFILE_FUNC = "load_profile";
        private const string SAVE_PROFILE_FUNC = "save_profile";

        private string _holochainURI;
        private string _hcinstance;
        private TaskCompletionSource<IProfile> _taskCompletionSourceIProfile = new TaskCompletionSource<IProfile>();
        // private TaskCompletionSource<IProfile> _taskCompletionSourceGetInstance = new TaskCompletionSource<IProfile>();

        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        public delegate void ProfileSaved(object sender, ProfileSavedEventArgs e);
        public event ProfileSaved OnPlayerProfileSaved;

        public delegate void ProfileLoaded(object sender, ProfileLoadedEventArgs e);
        public event ProfileLoaded OnPlayerProfileLoaded;

        public delegate void HoloOASISError(object sender, ErrorEventArgs e);
        public event HoloOASISError OnHoloOASISError;

        //public delegate void ProfileSaved(object sender, ProfileSavedEventArgs e);
        //public event ProfileSaved OnProfileSaved2;

        public HoloNETClient HoloNETClient { get; private set; }


        public HoloOASIS(string holochainURI)
        {
            _holochainURI = holochainURI;
            Initialize();
        }

        public async Task Initialize()
        {
            HoloNETClient = new HoloNETClient(_holochainURI);

            HoloNETClient.OnConnected += HoloOASIS_OnConnected;
            HoloNETClient.OnDisconnected += HoloOASIS_OnDisconnected;
            HoloNETClient.OnError += HoloNETClient_OnError;
            HoloNETClient.OnDataReceived += HoloOASIS_OnDataReceived;
            HoloNETClient.OnGetInstancesCallBack += HoloOASIS_OnGetInstancesCallBack;
            HoloNETClient.OnSignalsCallBack += HoloOASIS_OnSignalsCallBack;
            HoloNETClient.OnZomeFunctionCallBack += HoloOASIS_OnZomeFunctionCallBack;

            await HoloNETClient.Connect();
           // await HoloNETClient.GetHolochainInstancesAsync();
            //GetInstancesCallBackEventArgs args = await HoloNETClient.GetHolochainInstancesAsync();
            //_hcinstance = args.Instances[0];
            //_hcinstance = await GetHolochainInstancesAsync().Result.Instances[0];
        }

        private void HoloNETClient_OnError(object sender, Holochain.HoloNET.Client.Core.ErrorEventArgs e)
        {
            OnHoloOASISError?.Invoke(this, new ErrorEventArgs { EndPoint = _holochainURI, Reason = "Error occured in HoloNET. See ErrorDetial for reason.", HoloNETErrorDetails = e });
        }

        private void HoloOASIS_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
        {
            switch (e.ZomeFunction)
            {
                case LOAD_PROFILE_FUNC:
                    OnPlayerProfileLoaded?.Invoke(this, new ProfileLoadedEventArgs { Profile = JsonConvert.DeserializeObject<Profile>(string.Concat("{", e.ZomeReturnData, "}")) });
                    //_taskCompletionSourceIProfile.SetResult(JsonConvert.DeserializeObject<IProfile>(e.ZomeReturnData));
                    break;

                case SAVE_PROFILE_FUNC:
                    OnPlayerProfileSaved?.Invoke(this, new ProfileSavedEventArgs { ProfileEntryHash = e.ZomeReturnData });
                  // _taskCompletionSourceIProfile.SetResult(JsonConvert.DeserializeObject<IProfile>(e.ZomeReturnData));
                    break;
            }
        }

        private void HoloOASIS_OnSignalsCallBack(object sender, SignalsCallBackEventArgs e)
        {
            
        }

        private void HoloOASIS_OnGetInstancesCallBack(object sender, GetInstancesCallBackEventArgs e)
        {
            _hcinstance = e.Instances[0];
            OnInitialized?.Invoke(this, new EventArgs());
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

        #region IOASISSTORAGE Implementation

        public async Task<IProfile> LoadProfileAsync(string profileEntryHash)
        {
            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_PROFILE_FUNC, new { address = profileEntryHash });
                //return await _taskCompletionSourceIProfile.Task;
            }

            return null;
        }

        public async Task SaveProfileAsync(IProfile profile)
        {
            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, SAVE_PROFILE_FUNC, new { entry = profile });
              //  return await _taskCompletionSourceIProfile.Task;
            }

            //return null;
        }

        public Task<bool> AddKarmaToProfileAsync(IProfile profile, int karma)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveKarmaFromProfileAsync(IProfile profile, int karma)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISNET Implementation

        public List<Holon> GetHolonsNearMe(HolonType type)
        {
            throw new NotImplementedException();
        }

        public List<Player> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
