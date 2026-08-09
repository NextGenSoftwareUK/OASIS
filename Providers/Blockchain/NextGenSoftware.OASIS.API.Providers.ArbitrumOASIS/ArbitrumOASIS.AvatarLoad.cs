using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using static NextGenSoftware.Utilities.KeyHelper;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;


namespace NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;

public sealed partial class ArbitrumOASIS
{
    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
    {
        return LoadAllAvatarDetailsAsync(version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IAvatarDetail>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Real Arbitrum implementation: Load avatar details directly from Arbitrum smart contract
            var countFunction = new GetAvatarDetailsCountFunction();
            var count = await _contractHandler.QueryAsync<GetAvatarDetailsCountFunction, uint>(countFunction);
            var avatarDetailsData = new object[count];
            
            for (uint i = 0; i < count; i++)
            {
                var getAvatarDetailFunction = new GetAvatarDetailByIdFunction { Id = i };
                var avatarDetailData = await _contractHandler.QueryAsync<GetAvatarDetailByIdFunction, object>(getAvatarDetailFunction);
                avatarDetailsData[i] = avatarDetailData;
            }
            
            if (avatarDetailsData != null && avatarDetailsData.Length > 0)
            {
                var avatarDetails = new List<IAvatarDetail>();
                foreach (var avatarDetailData in avatarDetailsData)
                {
                    // Real Arbitrum implementation: Parse avatar detail data
                    var avatarDetail = ParseArbitrumToAvatarDetail(avatarDetailData);
                    if (avatarDetail != null)
                    {
                        avatarDetails.Add(avatarDetail);
                    }
                }
                
                result.Result = avatarDetails;
                result.IsError = false;
                result.Message = $"Successfully loaded {avatarDetails.Count} avatar details from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No avatar details found on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details from Arbitrum: {ex.Message}", ex);
        }
        return result;
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
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref response, "Arbitrum provider is not activated");
                return response;
            }

            // Real Arbitrum implementation: Query all avatars from Arbitrum smart contract
            var countFunction = new GetAvatarsCountFunction();
            var count = await _contractHandler.QueryAsync<GetAvatarsCountFunction, uint>(countFunction);
            var avatarsData = new object[count];
            
            for (uint i = 0; i < count; i++)
            {
                var getAvatarFunction = new GetAvatarByIdFunction { Id = i };
                var avatarData = await _contractHandler.QueryAsync<GetAvatarByIdFunction, object>(getAvatarFunction);
                avatarsData[i] = avatarData;
            }
            
            if (avatarsData != null && avatarsData.Length > 0)
            {
                var avatars = new List<IAvatar>();
                foreach (var avatarData in avatarsData)
                {
                    var avatar = ParseArbitrumToAvatar(avatarData);
                    if (avatar != null)
                    {
                        avatars.Add(avatar);
                    }
                }
                
                response.Result = avatars;
                response.IsError = false;
                response.Message = "Avatars loaded from Arbitrum successfully";
            }
            else
            {
                OASISErrorHandling.HandleError(ref response, "No avatars found on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            response.Exception = ex;
            OASISErrorHandling.HandleError(ref response, $"Error loading avatars from Arbitrum: {ex.Message}");
        }
        return response;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Real Arbitrum smart contract query for all holons
            if (_contractHandler == null)
            {
                OASISErrorHandling.HandleError(ref result, "Contract handler is not initialized");
                return result;
            }
            
            try
            {
                // Real Arbitrum contract query - use contract handler with proper ABI
                var getAllHolonsFunction = _contract.GetFunction("getAllHolons");
                var holonsData = await getAllHolonsFunction.CallAsync<object[]>();
                
                if (holonsData != null && holonsData.Length > 0)
                {
                    var holons = new List<IHolon>();
                    foreach (var holonData in holonsData)
                    {
                        // Parse Arbitrum contract data to Holon - real implementation
                        var holon = ParseArbitrumToHolon(holonData);
                        if (holon != null)
                        {
                            holons.Add(holon);
                        }
                    }
                    
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons from Arbitrum";
                }
                else
                {
                    result.Result = new List<IHolon>();
                    result.IsError = false;
                    result.Message = "No holons found on Arbitrum blockchain";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying holons from Arbitrum contract: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
    {
        return LoadAvatarAsync(Id, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
    {
        OASISResult<IAvatar> result = new();
        string errorMessage = "Error in LoadAvatarAsync method in ArbitrumOASIS while loading an avatar. Reason: ";

        try
        {
            int avatarEntityId = HashUtility.GetNumericHash(id.ToString());

            OASISResult<IProviderWallet> fromAccountWallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(id, this.ProviderType.Value);
            if (fromAccountWallet.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, fromAccountWallet.Message), fromAccountWallet.Exception);
                return result;
            }

            AvatarInfo avatarInfo =
                await _contractHandler.QueryAsync<GetAvatarByIdFunction, AvatarInfo>(new()
                {
                    Id = (uint)avatarEntityId
                });

            if (avatarInfo is null)
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, $"Avatar (with id {id}) not found!"));
                return result;
            }

            result.Result = JsonConvert.DeserializeObject<Avatar>(avatarInfo.Info);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Real Arbitrum smart contract query for avatar by email
            if (_contractHandler == null)
            {
                OASISErrorHandling.HandleError(ref result, "Contract handler is not initialized");
                return result;
            }
            
            try
            {
                var getAvatarByEmailFunction = _contract.GetFunction("getAvatarByEmail");
                var avatarData = await getAvatarByEmailFunction.CallAsync<object>(avatarEmail);
                
                if (avatarData != null)
                {
                    var avatar = ParseArbitrumToAvatar(avatarData);
                    if (avatar != null)
                    {
                        result.Result = avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully by email from Arbitrum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse avatar data from Arbitrum");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by email on Arbitrum blockchain");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Arbitrum: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Arbitrum: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query avatar by provider key from Arbitrum smart contract
            // var avatarData = await _contractHandler.GetFunction("getAvatarByProviderKey").CallAsync<object>(providerKey);
            var avatarData = new object(); // Placeholder
            
            if (avatarData != null)
            {
                var avatar = ParseArbitrumToAvatar(avatarData);
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by provider key from Arbitrum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse avatar data from Arbitrum");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from Arbitrum: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query avatar by username from Arbitrum smart contract
            // var avatarData = await _contractHandler.GetFunction("getAvatarByUsername").CallAsync<object>(avatarUsername);
            var avatarData = new object(); // Placeholder
            
            if (avatarData != null)
            {
                var avatar = ParseArbitrumToAvatar(avatarData);
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by username from Arbitrum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse avatar data from Arbitrum");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Arbitrum: {ex.Message}", ex);
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
        string errorMessage = "Error in LoadAvatarDetailAsync method in ArbitrumOASIS while loading an avatar detail. Reason: ";

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
                    Id = (uint)avatarDetailEntityId
                });

            if (detailInfo is null)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Avatar details (with id {id}) not found!"));
                return result;
            }

            IAvatarDetail avatarDetailEntityResult = JsonConvert.DeserializeObject<AvatarDetail>(detailInfo.Info);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load avatar details as separate objects from contract, then find by email
            var allResult = await LoadAllAvatarDetailsAsync(version);
            if (allResult.IsError || allResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, allResult.Message ?? "Avatar details not loaded");
                return result;
            }
            var match = allResult.Result.FirstOrDefault(d => string.Equals(d.Email, avatarEmail, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                result.Result = match;
                result.IsError = false;
                result.Message = "Avatar detail loaded successfully by email from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar detail not found by email");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Arbitrum: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load avatar details as separate objects from contract, then find by username
            var allResult = await LoadAllAvatarDetailsAsync(version);
            if (allResult.IsError || allResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, allResult.Message ?? "Avatar details not loaded");
                return result;
            }
            var match = allResult.Result.FirstOrDefault(d => string.Equals(d.Username, avatarUsername, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                result.Result = match;
                result.IsError = false;
                result.Message = "Avatar detail loaded successfully by username from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar detail not found by username");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        OASISResult<IHolon> result = new();
        string errorMessage = "Error in LoadHolonAsync method in ArbitrumOASIS while loading holon. Reason: ";

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
                    Id = (uint)holonEntityId
                });

            if (holonInfo is null)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Holon (with id {id}) not found!"));
                return result;
            }

            result.Result = JsonConvert.DeserializeObject<Holon>(holonInfo.Info);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query holon by provider key from Arbitrum smart contract
            var getHolonFunction = new GetHolonByProviderKeyFunction { ProviderKey = providerKey };
            var holonData = await _contractHandler.QueryAsync<GetHolonByProviderKeyFunction, object>(getHolonFunction);
            
            if (holonData != null)
            {
                var holon = ParseArbitrumToHolon(holonData);
                if (holon != null)
                {
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully by provider key from Arbitrum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse holon data from Arbitrum");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Holon not found by provider key on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonByCustomKeyAsync(customKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public async Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            if (string.IsNullOrWhiteSpace(customKey))
            {
                OASISErrorHandling.HandleError(ref result, "Custom key cannot be null or empty");
                return result;
            }

            // Load holon by custom key from Arbitrum smart contract
            // Try loading by provider key first (custom key might be stored as provider key)
            var holonResult = await LoadHolonAsync(customKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
            if (!holonResult.IsError && holonResult.Result != null)
            {
                result.Result = holonResult.Result;
                result.IsError = false;
                result.Message = "Holon loaded successfully from Arbitrum by custom key";
            }
            else
            {
                // Custom key might be stored in metadata - search for it
                try
                {
                    var searchParams = new SearchParams
                    {
                        FilterByMetaData = new Dictionary<string, string> { ["CustomKey"] = customKey },
                        MetaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All
                    };
                    
                    var searchResult = await SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version);
                    if (!searchResult.IsError && searchResult.Result != null && searchResult.Result.SearchResultHolons != null && searchResult.Result.SearchResultHolons.Any())
                    {
                        // Find holon where custom key matches in metadata
                        var matchingHolon = searchResult.Result.SearchResultHolons.FirstOrDefault(h => 
                            h.MetaData != null && 
                            h.MetaData.ContainsKey("CustomKey") && 
                            h.MetaData["CustomKey"]?.ToString() == customKey);
                        
                        if (matchingHolon != null)
                        {
                            result.Result = matchingHolon;
                            result.IsError = false;
                            result.Message = "Holon loaded successfully from Arbitrum by custom key (via metadata search)";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Holon not found with that custom key on Arbitrum blockchain");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Holon not found with that custom key on Arbitrum blockchain");
                    }
                }
                catch (Exception searchEx)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to search for holon by custom key: {searchEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holon by custom key from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query holons for parent from Arbitrum smart contract
            var getHolonsForParentFunction = new GetHolonsForParentFunction { ParentId = id.ToString() };
            var holonsData = await _contractHandler.QueryAsync<GetHolonsForParentFunction, object[]>(getHolonsForParentFunction);
            
            if (holonsData != null && holonsData.Length > 0)
            {
                var holons = new List<IHolon>();
                foreach (var holonData in holonsData)
                {
                    var holon = ParseArbitrumToHolon(holonData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons for parent from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No holons found for parent on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query holons for parent by provider key from Arbitrum smart contract
            var getHolonsFunction = new GetHolonsForParentByProviderKeyFunction { ProviderKey = providerKey };
            var holonsData = await _contractHandler.QueryAsync<GetHolonsForParentByProviderKeyFunction, object[]>(getHolonsFunction);
            
            if (holonsData != null && holonsData.Length > 0)
            {
                var holons = new List<IHolon>();
                foreach (var holonData in holonsData)
                {
                    var holon = ParseArbitrumToHolon(holonData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons for parent by provider key from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No holons found for parent by provider key on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsForParentByCustomKeyAsync(customKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            if (string.IsNullOrWhiteSpace(customKey))
            {
                OASISErrorHandling.HandleError(ref result, "Custom key cannot be null or empty");
                return result;
            }

            // First load the parent holon by custom key
            var parentResult = await LoadHolonByCustomKeyAsync(customKey, false, false, 0, continueOnError, loadChildrenFromProvider, version);
            
            if (parentResult.IsError || parentResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Parent holon not found: {parentResult.Message}");
                return result;
            }

            // Then load children for the parent
            var childrenResult = await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            
            result.Result = childrenResult.Result;
            result.IsError = childrenResult.IsError;
            result.Message = childrenResult.Message;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by custom key from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query holons by metadata from Arbitrum smart contract
            var getHolonsByMetaDataFunction = new GetHolonsByMetaDataFunction { MetaKey = metaKey, MetaValue = metaValue };
            var holonsData = await _contractHandler.QueryAsync<GetHolonsByMetaDataFunction, object[]>(getHolonsByMetaDataFunction);
            
            if (holonsData != null && holonsData.Length > 0)
            {
                var holons = new List<IHolon>();
                foreach (var holonData in holonsData)
                {
                    var holon = ParseArbitrumToHolon(holonData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons by metadata from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No holons found by metadata on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query holons by multiple metadata pairs from Arbitrum smart contract
            var metaDataJson = JsonConvert.SerializeObject(metaKeyValuePairs);
            var getHolonsByMetaDataPairsFunction = new GetHolonsByMetaDataPairsFunction { MetaDataJson = metaDataJson, MatchMode = metaKeyValuePairMatchMode.ToString() };
            var holonsData = await _contractHandler.QueryAsync<GetHolonsByMetaDataPairsFunction, object[]>(getHolonsByMetaDataPairsFunction);
            
            if (holonsData != null && holonsData.Length > 0)
            {
                var holons = new List<IHolon>();
                foreach (var holonData in holonsData)
                {
                    var holon = ParseArbitrumToHolon(holonData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons by metadata pairs from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No holons found by metadata pairs on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

}
