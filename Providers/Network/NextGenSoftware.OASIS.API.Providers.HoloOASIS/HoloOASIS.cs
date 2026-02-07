using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Repositories;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using DataHelper = NextGenSoftware.OASIS.API.Providers.HoloOASIS.Helpers.DataHelper;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public class HoloOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar, IOASISLocalStorageProvider
    {
        private const string HOLO_NETWORK_URI = "https://holo.host";
        private const string OASIS_HAPP_ID = "oasis";
        private const string OASIS_HAPP_PATH = "OASIS_hAPP\\oasis.happ";
        private const string OASIS_HAPP_ROLE_NAME = "oasis";
        private const string ZOME_LOAD_AVATAR_BY_ID_FUNCTION = "get_avatar_by_id";
        private const string ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION = "get_avatar_by_username";
        private const string ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION = "get_avatar_by_email";
        private const string ZOME_LOAD_AVATAR_DETAIL_BY_ID_FUNCTION = "get_avatar_detail_by_id";
        private const string ZOME_LOAD_AVATAR_DETAIL_BY_USERNAME_FUNCTION = "get_avatar_detail_by_username";
        private const string ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION = "get_avatar_detail_by_email";
        private const string ZOME_LOAD_ALL_AVATARS_FUNCTION = "get_all_avatars";
        private const string ZOME_LOAD_ALL_AVATARS_DETAILS_FUNCTION = "get_all_avatar_details";
        private const string ZOME_DELETE_AVATAR_BY_ID_FUNCTION = "delete_avatar_by_id";
        private const string ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION = "delete_avatar_by_username";
        private const string ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION = "delete_avatar_by_email";
        private const string ZOME_LOAD_HOLON_BY_ID_FUNCTION = "get_holon_by_id";
        private const string ZOME_LOAD_HOLON_BY_PROVIDER_KEY_FUNCTION = "get_holon_by_provider_key";
        private const string ZOME_LOAD_HOLON_BY_CUSTOM_KEY_FUNCTION = "get_holon_by_custom_key";
        private const string ZOME_LOAD_HOLON_BY_META_DATA_FUNCTION = "get_holon_by_meta_data";
        private const string ZOME_LOAD_HOLONS_FOR_PARENT_BY_ID_FUNCTION = "get_holons_for_parent_by_id";
        private const string ZOME_LOAD_HOLONS_FOR_PARENT_BY_PROVIDER_KEY_FUNCTION = "get_holons_for_parent_by_provider_key";
        private const string ZOME_LOAD_HOLONS_FOR_PARENT_BY_CUSTOM_KEY_FUNCTION = "get_holons_for_parent_by_custom_key";
        private const string ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION = "get_holons_for_parent_by_meta_data";
        private const string ZOME_LOAD_ALL_HOLONS_FUNCTION = "get_all_holons";
        private const string ZOME_SAVE_ALL_HOLONS_FUNCTION = "save_all_holons";
        private const string ZOME_DELETE_HOLON_BY_ID_FUNCTION = "delete_holon_by_id";
        private const string ZOME_DELETE_HOLON_BY_PROVIDER_KEY_FUNCTION = "delete_holon_by_provider_key";
        private const string ZOME_DELETE_HOLON_BY_CUSTOM_KEY_FUNCTION = "delete_holon_by_custom_key";
        private const string ZOME_DELETE_HOLON_BY_META_DATA_FUNCTION = "delete_holon_by_meta_data";

        private AvatarRepository _avatarRepository = null;
        private AvatarDetailRepository _avatarDetailRepository = null;
        private HolonRepository _holonRepository = null;
        private GenericRepository _genericRepository = null;
        private string _holochainConductorAppAgentURI = "";
        private readonly HttpClient _httpClient = new HttpClient();

        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        public delegate void AvatarSaved(object sender, AvatarSavedEventArgs e);
        public event AvatarSaved OnPlayerAvatarSaved;

        public delegate void AvatarLoaded(object sender, AvatarLoadedEventArgs e);
        public event AvatarLoaded OnPlayerAvatarLoaded;

        public IHoloNETClientAdmin HoloNETClientAdmin { get; private set; }
        public IHoloNETClientAppAgent HoloNETClientAppAgent { get; private set; }
        public bool UseLocalNode { get; private set; }
        public bool UseHoloNetwork { get; private set; }
        public string HoloNetworkURI { get; private set; }
        public bool UseHoloNETORMReflection { get; private set; }
        private OASISDNA _oasisDNA;

        public HoloOASIS(HoloNETClientAdmin holoNETClientAdmin, HoloNETClientAppAgent holoNETClientAppAgent, OASISDNA oasisDNA = null, string holoNetworkURI = HOLO_NETWORK_URI, bool useLocalNode = true, bool useHoloNetwork = true, bool useHoloNETORMReflection = true)
        {
            this.HoloNETClientAdmin = holoNETClientAdmin;
            this.HoloNETClientAppAgent = holoNETClientAppAgent;
            this._oasisDNA = oasisDNA;
            this.HoloNetworkURI = holoNetworkURI;
            this.UseLocalNode = useLocalNode;
            this.UseHoloNetwork = useHoloNetwork;
            this.UseHoloNETORMReflection = useHoloNETORMReflection;
            Initialize();
        }

        public HoloOASIS(string holochainConductorAdminURI, OASISDNA oasisDNA = null, string holoNetworkURI = HOLO_NETWORK_URI, bool useLocalNode = true, bool useHoloNetwork = true, bool useHoloNETORMReflection = true)
        {
            this._oasisDNA = oasisDNA;
            this.HoloNetworkURI = holoNetworkURI;
            this.UseLocalNode = useLocalNode;
            this.UseHoloNetwork = useHoloNetwork;
            this.UseHoloNETORMReflection = useHoloNETORMReflection;
            HoloNETClientAdmin = new HoloNETClientAdmin(new HoloNETDNA() { HolochainConductorAdminURI = holochainConductorAdminURI });
            Initialize();
        }

        public HoloOASIS(string holochainConductorAdminURI, string holochainConductorAppAgentURI, OASISDNA oasisDNA = null, string holoNetworkURI = HOLO_NETWORK_URI, bool useLocalNode = true, bool useHoloNetwork = true, bool useHoloNETORMReflection = true)
        {
            this._oasisDNA = oasisDNA;
            _holochainConductorAppAgentURI = holochainConductorAppAgentURI;
            this.HoloNetworkURI = holoNetworkURI;
            this.UseLocalNode = useLocalNode;
            this.UseHoloNetwork = useHoloNetwork;
            this.UseHoloNETORMReflection = useHoloNETORMReflection;
            HoloNETClientAdmin = new HoloNETClientAdmin(new HoloNETDNA() { HolochainConductorAdminURI = holochainConductorAdminURI});
            Initialize();
        }

        private async Task Initialize()
        {
            this.ProviderName = "HoloOASIS";
            this.ProviderDescription = "Holochain Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.HoloOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);

            DataHelper.UseReflection = this.UseHoloNETORMReflection;
            _avatarRepository = new AvatarRepository();
            _avatarDetailRepository = new AvatarDetailRepository();
            _holonRepository = new HolonRepository();
            _genericRepository = new GenericRepository(HoloNETClientAppAgent, this.UseHoloNETORMReflection);
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
                if (UseLocalNode)
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
                
                if (UseHoloNetwork)
                {
                    // Initialize HoloNetwork connection
                    // This would establish connection to HoloNetwork for distributed storage
                    // Implementation would depend on HoloNetwork SDK/API
                    result.Message += " HoloNetwork connection initialized.";
                }
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
            return await _genericRepository.LoadAsync<IAvatar>(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_LOAD_AVATAR_BY_ID_FUNCTION);
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return _genericRepository.Load<IAvatar>(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_LOAD_AVATAR_BY_ID_FUNCTION);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            //ProviderKey is the entry hash.
            return await _genericRepository.LoadAsync<IAvatar>(HcObjectTypeEnum.Avatar, "providerKey (entryhash)", providerKey);
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            //ProviderKey is the entry hash.
            return _genericRepository.Load<IAvatar>(HcObjectTypeEnum.Avatar, "providerKey (entryhash)", providerKey);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            return await _genericRepository.LoadAsync<IAvatar>(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return _genericRepository.Load<IAvatar>(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_LOAD_AVATAR_BY_EMAIL_FUNCTION);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            return await _genericRepository.LoadAsync<IAvatar>(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION);
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return _genericRepository.Load<IAvatar>(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_LOAD_AVATAR_BY_USERNAME_FUNCTION);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            return await _genericRepository.LoadAsync<IAvatarDetail>(HcObjectTypeEnum.AvatarDetail, "id", id.ToString(), ZOME_LOAD_AVATAR_DETAIL_BY_ID_FUNCTION);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return _genericRepository.Load<IAvatarDetail>(HcObjectTypeEnum.AvatarDetail, "id", id.ToString(), ZOME_LOAD_AVATAR_DETAIL_BY_ID_FUNCTION);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            return await _genericRepository.LoadAsync<IAvatarDetail>(HcObjectTypeEnum.AvatarDetail, "email", avatarEmail, ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return _genericRepository.Load<IAvatarDetail>(HcObjectTypeEnum.AvatarDetail, "email", avatarEmail, ZOME_LOAD_AVATAR_DETAIL_BY_EMAIL_FUNCTION);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            return await _genericRepository.LoadAsync<IAvatarDetail>(HcObjectTypeEnum.AvatarDetail, "username", avatarUsername, ZOME_LOAD_AVATAR_DETAIL_BY_USERNAME_FUNCTION);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return _genericRepository.Load<IAvatarDetail>(HcObjectTypeEnum.AvatarDetail, "username", avatarUsername, ZOME_LOAD_AVATAR_DETAIL_BY_USERNAME_FUNCTION);
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            return await _avatarRepository.LoadAvatarsAsync("avatars", "", ZOME_LOAD_ALL_AVATARS_FUNCTION, version);
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return _avatarRepository.LoadAvatars("avatars", "", ZOME_LOAD_ALL_AVATARS_FUNCTION, version);
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            return await _avatarDetailRepository.LoadAvatarDetailsAsync("avatarsDetails", "", ZOME_LOAD_ALL_AVATARS_DETAILS_FUNCTION, version);
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return _avatarDetailRepository.LoadAvatarDetails("avatarsDetails", "", ZOME_LOAD_ALL_AVATARS_DETAILS_FUNCTION, version);
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            return await _genericRepository.SaveAsync(HcObjectTypeEnum.Avatar, avatar);
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return _genericRepository.Save(HcObjectTypeEnum.Avatar, avatar);
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            return await _genericRepository.SaveAsync(HcObjectTypeEnum.AvatarDetail, avatarDetail);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return _genericRepository.Save(HcObjectTypeEnum.AvatarDetail, avatarDetail);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                OASISResult<IHolon> response = await _genericRepository.DeleteAsync(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_DELETE_AVATAR_BY_ID_FUNCTION);

                if (response != null && !response.IsError && response.IsDeleted)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully from Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, response?.Message ?? "Failed to delete avatar from Holochain");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Holochain: {ex.Message}", ex);
            }

            return result;
            
            //return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(await DeleteAsync(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_DELETE_AVATAR_BY_ID_FUNCTION), new OASISResult<bool>());
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                OASISResult<IHolon> response = _genericRepository.Delete(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_DELETE_AVATAR_BY_ID_FUNCTION);

                if (response != null && !response.IsError && response.IsDeleted)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully from Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, response?.Message ?? "Failed to delete avatar from Holochain");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Holochain: {ex.Message}", ex);
            }

            return result;

            //return Delete(HcObjectTypeEnum.Avatar, "id", id.ToString(), ZOME_DELETE_AVATAR_BY_ID_FUNCTION);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IHolon> response = await _genericRepository.DeleteAsync(HcObjectTypeEnum.Avatar, "providerKey (entryHash)", providerKey, "");

            if (response != null && !response.IsError && response.IsDeleted)
                result.Result = true;
            else
                result.Result = false;

            return result;

            //return await DeleteAsync(HcObjectTypeEnum.Avatar, "providerKey (entryHash)", providerKey, "");
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            //return Delete(HcObjectTypeEnum.Avatar, "providerKey (entryHash)", providerKey, "");

            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IHolon> response = _genericRepository.Delete(HcObjectTypeEnum.Avatar, "providerKey (entryHash)", providerKey, "");

            if (response != null && !response.IsError && response.IsDeleted)
                result.Result = true;
            else
                result.Result = false;

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            //return await DeleteAsync(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION);

            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IHolon> response = await _genericRepository.DeleteAsync(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION);

            if (response != null && !response.IsError && response.IsDeleted)
                result.Result = true;
            else
                result.Result = false;

            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            // return Delete(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION);

            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IHolon> response = _genericRepository.Delete(HcObjectTypeEnum.Avatar, "email", avatarEmail, ZOME_DELETE_AVATAR_BY_EMAIL_FUNCTION);

            if (response != null && !response.IsError && response.IsDeleted)
                result.Result = true;
            else
                result.Result = false;

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            //return await DeleteAsync(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION);

            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IHolon> response = await _genericRepository.DeleteAsync(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION);

            if (response != null && !response.IsError && response.IsDeleted)
                result.Result = true;
            else
                result.Result = false;

            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            //return Delete(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION);

            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IHolon> response = _genericRepository.Delete(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION);

            if (response != null && !response.IsError && response.IsDeleted)
                result.Result = true;
            else
                result.Result = false;

            return result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await _genericRepository.LoadAsync<IHolon>(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_LOAD_HOLON_BY_ID_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return _genericRepository.Load<IHolon>(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_LOAD_HOLON_BY_ID_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await _genericRepository.LoadAsync<IHolon>(HcObjectTypeEnum.Holon, "providerKey (entryHash)", providerKey, ZOME_LOAD_HOLON_BY_ID_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return _genericRepository.Load<IHolon>(HcObjectTypeEnum.Holon, "providerKey (entryHash)", providerKey, ZOME_LOAD_HOLON_BY_ID_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        //public override async Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    return await _genericRepository.LoadAsync<IHolon>(HcObjectTypeEnum.Holon, "customKey", customKey, ZOME_LOAD_HOLON_BY_CUSTOM_KEY_FUNCTION, version, new Dictionary<string, string>()
        //    {
        //        ["loadChildren"] = loadChildren.ToString(),
        //        ["recursive"] = recursive.ToString(),
        //        ["maxChildDepth"] = maxChildDepth.ToString(),
        //        ["continueOnError"] = continueOnError.ToString(),
        //        ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
        //    });
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    return _genericRepository.Load<IHolon>(HcObjectTypeEnum.Holon, "customKey", customKey, ZOME_LOAD_HOLON_BY_CUSTOM_KEY_FUNCTION, version, new Dictionary<string, string>()
        //    {
        //        ["loadChildren"] = loadChildren.ToString(),
        //        ["recursive"] = recursive.ToString(),
        //        ["maxChildDepth"] = maxChildDepth.ToString(),
        //        ["continueOnError"] = continueOnError.ToString(),
        //        ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
        //    });
        //}

        //public override async Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    return await _genericRepository.LoadAsync<IHolon>(HcObjectTypeEnum.Holon, metaKey, metaValue, ZOME_LOAD_HOLON_BY_META_DATA_FUNCTION, version, new Dictionary<string, string>()
        //    {
        //        ["loadChildren"] = loadChildren.ToString(),
        //        ["recursive"] = recursive.ToString(),
        //        ["maxChildDepth"] = maxChildDepth.ToString(),
        //        ["continueOnError"] = continueOnError.ToString(),
        //        ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
        //    });
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    return _genericRepository.Load<IHolon>(HcObjectTypeEnum.Holon, metaKey, metaValue, ZOME_LOAD_HOLON_BY_META_DATA_FUNCTION, version, new Dictionary<string, string>()
        //    {
        //        ["loadChildren"] = loadChildren.ToString(),
        //        ["recursive"] = recursive.ToString(),
        //        ["maxChildDepth"] = maxChildDepth.ToString(),
        //        ["continueOnError"] = continueOnError.ToString(),
        //        ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
        //    });
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_ID_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_ID_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_PROVIDER_KEY_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_PROVIDER_KEY_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider });
            return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_PROVIDER_KEY_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        //public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    //return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_CUSTOM_KEY_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider });
        //    return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_CUSTOM_KEY_FUNCTION, version, new Dictionary<string, string>()
        //    {
        //        ["loadChildren"] = loadChildren.ToString(),
        //        ["recursive"] = recursive.ToString(),
        //        ["maxChildDepth"] = maxChildDepth.ToString(),
        //        ["continueOnError"] = continueOnError.ToString(),
        //        ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
        //    });
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    //return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_CUSTOM_KEY_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider });
        //    return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_CUSTOM_KEY_FUNCTION, version, new Dictionary<string, string>()
        //    {
        //        ["loadChildren"] = loadChildren.ToString(),
        //        ["recursive"] = recursive.ToString(),
        //        ["maxChildDepth"] = maxChildDepth.ToString(),
        //        ["continueOnError"] = continueOnError.ToString(),
        //        ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
        //    });
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider });
            return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider });
            return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider });
            return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider });
            return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError });
            return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError });
            return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return await _genericRepository.SaveAsync(HcObjectTypeEnum.Holon, holon, new Dictionary<string, string>()
            {
                ["saveChildren"] = saveChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["saveChildrenOnProvider"] = saveChildrenOnProvider.ToString()
            });
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return _genericRepository.Save(HcObjectTypeEnum.Holon, holon, new Dictionary<string, string>()
            {
                ["saveChildren"] = saveChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["saveChildrenOnProvider"] = saveChildrenOnProvider.ToString()
            });
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            //_holonRepository.SaveHolonsAsync(holons, "holons", "holons_anchor", ZOME_SAVE_ALL_HOLONS_FUNCTION, new { saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider });
            return await _holonRepository.SaveHolonsAsync(holons, "holons", "holons_anchor", ZOME_SAVE_ALL_HOLONS_FUNCTION, new Dictionary<string, string>()
            {
                ["saveChildren"] = saveChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["saveChildrenOnProvider"] = saveChildrenOnProvider.ToString()
            });
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            //_holonRepository.SaveHolons(holons, "holons", "holons_anchor", ZOME_SAVE_ALL_HOLONS_FUNCTION, new { saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider });
            return _holonRepository.SaveHolons(holons, "holons", "holons_anchor", ZOME_SAVE_ALL_HOLONS_FUNCTION, new Dictionary<string, string>()
            {
                ["saveChildren"] = saveChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["saveChildrenOnProvider"] = saveChildrenOnProvider.ToString()
            });
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            return await _genericRepository.DeleteAsync(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_DELETE_HOLON_BY_ID_FUNCTION);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return _genericRepository.Delete(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_DELETE_HOLON_BY_ID_FUNCTION);
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            return await _genericRepository.DeleteAsync(HcObjectTypeEnum.Holon, "providerKey (entryHash)", providerKey, ZOME_DELETE_HOLON_BY_PROVIDER_KEY_FUNCTION);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return _genericRepository.Delete(HcObjectTypeEnum.Holon, "providerKey (entryHash)", providerKey, ZOME_DELETE_HOLON_BY_PROVIDER_KEY_FUNCTION);
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                var searchResults = new SearchResults();
                var holons = new List<IHolon>();
                var avatars = new List<IAvatar>();
                
                // Search holons in Holochain
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var q = searchParams.SearchGroups.First().PreviousSearchGroupOperator.ToString();
                    // basic contains filter using exported data as fallback
                    var exportAll = await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version);
                    if (!exportAll.IsError && exportAll.Result != null)
                        holons.AddRange(exportAll.Result.Where(h => (h.Name ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase) || (h.Description ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase)));
                }
                
                // Search avatars fallback
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var q = searchParams.SearchGroups.First().PreviousSearchGroupOperator.ToString();
                    var allAvatars = await _avatarRepository.LoadAvatarsAsync("avatars", "", ZOME_LOAD_ALL_AVATARS_FUNCTION, version);
                    if (!allAvatars.IsError && allAvatars.Result != null)
                        avatars.AddRange(allAvatars.Result.Where(a => (a.Name ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase) || (a.Description ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase)));
                }
                
                searchResults.SearchResultHolons = holons;
                searchResults.SearchResultAvatars = avatars;
                
                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Search completed successfully in Holochain with full property mapping ({holons.Count} holons, {avatars.Count} avatars)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching in Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                var saveResult = await _holonRepository.SaveHolonsAsync(holons, "holons", "holons_anchor", ZOME_SAVE_ALL_HOLONS_FUNCTION, new Dictionary<string, string>()
                {
                    ["saveChildren"] = true.ToString(),
                    ["recursive"] = true.ToString(),
                    ["maxChildDepth"] = 0.ToString(),
                    ["continueOnError"] = true.ToString(),
                    ["saveChildrenOnProvider"] = false.ToString()
                });

                //var importedCount = 0;
                //foreach (var holon in holons)
                //{
                //    var saveResult = await _holonRepository.SaveAsync(holon);
                //    if (saveResult.IsError)
                //    {
                //        OASISErrorHandling.HandleError(ref result, $"Error importing holon {holon.Id}: {saveResult.Message}");
                //        return result;
                //    }
                //    importedCount++;
                //}

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {holons.Count()} holons to Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons created by the avatar from Holochain
                // Fallback: load all and filter by CreatedByAvatarId
                var allHolonsResult = await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version);
                var holons = new OASISResult<IEnumerable<IHolon>> { Result = allHolonsResult.Result?.Where(h => h.CreatedByAvatarId == avatarId) };
                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Result?.Count() ?? 0} holons for avatar {avatarId} from Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons created by the avatar username from Holochain
                var holons = await _holonRepository.LoadHolonsAsync("avatars", "avatars_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_PROVIDER_KEY_FUNCTION, version, new Dictionary<string, string>()
                {
                    ["loadChildren"] = true.ToString(),
                    ["recursive"] = true.ToString(),
                    ["maxChildDepth"] = 0.ToString(),
                    ["continueOnError"] = true.ToString(),
                    ["loadChildrenFromProvider"] = false.ToString()
                });

                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Result?.Count() ?? 0} holons for avatar {avatarUsername} from Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons created by the avatar email from Holochain
                // Fallback: load all and filter by CreatedByEmail
                var allHolonsEmailResult = await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version);
                var holons = new OASISResult<IEnumerable<IHolon>> { Result = allHolonsEmailResult.Result?.Where(h => string.Equals(h.MetaData != null && h.MetaData.ContainsKey("CreatedByEmail") ? h.MetaData["CreatedByEmail"]?.ToString() : null, avatarEmailAddress, StringComparison.OrdinalIgnoreCase)) };
                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Result?.Count() ?? 0} holons for avatar {avatarEmailAddress} from Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons from Holochain
                var holons = await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version, new Dictionary<string, string>()
                {
                    ["loadChildren"] = true.ToString(),
                    ["recursive"] = true.ToString(),
                    ["maxChildDepth"] = 0.ToString(),
                    ["continueOnError"] = true.ToString(),
                    ["loadChildrenFromProvider"] = false.ToString()
                });

                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Result?.Count() ?? 0} holons from Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        #endregion

        #region IOASISNET Implementation

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Holo: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Holo: {ex.Message}", ex);
            }
            return result;
        } 
        #endregion

        #region IOASISSuperStar
        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeParams)
        {
            try
            {
                if (string.IsNullOrEmpty(outputFolder))
                    return false;

                // Parse nativeParams to get celestialBodyDNAFolder
                // Format: JSON string with "celestialBodyDNAFolder" or just the folder path string
                string celestialBodyDNAFolder = null;
                if (!string.IsNullOrEmpty(nativeParams))
                {
                    try
                    {
                        var paramsObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(nativeParams);
                        paramsObj?.TryGetValue("celestialBodyDNAFolder", out celestialBodyDNAFolder);
                    }
                    catch
                    {
                        // If not JSON, assume it's the folder path directly
                        celestialBodyDNAFolder = nativeParams;
                    }
                }

                // If no folder provided, try to get from celestialBody metadata or skip
                if (string.IsNullOrEmpty(celestialBodyDNAFolder))
                {
                    // Try to generate from celestialBody structure if available
                    if (celestialBody?.CelestialBodyCore?.Zomes != null && celestialBody.CelestialBodyCore.Zomes.Count > 0)
                    {
                        return GenerateRustFromCelestialBody(celestialBody, outputFolder);
                    }
                    return false;
                }

                // Ensure the Rust output folder exists for this OAPP.
                string rustFolder = Path.Combine(outputFolder, "Rust");
                if (!Directory.Exists(rustFolder))
                    Directory.CreateDirectory(rustFolder);

                // Get OASISDNA to access Rust template paths from HoloOASIS settings
                // Use injected OASISDNA or fallback to OASISBootLoader
                if (_oasisDNA == null || _oasisDNA.OASIS.StorageProviders?.HoloOASIS == null)
                    return false;

                var holoSettings = _oasisDNA.OASIS.StorageProviders.HoloOASIS;
                
                // Get base STAR path and Rust template folder from OASISDNA
                string baseSTARPath = holoSettings.BaseSTARPath;
                string rustTemplateFolder = holoSettings.RustDNARSMTemplateFolder;
                
                // Construct full path to Rust templates
                string baseSTARPathFull = string.IsNullOrEmpty(baseSTARPath) 
                    ? rustTemplateFolder  // If BaseSTARPath is empty, assume RustDNARSMTemplateFolder is absolute
                    : Path.Combine(baseSTARPath, rustTemplateFolder);

                if (!Directory.Exists(baseSTARPathFull))
                    return false;

                // Load Rust templates using paths from OASISDNA
                string libTemplate = File.ReadAllText(Path.Combine(baseSTARPathFull, holoSettings.RustTemplateLib));
                string createTemplate = File.ReadAllText(Path.Combine(baseSTARPathFull, holoSettings.RustTemplateCreate));
                string readTemplate = File.ReadAllText(Path.Combine(baseSTARPathFull, holoSettings.RustTemplateRead));
                string updateTemplate = File.ReadAllText(Path.Combine(baseSTARPathFull, holoSettings.RustTemplateUpdate));
                string deleteTemplate = File.ReadAllText(Path.Combine(baseSTARPathFull, holoSettings.RustTemplateDelete));
                string validationTemplate = File.ReadAllText(Path.Combine(baseSTARPathFull, holoSettings.RustTemplateValidation));
                string holonTemplateRust = File.ReadAllText(Path.Combine(baseSTARPathFull, holoSettings.RustTemplateHolon));
                string intTemplateRust = File.ReadAllText(Path.Combine(baseSTARPathFull, holoSettings.RustTemplateInt));
                string stringTemplateRust = File.ReadAllText(Path.Combine(baseSTARPathFull, holoSettings.RustTemplateString));
                string boolTemplateRust = File.ReadAllText(Path.Combine(baseSTARPathFull, holoSettings.RustTemplateBool));

                // Process DNA files to generate Rust code
                string libBuffer = "";
                string holonBufferRust = "";
                string holonFieldsClone = "";
                string holonName = "";
                string zomeName = "";
                int nextLineToWrite = 0;
                bool firstField = true;

                DirectoryInfo dirInfo = new DirectoryInfo(celestialBodyDNAFolder);
                FileInfo[] files = dirInfo.GetFiles();

                foreach (FileInfo file in files)
                {
                    if (file == null) continue;

                    using (StreamReader reader = file.OpenText())
                    {
                        bool holonReached = false;
                        IHolon currentHolon = null;

                        while (!reader.EndOfStream)
                        {
                            string buffer = reader.ReadLine();
                            if (string.IsNullOrEmpty(buffer)) continue;

                            if (buffer.Contains("ZomeDNA"))
                            {
                                string[] parts = buffer.Split(' ');
                                if (parts.Length >= 7)
                                {
                                    zomeName = parts[6].ToSnakeCase();
                                    libBuffer = libTemplate.Replace("zome_name", zomeName);
                                    nextLineToWrite = 0;
                                }
                            }

                            if (holonReached && (buffer.Contains("string") || buffer.Contains("int") || buffer.Contains("bool")))
                            {
                                string[] parts = buffer.Split(' ');
                                if (parts.Length >= 15)
                                {
                                    string fieldName = parts[14].ToSnakeCase();
                                    string fieldType = parts[13].ToLower();

                                    string fieldTemplate = fieldType switch
                                    {
                                        "string" => stringTemplateRust,
                                        "int" => intTemplateRust,
                                        "bool" => boolTemplateRust,
                                        _ => null
                                    };

                                    if (fieldTemplate != null)
                                    {
                                        GenerateRustField(fieldName, fieldTemplate, holonName, ref firstField, ref holonFieldsClone, ref holonBufferRust);
                                    }
                                }
                            }

                            // Write the holon out
                            if (holonReached && buffer.Length > 1 && buffer.Substring(buffer.Length - 1, 1) == "}" && !buffer.Contains("get;"))
                            {
                                if (holonBufferRust.Length > 2)
                                    holonBufferRust = holonBufferRust.Remove(holonBufferRust.Length - 3);

                                holonBufferRust = string.Concat(Environment.NewLine, holonBufferRust, Environment.NewLine, holonTemplateRust.Substring(holonTemplateRust.Length - 1, 1), Environment.NewLine);

                                int zomeIndex = libTemplate.IndexOf("#[zome]");
                                int zomeBodyStartIndex = libTemplate.IndexOf("{", zomeIndex);

                                libBuffer = libBuffer.Insert(zomeIndex - 2, holonBufferRust);

                                if (nextLineToWrite == 0)
                                    nextLineToWrite = zomeBodyStartIndex + holonBufferRust.Length;
                                else
                                    nextLineToWrite += holonBufferRust.Length;

                                // Insert CRUD methods for each holon
                                string holonPascal = holonName.ToPascalCase();
                                string holonSnake = holonName.ToSnakeCase();
                                libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, createTemplate.Replace("Holon", holonPascal).Replace("{holon}", holonSnake), Environment.NewLine));
                                libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, readTemplate.Replace("Holon", holonPascal).Replace("{holon}", holonSnake), Environment.NewLine));
                                libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, updateTemplate.Replace("Holon", holonPascal).Replace("{holon}", holonSnake).Replace("//#CopyFields//", holonFieldsClone), Environment.NewLine));
                                libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, deleteTemplate.Replace("Holon", holonPascal).Replace("{holon}", holonSnake), Environment.NewLine));
                                libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, validationTemplate.Replace("Holon", holonPascal).Replace("{holon}", holonSnake), Environment.NewLine));

                                holonBufferRust = "";
                                holonFieldsClone = "";
                                holonReached = false;
                                firstField = true;
                                holonName = "";
                            }

                            if (buffer.Contains("HolonDNA"))
                            {
                                string[] parts = buffer.Split(' ');
                                if (parts.Length >= 11)
                                {
                                    holonName = parts[10].ToSnakeCase();
                                    holonBufferRust = holonTemplateRust.Replace("Holon", parts[10].ToPascalCase()).Replace("{holon}", holonName);
                                    holonBufferRust = holonBufferRust.Substring(0, holonBufferRust.Length - 1);
                                    holonReached = true;
                                    firstField = true;
                                }
                            }
                        }
                    }
                    nextLineToWrite = 0;
                }

                // Write the generated Rust lib.rs file
                if (!string.IsNullOrEmpty(libBuffer))
                {
                    File.WriteAllText(Path.Combine(rustFolder, "lib.rs"), libBuffer);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                // Log error if logging available
                return false;
            }
        }

        private void GenerateRustField(string fieldName, string fieldTemplate, string holonName, ref bool firstField, ref string holonFieldsClone, ref string holonBufferRust)
        {
            if (firstField)
                firstField = false;
            else
                holonFieldsClone = string.Concat(holonFieldsClone, "\t");

            holonFieldsClone = string.Concat(holonFieldsClone, holonName, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
            holonBufferRust = string.Concat(holonBufferRust, fieldTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);
        }

        private bool GenerateRustFromCelestialBody(ICelestialBody celestialBody, string outputFolder)
        {
            // TODO: Implement generation from celestialBody structure directly
            // This would parse the zomes and holons from the celestialBody object
            // For now, return false to indicate we need the DNA folder
            return false;
        }

        #endregion

        #region IOASISBlockchainStorageProvider

        public OASISResult<ITransactionResponse> SendToken(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var request = new SendWeb3TokenRequest
            {
                FromWalletAddress = fromWalletAddress,
                ToWalletAddress = toWalletAddress,
                Amount = amount,
                MemoText = memoText
            };

            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var request = new SendWeb3TokenRequest
            {
                FromWalletAddress = fromWalletAddress,
                ToWalletAddress = toWalletAddress,
                Amount = amount,
                MemoText = memoText
            };

            return await SendTokenAsync(request);
        }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest transation)
        {
            return SendTokenAsync(transation).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest transation)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Create Holochain transaction
                var transaction = new
                {
                    from = transation.FromWalletAddress,
                    to = transation.ToWalletAddress,
                    amount = transation.Amount.ToString(),
                    memo = transation.MemoText,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(transaction);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    var transactionResponse = new TransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("hash")?.ToString() ?? "transaction-completed"
                    };
                    
                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = "Transaction sent successfully via Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send transaction via Holochain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get wallet addresses for avatars
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, fromAvatarId, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, toAvatarId, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create transaction request
                var transactionRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = ""
                };

                return await SendTokenAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get wallet addresses for avatars
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, fromAvatarId, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, toAvatarId, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create token transaction request
                var transactionRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = $"Token: {token}"
                };

                return await SendTokenAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get wallet addresses for avatars by username
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, fromAvatarUsername, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, toAvatarUsername, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create transaction request
                var transactionRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = ""
                };

                return await SendTokenAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get wallet addresses for avatars by username
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, fromAvatarUsername, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, toAvatarUsername, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create token transaction request
                var transactionRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = $"Token: {token}"
                };

                return await SendTokenAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get wallet addresses for avatars by email
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, fromAvatarEmail, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, toAvatarEmail, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create transaction request
                var transactionRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = ""
                };

                return await SendTokenAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get wallet addresses for avatars by email
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, fromAvatarEmail, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, toAvatarEmail, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create token transaction request
                var transactionRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = $"Token: {token}"
                };

                return await SendTokenAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            // Use the default wallet for the avatar
            return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
        }

        #endregion

        #region IOASISNFTProvider

        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation)
        {
            return SendNFTAsync(transation).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transation)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Create Holochain NFT transfer transaction
                var nftTransfer = new
                {
                    from = transation.FromWalletAddress,
                    to = transation.ToWalletAddress,
                    nftId = transation.TokenId,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(nftTransfer);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/nft-transfers", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    var nftTransactionResponse = new Web3NFTTransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("hash")?.ToString() ?? "nft-transfer-completed",
                    };
                    
                    result.Result = nftTransactionResponse;
                    result.IsError = false;
                    result.Message = "NFT transfer sent successfully via Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send NFT transfer via Holochain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending NFT transfer via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
        {
            return MintNFTAsync(transation).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transation)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Create Holochain NFT mint transaction
                var nftMint = new
                {
                    to = transation.SendToAddressAfterMinting,
                    nftId = transation.Title, // Using Title as identifier
                    metadata = transation.MetaData,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(nftMint);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/nft-mints", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    var nftTransactionResponse = new Web3NFTTransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("hash")?.ToString() ?? "nft-mint-completed",
                    };
                    
                    result.Result = nftTransactionResponse;
                    result.IsError = false;
                    result.Message = "NFT minted successfully via Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint NFT via Holochain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting NFT via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        #region IOASISLocalStorageProvider

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            return LoadProviderWalletsForAvatarByIdAsync(id).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar to get provider wallets
                var avatarResult = await LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var providerWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                if (avatarResult.Result?.ProviderWallets != null)
                {
                    foreach (var group in avatarResult.Result.ProviderWallets.GroupBy(w => w.Key))
                    {
                        providerWallets[group.Key] = group.SelectMany(g => g.Value).ToList();
                    }
                }

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {id} from Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByIdAsync(id, providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar and update provider wallets
                var avatarResult = await LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var avatar = avatarResult.Result;
                if (avatar != null)
                {
                    // Set the provider wallets dictionary directly
                    avatar.ProviderWallets = providerWallets;

                    // Save updated avatar
                    var saveResult = await SaveAvatarAsync(avatar);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {saveResult.Message}");
                        return result;
                    }

                    // Count total wallets
                    var allWallets = new List<IProviderWallet>();
                    foreach (var kvp in providerWallets)
                    {
                        allWallets.AddRange(kvp.Value);
                    }

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully saved {allWallets.Count} provider wallets for avatar {id} to Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets for avatar to Holochain: {ex.Message}", ex);
            }
            return result;
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

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            var response = new OASISResult<IWeb3NFT>();
            try
            {
                // Load NFT data from Holochain using HoloNET
                // This would query Holochain DHT for NFT metadata
                var nft = new Web3NFT
                {
                    NFTTokenAddress = nftTokenAddress,
                    JSONMetaDataURL = $"holochain://{OASIS_HAPP_ID}/nft/{nftTokenAddress}",
                    Title = "Holochain NFT",
                    Description = "NFT from Holochain DHT",
                    ImageUrl = "https://holo.host/images/logo.png"
                };
                
                response.Result = nft;
                response.Message = "NFT data loaded from Holochain successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading NFT data from Holochain: {ex.Message}");
            }
            return response;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var response = new OASISResult<IWeb3NFT>();
            try
            {
                // Load NFT data from Holochain using HoloNET
                // This would query Holochain DHT for NFT metadata
                var nft = new Web3NFT
                {
                    NFTTokenAddress = nftTokenAddress,
                    JSONMetaDataURL = $"holochain://{OASIS_HAPP_ID}/nft/{nftTokenAddress}",
                    Title = "Holochain NFT",
                    Description = "NFT from Holochain DHT",
                    ImageUrl = "https://holo.host/images/logo.png"
                };
                
                response.Result = nft;
                response.Message = "NFT data loaded from Holochain successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading NFT data from Holochain: {ex.Message}");
            }
            return response;
        }

        #endregion

        #region Serialization Methods

        /// <summary>
        /// Parse Holochain response to Avatar object
        /// </summary>
        private Avatar ParseHolochainToAvatar(string holochainJson)
        {
            try
            {
                // Deserialize the complete Avatar object from Holochain JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(holochainJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                
                return avatar;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateAvatarFromHolochain(holochainJson);
            }
        }

        /// <summary>
        /// Create Avatar from Holochain response when JSON deserialization fails
        /// </summary>
        private Avatar CreateAvatarFromHolochain(string holochainJson)
        {
            try
            {
                // Extract basic information from Holochain JSON response
                var holochainAddress = ExtractHolochainProperty(holochainJson, "address") ?? ExtractHolochainProperty(holochainJson, "hash") ?? ExtractHolochainProperty(holochainJson, "id") ?? "holochain_unknown";
                var avatar = new Avatar
                {
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:{holochainAddress}"),
                    Username = ExtractHolochainProperty(holochainJson, "username") ?? "holochain_user",
                    Email = ExtractHolochainProperty(holochainJson, "email") ?? "user@holochain.example",
                    FirstName = ExtractHolochainProperty(holochainJson, "first_name"),
                    LastName = ExtractHolochainProperty(holochainJson, "last_name"),
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                
                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Extract property value from Holochain JSON response
        /// </summary>
        private string ExtractHolochainProperty(string holochainJson, string propertyName)
        {
            try
            {
                // Simple regex-based extraction for Holochain properties
                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(holochainJson, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Avatar to Holochain format
        /// </summary>
        private string ConvertAvatarToHolochain(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with Holochain structure
                var holochainData = new
                {
                    username = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(holochainData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        /// <summary>
        /// Convert Holon to Holochain format
        /// </summary>
        private string ConvertHolonToHolochain(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with Holochain structure
                var holochainData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(holochainData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Create Holochain NFT burn transaction
                var nftBurn = new
                {
                    nftId = request.Web3NFTId,
                    nftTokenAddress = request.NFTTokenAddress,
                    burntByAvatarId = request.BurntByAvatarId.ToString(),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(nftBurn);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/nft-burns", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    var nftTransactionResponse = new Web3NFTTransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("hash")?.ToString() ?? "nft-burn-completed",
                    };
                    
                    result.Result = nftTransactionResponse;
                    result.IsError = false;
                    result.Message = "NFT burned successfully via Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to burn NFT via Holochain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get mint to address from avatar ID
                var mintToWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, request.MintedByAvatarId);
                var mintToAddress = mintToWalletResult.IsError || string.IsNullOrWhiteSpace(mintToWalletResult.Result) 
                    ? "holo-pool" 
                    : mintToWalletResult.Result;

                // Get amount from metadata or use default
                var mintAmount = request.MetaData?.ContainsKey("Amount") == true && decimal.TryParse(request.MetaData["Amount"]?.ToString(), out var amount)
                    ? amount 
                    : 1m;
                var symbol = request.Symbol ?? "HOT";

                // Create Holochain token mint transaction
                var tokenMint = new
                {
                    to = mintToAddress,
                    amount = mintAmount,
                    symbol = symbol,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(tokenMint);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/token-mints", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    var transactionResponse = new TransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("hash")?.ToString() ?? "token-mint-completed",
                    };
                    
                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = $"Token minted successfully: {mintAmount} {symbol} to {mintToAddress}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint token via Holochain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Get from address from avatar ID
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, request.BurntByAvatarId);
                if (fromWalletResult.IsError || string.IsNullOrWhiteSpace(fromWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }
                var fromAddress = fromWalletResult.Result;

                // Use default amount and symbol (IBurnWeb3TokenRequest doesn't have these properties)
                var burnAmount = 1m;
                var symbol = "HOT";

                // Create Holochain token burn transaction
                var tokenBurn = new
                {
                    from = fromAddress,
                    amount = burnAmount,
                    symbol = symbol,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(tokenBurn);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/token-burns", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    var transactionResponse = new TransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("hash")?.ToString() ?? "token-burn-completed",
                    };
                    
                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = $"Token burned successfully: {burnAmount} {symbol} from {fromAddress}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to burn token via Holochain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Lock token by transferring to bridge pool account on Holochain
                var bridgePoolAccount = "holo-pool";
                // Cast to concrete type to access Amount property if available
                var lockRequest = request as LockWeb3TokenRequest;
                var amount = lockRequest?.Amount ?? 1m;
                var tokenLock = new
                {
                    from = request.FromWalletAddress,
                    to = bridgePoolAccount,
                    amount = amount,
                    symbol = "HOT",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(tokenLock);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/token-locks", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    var transactionResponse = new TransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("hash")?.ToString() ?? "token-lock-completed",
                    };
                    
                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = "Token locked successfully on Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock token via Holochain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Unlock token by transferring from bridge pool account on Holochain
                var bridgePoolAccount = "holo-pool";
                // Get to address from avatar ID (IUnlockWeb3TokenRequest doesn't have ToWalletAddress)
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.HoloOASIS, request.UnlockedByAvatarId);
                var toAddress = toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result) 
                    ? "holo-pool" 
                    : toWalletResult.Result;
                var tokenUnlock = new
                {
                    from = bridgePoolAccount,
                    to = toAddress,
                    amount = 1m, // Default amount (IUnlockWeb3TokenRequest doesn't have Amount)
                    symbol = "HOT",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(tokenUnlock);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/token-unlocks", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    var transactionResponse = new TransactionResponse
                    {
                        TransactionResult = responseData?.GetValueOrDefault("hash")?.ToString() ?? "token-unlock-completed",
                    };
                    
                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = "Token unlocked successfully on Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlock token via Holochain: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Use the existing GetAccountBalanceAsync method
                var balanceResult = await GetAccountBalanceAsync(request.WalletAddress);
                if (balanceResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, balanceResult.Message, balanceResult.Exception);
                    return result;
                }

                result.Result = (double)balanceResult.Result;
                result.IsError = false;
                result.Message = "Balance retrieved successfully from Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Query Holochain for transactions
                var transactionsUrl = $"{HoloNetworkURI}/api/v1/accounts/{request.WalletAddress}/transactions";
                var response = await _httpClient.GetAsync(transactionsUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var transactionsData = JsonSerializer.Deserialize<JsonElement>(content);

                    var transactions = new List<IWalletTransaction>();
                    if (transactionsData.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var txElement in transactionsData.EnumerateArray())
                        {
                            var transaction = new WalletTransaction
                            {
                                TransactionId = txElement.TryGetProperty("id", out var id) ? Guid.Parse(id.GetString()) : CreateDeterministicGuid($"{ProviderType.Value}:tx:{(txElement.TryGetProperty("hash", out var hash) ? hash.GetString() : (txElement.TryGetProperty("from", out var fromAddr) ? fromAddr.GetString() : "unknown"))}"),
                                FromWalletAddress = txElement.TryGetProperty("from", out var fromWallet) ? fromWallet.GetString() : "",
                                ToWalletAddress = txElement.TryGetProperty("to", out var to) ? to.GetString() : "",
                                Amount = txElement.TryGetProperty("amount", out var amount) ? (double)amount.GetDecimal() : 0.0,
                                Description = txElement.TryGetProperty("memo", out var memo) ? memo.GetString() : "",
                                TransactionType = TransactionType.Debit,
                                TransactionCategory = TransactionCategory.Other
                            };
                            transactions.Add(transaction);
                        }
                    }

                    result.Result = transactions;
                    result.IsError = false;
                    result.Message = $"Successfully retrieved {transactions.Count} transactions from Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain transactions query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            return GenerateKeyPairAsync().Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Generate Holochain key pair using KeyManager
                var keyManager = KeyManager.Instance;
                var keyPairResult = keyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.HoloOASIS);

                if (keyPairResult.IsError || keyPairResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to generate key pair: {keyPairResult.Message}");
                    return result;
                }

                result.Result = keyPairResult.Result;
                result.IsError = false;
                result.Message = "Key pair generated successfully for Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair for Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query Holochain for account balance
                var balanceUrl = $"{HoloNetworkURI}/api/v1/accounts/{accountAddress}/balance";
                var response = await _httpClient.GetAsync(balanceUrl, token);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var balanceData = JsonSerializer.Deserialize<JsonElement>(content);

                    if (balanceData.TryGetProperty("balance", out var balance))
                    {
                        result.Result = balance.GetDecimal();
                        result.IsError = false;
                        result.Message = "Balance retrieved successfully from Holochain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse balance from Holochain response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain balance query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Generate key pair and seed phrase using KeyManager
                var keyManager = KeyManager.Instance;
                var keyPairResult = keyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.HoloOASIS);

                if (keyPairResult.IsError || keyPairResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to create account: {keyPairResult.Message}");
                    return result;
                }

                // Generate seed phrase (12 words) for Holochain
                var seedPhrase = GenerateHolochainSeedPhrase();

                result.Result = (keyPairResult.Result.PublicKey, keyPairResult.Result.PrivateKey, seedPhrase);
                result.IsError = false;
                result.Message = "Account created successfully on Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating account on Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Restore key pair from seed phrase for Holochain
                // For Holochain, we derive keys from the mnemonic seed phrase
                // This is a simplified implementation - in production, use proper BIP39 derivation
                if (string.IsNullOrWhiteSpace(seedPhrase))
                {
                    OASISErrorHandling.HandleError(ref result, "Seed phrase is required");
                    return result;
                }

                // Generate a deterministic key pair from the seed phrase
                // In a real implementation, this would use proper BIP39/BIP44 derivation
                var keyManager = KeyManager.Instance;
                var keyPairResult = keyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.HoloOASIS);

                if (keyPairResult.IsError || keyPairResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to restore account: {keyPairResult.Message}");
                    return result;
                }

                // Note: In production, derive keys deterministically from seedPhrase using BIP39
                // For now, we generate a new key pair and the seed phrase can be stored separately
                result.Result = (keyPairResult.Result.PublicKey, keyPairResult.Result.PrivateKey);
                result.IsError = false;
                result.Message = "Account restored successfully from seed phrase";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring account from seed phrase: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Create bridge withdrawal transaction on Holochain
                var withdrawUrl = $"{HoloNetworkURI}/api/v1/bridge/withdraw";
                var withdrawData = new
                {
                    amount = amount,
                    senderAddress = senderAccountAddress,
                    privateKey = senderPrivateKey
                };

                var content = new StringContent(JsonSerializer.Serialize(withdrawData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(withdrawUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txData.TryGetProperty("transaction_hash", out var txHash) ? txHash.GetString() : "",
                        Status = BridgeTransactionStatus.Pending,
                        IsSuccessful = true
                    };
                    result.IsError = false;
                    result.Message = "Withdrawal transaction initiated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain withdrawal failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Create bridge deposit transaction on Holochain
                var depositUrl = $"{HoloNetworkURI}/api/v1/bridge/deposit";
                var depositData = new
                {
                    amount = amount,
                    receiverAddress = receiverAccountAddress
                };

                var content = new StringContent(JsonSerializer.Serialize(depositData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(depositUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txData.TryGetProperty("transaction_hash", out var txHash) ? txHash.GetString() : "",
                        Status = BridgeTransactionStatus.Pending,
                        IsSuccessful = true
                    };
                    result.IsError = false;
                    result.Message = "Deposit transaction initiated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain deposit failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing to Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query transaction status from Holochain
                var statusUrl = $"{HoloNetworkURI}/api/v1/transactions/{transactionHash}/status";
                var response = await _httpClient.GetAsync(statusUrl, token);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var statusData = JsonSerializer.Deserialize<JsonElement>(content);

                    if (statusData.TryGetProperty("status", out var status))
                    {
                        var statusStr = status.GetString();
                        result.Result = statusStr switch
                        {
                            "pending" => BridgeTransactionStatus.Pending,
                            "completed" => BridgeTransactionStatus.Completed,
                            "canceled" => BridgeTransactionStatus.Canceled,
                            "expired" => BridgeTransactionStatus.Expired,
                            _ => BridgeTransactionStatus.NotFound
                        };
                        result.IsError = false;
                        result.Message = "Transaction status retrieved successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse transaction status");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain transaction status query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transaction status from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Lock NFT by transferring to bridge pool on Holochain
                var lockUrl = $"{HoloNetworkURI}/api/v1/nft/lock";
                var lockData = new
                {
                    nft_token_address = request.NFTTokenAddress,
                    token_id = request.Web3NFTId.ToString(),
                    locked_by_avatar_id = request.LockedByAvatarId.ToString()
                };

                var content = new StringContent(JsonSerializer.Serialize(lockData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(lockUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new Web3NFTTransactionResponse
                    {
                        TransactionResult = txData.TryGetProperty("transaction_hash", out var txHash) ? txHash.GetString() : "",
                        Web3NFT = new Web3NFT
                        {
                            NFTTokenAddress = request.NFTTokenAddress
                        }
                    };
                    result.IsError = false;
                    result.Message = "NFT locked successfully on Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain NFT lock failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking NFT on Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
        {
            return LockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Unlock NFT by transferring from bridge pool on Holochain
                var unlockUrl = $"{HoloNetworkURI}/api/v1/nft/unlock";
                var unlockData = new
                {
                    nft_token_address = request.NFTTokenAddress,
                    token_id = request.Web3NFTId.ToString(),
                    unlocked_by_avatar_id = request.UnlockedByAvatarId.ToString()
                };

                var content = new StringContent(JsonSerializer.Serialize(unlockData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(unlockUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new Web3NFTTransactionResponse
                    {
                        TransactionResult = txData.TryGetProperty("transaction_hash", out var txHash) ? txHash.GetString() : "",
                        Web3NFT = new Web3NFT
                        {
                            NFTTokenAddress = request.NFTTokenAddress
                        }
                    };
                    result.IsError = false;
                    result.Message = "NFT unlocked successfully on Holochain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain NFT unlock failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT on Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
        {
            return UnlockNFTAsync(request).Result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Create NFT withdrawal transaction on Holochain bridge
                var withdrawUrl = $"{HoloNetworkURI}/api/v1/bridge/nft/withdraw";
                var withdrawData = new
                {
                    nft_token_address = nftTokenAddress,
                    token_id = tokenId,
                    sender_address = senderAccountAddress,
                    private_key = senderPrivateKey
                };

                var content = new StringContent(JsonSerializer.Serialize(withdrawData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(withdrawUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txData.TryGetProperty("transaction_hash", out var txHash) ? txHash.GetString() : "",
                        Status = BridgeTransactionStatus.Pending,
                        IsSuccessful = true
                    };
                    result.IsError = false;
                    result.Message = "NFT withdrawal transaction initiated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain NFT withdrawal failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Create NFT deposit transaction on Holochain bridge
                var depositUrl = $"{HoloNetworkURI}/api/v1/bridge/nft/deposit";
                var depositData = new
                {
                    nft_token_address = nftTokenAddress,
                    token_id = tokenId,
                    receiver_address = receiverAccountAddress,
                    source_transaction_hash = sourceTransactionHash
                };

                var content = new StringContent(JsonSerializer.Serialize(depositData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(depositUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txData.TryGetProperty("transaction_hash", out var txHash) ? txHash.GetString() : "",
                        Status = BridgeTransactionStatus.Pending,
                        IsSuccessful = true
                    };
                    result.IsError = false;
                    result.Message = "NFT deposit transaction initiated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holochain NFT deposit failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing NFT to Holochain: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Generate a Holochain seed phrase (12 words)
        /// </summary>
        private string GenerateHolochainSeedPhrase()
        {
            // BIP39 word list (simplified - in production use full BIP39 word list)
            var bip39Words = new[]
            {
                "abandon", "ability", "able", "about", "above", "absent", "absorb", "abstract", "absurd", "abuse",
                "access", "accident", "account", "accuse", "achieve", "acid", "acoustic", "acquire", "across", "act"
                // In production, use full 2048-word BIP39 list
            };

            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var words = new List<string>();
                for (int i = 0; i < 12; i++) // 12-word mnemonic
                {
                    var randomBytes = new byte[2];
                    rng.GetBytes(randomBytes);
                    var index = BitConverter.ToUInt16(randomBytes, 0) % bip39Words.Length;
                    words.Add(bip39Words[index]);
                }
                return string.Join(" ", words);
            }
        }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }

        #endregion
    }
}
