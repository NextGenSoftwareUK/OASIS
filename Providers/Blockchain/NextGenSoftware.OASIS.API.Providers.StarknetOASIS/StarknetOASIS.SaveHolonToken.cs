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
    public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        var result = new OASISResult<IHolon>();
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

            if (holon == null)
            {
                OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                return result;
            }

            // Get wallet for the holon (use avatar's wallet if holon has CreatedByAvatarId)
            Guid avatarId = holon.CreatedByAvatarId != Guid.Empty ? holon.CreatedByAvatarId : holon.Id;
            var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatarId, Core.Enums.ProviderType.StarknetOASIS);
            if (walletResult.IsError || walletResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for holon");
                return result;
            }

            // Serialize holon to JSON
            string holonInfo = JsonSerializer.Serialize(holon);
            string holonId = holon.Id.ToString();

            // Use Starknet contract to store holon data
            if (string.IsNullOrEmpty(_contractAddress))
            {
                // No contract configured - delegate to ProviderManager as fallback
                return await HolonManager.Instance.SaveHolonAsync(holon, Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            }

            // Call Starknet contract using RPC client with proper invoke transaction
            // Note: This requires a deployed OASIS contract on Starknet with create_holon function
            // Build proper Starknet invoke transaction with entry point selector and calldata
            var holonIdBytes = System.Text.Encoding.UTF8.GetBytes(holonId);
            var holonInfoBytes = System.Text.Encoding.UTF8.GetBytes(holonInfo);
            
            // Convert to hex strings for Starknet calldata
            var holonIdHex = "0x" + Convert.ToHexString(holonIdBytes).ToLowerInvariant();
            var holonInfoHex = "0x" + Convert.ToHexString(holonInfoBytes).ToLowerInvariant();
            
            // Build invoke transaction payload for Starknet contract call
            var invokePayload = new
            {
                contract_address = _contractAddress,
                entry_point_selector = GetEntryPointSelector("create_holon"), // Keccak256 hash of function name
                calldata = new[]
                {
                    holonIdHex,
                    holonInfoHex
                }
            };

            // Submit invoke transaction via Starknet RPC
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "starknet_addInvokeTransaction",
                @params = new
                {
                    invoke_transaction = invokePayload
                },
                id = 1
            };

            var jsonContent = JsonSerializer.Serialize(rpcRequest);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync("", content);

            string transactionHash = null;
            if (httpResponse.IsSuccessStatusCode)
            {
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (responseJson.TryGetProperty("result", out var result2) && 
                    result2.TryGetProperty("transaction_hash", out var txHash))
                {
                    transactionHash = txHash.GetString();
                }
            }

            if (string.IsNullOrEmpty(transactionHash))
            {
                // Fallback to RPC client if direct HTTP call fails
                var payload = new StarknetTransactionPayload
                {
                    From = walletResult.Result.WalletAddress,
                    To = _contractAddress,
                    Amount = 0m,
                    Memo = holonInfo
                };
                var txResult = await _rpcClient.SubmitTransactionAsync(payload);
                if (txResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holon to Starknet contract: {txResult.Message}");
                    return result;
                }
                transactionHash = txResult.Result;
            }

            // Store transaction hash in provider unique storage key
            if (holon.ProviderUniqueStorageKey == null)
                holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.StarknetOASIS] = transactionHash;

            result.Result = holon;
            result.IsError = false;
            result.IsSaved = true;
            //result.Message = $"Holon saved successfully to Starknet contract: {txResult.Result}";
            result.Message = $"Holon saved successfully to Starknet.";

            // Handle children if requested
            if (saveChildren && holon.Children != null && holon.Children.Any())
            {
                var childResults = new List<OASISResult<IHolon>>();
                foreach (var child in holon.Children)
                {
                    var childResult = await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth - 1, continueOnError, saveChildrenOnProvider);
                    childResults.Add(childResult);
                    
                    if (!continueOnError && childResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to save child holon {child.Id}: {childResult.Message}");
                        return result;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error saving holon to Starknet: {ex.Message}", ex);
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
            result.Message = errors.Any() ? string.Join("; ", errors) : $"Successfully saved {savedHolons.Count} holons to Starknet";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error saving holons to Starknet: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
    }
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
            // Derive wallet address from private key (simplified - in production use proper key derivation)
            // Derive wallet address from private key (simplified - in production use proper Starknet key derivation)
            var fromAddress = !string.IsNullOrWhiteSpace(request.OwnerPrivateKey) 
                ? DeriveStarknetAddressFromPrivateKey(request.OwnerPrivateKey) 
                : "";
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
