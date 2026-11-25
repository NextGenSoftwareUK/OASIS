using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Starknet;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.StarknetOASIS;

public sealed partial class StarknetOASIS
{


//    public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => NotImplementedAsync<IHolon>(nameof(DeleteHolonAsync));
//    public override OASISResult<IHolon> DeleteHolon(Guid id) => NotImplemented<IHolon>(nameof(DeleteHolon));
//    public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey) => NotImplementedAsync<IHolon>(nameof(DeleteHolonAsync));
//    public override OASISResult<IHolon> DeleteHolon(string providerKey) => NotImplemented<IHolon>(nameof(DeleteHolon));
//    public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) => NotImplementedAsync<bool>(nameof(ImportAsync));
//    public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => NotImplemented<bool>(nameof(Import));
//    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) => NotImplementedAsync<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarByIdAsync));
//    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => NotImplemented<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarById));
//    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0) => NotImplementedAsync<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarByUsernameAsync));
//    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => NotImplemented<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarByUsername));
//    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0) => NotImplementedAsync<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarByEmailAsync));
//    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => NotImplemented<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarByEmail));
//    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => NotImplementedAsync<IEnumerable<IHolon>>(nameof(ExportAllAsync));
//    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => NotImplemented<IEnumerable<IHolon>>(nameof(ExportAll));

//    public Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromAddress, string toAddress, decimal amount, string memo) => NotImplementedAsync<ITransactionRespone>(nameof(SendTransactionAsync));
//    public OASISResult<ITransactionRespone> SendTransaction(string fromAddress, string toAddress, decimal amount, string memo) => NotImplemented<ITransactionRespone>(nameof(SendTransaction));

    public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
    {
        var result = new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Starknet provider is not activated");
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
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Starknet provider is not activated");
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

    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
    {
        return SendTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }
            if (_rpcClient == null)
            {
                OASISErrorHandling.HandleError(ref result, "Starknet RPC client is not initialized");
                return result;
            }

            // Starknet token transfer using RPC client
            // Build transaction payload for token transfer
            var payload = new StarknetTransactionPayload
            {
                From = request.FromWalletAddress,
                To = request.FromTokenAddress, // Token contract address
                Amount = request.Amount,
                Memo = request.ToWalletAddress // Recipient address in memo
            };
            var txResult = await _rpcClient.SubmitTransactionAsync(payload);
            if (txResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Token transfer failed: {txResult.Message}");
                return result;
            }

            result.Result.TransactionResult = txResult.Result;
            result.IsError = false;
            result.Message = "Token sent successfully on Starknet.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error sending token: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
    {
        return MintTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }
            if (_rpcClient == null)
            {
                OASISErrorHandling.HandleError(ref result, "Starknet RPC client is not initialized");
                return result;
            }

            // Get values from MetaData (IMintWeb3TokenRequest doesn't have these properties directly)
            var tokenAddress = request.MetaData?.ContainsKey("TokenAddress") == true 
                ? request.MetaData["TokenAddress"]?.ToString() 
                : "";
            var mintToAddress = request.MetaData?.ContainsKey("MintToWalletAddress") == true 
                ? request.MetaData["MintToWalletAddress"]?.ToString() 
                : "";
            var amount = request.MetaData?.ContainsKey("Amount") == true && 
                decimal.TryParse(request.MetaData["Amount"]?.ToString(), out var amt) ? amt : 0m;

            if (string.IsNullOrWhiteSpace(tokenAddress) || string.IsNullOrWhiteSpace(mintToAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and mint to wallet address are required in MetaData");
                return result;
            }

            // Starknet token minting using RPC client
            var payload = new StarknetTransactionPayload
            {
                From = mintToAddress,
                To = tokenAddress,
                Amount = amount,
                Memo = "mint"
            };
            var txResult = await _rpcClient.SubmitTransactionAsync(payload);
            if (txResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Token minting failed: {txResult.Message}");
                return result;
            }

            result.Result.TransactionResult = txResult.Result;
            result.IsError = false;
            result.Message = "Token minted successfully on Starknet.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error minting token: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
    {
        return BurnTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }
            if (_rpcClient == null)
            {
                OASISErrorHandling.HandleError(ref result, "Starknet RPC client is not initialized");
                return result;
            }

            // Get values from request properties (IBurnWeb3TokenRequest doesn't have MetaData)
            var tokenAddress = request.TokenAddress;
            var fromAddress = "";
            if (!string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
            {
                var derived = await GetAccountAddressAsync(request.OwnerPrivateKey);
                if (derived.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, derived.Message, derived.Exception);
                    return result;
                }
                fromAddress = derived.Result;
            }
            // IBurnWeb3TokenRequest doesn't have Amount - use default or get from balance
            var amount = 0m; // Amount would need to be specified separately or retrieved from balance

            if (string.IsNullOrWhiteSpace(tokenAddress) || string.IsNullOrWhiteSpace(fromAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and from wallet address are required");
                return result;
            }

            // Starknet token burning using RPC client
            var payload = new StarknetTransactionPayload
            {
                From = fromAddress,
                To = tokenAddress,
                Amount = amount,
                Memo = "burn"
            };
            var txResult = await _rpcClient.SubmitTransactionAsync(payload);
            if (txResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Token burning failed: {txResult.Message}");
                return result;
            }

            result.Result.TransactionResult = txResult.Result;
            result.IsError = false;
            result.Message = "Token burned successfully on Starknet.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error burning token: {ex.Message}", ex);
        }
        return result;
    }

}
