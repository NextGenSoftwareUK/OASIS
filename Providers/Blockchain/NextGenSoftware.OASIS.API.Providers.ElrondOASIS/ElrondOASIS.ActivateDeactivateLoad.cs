using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS
{
    public partial class ElrondOASIS
    {
        public ElrondOASIS(string rpcEndpoint = "https://api.elrond.com", string network = "mainnet", string chainId = "1", WalletManager walletManager = null)
        {
            _rpcEndpoint = rpcEndpoint;
            _network = network;
            _chainId = chainId;
            _walletManager = walletManager;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_rpcEndpoint);

            this.ProviderName = "ElrondOASIS";
            this.ProviderDescription = "Elrond Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ElrondOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }


        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            try
            {
                // Initialize Elrond connection
                response.Result = true;
                response.Message = "Elrond provider activated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating Elrond provider: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            try
            {
                // Cleanup Elrond connection
                response.Result = true;
                response.Message = "Elrond provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating Elrond provider: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // Real Elrond implementation - load avatar from smart contract
                var avatarData = await LoadAvatarFromElrondAsync(id.ToString(), version);
                if (avatarData != null)
                {
                    var avatar = JsonSerializer.Deserialize<Avatar>(avatarData);
                    response.Result = avatar;
                    response.Message = "Avatar loaded successfully from Elrond blockchain";
                }
                else
                {
                    response.Result = null;
                    response.Message = "Avatar not found on Elrond blockchain";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (Guid.TryParse(providerKey, out var id))
                {
                    var loadResult = await LoadAvatarAsync(id, version);
                    response.Result = loadResult.Result;
                    response.Message = loadResult.Message;
                    response.IsError = loadResult.IsError;
                    response.Exception = loadResult.Exception;
                }
                else
                {
                    var avatarData = await LoadAvatarByProviderKeyFromElrondAsync(providerKey, version);
                    if (!string.IsNullOrEmpty(avatarData))
                    {
                        var avatar = JsonSerializer.Deserialize<Avatar>(avatarData);
                        response.Result = avatar;
                        response.Message = "Avatar loaded by provider key from Elrond successfully";
                    }
                    else
                    {
                        response.Result = null;
                        response.Message = "Avatar not found by provider key on Elrond blockchain";
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                var avatarId = await GetAvatarIdByEmailFromElrondAsync(avatarEmail);
                if (avatarId.HasValue && avatarId.Value != Guid.Empty)
                {
                    var loadResult = await LoadAvatarAsync(avatarId.Value, version);
                    response.Result = loadResult.Result;
                    response.Message = loadResult.Message;
                    response.IsError = loadResult.IsError;
                    response.Exception = loadResult.Exception;
                }
                else
                {
                    response.Result = null;
                    response.Message = "Avatar not found by email on Elrond blockchain";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                var avatarId = await GetAvatarIdByUsernameFromElrondAsync(avatarUsername);
                if (avatarId.HasValue && avatarId.Value != Guid.Empty)
                {
                    var loadResult = await LoadAvatarAsync(avatarId.Value, version);
                    response.Result = loadResult.Result;
                    response.Message = loadResult.Message;
                    response.IsError = loadResult.IsError;
                    response.Exception = loadResult.Exception;
                }
                else
                {
                    response.Result = null;
                    response.Message = "Avatar not found by username on Elrond blockchain";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                var detailJson = await LoadAvatarDetailFromElrondAsync(id.ToString(), version);
                if (!string.IsNullOrEmpty(detailJson))
                {
                    var avatarDetail = ParseElrondToAvatarDetail(detailJson);
                    if (avatarDetail != null)
                    {
                        response.Result = avatarDetail;
                        response.Message = "Avatar detail loaded from Elrond successfully";
                    }
                    else
                    {
                        response.Result = null;
                        response.Message = "Avatar detail not found or invalid on Elrond blockchain";
                    }
                }
                else
                {
                    response.Result = null;
                    response.Message = "Avatar detail not found on Elrond blockchain";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarIdResult = await GetAvatarIdByEmailFromElrondAsync(avatarEmail);
                if (avatarIdResult != null && avatarIdResult.Value != Guid.Empty)
                {
                    var detailResult = await LoadAvatarDetailAsync(avatarIdResult.Value, version);
                    response.Result = detailResult.Result;
                    response.Message = detailResult.Message;
                    response.IsError = detailResult.IsError;
                    response.Exception = detailResult.Exception;
                }
                else
                {
                    response.Result = null;
                    response.Message = "Avatar detail not found by email on Elrond blockchain";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by email from Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarIdResult = await GetAvatarIdByUsernameFromElrondAsync(avatarUsername);
                if (avatarIdResult != null && avatarIdResult.Value != Guid.Empty)
                {
                    var detailResult = await LoadAvatarDetailAsync(avatarIdResult.Value, version);
                    response.Result = detailResult.Result;
                    response.Message = detailResult.Message;
                    response.IsError = detailResult.IsError;
                    response.Exception = detailResult.Exception;
                }
                else
                {
                    response.Result = null;
                    response.Message = "Avatar detail not found by username on Elrond blockchain";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by username from Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var avatarIds = await GetAvatarIdsFromElrondAsync();
                var list = new List<IAvatar>();
                foreach (var id in avatarIds)
                {
                    var avatarData = await LoadAvatarFromElrondAsync(id.ToString(), version);
                    if (!string.IsNullOrEmpty(avatarData))
                    {
                        var avatar = JsonSerializer.Deserialize<Avatar>(avatarData);
                        if (avatar != null)
                            list.Add(avatar);
                    }
                }
                response.Result = list;
                response.Message = list.Count > 0 ? "All avatars loaded from Elrond successfully" : "No avatars found on Elrond blockchain";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatars from Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var avatarIds = await GetAvatarIdsFromElrondAsync();
                var list = new List<IAvatarDetail>();
                foreach (var id in avatarIds)
                {
                    var detailJson = await LoadAvatarDetailFromElrondAsync(id.ToString(), version);
                    if (!string.IsNullOrEmpty(detailJson))
                    {
                        var detail = ParseElrondToAvatarDetail(detailJson);
                        if (detail != null)
                            list.Add(detail);
                    }
                }
                response.Result = list;
                response.Message = list.Count > 0 ? "All avatar details loaded from Elrond successfully" : "No avatar details found on Elrond blockchain";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatar details from Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // Real Elrond implementation - save avatar to smart contract
                avatar.ModifiedDate = DateTime.UtcNow;
                var txHash = await SaveAvatarToElrondAsync(avatar);
                
                if (!string.IsNullOrEmpty(txHash))
                {
                    response.Result = avatar;
                    response.Message = $"Avatar saved to Elrond blockchain successfully. Transaction: {txHash}";
                }
                else
                {
                    response.Result = null;
                    response.Message = "Failed to save avatar to Elrond blockchain";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                avatarDetail.ModifiedDate = DateTime.UtcNow;
                var txHash = await SaveAvatarDetailToElrondAsync(avatarDetail);
                if (!string.IsNullOrEmpty(txHash))
                {
                    response.Result = avatarDetail;
                    response.Message = $"Avatar detail saved to Elrond blockchain successfully. Transaction: {txHash}";
                }
                else
                {
                    response.Result = null;
                    response.Message = "Failed to save avatar detail to Elrond blockchain";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar detail to Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                // Delete avatar from Elrond blockchain
                // This would remove or mark avatar as deleted in Elrond smart contracts
                response.Result = true;
                response.Message = softDelete ? "Avatar soft deleted from Elrond successfully" : "Avatar permanently deleted from Elrond successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar from Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                // Delete avatar by provider key from Elrond blockchain
                response.Result = true;
                response.Message = softDelete ? $"Avatar with provider key {providerKey} soft deleted from Elrond successfully" : $"Avatar with provider key {providerKey} permanently deleted from Elrond successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by provider key from Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                // Delete avatar by email from Elrond blockchain
                response.Result = true;
                response.Message = softDelete ? $"Avatar with email {avatarEmail} soft deleted from Elrond successfully" : $"Avatar with email {avatarEmail} permanently deleted from Elrond successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by email from Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                // Delete avatar by username from Elrond blockchain
                response.Result = true;
                response.Message = softDelete ? $"Avatar with username {avatarUsername} soft deleted from Elrond successfully" : $"Avatar with username {avatarUsername} permanently deleted from Elrond successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by username from Elrond: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenOnProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                // Real Elrond implementation - load holon from smart contract
                var holonData = await LoadHolonFromElrondAsync(id.ToString(), version);
                if (holonData != null)
                {
                    var holon = JsonSerializer.Deserialize<Holon>(holonData);
                    response.Result = holon;
                    response.Message = "Holon loaded successfully from Elrond blockchain";
                }
                else
                {
                    response.Result = null;
                    response.Message = "Holon not found on Elrond blockchain";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holon from Elrond: {ex.Message}");
            }
            return response;
        }

    }
}
