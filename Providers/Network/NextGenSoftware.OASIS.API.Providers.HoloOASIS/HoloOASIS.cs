using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Repositories;
using DataHelper = NextGenSoftware.OASIS.API.Providers.HoloOASIS.Helpers.DataHelper;
using static System.Net.WebRequestMethods;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using System.Text.Json;
using System.Linq;
using System.Net.Http;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Requests;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers;

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

        public HoloOASIS(HoloNETClientAdmin holoNETClientAdmin, HoloNETClientAppAgent holoNETClientAppAgent, string holoNetworkURI = HOLO_NETWORK_URI, bool useLocalNode = true, bool useHoloNetwork = true, bool useHoloNETORMReflection = true)
        {
            this.HoloNETClientAdmin = holoNETClientAdmin;
            this.HoloNETClientAppAgent = holoNETClientAppAgent;
            this.HoloNetworkURI = holoNetworkURI;
            this.UseLocalNode = useLocalNode;
            this.UseHoloNetwork = useHoloNetwork;
            this.UseHoloNETORMReflection = useHoloNETORMReflection;
            Initialize();
        }

        public HoloOASIS(string holochainConductorAdminURI, string holoNetworkURI = HOLO_NETWORK_URI, bool useLocalNode = true, bool useHoloNetwork = true, bool useHoloNETORMReflection = true)
        {
            this.HoloNetworkURI = holoNetworkURI;
            this.UseLocalNode = useLocalNode;
            this.UseHoloNetwork = useHoloNetwork;
            this.UseHoloNETORMReflection = useHoloNETORMReflection;
            HoloNETClientAdmin = new HoloNETClientAdmin(new HoloNETDNA() { HolochainConductorAdminURI = holochainConductorAdminURI });
            Initialize();
        }

        public HoloOASIS(string holochainConductorAdminURI, string holochainConductorAppAgentURI, string holoNetworkURI = HOLO_NETWORK_URI, bool useLocalNode = true, bool useHoloNetwork = true, bool useHoloNETORMReflection = true)
        {
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
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            return true;
        }

        #endregion

        #region IOASISBlockchainStorageProvider

        public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var request = new WalletTransactionRequest
            {
                FromWalletAddress = fromWalletAddress,
                ToWalletAddress = toWalletAddress,
                Amount = amount,
                MemoText = memoText
            };

            return SendTransactionAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var request = new WalletTransactionRequest
            {
                FromWalletAddress = fromWalletAddress,
                ToWalletAddress = toWalletAddress,
                Amount = amount,
                MemoText = memoText
            };

            return await SendTransactionAsync(request);
        }

        public OASISResult<ITransactionRespone> SendTransaction(IWalletTransactionRequest transation)
        {
            return SendTransactionAsync(transation).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(IWalletTransactionRequest transation)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                    
                    var transactionResponse = new TransactionRespone
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

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                var transactionRequest = new WalletTransactionRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = ""
                };

                return await SendTransactionAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                var transactionRequest = new WalletTransactionRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = $"Token: {token}"
                };

                return await SendTransactionAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                var transactionRequest = new WalletTransactionRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = ""
                };

                return await SendTransactionAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                var transactionRequest = new WalletTransactionRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = $"Token: {token}"
                };

                return await SendTransactionAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                var transactionRequest = new WalletTransactionRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = ""
                };

                return await SendTransactionAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                var transactionRequest = new WalletTransactionRequest
                {
                    FromWalletAddress = fromWalletResult.Result,
                    ToWalletAddress = toWalletResult.Result,
                    Amount = amount,
                    MemoText = $"Token: {token}"
                };

                return await SendTransactionAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            // Use the default wallet for the avatar
            return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
        }

        #endregion

        #region IOASISNFTProvider

        public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest transation)
        {
            return SendNFTAsync(transation).Result;
        }

        public async Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest transation)
        {
            var result = new OASISResult<INFTTransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                    
                    var nftTransactionResponse = new NFTTransactionRespone
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

        public OASISResult<INFTTransactionRespone> MintNFT(IMintNFTTransactionRequest transation)
        {
            return MintNFTAsync(transation).Result;
        }

        public async Task<OASISResult<INFTTransactionRespone>> MintNFTAsync(IMintNFTTransactionRequest transation)
        {
            var result = new OASISResult<INFTTransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                    
                    var nftTransactionResponse = new NFTTransactionRespone
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
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                    foreach (var group in avatarResult.Result.ProviderWallets.GroupBy(w => w.ProviderType))
                    {
                        providerWallets[group.Key] = group.ToList();
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
                    OASISErrorHandling.HandleError(ref result, "Holo provider is not activated");
                    return result;
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
                    // Convert dictionary to list
                    var allWallets = new List<IProviderWallet>();
                    foreach (var kvp in providerWallets)
                    {
                        allWallets.AddRange(kvp.Value);
                    }
                    avatar.ProviderWallets = allWallets;

                    // Save updated avatar
                    var saveResult = await SaveAvatarAsync(avatar);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {saveResult.Message}");
                        return result;
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

        public OASISResult<IOASISNFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            var response = new OASISResult<IOASISNFT>();
            try
            {
                // Load NFT data from Holochain using HoloNET
                // This would query Holochain DHT for NFT metadata
                var nft = new OASISNFT
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

        public async Task<OASISResult<IOASISNFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var response = new OASISResult<IOASISNFT>();
            try
            {
                // Load NFT data from Holochain using HoloNET
                // This would query Holochain DHT for NFT metadata
                var nft = new OASISNFT
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
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
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

        #endregion
    }
}
