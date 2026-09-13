using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Oracle;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Extensions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

public partial class RadixOASIS
{
    public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0)
    {
        return LoadAvatarByEmailAsync(email, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await AvatarManager.Instance.LoadAvatarAsync(username, false, true, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default, version);
            }

            // Query avatar by username from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "get_avatar_by_username",
                args = new[] { username, version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var avatarJson = response.Result.GetRawText();
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(avatarJson);
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Successfully loaded avatar by username from Radix";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Radix response");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load avatar by username from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0)
    {
        return LoadAvatarByUsernameAsync(username, version).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await AvatarManager.Instance.LoadAvatarDetailAsync(id, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default, version);
            }

            // Query avatar detail by ID from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "get_avatar_detail_by_id",
                args = new[] { id.ToString(), version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var avatarDetailJson = response.Result.GetRawText();
                var avatarDetail = System.Text.Json.JsonSerializer.Deserialize<AvatarDetail>(avatarDetailJson);
                if (avatarDetail != null)
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Successfully loaded avatar detail from Radix";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar detail from Radix response");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load avatar detail from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
    {
        return LoadAvatarDetailAsync(id, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }
            if (_radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix service is not initialized");
                return result;
            }

            if (avatar == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avatar cannot be null");
                return result;
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await AvatarManager.Instance.SaveAvatarAsync(avatar);
            }

            // Serialize avatar to JSON
            string avatarInfo = System.Text.Json.JsonSerializer.Serialize(avatar);
            string avatarId = avatar.Id.ToString();

            // Get wallet for signing
            var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatar.Id, ProviderType.Value);
            if (walletResult.IsError || walletResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet for avatar");
                return result;
            }

            // Build transaction manifest calling OASIS blueprint's create_avatar function
            var network = _config.NetworkId == 1 ? "mainnet" : "stokenet";
            var manifest = new
            {
                instructions = new[]
                {
                    new
                    {
                        kind = "CallMethod",
                        componentAddress = _config.OasisBlueprintAddress,
                        methodName = "create_avatar",
                        args = new[]
                        {
                            new { kind = "String", value = avatarId },
                            new { kind = "String", value = avatarInfo }
                        }
                    }
                },
                blobs = new object[0]
            };

            // Get construction metadata for transaction header
            var metadataResult = await _httpClient.GetConstructionMetadataAsync(_config);
            if (metadataResult == null)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get Radix construction metadata");
                return result;
            }

            // Build transaction header
            var transactionHeader = new
            {
                networkId = _config.NetworkId,
                startEpochInclusive = metadataResult.CurrentEpoch,
                endEpochExclusive = metadataResult.CurrentEpoch + 50,
                nonce = new Random().Next(),
                notaryPublicKey = walletResult.Result.PublicKey,
                notaryIsSignatory = true,
                tipPercentage = 0
            };

            // Build complete transaction
            var transactionData = new
            {
                network = network,
                manifest = System.Text.Json.JsonSerializer.Serialize(manifest),
                header = transactionHeader,
                message = new { kind = "None" }
            };

            // Submit transaction via Radix Gateway API
            var submitResult = await HttpClientHelper.PostAsync<object, TransactionSubmitResponse>(
                _httpClient,
                $"{_config.HostUri}/core/lts/transaction/submit",
                transactionData);

            if (submitResult.IsError || submitResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to submit Radix transaction: {submitResult.Message}");
                return result;
            }

            // Store transaction hash
            if (!string.IsNullOrEmpty(submitResult.Result.TransactionHash))
            {
                avatar.ProviderUniqueStorageKey[ProviderType.Value] = submitResult.Result.TransactionHash;
            }

            result.Result = avatar;
            result.IsError = false;
            result.IsSaved = true;
            result.Message = "Avatar saved to Radix blueprint successfully";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error saving avatar to Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
    {
        return SaveAvatarAsync(avatar).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            if (avatarDetail == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avatar detail cannot be null");
                return result;
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await AvatarManager.Instance.SaveAvatarDetailAsync(avatarDetail);
            }

            // Serialize avatar detail to JSON
            string avatarDetailInfo = System.Text.Json.JsonSerializer.Serialize(avatarDetail);
            string avatarDetailId = avatarDetail.Id.ToString();

            // Get wallet for signing
            var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatarDetail.Id, ProviderType.Value);
            if (walletResult.IsError || walletResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet for avatar detail");
                return result;
            }

            // Build transaction manifest calling OASIS blueprint's create_avatar_detail function
            var network = _config.NetworkId == 1 ? "mainnet" : "stokenet";
            var manifest = new
            {
                instructions = new[]
                {
                    new
                    {
                        kind = "CallMethod",
                        componentAddress = _config.OasisBlueprintAddress,
                        methodName = "create_avatar_detail",
                        args = new[]
                        {
                            new { kind = "String", value = avatarDetailId },
                            new { kind = "String", value = avatarDetailInfo }
                        }
                    }
                },
                blobs = new object[0]
            };

            // Get construction metadata for transaction header
            var metadataResult = await _httpClient.GetConstructionMetadataAsync(_config);
            if (metadataResult == null)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get Radix construction metadata");
                return result;
            }

            // Build transaction header
            var transactionHeader = new
            {
                networkId = _config.NetworkId,
                startEpochInclusive = metadataResult.CurrentEpoch,
                endEpochExclusive = metadataResult.CurrentEpoch + 50,
                nonce = new Random().Next(),
                notaryPublicKey = walletResult.Result.PublicKey,
                notaryIsSignatory = true,
                tipPercentage = 0
            };

            // Build complete transaction
            var transactionData = new
            {
                network = network,
                manifest = System.Text.Json.JsonSerializer.Serialize(manifest),
                header = transactionHeader,
                message = new { kind = "None" }
            };

            // Submit transaction via Radix Gateway API
            var submitResult = await HttpClientHelper.PostAsync<object, TransactionSubmitResponse>(
                _httpClient,
                $"{_config.HostUri}/core/lts/transaction/submit",
                transactionData);

            if (submitResult.IsError || submitResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to submit Radix transaction: {submitResult.Message}");
                return result;
            }

            // Store transaction hash
            if (!string.IsNullOrEmpty(submitResult.Result.TransactionHash))
            {
                avatarDetail.ProviderUniqueStorageKey[ProviderType.Value] = submitResult.Result.TransactionHash;
            }

            result.Result = avatarDetail;
            result.IsError = false;
            result.IsSaved = true;
            result.Message = "Avatar detail saved to Radix blueprint successfully";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
    {
        return SaveAvatarDetailAsync(avatarDetail).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await AvatarManager.Instance.DeleteAvatarAsync(id, softDelete);
            }

            // Get wallet for signing
            var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(id, ProviderType.Value);
            if (walletResult.IsError || walletResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet for avatar");
                return result;
            }

            // Build transaction manifest calling OASIS blueprint's delete_avatar function
            var network = _config.NetworkId == 1 ? "mainnet" : "stokenet";
            var manifest = new
            {
                instructions = new[]
                {
                    new
                    {
                        kind = "CallMethod",
                        componentAddress = _config.OasisBlueprintAddress,
                        methodName = softDelete ? "soft_delete_avatar" : "delete_avatar",
                        args = new[]
                        {
                            new { kind = "String", value = id.ToString() }
                        }
                    }
                },
                blobs = new object[0]
            };

            // Get construction metadata for transaction header
            var metadataResult = await _httpClient.GetConstructionMetadataAsync(_config);
            if (metadataResult == null)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get Radix construction metadata");
                return result;
            }

            // Build transaction header
            var transactionHeader = new
            {
                networkId = _config.NetworkId,
                startEpochInclusive = metadataResult.CurrentEpoch,
                endEpochExclusive = metadataResult.CurrentEpoch + 50,
                nonce = new Random().Next(),
                notaryPublicKey = walletResult.Result.PublicKey,
                notaryIsSignatory = true,
                tipPercentage = 0
            };

            // Build complete transaction
            var transactionData = new
            {
                network = network,
                manifest = System.Text.Json.JsonSerializer.Serialize(manifest),
                header = transactionHeader,
                message = new { kind = "None" }
            };

            // Submit transaction via Radix Gateway API
            var submitResult = await HttpClientHelper.PostAsync<object, TransactionSubmitResponse>(
                _httpClient,
                $"{_config.HostUri}/core/lts/transaction/submit",
                transactionData);

            if (submitResult.IsError || submitResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to submit Radix transaction: {submitResult.Message}");
                return result;
            }

            result.Result = true;
            result.IsError = false;
            result.Message = "Avatar deleted from Radix blueprint successfully";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Radix: {ex.Message}", ex);
        }
        return result;
    }

}
