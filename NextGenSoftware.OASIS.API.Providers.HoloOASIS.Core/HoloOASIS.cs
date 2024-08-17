using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.ORM.Interfaces;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Repositories;
using DataHelper = NextGenSoftware.OASIS.API.Providers.HoloOASIS.Helpers.DataHelper;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public class HoloOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar, IOASISLocalStorageProvider
    {
        private const string OASIS_HAPP_ID = "oasis";
        private const string OASIS_HAPP_PATH = "OASIS_hAPP\\oasis.happ";
        private const string OASIS_HAPP_ROLE_NAME = "oasis";
        private const string ZOME_LOAD_AVATAR_BY_ID_FUNCTION = "get_entry_avatar_by_id";
        private const string ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION = "get_entry_avatar_by_username";
        private const string ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION = "get_entry_avatar_by_email";
        private const string ZOME_LOAD_ALL_AVATARS_FUNCTION = "get_all_avatars";
        private const string ZOME_DELETE_AVATAR_BY_ID_FUNCTION = "delete_entry_avatar_by_id";
        private const string ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION = "delete_entry_avatar_by_username";
        private const string ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION = "delete_entry_avatar_by_email";
        private const string ZOME_LOAD_AVATAR_DETAIL_BY_ID_FUNCTION = "get_entry_avatar_detail_by_id";
        private const string ZOME_LOAD_AVATAR_DETAIL_BY_USERNAME_FUNCTION = "get_entry_avatar_detail_by_username";
        private const string ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION = "get_entry_avatar_detail_by_email";
        private const string ZOME_LOAD_HOLON_FUNCTION_BY_ID = "get_entry_holon_by_id";
        private const string ZOME_LOAD_HOLONS_FOR_PARENT_BY_ID_FUNCTION = "get_holons_for_parent_by_id";

        private AvatarRepository _avatarRepository = null;
        private AvatarDetailRepository _avatarDetailRepository = null;
        private HolonRepository _holonRepository = null;
        private string _holochainConductorAppAgentURI = "";

        private bool _useReflection = false;
        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        //TODO: Not sure if we need these?
        //public delegate void AvatarSaved(object sender, AvatarSavedEventArgs e);
        //public event AvatarSaved OnPlayerAvatarSaved;

        //public delegate void AvatarLoaded(object sender, AvatarLoadedEventArgs e);
        //public event AvatarLoaded OnPlayerAvatarLoaded;

        public IHoloNETClientAdmin HoloNETClientAdmin { get; private set; }
        public IHoloNETClientAppAgent HoloNETClientAppAgent { get; private set; }

        public HoloOASIS(HoloNETClientAdmin holoNETClientAdmin, HoloNETClientAppAgent holoNETClientAppAgent, bool useReflection = true)
        {
            _useReflection = useReflection;
            this.HoloNETClientAdmin = holoNETClientAdmin;
            this.HoloNETClientAppAgent = holoNETClientAppAgent;
            Initialize();
        }

        public HoloOASIS(string holochainConductorAdminURI, bool useReflection = true)
        {
            _useReflection = useReflection;
            HoloNETClientAdmin = new HoloNETClientAdmin(new HoloNETDNA() { HolochainConductorAdminURI = holochainConductorAdminURI });
            Initialize();
        }

        public HoloOASIS(string holochainConductorAdminURI, string holochainConductorAppAgentURI, bool useReflection = true)
        {
            _useReflection = useReflection;
            _holochainConductorAppAgentURI = holochainConductorAppAgentURI;
            HoloNETClientAdmin = new HoloNETClientAdmin(new HoloNETDNA() { HolochainConductorAdminURI = holochainConductorAdminURI});
            Initialize();
        }

        private async Task Initialize()
        {
            this.ProviderName = "HoloOASIS";
            this.ProviderDescription = "Holochain Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.HoloOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);

            DataHelper.UseReflection = _useReflection;
            _avatarRepository = new AvatarRepository();
            _avatarDetailRepository = new AvatarDetailRepository();
            _holonRepository = new HolonRepository();
        }

        private void HoloNETClientAdmin_OnError(object sender, HoloNETErrorEventArgs e)
        {
            HandleError("Error Occured in HoloOASIS Provider With HoloNETClientAdmin_OnError Event Handler.", null, e);
        }

        private void HoloNETClientAppAgent_OnError(object sender, HoloNETErrorEventArgs e)
        {
            HandleError("Error Occured in HoloOASIS Provider With HoloNETClientAppAgent_OnError Event Handler.", null, e);
        }

        #region IOASISStorageProvider Implementation

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            bool adminConnected = false;
            string errorMessage = "Error Occured In HoloOASIS Provider in ActivateProviderAsync method. Reason: ";

            try
            {
                HoloNETClientAdmin.OnError += HoloNETClientAdmin_OnError;

                if (HoloNETClientAdmin.State == System.Net.WebSockets.WebSocketState.Open)
                    adminConnected = true;

                else if (!HoloNETClientAdmin.IsConnecting)
                {
                    HoloNETConnectedEventArgs adminConnectResult = await HoloNETClientAdmin.ConnectAsync();

                    if (adminConnectResult != null && adminConnectResult.IsConnected)
                        adminConnected = true;
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error Occured Connecting To HoloNETClientAdmin EndPoint {HoloNETClientAdmin.EndPoint.AbsoluteUri}. Reason: {adminConnectResult.Message}");
                }
                
                if (adminConnected)
                {
                    if (HoloNETClientAppAgent == null)
                    {
                        InstallEnableSignAttachAndConnectToHappEventArgs installedAppResult = await HoloNETClientAdmin.InstallEnableSignAttachAndConnectToHappAsync(OASIS_HAPP_ID, OASIS_HAPP_PATH, OASIS_HAPP_ROLE_NAME);

                        if (installedAppResult != null && installedAppResult.IsSuccess && !installedAppResult.IsError)
                        {
                            HoloNETClientAppAgent = installedAppResult.HoloNETClientAppAgent;
                            IsProviderActivated = true;
                            result.Result = true;
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error Occured Calling InstallEnableSignAttachAndConnectToHappAsync On HoloNETClientAppAgent EndPoint {HoloNETClientAdmin.EndPoint.AbsoluteUri}. Reason: {installedAppResult.Message}");
                    }
                    else if (HoloNETClientAppAgent.State != System.Net.WebSockets.WebSocketState.Open)
                    {
                        HoloNETConnectedEventArgs connectedResult = await HoloNETClientAppAgent.ConnectAsync();

                        if (connectedResult != null && !connectedResult.IsError && connectedResult.IsConnected)
                        {
                            IsProviderActivated = true;
                            result.Result = true;
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error Occured Connecting To HoloNETClientAppAgent EndPoint {HoloNETClientAppAgent.EndPoint.AbsoluteUri}. Reason: {connectedResult.Message}");
                    }
                }

                if (HoloNETClientAppAgent != null)
                    HoloNETClientAppAgent.OnError += HoloNETClientAppAgent_OnError;
            }
            catch (Exception e) 
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{e}");
            }

            return result;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;

            //OASISResult<bool> result = new OASISResult<bool>();
            //bool adminConnected = false;

            //try
            //{
            //    HoloNETClientAdmin.OnError += HoloNETClientAdmin_OnError;

            //    if (HoloNETClientAdmin.State == System.Net.WebSockets.WebSocketState.Open)
            //        adminConnected = true;

            //    else if (!HoloNETClientAdmin.IsConnecting)
            //    {
            //        HoloNETConnectedEventArgs adminConnectResult = HoloNETClientAdmin.Connect();

            //        if (adminConnectResult != null && adminConnectResult.IsConnected)
            //            adminConnected = true;
            //    }

            //    if (adminConnected)
            //    {
            //        if (HoloNETClientAppAgent == null)
            //        {
            //            InstallEnableSignAttachAndConnectToHappEventArgs installedAppResult = HoloNETClientAdmin.InstallEnableSignAttachAndConnectToHapp(OASIS_HAPP_ID, OASIS_HAPP_PATH, OASIS_HAPP_ROLE_NAME);

            //            if (installedAppResult != null && installedAppResult.IsSuccess && !installedAppResult.IsError)
            //            {
            //                HoloNETClientAppAgent = installedAppResult.HoloNETClientAppAgent;
            //                IsProviderActivated = true;
            //                result.Result = true;
            //            }
            //        }
            //        else if (HoloNETClientAppAgent.State != System.Net.WebSockets.WebSocketState.Open)
            //        {
            //            HoloNETConnectedEventArgs connectedResult = HoloNETClientAppAgent.Connect();

            //            if (connectedResult != null && !connectedResult.IsError && connectedResult.IsConnected)
            //            {
            //                IsProviderActivated = true;
            //                result.Result = true;
            //            }
            //            else
            //                OASISErrorHandling.HandleError(ref result, $"Error Occured In HoloOASIS Provider in ActivateProvider method. Reason: Error Occured Connecting To HoloNETClientAppAgent EndPoint {HoloNETClientAppAgent.EndPoint.AbsoluteUri}. Reason: {connectedResult.Message}");
            //        }
            //    }

            //    if (HoloNETClientAppAgent != null)
            //        HoloNETClientAppAgent.OnError += HoloNETClientAppAgent_OnError;
            //}
            //catch (Exception e)
            //{
            //    OASISErrorHandling.HandleError(ref result, $"Error Occured In HoloOASIS Provider in ActivateProvider method. Reason: {e}");
            //}

            //return result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            HoloNETDisconnectedEventArgs holoNETClientAdminResult = null;
            HoloNETDisconnectedEventArgs holoNETClientAppAgent = null;

            try
            {
                if (HoloNETClientAdmin != null && !HoloNETClientAdmin.IsDisconnecting)
                {
                    holoNETClientAdminResult = await HoloNETClientAdmin.DisconnectAsync();

                    if (!(holoNETClientAdminResult != null && !holoNETClientAdminResult.IsError && holoNETClientAdminResult.IsDisconnected))
                        OASISErrorHandling.HandleError(ref result, $"Error Occured In HoloOASIS Provider in DeActivateProviderAsync calling HoloNETClientAdmin.DisconnectAsync() method. Reason: {holoNETClientAdminResult.Message}");
                }

                if (HoloNETClientAppAgent != null && !HoloNETClientAppAgent.IsDisconnecting)
                {
                    holoNETClientAppAgent = await HoloNETClientAppAgent.DisconnectAsync();

                    if (!(holoNETClientAppAgent != null && !holoNETClientAppAgent.IsError && holoNETClientAppAgent.IsDisconnected))
                        OASISErrorHandling.HandleError(ref result, $"Error Occured In HoloOASIS Provider in DeActivateProviderAsync calling HoloNETClientAdmin.DisconnectAsync() method. Reason: {holoNETClientAppAgent.Message}");
                }

                if (HoloNETClientAdmin != null)
                    HoloNETClientAdmin.OnError -= HoloNETClientAdmin_OnError;
                
                if (HoloNETClientAppAgent != null)
                    HoloNETClientAppAgent.OnError -= HoloNETClientAppAgent_OnError;

                if (holoNETClientAdminResult != null && holoNETClientAdminResult.IsDisconnected && !holoNETClientAdminResult.IsError && holoNETClientAppAgent != null && holoNETClientAppAgent.IsDisconnected && !holoNETClientAppAgent.IsError)
                {
                    result.Result = true;
                    IsProviderActivated = false;
                }
                else if (holoNETClientAdminResult == null || holoNETClientAppAgent == null)
                {
                    result.Result = true;
                    IsProviderActivated = false;
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error Occured In HoloOASIS Provider in DeActivateProviderAsync method. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;

            //OASISResult<bool> result = new OASISResult<bool>();
            //HoloNETDisconnectedEventArgs holoNETClientAdminResult = null;
            //HoloNETDisconnectedEventArgs holoNETClientAppAgent = null;

            //try
            //{
            //    if (HoloNETClientAdmin != null && !HoloNETClientAdmin.IsDisconnecting)
            //    {
            //        holoNETClientAdminResult = HoloNETClientAdmin.Disconnect();

            //        if (!(holoNETClientAdminResult != null && !holoNETClientAdminResult.IsError && holoNETClientAdminResult.IsDisconnected))
            //            OASISErrorHandling.HandleError(ref result, $"Error Occured In HoloOASIS.DeActivateProvider calling HoloNETClientAdmin.Disconnect() method. Reason: {holoNETClientAdminResult.Message}");
            //    }

            //    if (HoloNETClientAppAgent != null && !HoloNETClientAppAgent.IsDisconnecting)
            //    {
            //        holoNETClientAppAgent = HoloNETClientAppAgent.Disconnect();

            //        if (!(holoNETClientAppAgent != null && !holoNETClientAppAgent.IsError && holoNETClientAppAgent.IsDisconnected))
            //            OASISErrorHandling.HandleError(ref result, $"Error Occured In HoloOASIS.DeActivateProvider calling HoloNETClientAdmin.Disconnect() method. Reason: {holoNETClientAdminResult.Message}");
            //    }

            //    if (HoloNETClientAdmin != null)
            //        HoloNETClientAdmin.OnError -= HoloNETClientAdmin_OnError;

            //    if (HoloNETClientAppAgent != null)
            //        HoloNETClientAppAgent.OnError -= HoloNETClientAppAgent_OnError;

            //    if (holoNETClientAdminResult.IsDisconnected && !holoNETClientAdminResult.IsError && holoNETClientAppAgent.IsDisconnected && !holoNETClientAppAgent.IsError)
            //    {
            //        result.Result = true;
            //        IsProviderActivated = false;
            //    }
            //}
            //catch (Exception e)
            //{
            //    OASISErrorHandling.HandleError(ref result, $"Error Occured In HoloOASIS Provider in DeActivateProvider method. Reason: {e}");
            //}

            //return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            return await ExecuteOperationAsync<IAvatar>(HcOperationEnum.Read, HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_LOAD_AVATAR_BY_ID_FUNCTION);
            //return await LoadAsync<IAvatar>(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_LOAD_AVATAR_BY_ID_FUNCTION);
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return ExecuteOperation<IAvatar>(HcOperationEnum.Read, HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_LOAD_AVATAR_BY_ID_FUNCTION);
            //return Load<IAvatar>(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_LOAD_AVATAR_BY_ID_FUNCTION);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            //ProviderKey is the entry hash.
            return await LoadAsync<IAvatar>(HcObjectTypeEnum.Avatar, "providerKey (entryhash)", providerKey);
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            //ProviderKey is the entry hash.
            return Load<IAvatar>(HcObjectTypeEnum.Avatar, "providerKey (entryhash)", providerKey);
        }


        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            //return await ExecuteOperationAsync<IAvatar>(OperationEnum.Read, "avatar", "email", avatarEmail, ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION, version);
            return await LoadAsync<IAvatar>(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            //return ExecuteOperation<IAvatar>(OperationEnum.Read, "avatar", "email", avatarEmail, ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION, version);
            return Load<IAvatar>(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            //return await ExecuteOperationAsync<IAvatar>(HcOperationEnum.Read, "avatar", "username", avatarUsername, ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION, version);
            return await LoadAsync<IAvatar>(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION);
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            //return ExecuteOperation<IAvatar>(HcOperationEnum.Read, "avatar", "email", avatarUsername, ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION, version);
            return Load<IAvatar>(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            //return await ExecuteOperationAsync<IAvatarDetail>(HcOperationEnum.Read, "avatar detail", "id", id.ToString(), ZOME_LOAD_AVATAR_DETAIL_BY_ID_FUNCTION, version);
            return await LoadAsync<IAvatarDetail>(HcObjectTypeEnum.AvatarDetail, "id", id.ToString(), ZOME_LOAD_AVATAR_DETAIL_BY_ID_FUNCTION);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            //return ExecuteOperation<IAvatarDetail>(HcOperationEnum.Read, "avatar detail", "id", id.ToString(), ZOME_LOAD_AVATAR_DETAIL_BY_ID_FUNCTION, version);
            return Load<IAvatarDetail>(HcObjectTypeEnum.AvatarDetail, "id", id.ToString(), ZOME_LOAD_AVATAR_DETAIL_BY_ID_FUNCTION);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            //return await ExecuteOperationAsync<IAvatarDetail>(HcOperationEnum.Read, "avatar detail", "email", avatarEmail, ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION, version);
            return await LoadAsync<IAvatarDetail>(HcObjectTypeEnum.AvatarDetail, "email", avatarEmail, ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            //return ExecuteOperation<IAvatarDetail>(HcOperationEnum.Read, "avatar detail", "email", avatarEmail, ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION, version);
            return Load<IAvatarDetail>(HcObjectTypeEnum.AvatarDetail, "email", avatarEmail, ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            //return await ExecuteOperationAsync<IAvatarDetail>(HcOperationEnum.Read, "avatar detail", "username", avatarUsername, ZOME_LOAD_AVATAR_DETAIL_BY_USERNAME_FUNCTION, version);
            return await LoadAsync<IAvatarDetail>(HcObjectTypeEnum.AvatarDetail, "username", avatarUsername, ZOME_LOAD_AVATAR_DETAIL_BY_USERNAME_FUNCTION);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return Load<IAvatarDetail>(HcObjectTypeEnum.AvatarDetail, "username", avatarUsername, ZOME_LOAD_AVATAR_DETAIL_BY_USERNAME_FUNCTION);
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            //return null;
            return await ExecuteOperationAsync<IEnumerable<IAvatar>>(CollectionOperationEnum.ReadCollection, "avatars", "listanchor", ZOME_LOAD_ALL_AVATARS_FUNCTION, version);
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return null;
            // return ExecuteOperation<IEnumerable<IAvatar>>(CollectionOperationEnum.ReadCollection, "avatars", "listanchor", ZOME_LOAD_ALL_AVATARS_FUNCTION, version);
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            return null;
            // return await ExecuteOperationAsync<IEnumerable<IAvatarDetail>>(CollectionOperationEnum.ReadCollection, "avatar details", "listanchor", ZOME_LOAD_ALL_AVATAR_DETAILS_FUNCTION, version);
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return null;
            //return ExecuteOperation<IEnumerable<IAvatarDetail>>(CollectionOperationEnum.ReadCollection, "avatar details", "listanchor", ZOME_LOAD_ALL_AVATAR_DETAILS_FUNCTION, version);
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            return await SaveAsync(HcObjectTypeEnum.Avatar, avatar);
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return Save(HcObjectTypeEnum.Avatar, avatar);
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            return await SaveAsync(HcObjectTypeEnum.AvatarDetail, avatarDetail);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return Save(HcObjectTypeEnum.AvatarDetail, avatarDetail);
        }

        //public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        //{
        //    return await DeleteAsync(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_DELETE_AVATAR_BY_ID_FUNCTION, softDelete);
        //}

        //public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        //{
        //    return Delete(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_DELETE_AVATAR_BY_ID_FUNCTION, softDelete);
        //}

        //public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        //{
        //    return await DeleteAsync(HcObjectTypeEnum.Avatar, "providerKey (entryHash)", providerKey, "", softDelete);
        //}

        //public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        //{
        //    return Delete(HcObjectTypeEnum.Avatar, "providerKey (entryHash)", providerKey, "", softDelete);
        //}

        //public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        //{
        //    return await DeleteAsync(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION, softDelete);
        //}

        //public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        //{
        //    return Delete(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION, softDelete);
        //}

        //public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        //{
        //    return await DeleteAsync(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION, softDelete);
        //}

        //public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        //{
        //    return Delete(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION, softDelete);
        //}

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            return await DeleteAsync(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_DELETE_AVATAR_BY_ID_FUNCTION);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return Delete(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_DELETE_AVATAR_BY_ID_FUNCTION);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return await DeleteAsync(HcObjectTypeEnum.Avatar, "providerKey (entryHash)", providerKey, "");
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return Delete(HcObjectTypeEnum.Avatar, "providerKey (entryHash)", providerKey, "");
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            return await DeleteAsync(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return Delete(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            return await DeleteAsync(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return Delete(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION);
        }

        //public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        //{
        //    return await LoadAsync<IHolon>(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_LOAD_HOLON_FUNCTION_BY_ID, version, new { loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError});
        //}

        //public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        //{
        //    return Load<IHolon>(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_LOAD_HOLON_FUNCTION_BY_ID, version, new { loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        //}

        //public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        //{
        //    return await LoadAsync<IHolon>(HcObjectTypeEnum.Holon, "providerKey (entryHash)", providerKey, "", version, new { loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        //}

        //public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        //{
        //    return Load<IHolon>(HcObjectTypeEnum.Holon, "providerKey (entryHash)", providerKey, "", version, new { loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        //}

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await LoadAsync<IHolon>(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_LOAD_HOLON_FUNCTION_BY_ID, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
            });
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return Load<IHolon>(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_LOAD_HOLON_FUNCTION_BY_ID, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
            });
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await LoadAsync<IHolon>(HcObjectTypeEnum.Holon, "providerKey (entryHash)", providerKey, ZOME_LOAD_HOLON_FUNCTION_BY_ID, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
            });
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return Load<IHolon>(HcObjectTypeEnum.Holon, "providerKey (entryHash)", providerKey, ZOME_LOAD_HOLON_FUNCTION_BY_ID, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
            });
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await _avatarRepository.LoadAvatarsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_ID_FUNCTION, version, new { id = id, type = type, loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });

        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
            //return LoadCollection("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_ID_FUNCTION, version, new { id = id, type = type, loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
            //return await LoadCollectionAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_PROVIDER_KEY_FUNCTION, version, new { providerKey = providerKey, type = type, loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
            //return LoadCollection("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_PROVIDER_KEY_FUNCTION, version, new { providerKey = providerKey, type = type, loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
            //return await LoadCollectionAsync("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version, new { type = type, loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
            //return LoadCollection("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version, new { type = type, loadChildren = loadChildren, recursive = recursive, maxChildDepth = maxChildDepth, continueOnError = continueOnError });
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
 
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
          
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
        
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
         
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
    
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id, bool softDelete = true)
        {
           
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey, bool softDelete = true)
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

        public OASISResult<ITransactionRespone> SendTransaction(IWalletTransactionRequest transation)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<ITransactionRespone>> SendTransactionAsync(IWalletTransactionRequest transation)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ITransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISNFTProvider

        public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest transation)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest transation)
        {
            throw new NotImplementedException();
        }

        public OASISResult<INFTTransactionRespone> MintNFT(IMintNFTTransactionRequest transation)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<INFTTransactionRespone>> MintNFTAsync(IMintNFTTransactionRequest transation)
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

        #region Private Methods

        
        //private async Task<OASISResult<T>> ExecuteOperationAsync<T>(HcOperationEnum operation, HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeFunctionName = "", int version = 0) where T : IHolonBase
        //{
        //    OASISResult<T> result = new OASISResult<T>();

        //    try
        //    {
        //        IHoloNETAuditEntryBase hcObject = null;

        //        switch (hcObjectType)
        //        {
        //            case HcObjectTypeEnum.Avatar:
        //                hcObject = new HcAvatar(HoloNETClientAppAgent);
        //                break;

        //            case HcObjectTypeEnum.AvatarDetail:
        //                hcObject = new HcAvatarDetail(HoloNETClientAppAgent);
        //                break;

        //            case HcObjectTypeEnum.Holon:
        //                hcObject = new HcHolon(HoloNETClientAppAgent);
        //                break;
        //        }

        //        ZomeFunctionCallBackEventArgs response = null;

        //        if (hcObject != null)
        //        {
        //            //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
        //            if (!string.IsNullOrEmpty(zomeFunctionName))
        //            {
        //                switch (operation)
        //                {
        //                    case HcOperationEnum.Create:
        //                        hcObject.ZomeCreateEntryFunction = zomeFunctionName;
        //                        break;

        //                    case HcOperationEnum.Read:
        //                         hcObject.ZomeLoadEntryFunction = zomeFunctionName;
        //                        break;

        //                    case HcOperationEnum.Update:
        //                        hcObject.ZomeUpdateEntryFunction = zomeFunctionName;
        //                        break;

        //                    case HcOperationEnum.Delete:
        //                        hcObject.ZomeDeleteEntryFunction = zomeFunctionName;
        //                        break;
        //                } 
        //            }

        //            switch (operation)
        //            {
        //                case HcOperationEnum.Create:
        //                    response = await hcObject.SaveAsync(response);
        //                    break;

        //                case HcOperationEnum.Read:
        //                    response = await hcObject.LoadAsync(fieldValue);
        //                    break;

        //                case HcOperationEnum.Update:
        //                    response = await hcObject.SaveAsync(fieldValue);
        //                    break;

        //                case HcOperationEnum.Delete:
        //                    response = await hcObject.DeleteAsync(fieldValue);
        //                    break;
        //            }

        //            if (response != null)
        //            {
        //                if (!response.IsError)
        //                    result.Result = response.Records[0].EntryDataObject; 
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {Enum.GetName(hcObjectType)} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: { response.Message }");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {Enum.GetName(hcObjectType)} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: Unknown.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {Enum.GetName(hcObjectType)} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: {ex}.");
        //    }

        //    return result;
        //}

        //private OASISResult<T> ExecuteOperation<T>(HcOperationEnum operation, HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeFunctionName = "", int version = 0) where T : IHolonBase
        //{
        //    OASISResult<T> result = new OASISResult<T>();
        //    ZomeFunctionCallBackEventArgs response = null;

        //    try
        //    {
        //        IHoloNETAuditEntryBase hcObject = null;

        //        switch (hcObjectType)
        //        {
        //            case HcObjectTypeEnum.Avatar:
        //                hcObject = new HcAvatar(HoloNETClientAppAgent);
        //                break;

        //            case HcObjectTypeEnum.AvatarDetail:
        //                hcObject = new HcAvatarDetail(HoloNETClientAppAgent);
        //                break;

        //            case HcObjectTypeEnum.Holon:
        //                hcObject = new HcHolon(HoloNETClientAppAgent);
        //                break;
        //        }

        //        if (hcObject != null)
        //        {
        //            //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
        //            if (!string.IsNullOrEmpty(zomeFunctionName))
        //            {
        //                switch (operation)
        //                {
        //                    case HcOperationEnum.Create:
        //                        hcObject.ZomeCreateEntryFunction = zomeFunctionName;
        //                        break;

        //                    case HcOperationEnum.Read:
        //                        hcObject.ZomeLoadEntryFunction = zomeFunctionName;
        //                        break;

        //                    case HcOperationEnum.Update:
        //                        hcObject.ZomeUpdateEntryFunction = zomeFunctionName;
        //                        break;

        //                    case HcOperationEnum.Delete:
        //                        hcObject.ZomeDeleteEntryFunction = zomeFunctionName;
        //                        break;
        //                }
        //            }

        //            switch (operation)
        //            {
        //                case HcOperationEnum.Create:
        //                    response = hcObject.Save(response);
        //                    break;

        //                case HcOperationEnum.Read:
        //                    response = hcObject.Load(fieldValue);
        //                    break;

        //                case HcOperationEnum.Update:
        //                    response = hcObject.Save(fieldValue);
        //                    break;

        //                case HcOperationEnum.Delete:
        //                    response = hcObject.Delete(fieldValue);
        //                    break;
        //            }

        //            if (response != null)
        //            {
        //                if (!response.IsError)
        //                    result.Result = response.Records[0].EntryDataObject;
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {Enum.GetName(hcObjectType)} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: {response.Message}");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {Enum.GetName(hcObjectType)} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: Unknown.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on object {Enum.GetName(hcObjectType)} with field {fieldName} and value {fieldValue} for HoloOASIS Provider. Reason: {ex}.");
        //    }

        //    return result;
        //}

        //private async Task<OASISResult<IEnumerable<T>>> ExecuteOperationAsync<T1, T2>(CollectionOperationEnum operation, HcObjectTypeEnum hcObjectType, string collectionName, string collectionAnchor, string zomeFunctionName = "", int version = 0) where T1 : IHolonBase where T2 : IHoloNETEntryBase
        //{
        //    OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();

        //    try
        //    {
        //        HcAvatar hcAvatar = new HcAvatar(HoloNETClientAppAgent);
        //        ZomeFunctionCallBackEventArgs response = null;

        //        HoloNETCollection<T2> collection = null;

        //        switch (hcObjectType)
        //        {
        //            case HcObjectTypeEnum.Avatar:
        //                collection = new HcAvatarCollection(HoloNETClientAppAgent);
        //                break;

        //            case HcObjectTypeEnum.AvatarDetail:
        //                collection = new HcAvatarDetail(HoloNETClientAppAgent);
        //                break;

        //            case HcObjectTypeEnum.Holon:
        //                collection = new HcHolon(HoloNETClientAppAgent);
        //                break;
        //        }

        //        if (hcAvatar != null)
        //        {
        //            //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
        //            if (!string.IsNullOrEmpty(zomeFunctionName))
        //            {
        //                switch (operation)
        //                {
        //                    //case CollectionOperationEnum.CreateCollection:
        //                    //    {
        //                    //        hcAvatar.ZomeCreateEntryCollectionFunction = zomeFunctionName;
        //                    //        response = await hcAvatar.SaveCollectionAsync(collectionAnchor);
        //                    //    }
        //                    //    break;

        //                    case CollectionOperationEnum.ReadCollection:
        //                        {
        //                            hcAvatar.ZomeLoadCollectionFunction = zomeFunctionName;
        //                            response = await hcAvatar.LoadCollectionAsync(collectionAnchor);
        //                        }
        //                        break;

        //                    case CollectionOperationEnum.AddToCollection:
        //                        {
        //                            hcAvatar.ZomeAddToCollectionFunction = zomeFunctionName;
        //                            response = await hcAvatar.AddToCollectionAsync(collectionAnchor);
        //                        }
        //                        break;

        //                    case CollectionOperationEnum.UpdateCollection:
        //                        {
        //                            hcAvatar.ZomeUpdateCollectionFunction = zomeFunctionName;
        //                            response = await hcAvatar.UpdateCollectionAsync(collectionAnchor);
        //                        }
        //                        break;

        //                    case CollectionOperationEnum.RemoveFromCollection:
        //                        {
        //                            hcAvatar.ZomeRemoveFromCollectionFunction = zomeFunctionName;
        //                            response = await hcAvatar.RemoveFromCollectionAsync(collectionAnchor);
        //                        }
        //                        break;

        //                        //case CollectionOperationEnum.DeleteCollection:
        //                        //    {
        //                        //        hcAvatar.ZomeDeleteEntryCollectionFunction = zomeFunctionName;
        //                        //        response = await hcAvatar.DeleteCollectionAsync(collectionAnchor);
        //                        //    }
        //                        //    break;
        //                }
        //            }

        //            if (response != null)
        //            {
        //                if (response.IsCallSuccessful && !response.IsError)
        //                    result.Result = response.Entry.EntryDataObject;
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} in the ExecuteOperationAsync method in the HoloOASIS Provider. Reason: {response.Message}");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} in the ExecuteOperationAsync method in the HoloOASIS Provider. Reason: Unknown.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} in the ExecuteOperationAsync method in the HoloOASIS Provider. Reason: {ex}.");
        //    }

        //    return result;
        //}

        //private OASISResult<IEnumerable<T>> ExecuteOperation<T>(CollectionOperationEnum operation, string collectionName, string collectionAnchor, string zomeFunctionName = "", int version = 0) where T : IHolonBase
        //{
        //    OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

        //    //TODO: Need to implement loading collections in HoloNET ASAP! :)

        //    try
        //    {
        //        HcAvatar hcAvatar = new HcAvatar(HoloNETClientAppAgent);
        //        ZomeFunctionCallBackEventArgs response = null;

        //        if (hcAvatar != null)
        //        {
        //            //If it is not null then override the zome function, otherwise it will use the defaults passed in via the constructors for HcAvatar.
        //            if (!string.IsNullOrEmpty(zomeFunctionName))
        //            {
        //                switch (operation)
        //                {
        //                    //case CollectionOperationEnum.CreateCollection:
        //                    //    {
        //                    //        hcAvatar.ZomeCreateEntryCollectionFunction = zomeFunctionName;
        //                    //        response = await hcAvatar.SaveCollectionAsync(collectionAnchor);
        //                    //    }
        //                    //    break;

        //                    case CollectionOperationEnum.ReadCollection:
        //                        {
        //                            hcAvatar.ZomeLoadCollectionFunction = zomeFunctionName;
        //                            response = await hcAvatar.LoadCollectionAsync(collectionAnchor);
        //                        }
        //                        break;

        //                    case CollectionOperationEnum.AddToCollection:
        //                        {
        //                            hcAvatar.ZomeAddToCollectionFunction = zomeFunctionName;
        //                            response = await hcAvatar.AddToCollectionAsync(collectionAnchor);
        //                        }
        //                        break;

        //                    case CollectionOperationEnum.UpdateCollection:
        //                        {
        //                            hcAvatar.ZomeUpdateCollectionFunction = zomeFunctionName;
        //                            response = await hcAvatar.UpdateCollectionAsync(collectionAnchor);
        //                        }
        //                        break;

        //                    case CollectionOperationEnum.RemoveFromCollection:
        //                        {
        //                            hcAvatar.ZomeRemoveFromCollectionFunction = zomeFunctionName;
        //                            response = await hcAvatar.RemoveFromCollectionAsync(collectionAnchor);
        //                        }
        //                        break;

        //                        //case CollectionOperationEnum.DeleteCollection:
        //                        //    {
        //                        //        hcAvatar.ZomeDeleteEntryCollectionFunction = zomeFunctionName;
        //                        //        response = await hcAvatar.DeleteCollectionAsync(collectionAnchor);
        //                        //    }
        //                        //    break;
        //                }
        //            }

        //            if (response != null)
        //            {
        //                if (response.IsCallSuccessful && !response.IsError)
        //                    result.Result = response.Entry.EntryDataObject;
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} in the ExecuteOperation method in the HoloOASIS Provider. Reason: {response.Message}");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} in the ExecuteOperation method in the HoloOASIS Provider. Reason: Unknown.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error executing operation {Enum.GetName(operation)} on collection {collectionName} with anchor {collectionAnchor} in the ExecuteOperation method in the HoloOASIS Provider. Reason: {ex}.");
        //    }

        //    return result;
        //}


        private async Task<OASISResult<T>> LoadAsync<T>(HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeLoadFunctionName = "", int version = 0, Dictionary<string, string> customDataKeyValuePairs = null) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                IHoloNETAuditEntryBase hcObject = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.Holon:
                        hcObject = new HcHolon(HoloNETClientAppAgent);
                        break;
                }

                if (hcObject != null)
                {
                    if (!string.IsNullOrEmpty(zomeLoadFunctionName))
                        hcObject.ZomeLoadEntryFunction = zomeLoadFunctionName;

                    result = HandleLoadResponse(await hcObject.LoadByCustomFieldAsync(fieldValue, fieldName, customDataKeyValuePairs, _useReflection), hcObjectType, fieldName, fieldValue, hcObject, result);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading {Enum.GetName(hcObjectType)} with {fieldValue} in the LoadAsync method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private OASISResult<T> Load<T>(HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeLoadFunctionName = "", int version = 0, Dictionary<string, string> customDataKeyValuePairs = null) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                IHoloNETAuditEntryBase hcObject = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.Holon:
                        hcObject = new HcHolon(HoloNETClientAppAgent);
                        break;
                }

                if (hcObject != null)
                {
                    if (!string.IsNullOrEmpty(zomeLoadFunctionName))
                        hcObject.ZomeLoadEntryFunction = zomeLoadFunctionName;

                    result = HandleLoadResponse(hcObject.LoadByCustomField(fieldValue, fieldName, version, customDataKeyValuePairs, _useReflection), hcObjectType, fieldName, fieldValue, hcObject, result);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading {Enum.GetName(hcObjectType)} with {fieldValue} in the Load method in the HoloOASIS Provider. Reason: {ex}.");
            }

            return result;
        }

        private async Task<OASISResult<T>> SaveAsync<T>(HcObjectTypeEnum hcObjectType, T holon) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                ZomeFunctionCallBackEventArgs response = null;
                IHoloNETAuditEntryBase hcObject = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.Holon:
                        hcObject = new HcHolon(HoloNETClientAppAgent);
                        break;
                }

                if (holon.Id == Guid.Empty)
                    holon.Id = Guid.NewGuid();

                //If it is configured to not use Reflection then we would do it like this passing in our own params object.
                if (!_useReflection)
                {
                    switch (hcObjectType)
                    {
                        case HcObjectTypeEnum.Avatar:
                            response = await hcObject.SaveAsync(DataHelper.ConvertAvatarToParamsObject((IAvatar)holon));
                            break;

                        case HcObjectTypeEnum.AvatarDetail:
                            response = await hcObject.SaveAsync(DataHelper.ConvertAvatarDetailToParamsObject((IAvatarDetail)holon));
                            break;
                    }
                }
                else
                {
                    switch (hcObjectType)
                    {
                        case HcObjectTypeEnum.Avatar:
                            hcObject = DataHelper.ConvertAvatarToHoloOASISAvatar((IAvatar)holon, (IHcAvatar)hcObject);
                            break;

                        case HcObjectTypeEnum.AvatarDetail:
                            hcObject = DataHelper.ConvertAvatarDetailToHoloOASISAvatarDetail((IAvatarDetail)holon, (IHcAvatarDetail)hcObject);
                            break;
                    }

                    //Otherwise we could just use this dyanmic version (which uses reflection) to dyamically build the params object (but we need to make sure properties have the HolochainFieldName attribute).
                    response = await hcObject.SaveAsync();
                }

                if (response != null)
                    result = HandleSaveResponse(response, hcObjectType, holon, hcObject, result);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknwon error has occured saving {Enum.GetName(hcObjectType)} with id {holon.Id} and name {holon.Name} in the SaveAsync method in HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        private OASISResult<T> Save<T>(HcObjectTypeEnum hcObjectType, T holon) where T : IHolonBase
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                IHoloNETAuditEntryBase hcObject = null;
                ZomeFunctionCallBackEventArgs response = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.Holon:
                        hcObject = new HcHolon(HoloNETClientAppAgent);
                        break;
                }

                if (holon.Id == Guid.Empty)
                    holon.Id = Guid.NewGuid();

                //If it is configured to not use Reflection then we would do it like this passing in our own params object.
                if (!_useReflection)
                {
                    switch (hcObjectType)
                    {
                        case HcObjectTypeEnum.Avatar:
                            response = hcObject.Save(ConvertAvatarToParamsObject((IAvatar)holon));
                            break;

                        case HcObjectTypeEnum.AvatarDetail:
                            response = hcObject.Save(ConvertAvatarDetailToParamsObject((IAvatarDetail)holon));
                            break;
                    }
                }
                else
                {
                    switch (hcObjectType)
                    {
                        case HcObjectTypeEnum.Avatar:
                            hcObject = ConvertAvatarToHoloOASISAvatar((IAvatar)holon, (IHcAvatar)hcObject);
                            break;

                        case HcObjectTypeEnum.AvatarDetail:
                            hcObject = ConvertAvatarDetailToHoloOASISAvatarDetail((IAvatarDetail)holon, (IHcAvatarDetail)hcObject);
                            break;
                    }

                    //Otherwise we could just use this dyanmic version (which uses reflection) to dyamically build the params object (but we need to make sure properties have the HolochainFieldName attribute).
                    response = hcObject.Save();
                }

                if (response != null)
                    result = HandleSaveResponse(response, hcObjectType, holon, hcObject, result);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknwon error has occured saving {Enum.GetName(hcObjectType)} with id {holon.Id} and name {holon.Name} in the Save method in HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        //private async Task<OASISResult<bool>> DeleteAsync(HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeDeleteFunctionName = "", bool softDelete = true, object additionalParams = null)
        private async Task<OASISResult<bool>> DeleteAsync(HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeDeleteFunctionName = "", Dictionary<string, string> customDataKeyValuePairs = null)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);

            try
            {
                IHoloNETAuditEntryBase hcObject = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.Holon:
                        hcObject = new HcHolon(HoloNETClientAppAgent);
                        break;
                }

                if (!string.IsNullOrEmpty(zomeDeleteFunctionName))
                    hcObject.ZomeDeleteEntryFunction = zomeDeleteFunctionName;

                hcObject.DeleteByCustomField(fieldValue, fieldName, customDataKeyValuePairs);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknwon error has occured deleting the {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the DeleteAsync method in HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        //private OASISResult<bool> Delete(HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeDeleteFunctionName = "", bool softDelete = true, Dictionary<string, string> customDataKeyValuePairs = null)
        private OASISResult<bool> Delete(HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, string zomeDeleteFunctionName = "", Dictionary<string, string> customDataKeyValuePairs = null)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);

            try
            {
                IHoloNETAuditEntryBase hcObject = null;

                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        hcObject = new HcAvatar(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        hcObject = new HcAvatarDetail(HoloNETClientAppAgent);
                        break;

                    case HcObjectTypeEnum.Holon:
                        hcObject = new HcHolon(HoloNETClientAppAgent);
                        break;
                }

                if (!string.IsNullOrEmpty(zomeDeleteFunctionName))
                    hcObject.ZomeDeleteEntryFunction = zomeDeleteFunctionName;

                //hcAvatar.DeleteByCustomField(fieldValue, fieldName, customDataKeyValuePairs, softDelete);
                hcObject.DeleteByCustomField(fieldValue, fieldName, customDataKeyValuePairs);
  
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknwon error has occured deleting the {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the Delete method in HoloOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        private OASISResult<T> HandleLoadResponse<T>(ZomeFunctionCallBackEventArgs response, HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, IHoloNETAuditEntryBase hcObject, OASISResult<T> result) where T : IHolonBase
        {
            if (response != null)
            {
                if (!response.IsError)
                    result = ConvertHCResponseToOASISResult(response, hcObjectType, hcObject, result);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error loading {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the HandleLoadResponse method in the HoloOASIS Provider. Reason: { response.Message }");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error loading {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the HandleLoadResponse method in the HoloOASIS Provider. Reason: Unknown.");

            return result;
        }

        private OASISResult<T> HandleSaveResponse<T>(ZomeFunctionCallBackEventArgs response, HcObjectTypeEnum hcObjectType, IHolonBase holon, IHoloNETAuditEntryBase hcObject, OASISResult<T> result) where T : IHolonBase
        {
            if (response != null)
            {
                if (!response.IsError)
                    result = ConvertHCResponseToOASISResult(response, hcObjectType, hcObject, result);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error saving {Enum.GetName(hcObjectType)} with id {holon.Id} and name {holon.Name} in the HandleSaveResponse method in the HoloOASIS Provider. Reason: { response.Message }");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error saving {Enum.GetName(hcObjectType)} with id {holon.Id} and name {holon.Name} in the HandleSaveResponse method in the HoloOASIS Provider. Reason: Unknown.");

            return result;
        }

        private OASISResult<T> HandleDeleteResponse<T>(ZomeFunctionCallBackEventArgs response, HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, IHoloNETAuditEntryBase hcObject, OASISResult<T> result, string methodName) where T : IHolonBase
        {
            if (response != null)
            {
                if (!response.IsError)
                    result = ConvertHCResponseToOASISResult(response, hcObjectType, hcObject, result);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error deleting {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the {methodName} method in the HoloOASIS Provider. Reason: { response.Message }");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error deleting {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the {methodName} method in the HoloOASIS Provider. Reason: Unknown.");

            return result;
        }

        private OASISResult<T> HandleResponse<T>(ZomeFunctionCallBackEventArgs response, HcObjectTypeEnum hcObjectType, string fieldName, string fieldValue, IHoloNETAuditEntryBase hcObject, OASISResult<T> result, string methodName) where T : IHolonBase
        {
            if (response != null)
            {
                if (!response.IsError)
                    result = DataHelper.ConvertHCResponseToOASISResult(response, hcObjectType, hcObject, result);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error deleting {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the {methodName} method in the HoloOASIS Provider. Reason: { response.Message }");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error deleting {Enum.GetName(hcObjectType)} with {fieldName} {fieldValue} in the {methodName} method in the HoloOASIS Provider. Reason: Unknown.");

            return result;
        }

        //private OASISResult<T2> HandleLoadCollectionResponse<T1, T2>(HoloNETCollectionLoadedResult<T1> response, string collectionName, string collectionAnchor, OASISResult<T2> result) where T1 : IHoloNETEntryBase where T2 : IHolonBase
        //{
        //    if (response != null)
        //    {
        //        if (!response.IsError)
        //            result.Result = response.EntriesLoaded;
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"Error loading collection {collectionName} with anchor {collectionAnchor} in the LoadCollectionAsync method in the HoloOASIS Provider. Reason: {response.Message}");
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error loading collection {collectionName} with anchor {collectionAnchor} in the LoadCollectionAsync method in the HoloOASIS Provider. Reason: Unknown.");

        //    return result;
        //}


        /// <summary>
        /// Handles any errors thrown by HoloNET or HoloOASIS. It fires the OnHoloOASISError error handler if there are any 
        /// subscriptions. The same applies to the OnStorageProviderError event implemented as part of the IOASISStorageProvider interface.
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="errorDetails"></param>
        /// <param name="holoNETEventArgs"></param>
        private void HandleError(string reason, Exception errorDetails, HoloNETErrorEventArgs holoNETEventArgs)
        {
            RaiseStorageProviderErrorEvent(holoNETEventArgs.EndPoint.AbsoluteUri, string.Concat(reason, holoNETEventArgs != null ? string.Concat(". HoloNET Error: ", holoNETEventArgs.Reason, ". Error Details: ", holoNETEventArgs.ErrorDetails) : ""), errorDetails);

            //OnStorageProviderError?.Invoke(this, new OASISErrorEventArgs { EndPoint = HoloNETClientAppAgent.EndPoint.AbsoluteUri, Reason = string.Concat(reason, holoNETEventArgs != null ? string.Concat(" - HoloNET Error: ", holoNETEventArgs.Reason, " - ", holoNETEventArgs.ErrorDetails.ToString()) : ""), Exception = errorDetails });
            //OnStorageProviderError?.Invoke(this, new AvatarManagerErrorEventArgs { EndPoint = this.HoloNETClientAppAgent.EndPoint, Reason = string.Concat(reason, holoNETEventArgs != null ? string.Concat(" - HoloNET Error: ", holoNETEventArgs.Reason, " - ", holoNETEventArgs.ErrorDetails.ToString()) : ""), ErrorDetails = errorDetails });
            // OnHoloOASISError?.Invoke(this, new HoloOASISErrorEventArgs() { EndPoint = HoloNETClientAppAgent.EndPoint.AbsoluteUri, Reason = reason, ErrorDetails = errorDetails, HoloNETErrorDetails = holoNETEventArgs });
        }

        public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
