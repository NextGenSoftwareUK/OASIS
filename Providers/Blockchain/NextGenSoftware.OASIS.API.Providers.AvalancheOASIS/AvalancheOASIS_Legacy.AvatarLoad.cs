using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
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
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using System.Text;


using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.AvalancheOASIS;

public sealed partial class AvalancheOASIS_Legacy
{
    public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
    {
        return LoadAvatarAsync(Id, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
    {
        OASISResult<IAvatar> result = new();
        string errorMessage = "Error in LoadAvatarAsync method in AvalancheOASIS while loading an avatar. Reason: ";

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
                    EntityId = avatarEntityId
                });

            if (avatarInfo is null)
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, $"Avatar (with id {id}) not found!"));
                return result;
            }

            result.Result = JsonSerializer.Deserialize<Avatar>(avatarInfo.Info);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Avalanche provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query Avalanche smart contract for avatar by email
            try
            {
                // For now, we'll search through all avatars to find one with matching email
                // In a real implementation, you'd have a mapping or index for email lookups
                var avatarsCountFunction = _contract.GetFunction(GetAvatarsCountFuncName);
                var avatarsCount = await avatarsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < avatarsCount; i++)
                {
                    try
                    {
                        var getAvatarFunction = _contract.GetFunction(GetAvatarByIdFuncName);
                        var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<AvatarStruct>(i);
                        
                        // Check if this avatar matches the email (you'd need to store email in the Info field)
                        if (avatarData.Info.Contains(avatarEmail))
                        {
                            var avatar = new Avatar();
                            avatar.Id = Guid.NewGuid();
                            avatar.Email = avatarEmail;
                            avatar.Username = avatarData.AvatarId;
                        avatar.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", avatarData.EntityId.ToString()},
                            {"AvalancheInfo", avatarData.Info}
                        });
                            
                            result.Result = avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded successfully by email from Avalanche";
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid avatars
                        continue;
                    }
                }
                
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email in Avalanche");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying avatar by email from Avalanche: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Avalanche: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Avalanche provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query Avalanche smart contract for avatar by provider key
            try
            {
                // Try to parse provider key as entity ID
                if (uint.TryParse(providerKey, out uint entityId))
                {
                    var getAvatarFunction = _contract.GetFunction(GetAvatarByIdFuncName);
                    var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<AvatarStruct>(entityId);
                    
                    var avatar = new Avatar();
                    avatar.Id = Guid.NewGuid();
                    avatar.Username = avatarData.AvatarId;
                        avatar.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", avatarData.EntityId.ToString()},
                            {"AvalancheInfo", avatarData.Info}
                        });
                    
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by provider key from Avalanche";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Invalid provider key format for Avalanche");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying avatar by provider key from Avalanche: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from Avalanche: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Avalanche provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query Avalanche smart contract for avatar by username
            try
            {
                // Search through all avatars to find one with matching username
                var avatarsCountFunction = _contract.GetFunction(GetAvatarsCountFuncName);
                var avatarsCount = await avatarsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < avatarsCount; i++)
                {
                    try
                    {
                        var getAvatarFunction = _contract.GetFunction(GetAvatarByIdFuncName);
                        var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<AvatarStruct>(i);
                        
                        // Check if this avatar matches the username
                        if (avatarData.AvatarId == avatarUsername)
                        {
                            var avatar = new Avatar();
                            avatar.Id = Guid.NewGuid();
                            avatar.Username = avatarUsername;
                        avatar.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", avatarData.EntityId.ToString()},
                            {"AvalancheInfo", avatarData.Info}
                        });
                            
                            result.Result = avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded successfully by username from Avalanche";
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid avatars
                        continue;
                    }
                }
                
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username in Avalanche");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying avatar by username from Avalanche: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Avalanche: {ex.Message}", ex);
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
        string errorMessage = "Error in LoadAvatarDetailAsync method in AvalancheOASIS while loading an avatar detail. Reason: ";

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

            IAvatarDetail avatarDetailEntityResult = JsonSerializer.Deserialize<AvatarDetail>(detailInfo.Info);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Avalanche provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load avatar by email first, then create avatar detail
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmail, version);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                var avatarDetail = new AvatarDetail();
                avatarDetail.Username = avatarResult.Result.Username;
                result.Result = avatarDetail;
                result.IsError = false;
                result.Message = "Avatar detail loaded successfully by email from Avalanche";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email in Avalanche");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Avalanche: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Avalanche provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load avatar by username first, then create avatar detail
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                var avatarDetail = new AvatarDetail();
                avatarDetail.Username = avatarResult.Result.Username;
                result.Result = avatarDetail;
                result.IsError = false;
                result.Message = "Avatar detail loaded successfully by username from Avalanche";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username in Avalanche");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Avalanche: {ex.Message}", ex);
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
        string errorMessage = "Error in LoadHolonAsync method in AvalancheOASIS while loading holon. Reason: ";

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

            result.Result = JsonSerializer.Deserialize<Holon>(holonInfo.Info);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Avalanche provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query Avalanche smart contract for holon by provider key
            try
            {
                // Try to parse provider key as entity ID
                if (uint.TryParse(providerKey, out uint entityId))
                {
                    var getHolonFunction = _contract.GetFunction(GetHolonByIdFuncName);
                    var holonData = await getHolonFunction.CallDeserializingToObjectAsync<HolonStruct>(entityId);
                    
                    var holon = new Holon();
                    holon.Id = AvalancheContractHelper.CreateDeterministicGuid($"{ProviderType.Value}:holon:{holonData.EntityId}");
                    holon.Name = holonData.HolonId;
                    holon.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                    {
                        {"AvalancheEntityId", holonData.EntityId.ToString()},
                        {"AvalancheInfo", holonData.Info}
                    });
                    
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully by provider key from Avalanche";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Invalid provider key format for Avalanche");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying holon by provider key from Avalanche: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Avalanche: {ex.Message}", ex);
        }
        return result;
    }

}
