using System;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.FantomOASIS
{
    public partial class FantomOASIS_Legacy
    {
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Fantom implementation: Load avatar by provider key (avatarId)
                try
                {
                    var getAvatarFunction = _contract.GetFunction("getAvatar");
                    var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<GetAvatarOutputDTO>(providerKey);

                    if (avatarData != null)
                    {
                        var avatar = new Avatar
                        {
                            Id = Guid.Parse(providerKey),
                            Username = avatarData.Username,
                            Email = avatarData.Email,
                            FirstName = avatarData.FirstName,
                            LastName = avatarData.LastName,
                            AvatarType = new EnumValue<AvatarType>(Enum.Parse<AvatarType>(avatarData.AvatarType))
                        };

                        if (!string.IsNullOrEmpty(avatarData.Metadata))
                        {
                            try
                            {
                                avatar.MetaData = JsonSerializer.Deserialize<Dictionary<string, object>>(avatarData.Metadata);
                            }
                            catch { }
                        }

                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Fantom by provider key successfully";
                        return response;
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("does not exist") || ex.Message.Contains("Avatar does not exist"))
                    {
                        OASISErrorHandling.HandleError(ref response, $"Avatar with provider key {providerKey} not found on Fantom");
                        return response;
                    }
                    throw;
                }

                OASISErrorHandling.HandleError(ref response, $"Avatar with provider key {providerKey} not found on Fantom");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarByProviderKeyAsync: {ex.Message}", ex);
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Fantom implementation: Load all avatars for the current user
                var getUserAvatarsFunction = _contract.GetFunction("getUserAvatars");
                var avatarIds = await getUserAvatarsFunction.CallAsync<List<string>>(_account.Address);

                var avatars = new List<IAvatar>();
                foreach (var avatarId in avatarIds)
                {
                    try
                    {
                        var getAvatarFunction = _contract.GetFunction("getAvatar");
                        var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<GetAvatarOutputDTO>(avatarId);

                        if (avatarData != null)
                        {
                            var avatar = new Avatar
                            {
                                Id = Guid.Parse(avatarId),
                                Username = avatarData.Username,
                                Email = avatarData.Email,
                                FirstName = avatarData.FirstName,
                                LastName = avatarData.LastName,
                                AvatarType = new EnumValue<AvatarType>(Enum.Parse<AvatarType>(avatarData.AvatarType))
                            };

                            if (!string.IsNullOrEmpty(avatarData.Metadata))
                            {
                                try
                                {
                                    avatar.MetaData = JsonSerializer.Deserialize<Dictionary<string, object>>(avatarData.Metadata);
                                }
                                catch { }
                            }

                            avatars.Add(avatar);
                        }
                    }
                    catch { continue; }
                }

                response.Result = avatars;
                response.IsError = false;
                response.Message = $"Loaded {avatars.Count} avatars from Fantom";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllAvatarsAsync: {ex.Message}", ex);
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarDetail is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarDetail: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarDetailAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarDetailAsync: {ex.Message}");
            }
            return response;
        }

        // Holon-related methods
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolon is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolon: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolon is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolon: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsForParent is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsForParent: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsForParentAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsForParentAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsForParent is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsForParent: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsForParentAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsForParentAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsByMetaData is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsByMetaData: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsByMetaDataAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsByMetaDataAsync: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsByMetaDataAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsByMetaDataAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        // Save/Delete Holon methods
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Holon cannot be null");
                    return response;
                }

                // Real Fantom implementation: Save holon to smart contract
                var holonId = holon.Id.ToString();
                var holonData = new
                {
                    holonId = holonId,
                    name = holon.Name ?? "",
                    description = holon.Description ?? "",
                    holonType = holon.HolonType.ToString() ?? "All",
                    metadata = JsonSerializer.Serialize(holon.MetaData ?? new Dictionary<string, object>()),
                    parentId = holon.ParentHolonId != Guid.Empty ? holon.ParentHolonId.ToString() : ""
                };

                // Check if holon exists
                var getHolonFunction = _contract.GetFunction("getHolon");
                bool holonExists = false;
                try
                {
                    await getHolonFunction.CallDeserializingToObjectAsync<GetHolonOutputDTO>(holonId);
                    holonExists = true;
                }
                catch { }

                Nethereum.Contracts.Function function;
                if (holonExists)
                {
                    // Update existing holon
                    function = _contract.GetFunction("updateHolon");
                }
                else
                {
                    // Create new holon
                    function = _contract.GetFunction("createHolon");
                }

                var gasEstimate = await function.EstimateGasAsync(
                    holonData.holonId,
                    holonData.name,
                    holonData.description,
                    holonData.holonType,
                    holonData.metadata,
                    holonData.parentId
                );

                var transactionReceipt = await function.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    holonData.holonId,
                    holonData.name,
                    holonData.description,
                    holonData.holonType,
                    holonData.metadata,
                    holonData.parentId
                );

                if (transactionReceipt.Status.Value == 1)
                {
                    // Save children if requested
                    if (saveChildren && holon.Children != null && holon.Children.Any() && (maxChildDepth == 0 || maxChildDepth > 0))
                    {
                        var childrenResult = await SaveHolonsAsync(holon.Children, saveChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider);
                        if (childrenResult.IsError && !continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref response, $"Error saving holon children: {childrenResult.Message}");
                            return response;
                        }
                    }

                    response.Result = holon;
                    response.IsError = false;
                    response.Message = $"Holon saved to Fantom successfully. Transaction hash: {transactionReceipt.TransactionHash}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Transaction failed on Fantom");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving holon to Fantom: {ex.Message}", ex);
            }
            return response;
        }

    }
}
