using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.Holochain.HoloNET.Client;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public class HoloOASIS : OASISStorageProviderBase, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISLocalStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar
    {
        private const string ZOME_LOAD_AVATAR_BY_ID_FUNCTION = "get_entry_avatar_by_id";
        private const string ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION = "get_entry_avatar_by_username";
        private const string ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION = "get_entry_avatar_by_email";
        private const string ZOME_DELETE_AVATAR_BY_ID_FUNCTION = "delete_entry_avatar_by_id";
        private const string ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION = "delete_entry_avatar_by_username";
        private const string ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION = "delete_entry_avatar_by_email";
        private const string ZOME_LOAD_HOLON_BY_ID_FUNCTION = "get_entry_holon_by_id";

        private bool _useReflectionForSaving = false;
        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        public delegate void AvatarSaved(object sender, AvatarSavedEventArgs e);
        public event AvatarSaved OnPlayerAvatarSaved;

        public delegate void AvatarLoaded(object sender, AvatarLoadedEventArgs e);
        public event AvatarLoaded OnPlayerAvatarLoaded;

        public delegate void HoloOASISError(object sender, HoloOASISErrorEventArgs e);
        public event HoloOASISError OnHoloOASISError;

        public HoloNETClient HoloNETClient { get; private set; }

        public HoloOASIS(HoloNETClient holoNETClient, bool useReflectionForSaving = true)
        {
            _useReflectionForSaving = useReflectionForSaving;
            this.HoloNETClient = holoNETClient;
            Initialize();
        }

        public HoloOASIS(string holochainConductorURI, bool useReflectionForSaving = true)
        {
            _useReflectionForSaving = useReflectionForSaving;
            HoloNETClient = new HoloNETClient(holochainConductorURI);
            Initialize();
        }

        private async Task Initialize()
        {
            this.ProviderName = "HoloOASIS";
            this.ProviderDescription = "Holochain Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.HoloOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);

            //HcAvatar.OnLoaded += HcAvatar_OnLoaded;
            //HcAvatar.OnSaved += HcAvatar_OnSaved;
            //HcAvatar.OnError += HcAvatar_OnError;
            //HcAvatar.OnClosed += HcAvatar_OnClosed;
            //HcAvatar.OnInitialized += HcAvatar_OnInitialized;

            //await HcAvatar.WaitTillHoloNETInitializedAsync();

            //HoloNETClient.Config.ShowHolochainConductorWindow = true;
            //HoloNETClient.Config.HolochainConductorMode = HolochainConductorModeEnum.UseEmbedded;
            //HoloNETClient.OnConnected += HoloOASIS_OnConnected;
            //HoloNETClient.OnDisconnected += HoloOASIS_OnDisconnected;
            //HoloNETClient.OnError += HoloNETClient_OnError;
            //HoloNETClient.OnDataReceived += HoloOASIS_OnDataReceived;
            //HoloNETClient.OnSignalCallBack += HoloOASIS_OnSignalCallBack;
            //HoloNETClient.OnZomeFunctionCallBack += HoloOASIS_OnZomeFunctionCallBack;

            // HoloNETClient.Config.AutoStartConductor = true;
            //  HoloNETClient.Config.AutoShutdownConductor = true;
            //  HoloNETClient.Config.FullPathToExternalHolochainConductor = string.Concat(Directory.GetCurrentDirectory(), "\\hc.exe");
            //   HoloNETClient.Config.FullPathToHolochainAppDNA = string.Concat(Directory.GetCurrentDirectory(), "\\our_world\\dist\\our_world.dna.json"); 


            //await HoloNETClient.ConnectAsync();
        }

        //private async Task<HcAvatar> InitHcAvatar(Guid id)
        //{
        //    HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
        //    //hcAvatar.OnLoaded += HcAvatar_OnLoaded;
        //    //hcAvatar.OnSaved += HcAvatar_OnSaved;
        //    //hcAvatar.OnError += HcAvatar_OnError;
        //    //hcAvatar.OnClosed += HcAvatar_OnClosed;
        //    //hcAvatar.OnInitialized += HcAvatar_OnInitialized;

        //    await hcAvatar.WaitTillHoloNETInitializedAsync();
        //    _hcAvatars[id.ToString()] = hcAvatar;

        //    return hcAvatar;
        //}

        //private void DisposeHcAvatar(Guid id)
        //{
        //    //_hcAvatars[id.ToString()].OnLoaded -= HcAvatar_OnLoaded;
        //    //_hcAvatars[id.ToString()].OnSaved -= HcAvatar_OnSaved;
        //    //_hcAvatars[id.ToString()].OnError -= HcAvatar_OnError;
        //    //_hcAvatars[id.ToString()].OnClosed -= HcAvatar_OnClosed;
        //    //_hcAvatars[id.ToString()].OnInitialized -= HcAvatar_OnInitialized;
        //    _hcAvatars[id.ToString()] = null;
        //    _hcAvatars.Remove(id.ToString());
        //}

        //private void HcAvatar_OnInitialized(object sender, ReadyForZomeCallsEventArgs e)
        //{

        //}

        //private void HcAvatar_OnClosed(object sender, HoloNETShutdownEventArgs e)
        //{

        //}

        //private void HcAvatar_OnError(object sender, HoloNETErrorEventArgs e)
        //{
        //    HandleError("Error occured in HoloNET. See ErrorDetial for reason.", null, e);
        //}

        //private void HcAvatar_OnSaved(object sender, ZomeFunctionCallBackEventArgs e)
        //{

        //}

        //private void HcAvatar_OnLoaded(object sender, ZomeFunctionCallBackEventArgs e)
        //{

        //}

        //private void HoloNETClient_OnError(object sender, HoloNETErrorEventArgs e)
        //{
        //    HandleError("Error occured in HoloNET. See ErrorDetial for reason.", null, e);
        //    //OnHoloOASISError?.Invoke(this, new ErrorEventArgs { EndPoint = HoloNETClient.EndPoint, Reason = "Error occured in HoloNET. See ErrorDetial for reason.", HoloNETErrorDetails = e });
        //}

        /*
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
                        //HcAvatar hcAvatar = JsonConvert.DeserializeObject<HcAvatar>(string.Concat("{", e.ZomeReturnData, "}"));
                        HcAvatar hcAvatar = e.Entry.EntryDataObject as HcAvatar;
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
                        _savingAvatars[e.Id].hc_address_hash = e.ZomeReturnData["address_hash"].ToString();
                        _savingAvatars[e.Id].provider_key = e.ZomeReturnData["provider_key"].ToString(); //Generic field for providers to store their key (in this case the address hash)

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

        private void HoloOASIS_OnSignalCallBack(object sender, SignalCallBackEventArgs e)
        {
            
        }

        //private void HoloOASIS_OnGetInstancesCallBack(object sender, GetInstancesCallBackEventArgs e)
        //{
        //    _hcinstance = e.Instances[0];
        //    OnInitialized?.Invoke(this, new EventArgs());
        //    _taskCompletionSourceGetInstance.SetResult(_hcinstance);
        //}

        private void HoloOASIS_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            
        }

        private void HoloOASIS_OnDataReceived(object sender, HoloNETDataReceivedEventArgs e)
        {
            
        }

        private void HoloOASIS_OnConnected(object sender, ConnectedEventArgs e)
        {
           // HoloNETClient.GetHolochainInstancesAsync();
        }*/

        #region IOASISStorageProvider Implementation

        public override OASISResult<bool> ActivateProvider()
        {
            if (HoloNETClient.State != System.Net.WebSockets.WebSocketState.Open && HoloNETClient.State != System.Net.WebSockets.WebSocketState.Connecting)
                HoloNETClient.Connect();

            return base.ActivateProvider();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            HoloNETClient.Disconnect();
            // HoloNETClient = null;
            return base.DeActivateProvider();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);

                if (hcAvatar != null)
                {
                    //ProviderKey is the entry hash.
                    ZomeFunctionCallBackEventArgs response = await hcAvatar.LoadAsync(providerKey);

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                        {
                            result.Result = response.Entry.EntryDataObject;
                            hcAvatar = null;
                        }
                        else
                            ErrorHandling.HandleError(ref result, $"Error loading avatar with providerKey (entryhash) {providerKey} for HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error loading avatar with providerKey (entryhash) {providerKey} for HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error loading avatar with providerKey (entryhash) {providerKey} for HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);

                if (hcAvatar != null)
                {
                    //ProviderKey is the entry hash.
                    ZomeFunctionCallBackEventArgs response = hcAvatar.Load(providerKey);

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                        {
                            result.Result = response.Entry.EntryDataObject;
                            hcAvatar = null;
                        }
                        else
                            ErrorHandling.HandleError(ref result, $"Error loading avatar with providerKey (entryhash) {providerKey} for HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error loading avatar with providerKey (entryhash) {providerKey} for HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error loading avatar with providerKey (entryhash) {providerKey} for HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            return await ExecuteOperationAsync<IAvatar>(OperationEnum.Read, "avatar", "id", id.ToString(), ZOME_LOAD_AVATAR_BY_ID_FUNCTION);
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return ExecuteOperation<IAvatar>(OperationEnum.Read, "avatar", "id", id.ToString(), ZOME_LOAD_AVATAR_BY_ID_FUNCTION);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            return await ExecuteOperationAsync<IAvatar>(OperationEnum.Read, "avatar", "email", avatarEmail, ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION, version);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return ExecuteOperation<IAvatar>(OperationEnum.Read, "avatar", "email", avatarEmail, ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION, version);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            return await ExecuteOperationAsync<IAvatar>(OperationEnum.Read, "avatar", "email", avatarUsername, ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION, version);
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExecuteOperation<IAvatar>(OperationEnum.Read, "avatar", "email", avatarUsername, ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION, version);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            return await ExecuteOperationAsync<IAvatarDetail>(OperationEnum.Read, "avatar detail", "id", id.ToString(), ZOME_LOAD_AVATAR_DETAIL_BY_ID_FUNCTION, version);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return ExecuteOperation<IAvatarDetail>(OperationEnum.Read, "avatar detail", "id", id.ToString(), ZOME_LOAD_AVATAR_DETAIL_BY_ID_FUNCTION, version);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            return await ExecuteOperationAsync<IAvatarDetail>(OperationEnum.Read, "avatar detail", "email", avatarEmail, ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION, version);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return ExecuteOperation<IAvatarDetail>(OperationEnum.Read, "avatar detail", "email", avatarEmail, ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION, version);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            return await ExecuteOperationAsync<IAvatarDetail>(OperationEnum.Read, "avatar detail", "username", avatarUsername, ZOME_LOAD_AVATAR_DETAIL_BY_USERNAME_FUNCTION, version);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return ExecuteOperation<IAvatarDetail>(OperationEnum.Read, "avatar detail", "username", avatarUsername, ZOME_LOAD_AVATAR_DETAIL_BY_USERNAME_FUNCTION, version);
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            return await ExecuteOperationAsync<IEnumerable<IAvatarDetail>>(ListOperationEnum.ReadList, "avatars", "listanchor", ZOME_LOAD_ALL_AVATARS_DETAIL_FUNCTION, version);
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>(avatar);

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);

                if (avatar.Id == Guid.Empty)
                    avatar.Id = Guid.NewGuid();

                hcAvatar = (HcAvatar)ConvertAvatarToHoloOASISAvatar(avatar, hcAvatar);

                //If it is configured to not use Reflection then we would do it like this passing in our own params object.
                if (!_useReflectionForSaving)
                {
                    await hcAvatar.SaveAsync(new
                    {
                        id = hcAvatar.Id.ToString(),
                        username = hcAvatar.Username,
                        password = hcAvatar.Password,
                        email = hcAvatar.Email,
                        title = hcAvatar.Title,
                        first_name = hcAvatar.FirstName,
                        last_name = hcAvatar.LastName,
                        provider_key = hcAvatar.ProviderKey,
                        holon_type = hcAvatar.HolonType
                    });
                }
                else
                    //Otherwise we could just use this dyanmic version (which uses reflection) to dyamically build the params object (but we need to make sure properties have the HolochainFieldName attribute).
                    await hcAvatar.SaveAsync();
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An unknwon error has occured saving the avatar in the HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                hcAvatar.ZomeDeleteEntryFunction = ZOME_DELETE_AVATAR_BY_ID_FUNCTION;
                await hcAvatar.DeleteAsync(id.ToString());
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An unknwon error has occured deleting the avatar with id {id} in the HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                hcAvatar.ZomeDeleteEntryFunction = ZOME_DELETE_AVATAR_BY_ID_FUNCTION;
                hcAvatar.Delete(id.ToString());
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An unknwon error has occured deleting the avatar with id {id} in the HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                await hcAvatar.DeleteAsync(providerKey);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An unknwon error has occured deleting the avatar with providerKey (entryHash) {providerKey} in the HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                hcAvatar.Delete(providerKey);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An unknwon error has occured deleting the avatar with providerKey (entryHash) {providerKey} in the HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                hcAvatar.ZomeDeleteEntryFunction = ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION;
                await hcAvatar.DeleteAsync(avatarEmail);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An unknwon error has occured deleting the avatar with email {avatarEmail} in the HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                hcAvatar.ZomeDeleteEntryFunction = ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION;
                hcAvatar.Delete(avatarEmail);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An unknwon error has occured deleting the avatar with email {avatarEmail} in the HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                hcAvatar.ZomeDeleteEntryFunction = ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION;
                await hcAvatar.DeleteAsync(avatarUsername);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An unknwon error has occured deleting the avatar with username {avatarUsername} in the HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                hcAvatar.ZomeDeleteEntryFunction = ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION;
                hcAvatar.Delete(avatarUsername);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"An unknwon error has occured deleting the avatar with username {avatarUsername} in the HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);

                if (hcAvatar != null)
                {
                    hcAvatar.ZomeLoadEntryFunction = ZOME_LOAD_HOLON_FUNCTION_BY_ID;
                    ZomeFunctionCallBackEventArgs response = await hcAvatar.LoadAsync(id.ToString());

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                            result.Result = response.Entry.EntryDataObject;
                        else
                            ErrorHandling.HandleError(ref result, $"Error loading holon with id {id} for HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error loading holon with id {id} for HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error loading holon with id {id} for HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISNET Implementation

        OASISResult<IEnumerable<IPlayer>> IOASISNETProvider.GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISSuperStar
        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISBlockchainStorageProvider

        public OASISResult<string> SendTransaction(IWalletTransaction transation)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<string>> SendTransactionAsync(IWalletTransaction transation)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<string>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<string>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<string>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<string>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISNFTProvider

        public OASISResult<bool> SendNFT(IWalletTransaction transation)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<bool>> SendNFTAsync(IWalletTransaction transation)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISLocalStorageProvider

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            throw new NotImplementedException();
        }

        //public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWallets()
        //{
        //    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

        //    //TODO: Finish Implementing.

        //    return result;
        //}

        //public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsAsync()
        //{
        //    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

        //    //TODO: Finish Implementing.

        //    return result;
        //}

        //public OASISResult<bool> SaveProviderWallets(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();

        //    //TODO: Finish Implementing.

        //    return result;
        //}

        //public async Task<OASISResult<bool>> SaveProviderWalletsAsync(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();

        //    //TODO: Finish Implementing.

        //    return result;
        //}

        //public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsername(string username)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByUsernameAsync(string username)
        //{
        //    throw new NotImplementedException();
        //}

        //public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByEmail(string email)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByEmailAsync(string email)
        //{
        //    throw new NotImplementedException();
        //}



        //public OASISResult<bool> SaveProviderWalletsForAvatarByUsername(string username, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<OASISResult<bool>> SaveProviderWalletsForAvatarByUsernameAsync(string username, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    throw new NotImplementedException();
        //}

        //public OASISResult<bool> SaveProviderWalletsForAvatarByEmail(string email, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<OASISResult<bool>> SaveProviderWalletsForAvatarByEmailAsync(string email, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    throw new NotImplementedException();
        //}

        #endregion

        #region 

        private IHcAvatar ConvertAvatarToHoloOASISAvatar(IAvatar avatar, IHcAvatar hcAvatar)
        {
            hcAvatar.Id = avatar.Id;
            hcAvatar.Username = avatar.Username;
            hcAvatar.Password = avatar.Password;
            hcAvatar.Email = avatar.Email;
            hcAvatar.Title = avatar.Title;
            hcAvatar.FirstName = avatar.FirstName;
            hcAvatar.LastName = avatar.LastName;
            hcAvatar.ProviderKey = avatar.ProviderUniqueStorageKey == null ? string.Empty : avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.HoloOASIS];
            hcAvatar.HolonType = avatar.HolonType;

            return hcAvatar;
        }

        private Avatar ConvertHcAvatarToAvatar(IHcAvatar hcAvatar)
        {
            Avatar avatar = new Avatar
            {
                Email = hcAvatar.Email,
                FirstName = hcAvatar.FirstName,
                HolonType = hcAvatar.HolonType,
                Id = hcAvatar.Id,
                LastName = hcAvatar.LastName,
                Password = hcAvatar.Password,
                Title = hcAvatar.Title,
                Username = hcAvatar.Username
            };

            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.HoloOASIS] = hcAvatar.ProviderKey;
            return avatar;
        }

        private async Task<OASISResult<T>> ExecuteOperationAsync<T>(OperationEnum operation, string objectName, string fieldName, string fieldValue, string zomeFunctionName = "", int version = 0) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                ZomeFunctionCallBackEventArgs response = null;

                if (hcAvatar != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeFunctionName))
                    {
                        switch (operation)
                        {
                            case OperationEnum.Create:
                                {
                                    hcAvatar.ZomeCreateEntryFunction = zomeFunctionName;
                                    response = await hcAvatar.SaveAsync(fieldValue);
                                }
                                break;

                            case OperationEnum.Read:
                                {
                                    hcAvatar.ZomeLoadEntryFunction = zomeFunctionName;
                                    response = await hcAvatar.LoadAsync(fieldValue);
                                }
                                break;

                            case OperationEnum.Update:
                                {
                                    hcAvatar.ZomeUpdateEntryFunction = zomeFunctionName;
                                    response = await hcAvatar.SaveAsync(fieldValue);
                                }
                                break;

                            case OperationEnum.Delete:
                                {
                                    hcAvatar.ZomeDeleteEntryFunction = zomeFunctionName;
                                    response = await hcAvatar.DeleteAsync(fieldValue);
                                }
                                break;
                        }
                    }

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                            result.Result = response.Entry.EntryDataObject; 
                        else
                            ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {objectName} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {objectName} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {objectName} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private OASISResult<T> ExecuteOperation<T>(OperationEnum operation, string objectName, string fieldName, string fieldValue, string zomeFunctionName = "", int version = 0) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                ZomeFunctionCallBackEventArgs response = null;

                if (hcAvatar != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeFunctionName))
                    {
                        switch (operation)
                        {
                            case OperationEnum.Create:
                                {
                                    hcAvatar.ZomeCreateEntryFunction = zomeFunctionName;
                                    response = hcAvatar.Save(fieldValue);
                                }
                                break;

                            case OperationEnum.Read:
                                {
                                    hcAvatar.ZomeLoadEntryFunction = zomeFunctionName;
                                    response = hcAvatar.Load(fieldValue);
                                }
                                break;

                            case OperationEnum.Update:
                                {
                                    hcAvatar.ZomeUpdateEntryFunction = zomeFunctionName;
                                    response = hcAvatar.Save(fieldValue);
                                }
                                break;

                            case OperationEnum.Delete:
                                {
                                    hcAvatar.ZomeDeleteEntryFunction = zomeFunctionName;
                                    response = hcAvatar.Delete(fieldValue);
                                }
                                break;
                        }
                    }

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                            result.Result = response.Entry.EntryDataObject;
                        else
                            ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {objectName} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {objectName} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {objectName} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<T>>> ExecuteOperationAsync<T>(CollectionOperationEnum operation, string collectionName, string collectionAnchor, string zomeFunctionName = "", int version = 0) where T : IHolonBase
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            //TODO: Need to implement loading collections in HoloNET ASAP! :)

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                ZomeFunctionCallBackEventArgs response = null;

                if (hcAvatar != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeFunctionName))
                    {
                        switch (operation)
                        {
                            case CollectionOperationEnum.CreateCollection:
                                {
                                    hcAvatar.ZomeCreateEntryCollectionFunction = zomeFunctionName;
                                    response = await hcAvatar.SaveCollectionAsync(collectionAnchor);
                                }
                                break;

                            case CollectionOperationEnum.ReadCollection:
                                {
                                    hcAvatar.ZomeLoadEntryCollectionFunction = zomeFunctionName;
                                    response = await hcAvatar.LoadCollectionAsync(collectionAnchor);
                                }
                                break;

                            case CollectionOperationEnum.UpdateCollection:
                                {
                                    hcAvatar.ZomeUpdateEntryCollectionFunction = zomeFunctionName;
                                    response = await hcAvatar.SaveCollectionAsync(collectionAnchor);
                                }
                                break;

                            case CollectionOperationEnum.DeleteCollection:
                                {
                                    hcAvatar.ZomeDeleteEntryCollectionFunction = zomeFunctionName;
                                    response = await hcAvatar.DeleteCollectionAsync(collectionAnchor);
                                }
                                break;
                        }
                    }

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                            result.Result = response.Entry.EntryDataObject;
                        else
                            ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} for HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} for HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} for HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> ExecuteOperation<T>(CollectionOperationEnum operation, string collectionName, string collectionAnchor, string zomeFunctionName = "", int version = 0) where T : IHolonBase
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            //TODO: Need to implement loading collections in HoloNET ASAP! :)

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);
                ZomeFunctionCallBackEventArgs response = null;

                if (hcAvatar != null)
                {
                    //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
                    if (!string.IsNullOrEmpty(zomeFunctionName))
                    {
                        switch (operation)
                        {
                            case CollectionOperationEnum.CreateCollection:
                                {
                                    hcAvatar.ZomeCreateEntryCollectionFunction = zomeFunctionName;
                                    response = hcAvatar.SaveCollection(collectionAnchor);
                                }
                                break;

                            case CollectionOperationEnum.ReadCollection:
                                {
                                    hcAvatar.ZomeLoadEntryFunction = zomeFunctionName;
                                    response = hcAvatar.LoadCollection(collectionAnchor);
                                }
                                break;

                            case CollectionOperationEnum.UpdateCollection:
                                {
                                    hcAvatar.ZomeUpdateEntryCollectionFunction = zomeFunctionName;
                                    response = hcAvatar.SaveCollection(collectionAnchor);
                                }
                                break;

                            case CollectionOperationEnum.DeleteCollection:
                                {
                                    hcAvatar.ZomeDeleteEntryCollectionFunction = zomeFunctionName;
                                    response = hcAvatar.DeleteCollection(collectionAnchor);
                                }
                                break;
                        }
                    }

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                            result.Result = response.Entry.EntryDataObject;
                        else
                            ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} for HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} for HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} for HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        /*
        private async Task<OASISResult<T>> LoadAsync<T>(string objectName, string fieldName, string fieldValue, string zomeLoadFunctionName, int version = 0) where T : IHolon
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);

                if (hcAvatar != null)
                {
                    hcAvatar.ZomeLoadEntryFunction = zomeLoadFunctionName;
                    ZomeFunctionCallBackEventArgs response = await hcAvatar.LoadAsync(fieldValue);

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                            result.Result = response.Entry.EntryDataObject;
                        else
                            ErrorHandling.HandleError(ref result, $"Error loading {objectName} with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error loading {objectName} with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error loading avatar with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private OASISResult<T> Load<T>(string objectName, string fieldName, string fieldValue, string zomeLoadFunctionName, int version = 0) where T : IHolon
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);

                if (hcAvatar != null)
                {
                    hcAvatar.ZomeLoadEntryFunction = zomeLoadFunctionName;
                    ZomeFunctionCallBackEventArgs response = hcAvatar.Load(fieldValue);

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                            result.Result = response.Entry.EntryDataObject;
                        else
                            ErrorHandling.HandleError(ref result, $"Error loading {objectName} with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error loading {objectName} with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error loading avatar with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }
        */

        /*
        private async Task<OASISResult<IAvatar>> LoadAvatarByXAsync(string fieldName, string fieldValue, string zomeLoadFunctionName, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);

                if (hcAvatar != null)
                {
                    hcAvatar.ZomeLoadEntryFunction = zomeLoadFunctionName;
                    ZomeFunctionCallBackEventArgs response = await hcAvatar.LoadAsync(fieldValue);

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                            result.Result = response.Entry.EntryDataObject;
                        else
                            ErrorHandling.HandleError(ref result, $"Error loading avatar with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error loading avatar with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error loading avatar with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private OASISResult<IAvatar> LoadAvatarByX(string fieldName, string fieldValue, string zomeLoadFunctionName, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);

                if (hcAvatar != null)
                {
                    hcAvatar.ZomeLoadEntryFunction = zomeLoadFunctionName;
                    ZomeFunctionCallBackEventArgs response = hcAvatar.Load(fieldValue);

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                            result.Result = response.Entry.EntryDataObject;
                        else
                            ErrorHandling.HandleError(ref result, $"Error loading avatar with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error loading avatar with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error loading avatar with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByXAsync(string fieldName, string fieldValue, string zomeLoadFunctionName, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);

                if (hcAvatar != null)
                {
                    hcAvatar.ZomeLoadEntryFunction = zomeLoadFunctionName;
                    ZomeFunctionCallBackEventArgs response = await hcAvatar.LoadAsync(fieldValue);

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                            result.Result = response.Entry.EntryDataObject;
                        else
                            ErrorHandling.HandleError(ref result, $"Error loading avatar with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error loading avatar with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error loading avatar with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private OASISResult<IAvatar> LoadAvatarDetailByX(string fieldName, string fieldValue, string zomeLoadFunctionName, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            try
            {
                HcAvatar hcAvatar = new HcAvatar(HoloNETClient);

                if (hcAvatar != null)
                {
                    hcAvatar.ZomeLoadEntryFunction = zomeLoadFunctionName;
                    ZomeFunctionCallBackEventArgs response = hcAvatar.Load(fieldValue);

                    if (response != null)
                    {
                        if (response.IsCallSuccessful && !response.IsError)
                            result.Result = response.Entry.EntryDataObject;
                        else
                            ErrorHandling.HandleError(ref result, $"Error loading avatar with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: { response.Message }");
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"Error loading avatar with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: Unknown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error loading avatar with {fieldName} {fieldValue} for HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }*/

        /// <summary>
        /// Handles any errors thrown by HoloNET or HoloOASIS. It fires the OnHoloOASISError error handler if there are any 
        /// subscriptions. The same applies to the OnStorageProviderError event implemented as part of the IOASISStorageProvider interface.
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
    }
}
