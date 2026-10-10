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
            
            // The deployed contract has no getAllHolons function — enumerate via GetHolonsCount + GetHolonById.
            uint holonCount = await _contractHandler.QueryAsync<GetHolonsCountFunction, uint>(new GetHolonsCountFunction());
            var holons = new List<IHolon>();
            for (uint i = 1; i <= holonCount; i++)
            {
                HolonInfo info = await _contractHandler.QueryAsync<GetHolonByIdyIdFunction, HolonInfo>(new() { Id = i });
                if (info == null || string.IsNullOrEmpty(info.Info)) continue;
                var holon = JsonConvert.DeserializeObject<Holon>(info.Info);
                if (holon != null)
                {
                    if (type == HolonType.All || holon.HolonType == type)
                        holons.Add(holon);
                }
            }
            result.Result = holons;
            result.IsError = false;
            result.Message = $"Successfully loaded {holons.Count} holons from Arbitrum";
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
        string errorMessage = "Error in LoadAvatarByEmailAsync method in ArbitrumOASIS while loading an avatar by email. Reason: ";

        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, activateResult.Message));
                    return result;
                }
            }

            if (string.IsNullOrWhiteSpace(avatarEmail))
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Email cannot be null or empty."));
                return result;
            }

            // The Arbitrum contract stores avatars by entityId (hash of GUID) with no email index.
            // Scan all stored avatars and filter by email from the serialised JSON payload.
            var countFunction = new GetAvatarsCountFunction();
            uint count = await _contractHandler.QueryAsync<GetAvatarsCountFunction, uint>(countFunction);

            for (uint i = 1; i <= count; i++)
            {
                AvatarInfo info = await _contractHandler.QueryAsync<GetAvatarByIdFunction, AvatarInfo>(new() { Id = i });
                if (info == null || string.IsNullOrEmpty(info.Info)) continue;

                var candidate = JsonConvert.DeserializeObject<Avatar>(info.Info);
                if (candidate != null && string.Equals(candidate.Email, avatarEmail, StringComparison.OrdinalIgnoreCase))
                {
                    result.Result = candidate;
                    result.IsError = false;
                    result.IsLoaded = true;
                    result.Message = "Avatar loaded successfully by email from Arbitrum.";
                    return result;
                }
            }

            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Avatar with email '{avatarEmail}' not found on Arbitrum blockchain."));
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

    public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
    {
        return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        string errorMessage = "Error in LoadAvatarByProviderKeyAsync method in ArbitrumOASIS while loading an avatar by provider key. Reason: ";

        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, activateResult.Message));
                    return result;
                }
            }

            if (string.IsNullOrWhiteSpace(providerKey))
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Provider key cannot be null or empty."));
                return result;
            }

            int entityId = HashUtility.GetNumericHash(providerKey);
            AvatarInfo avatarInfo = await _contractHandler.QueryAsync<GetAvatarByIdFunction, AvatarInfo>(new()
            {
                Id = (uint)entityId
            });

            if (avatarInfo is null || string.IsNullOrEmpty(avatarInfo.Info))
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Avatar with provider key '{providerKey}' not found on Arbitrum blockchain."));
                return result;
            }

            result.Result = JsonConvert.DeserializeObject<Avatar>(avatarInfo.Info);
            result.IsError = false;
            result.IsLoaded = true;
            result.Message = "Avatar loaded successfully by provider key from Arbitrum.";
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

    public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
    {
        return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
    }
}
