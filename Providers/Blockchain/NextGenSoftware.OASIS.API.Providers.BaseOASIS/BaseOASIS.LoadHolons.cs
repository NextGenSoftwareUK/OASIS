using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NextGenSoftware.OASIS.API.Providers.BaseOASIS;

public sealed partial class BaseOASIS
{
    public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
    {
        return LoadAvatarByEmailAsync(avatarEmail, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Base provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load avatar by email from Base blockchain
            var avatarData = await GetAccountByEmailAsync(avatarEmail);
            if (!avatarData.IsSuccessStatusCode)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarData.ReasonPhrase}");
                return result;
            }

            var content = await avatarData.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(content))
            {
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(content);
                result.Result = avatar;
                result.IsError = false;
                result.Message = "Avatar loaded successfully by email from Base";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email on Base blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Base: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
    {
        return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Base provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load avatar by provider key from Base blockchain
            var avatarData = await GetAccountByProviderKeyAsync(providerKey);
            if (!avatarData.IsSuccessStatusCode)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from Base: {avatarData.ReasonPhrase}");
                return result;
            }

            var content = await avatarData.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(content))
            {
                var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);
                var avatar = BaseOASISHelpers.ParseBaseToAvatar(jsonElement);
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by provider key from Base with full object mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse avatar data from Base");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key in Base");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from Base: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
    {
        return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Base provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load avatar by username from Base blockchain
            var avatarData = await GetAccountByUsernameAsync(avatarUsername);
            if (!avatarData.IsSuccessStatusCode)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Base: {avatarData.ReasonPhrase}");
                return result;
            }

            var content = await avatarData.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(content))
            {
                var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);
                var avatar = BaseOASISHelpers.ParseBaseToAvatar(jsonElement);
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by username from Base with full object mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse avatar data from Base");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username in Base");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Base: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
    {
        return LoadAvatarDetailAsync(id, version).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
    {
        OASISResult<IAvatarDetail> result = new();
        string errorMessage = "Error in LoadAvatarDetailAsync method in BaseOASIS while loading an avatar detail. Reason: ";

        try
        {
            int avatarDetailEntityId = HashUtility.GetNumericHash(id.ToString());

            OASISResult<IProviderWallet> fromAccountWallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(id, this.ProviderType.Value);
            if (fromAccountWallet.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, fromAccountWallet.Message), fromAccountWallet.Exception);
                return result;
            }

            AvatarDetailInfo detailInfo =
                await _contractHandler.QueryAsync<GetAvatarDetailByIdFunction, AvatarDetailInfo>(new()
                {
                    EntityId = avatarDetailEntityId
                });

            if (detailInfo is null)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Avatar details (with id {id}) not found!"));
                return result;
            }

            IAvatarDetail avatarDetailEntityResult = System.Text.Json.JsonSerializer.Deserialize<AvatarDetail>(detailInfo.Info);
            result.IsError = false;
            result.IsLoaded = true;
            result.Result = avatarDetailEntityResult;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
    {
        return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Base provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load avatar detail by email from Base blockchain
            var avatarDetailData = await GetAvatarDetailByEmailAsync(avatarEmail);
            if (!avatarDetailData.IsSuccessStatusCode)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Base: {avatarDetailData.ReasonPhrase}");
                return result;
            }

            var content = await avatarDetailData.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(content))
            {
                var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);
                var avatarDetail = BaseOASISHelpers.ParseBaseToAvatarDetail(jsonElement);
                if (avatarDetail != null)
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by email from Base with full object mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse avatar detail data from Base");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar detail not found by email in Base");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Base: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
    {
        return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Base provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load avatar detail by username from Base blockchain
            var avatarDetailData = await GetAvatarDetailByUsernameAsync(avatarUsername);
            if (!avatarDetailData.IsSuccessStatusCode)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Base: {avatarDetailData.ReasonPhrase}");
                return result;
            }

            var content = await avatarDetailData.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(content))
            {
                var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);
                var avatarDetail = BaseOASISHelpers.ParseBaseToAvatarDetail(jsonElement);
                if (avatarDetail != null)
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by username from Base with full object mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse avatar detail data from Base");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar detail not found by username in Base");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Base: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new Exception();
    }

    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        OASISResult<IHolon> result = new();
        string errorMessage = "Error in LoadHolonAsync method in BaseOASIS while loading holon. Reason: ";

        try
        {
            int holonEntityId = HashUtility.GetNumericHash(id.ToString());

            OASISResult<IProviderWallet> fromAccountWallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(id, this.ProviderType.Value);
            if (fromAccountWallet.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, fromAccountWallet.Message), fromAccountWallet.Exception);
                return result;
            }

            HolonInfo holonInfo =
                await _contractHandler.QueryAsync<GetHolonByIdyIdFunction, HolonInfo>(new()
                {
                    EntityId = holonEntityId
                });

            if (holonInfo is null)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Holon (with id {id}) not found!"));
                return result;
            }

            result.Result = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonInfo.Info);
            result.IsError = false;
            result.IsLoaded = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Base provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load holon by provider key from Base blockchain
            var holonData = await GetHolonByProviderKeyAsync(providerKey);
            if (!holonData.IsSuccessStatusCode)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Base: {holonData.ReasonPhrase}");
                return result;
            }

            var content = await holonData.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(content))
            {
                var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);
                var holon = BaseOASISHelpers.ParseBaseToHolon(jsonElement);
                if (holon != null)
                {
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully by provider key from Base with full object mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse holon data from Base");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Holon not found by provider key in Base");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Base: {ex.Message}", ex);
        }
        return result;
    }

    //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Base provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load holons for parent from Base blockchain
            var holonsData = await GetHolonsForParentAsync(id.ToString());
            if (!holonsData.IsSuccessStatusCode)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Base: {holonsData.ReasonPhrase}");
                return result;
            }

            var content = await holonsData.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(content))
            {
                var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);
                var holons = ParseBaseToHolons(jsonElement);
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons?.Count() ?? 0} holons for parent from Base";
            }
            else
            {
                result.Result = new List<IHolon>();
                result.IsError = false;
                result.Message = "No holons found for parent from Base";
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Base: {ex.Message}", ex);
        }
        return result;
    }

}
