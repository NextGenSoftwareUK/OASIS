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
    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
    {
        return DeleteAvatarAsync(id, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
    {
        // First load the avatar to get its ID
        var avatarResult = await LoadAvatarByEmailAsync(email);
        if (avatarResult.IsError || avatarResult.Result == null)
        {
            return new OASISResult<bool>
            {
                IsError = true,
                Message = $"Failed to load avatar by email: {avatarResult.Message}"
            };
        }

        // Then delete using the ID
        return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
    }

    public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
    {
        return DeleteAvatarByEmailAsync(email, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
    {
        // First load the avatar to get its ID
        var avatarResult = await LoadAvatarByUsernameAsync(username);
        if (avatarResult.IsError || avatarResult.Result == null)
        {
            return new OASISResult<bool>
            {
                IsError = true,
                Message = $"Failed to load avatar by username: {avatarResult.Message}"
            };
        }

        // Then delete using the ID
        return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
    }

    public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
    {
        return DeleteAvatarByUsernameAsync(username, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
    {
        // First load the avatar to get its ID
        var avatarResult = await LoadAvatarByProviderKeyAsync(providerKey);
        if (avatarResult.IsError || avatarResult.Result == null)
        {
            return new OASISResult<bool>
            {
                IsError = true,
                Message = $"Failed to load avatar by provider key: {avatarResult.Message}"
            };
        }

        // Then delete using the ID
        return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
    }

    public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
    {
        return DeleteAvatarAsync(providerKey, softDelete).Result;
    }



    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
    {
        var result = new OASISResult<decimal>();
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

            return await _radixService.GetAccountBalanceAsync(accountAddress, token);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting account balance: {ex.Message}", ex);
            return result;
        }
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
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

            return await _radixService.CreateAccountAsync(token);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error creating account: {ex.Message}", ex);
            return result;
        }
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey)>();
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

            return await _radixService.RestoreAccountAsync(seedPhrase, token);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error restoring account: {ex.Message}", ex);
            return result;
        }
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
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

            return await _radixService.WithdrawAsync(amount, senderAccountAddress, senderPrivateKey);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
            return result;
        }
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
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

            return await _radixService.DepositAsync(amount, receiverAccountAddress);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
            return result;
        }
    }

    public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
    {
        var result = new OASISResult<BridgeTransactionStatus>();
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

            return await _radixService.GetTransactionStatusAsync(transactionHash, token);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting transaction status: {ex.Message}", ex);
            return result;
        }
    }



    public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
    {
        var result = new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Radix provider is not activated");
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
            OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Radix provider is not activated");
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
            OASISErrorHandling.HandleError(ref result, $"Error getting holons near me: {ex.Message}", ex);
        }
        return result;
    }



    public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        var result = new OASISResult<ISearchResults>();
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
                return await SearchManager.Instance.SearchAsync(searchParams, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default, null, true, loadChildren, recursive, maxChildDepth, continueOnError, version);
            }

            // Extract search query from searchParams
            string searchQuery = "";
            if (searchParams is ISearchTextGroup searchTextGroup)
            {
                searchQuery = searchTextGroup.SearchQuery ?? "";
            }

            // Query search results from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "search_holons",
                args = new[] { searchQuery, version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var searchResults = new SearchResults();
                
                // Parse avatars
                if (response.Result.TryGetProperty("avatars", out var avatarsElement) && avatarsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var avatars = new List<IAvatar>();
                    foreach (var avatarElement in avatarsElement.EnumerateArray())
                    {
                        var avatarJson = avatarElement.GetRawText();
                        var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(avatarJson);
                        if (avatar != null) avatars.Add(avatar);
                    }
                    searchResults.SearchResultAvatars = avatars;
                }

                // Parse holons
                if (response.Result.TryGetProperty("holons", out var holonsElement) && holonsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var holons = new List<IHolon>();
                    foreach (var holonElement in holonsElement.EnumerateArray())
                    {
                        var holonJson = holonElement.GetRawText();
                        var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonJson);
                        if (holon != null)
                        {
                            // Load children if requested
                            if (loadChildren && recursive && maxChildDepth > 0)
                            {
                                var childrenResult = await LoadHolonsForParentAsync(holon.Id, HolonType.All, loadChildren, recursive, maxChildDepth - 1, 0, continueOnError, false, version);
                                if (!childrenResult.IsError && childrenResult.Result != null)
                                {
                                    holon.Children = childrenResult.Result.ToList();
                                }
                            }
                            holons.Add(holon);
                        }
                    }
                    searchResults.SearchResultHolons = holons;
                }

                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Successfully searched Radix: found {searchResults.SearchResultAvatars?.Count ?? 0} avatars and {searchResults.SearchResultHolons?.Count ?? 0} holons";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to search Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error searching Radix: {ex.Message}", ex);
        }
        return result;
    }

}
