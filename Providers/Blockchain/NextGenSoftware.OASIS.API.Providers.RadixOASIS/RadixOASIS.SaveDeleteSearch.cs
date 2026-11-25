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

}
