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
    public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        var result = new OASISResult<IHolon>();
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

            if (holon == null)
            {
                OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                return result;
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await HolonManager.Instance.SaveHolonAsync(holon, Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            }

            // Serialize holon to JSON
            string holonInfo = System.Text.Json.JsonSerializer.Serialize(holon);
            string holonId = holon.Id.ToString();

            // Get wallet for signing (use creator's wallet or holon's wallet)
            Guid creatorId = holon.CreatedByAvatarId != Guid.Empty ? holon.CreatedByAvatarId : holon.Id;
            var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(creatorId, ProviderType.Value);
            if (walletResult.IsError || walletResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet for holon creator");
                return result;
            }

            // Build transaction manifest calling OASIS blueprint's create_holon function
            var network = _config.NetworkId == 1 ? "mainnet" : "stokenet";
            var manifest = new
            {
                instructions = new[]
                {
                    new
                    {
                        kind = "CallMethod",
                        componentAddress = _config.OasisBlueprintAddress,
                        methodName = "create_holon",
                        args = new[]
                        {
                            new { kind = "String", value = holonId },
                            new { kind = "String", value = holonInfo }
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
                holon.ProviderUniqueStorageKey[ProviderType.Value] = submitResult.Result.TransactionHash;
            }

            // Save child holons recursively if requested
            if (saveChildren && holon.Children != null && holon.Children.Any())
            {
                foreach (var child in holon.Children)
                {
                    await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth > 0 ? maxChildDepth - 1 : 0, continueOnError, saveChildrenOnProvider);
                }
            }

            result.Result = holon;
            result.IsError = false;
            result.IsSaved = true;
            result.Message = "Holon saved to Radix blueprint successfully";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error saving holon to Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
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

            if (holons == null)
            {
                OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                return result;
            }

            var savedHolons = new List<IHolon>();
            var errors = new List<string>();

            foreach (var holon in holons)
            {
                var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                
                if (saveResult.IsError)
                {
                    errors.Add($"Failed to save holon {holon.Id}: {saveResult.Message}");
                    if (!continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, string.Join("; ", errors));
                        return result;
                    }
                }
                else if (saveResult.Result != null)
                {
                    savedHolons.Add(saveResult.Result);
                }
            }

            result.Result = savedHolons;
            result.IsError = errors.Any();
            result.Message = errors.Any() ? string.Join("; ", errors) : $"Successfully saved {savedHolons.Count} holons to Radix";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error saving holons to Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
    }

    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
    {
        var result = new OASISResult<IHolon>();
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
                return await HolonManager.Instance.DeleteHolonAsync(id, Guid.Empty, true, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
            }

            // First load the holon to return it
            var holonResult = await LoadHolonAsync(id, false, false, 0, true, false, 0);
            if (holonResult.IsError || holonResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load holon: {holonResult.Message}");
                return result;
            }

            // Get wallet for signing (use holon's owner if available, otherwise use default)
            var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(holonResult.Result.CreatedByAvatarId, ProviderType.Value);
            if (walletResult.IsError || walletResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet for holon");
                return result;
            }

            // Build transaction manifest calling OASIS blueprint's delete_holon function
            var network = _config.NetworkId == 1 ? "mainnet" : "stokenet";
            var manifest = new
            {
                instructions = new[]
                {
                    new
                    {
                        kind = "CallMethod",
                        componentAddress = _config.OasisBlueprintAddress,
                        methodName = "delete_holon",
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

            result.Result = holonResult.Result;
            result.IsError = false;
            result.Message = "Holon deleted from Radix blueprint successfully";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> DeleteHolon(Guid id)
    {
        return DeleteHolonAsync(id).Result;
    }

    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
    {
        // First load the holon to get its ID
        var holonResult = await LoadHolonAsync(providerKey, false, false, 0, true, false, 0);
        if (holonResult.IsError || holonResult.Result == null)
        {
            return new OASISResult<IHolon>
            {
                IsError = true,
                Message = $"Failed to load holon by provider key: {holonResult.Message}"
            };
        }

        // Then delete using the ID
        return await DeleteHolonAsync(holonResult.Result.Id);
    }

    public override OASISResult<IHolon> DeleteHolon(string providerKey)
    {
        return DeleteHolonAsync(providerKey).Result;
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            if (holons == null || !holons.Any())
            {
                OASISErrorHandling.HandleError(ref result, "Holons collection cannot be null or empty");
                return result;
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                var fallback = ProviderManager.Instance.GetStorageProvider(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
                if (fallback != null && fallback != this)
                    return await fallback.ImportAsync(holons);
                OASISErrorHandling.HandleError(ref result, "No OASIS blueprint configured and no other storage provider available for Import fallback.");
                return result;
            }

            // Serialize holons to JSON
            var holonsJson = System.Text.Json.JsonSerializer.Serialize(holons);

            // Get wallet for signing (use first holon's owner if available)
            var firstHolon = holons.First();
            var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(firstHolon.CreatedByAvatarId, ProviderType.Value);
            if (walletResult.IsError || walletResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet for import");
                return result;
            }

            // Build transaction manifest calling OASIS blueprint's import_holons function
            var network = _config.NetworkId == 1 ? "mainnet" : "stokenet";
            var manifest = new
            {
                instructions = new[]
                {
                    new
                    {
                        kind = "CallMethod",
                        componentAddress = _config.OasisBlueprintAddress,
                        methodName = "import_holons",
                        args = new[]
                        {
                            new { kind = "String", value = holonsJson }
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
            result.Message = $"Successfully imported {holons.Count()} holons to Radix blueprint";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error importing holons to Radix: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                var fallback = ProviderManager.Instance.GetStorageProvider(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
                if (fallback != null && fallback != this)
                    return await fallback.ExportAllDataForAvatarByIdAsync(avatarId, version);
                OASISErrorHandling.HandleError(ref result, "No OASIS blueprint configured and no other storage provider available for ExportAllDataForAvatarById fallback.");
                return result;
            }

            // Query export data for avatar from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "export_all_data_for_avatar",
                args = new[] { avatarId.ToString(), version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var holons = new List<IHolon>();
                foreach (var holonElement in response.Result.EnumerateArray())
                {
                    var holonJson = holonElement.GetRawText();
                    var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonJson);
                    if (holon != null) holons.Add(holon);
                }
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count} holons for avatar from Radix";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
    {
        return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        // First load the avatar to get its ID
        var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
        if (avatarResult.IsError || avatarResult.Result == null)
        {
            return new OASISResult<IEnumerable<IHolon>>
            {
                IsError = true,
                Message = $"Failed to load avatar by username: {avatarResult.Message}"
            };
        }

        // Then export using the ID
        return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
    }

}
