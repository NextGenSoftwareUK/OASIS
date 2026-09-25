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
    public partial class HoloOASIS
    {


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

    }
}
