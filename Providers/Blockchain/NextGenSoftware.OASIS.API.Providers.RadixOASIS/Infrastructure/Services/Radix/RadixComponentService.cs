using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using RadixEngineToolkit;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.State;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Extensions;
using NextGenSoftware.OASIS.Common;
using TransactionBuilder = RadixEngineToolkit.TransactionBuilder;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;

/// <summary>
/// Service for interacting with Radix Scrypto components (OASIS Storage component)
/// </summary>
public sealed class RadixComponentService : IRadixComponentService
{
    private readonly RadixOASISConfig _config;
    private readonly HttpClient _httpClient;
    private readonly string _network;

    public RadixComponentService(RadixOASISConfig config, HttpClient httpClient)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        _network = _config.NetworkId == (byte)RadixNetworkType.MainNet
            ? RadixBridgeHelper.MainNet
            : RadixBridgeHelper.StokeNet;
    }

    /// <summary>
    /// Queries component state from Gateway API
    /// </summary>
    private async Task<OASISResult<StateEntityDetailsResponse>> GetComponentStateAsync(
        string componentAddress,
        CancellationToken token = default)
    {
        var result = new OASISResult<StateEntityDetailsResponse>();
        
        try
        {
            var request = new StateEntityDetailsRequest
            {
                Addresses = new List<string> { componentAddress }
            };

            var response = await HttpClientHelper.PostAsync<StateEntityDetailsRequest, StateEntityDetailsResponse>(
                _httpClient,
                $"{_config.HostUri}/state/entity/details",
                request,
                token);

            if (response.IsError || response.Result == null)
            {
                result.IsError = true;
                result.Message = response.Message ?? "Failed to query component state";
                return result;
            }

            result.Result = response.Result;
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            return OASISErrorHandling.HandleError<StateEntityDetailsResponse>(ref result,
                $"Error querying component state: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Maps component method name and arguments to store lookup information
    /// Returns: (IndexStoreName, IndexKey, ActualStoreName) - if index lookup needed, ActualStoreName is the target store
    /// </summary>
    private (string? IndexStoreName, object? IndexKey, string ActualStoreName, object? ActualKey) MapMethodToStoresAndKeys(string methodName, List<object> args)
    {
        return methodName.ToLower() switch
        {
            "get_avatar" => (null, null, "avatars", args.Count > 0 ? args[0] : throw new ArgumentException("get_avatar requires entity ID argument")),
            "get_avatar_by_username" => ("username_to_avatar_id", args.Count > 0 ? args[0] : throw new ArgumentException("get_avatar_by_username requires username argument"), "avatars", null),
            "get_avatar_by_email" => ("email_to_avatar_id", args.Count > 0 ? args[0] : throw new ArgumentException("get_avatar_by_email requires email argument"), "avatars", null),
            "get_holon" => (null, null, "holons", args.Count > 0 ? args[0] : throw new ArgumentException("get_holon requires entity ID argument")),
            "get_holon_by_provider_key" => ("provider_key_to_holon_id", args.Count > 0 ? args[0] : throw new ArgumentException("get_holon_by_provider_key requires provider key argument"), "holons", null),
            _ => throw new ArgumentException($"Unknown read-only method: {methodName}")
        };
    }

    /// <summary>
    /// Calls a component method (read-only query, no transaction required)
    /// Uses Gateway API /state/entity/details endpoint to query component state directly
    /// </summary>
    public async Task<OASISResult<string>> CallComponentMethodAsync(
        string componentAddress,
        string methodName,
        List<object> args,
        CancellationToken token = default)
    {
        var result = new OASISResult<string>();
        
        try
        {
            if (string.IsNullOrEmpty(componentAddress))
            {
                result.IsError = true;
                result.Message = "Component address is required";
                return result;
            }

            // Map method to stores and keys (handles both direct and index lookups)
            (string? indexStoreName, object? indexKey, string actualStoreName, object? actualKey) storeInfo;
            try
            {
                storeInfo = MapMethodToStoresAndKeys(methodName, args);
            }
            catch (ArgumentException ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                return result;
            }

            // Query component state
            var stateResult = await GetComponentStateAsync(componentAddress, token);
            if (stateResult.IsError || stateResult.Result == null)
            {
                result.IsError = true;
                result.Message = stateResult.Message ?? "Failed to query component state";
                return result;
            }

            string? value = null;

            // If this is an index lookup (e.g., get_avatar_by_username), do two-step lookup
            if (!string.IsNullOrEmpty(storeInfo.indexStoreName) && storeInfo.indexKey != null)
            {
                // Step 1: Look up entity ID from index store
                string? entityIdStr = RadixStateHelper.ExtractKeyValueStoreEntry(
                    stateResult.Result,
                    componentAddress,
                    storeInfo.indexStoreName,
                    storeInfo.indexKey);

                if (string.IsNullOrEmpty(entityIdStr))
                {
                    // Try alternative extraction for index
                    try
                    {
                        var rawResponse = JsonSerializer.Serialize(stateResult.Result);
                        entityIdStr = RadixStateHelper.ExtractValueFromComponentState(
                            rawResponse,
                            storeInfo.indexStoreName,
                            storeInfo.indexKey);
                    }
                    catch
                    {
                        // Extraction failed
                    }
                }

                if (string.IsNullOrEmpty(entityIdStr))
                {
                    result.IsError = true;
                    result.Message = $"Index lookup failed. Method: {methodName}, Index Key: {storeInfo.indexKey}, Index Store: {storeInfo.indexStoreName}";
                    return result;
                }

                // Step 2: Parse entity ID and look up actual entity
                if (ulong.TryParse(entityIdStr.Trim(), out ulong entityId))
                {
                    value = RadixStateHelper.ExtractKeyValueStoreEntry(
                        stateResult.Result,
                        componentAddress,
                        storeInfo.actualStoreName,
                        entityId);

                    if (string.IsNullOrEmpty(value))
                    {
                        // Try alternative extraction
                        try
                        {
                            var rawResponse = JsonSerializer.Serialize(stateResult.Result);
                            value = RadixStateHelper.ExtractValueFromComponentState(
                                rawResponse,
                                storeInfo.actualStoreName,
                                entityId);
                        }
                        catch
                        {
                            // Extraction failed
                        }
                    }
                }
            }
            else
            {
                // Direct lookup (e.g., get_avatar with entity ID)
                if (storeInfo.actualKey != null)
                {
                    value = RadixStateHelper.ExtractKeyValueStoreEntry(
                        stateResult.Result,
                        componentAddress,
                        storeInfo.actualStoreName,
                        storeInfo.actualKey);

                    if (string.IsNullOrEmpty(value))
                    {
                        // Try alternative extraction
                        try
                        {
                            var rawResponse = JsonSerializer.Serialize(stateResult.Result);
                            value = RadixStateHelper.ExtractValueFromComponentState(
                                rawResponse,
                                storeInfo.actualStoreName,
                                storeInfo.actualKey);
                        }
                        catch
                        {
                            // Extraction failed
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(value))
            {
                result.IsError = true;
                result.Message = $"Value not found in component state. Method: {methodName}";
                if (storeInfo.indexKey != null)
                {
                    result.Message += $", Index Key: {storeInfo.indexKey}, Index Store: {storeInfo.indexStoreName}";
                }
                if (storeInfo.actualKey != null)
                {
                    result.Message += $", Actual Key: {storeInfo.actualKey}, Actual Store: {storeInfo.actualStoreName}";
                }
                return result;
            }

            result.Result = value;
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            return OASISErrorHandling.HandleError<string>(ref result,
                $"Error calling component method {methodName}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Calls a component method that modifies state (requires transaction)
    /// </summary>
    public async Task<OASISResult<string>> CallComponentMethodTransactionAsync(
        string componentAddress,
        string methodName,
        List<object> args,
        string senderPrivateKey,
        CancellationToken token = default)
    {
        var result = new OASISResult<string>();
        
        try
        {
            if (string.IsNullOrEmpty(componentAddress))
            {
                result.IsError = true;
                result.Message = "Component address is required";
                return result;
            }

            if (string.IsNullOrEmpty(senderPrivateKey))
            {
                result.IsError = true;
                result.Message = "Sender private key is required";
                return result;
            }

            // Parse private key
            using PrivateKey privateKey = RadixBridgeHelper.GetPrivateKeyFromHex(senderPrivateKey);
            Address senderAddress = Address.VirtualAccountAddressFromPublicKey(
                privateKey.PublicKey(), _config.NetworkId);
            Address componentAddr = new(componentAddress);

            // Get current epoch
            ulong currentEpoch = (await _httpClient.GetConstructionMetadataAsync(_config))?.CurrentEpoch ?? 0;

            // Convert arguments to Scrypto-compatible format
            object[] scryptoArgs = RadixComponentHelper.ConvertToScryptoArgs(args);

            // Build transaction manifest to call component method
            using TransactionManifest manifest = new ManifestBuilder()
                .AccountLockFee(senderAddress, new("10"), _config.NetworkId)
                .CallMethod(componentAddr, methodName, scryptoArgs)
                .AccountTryDepositOrAbort(senderAddress, null, null)
                .Build(_config.NetworkId);

            manifest.StaticallyValidate();

            // Build and sign transaction
            using NotarizedTransaction transaction = new TransactionBuilder()
                .Header(new TransactionHeader(
                    networkId: _config.NetworkId,
                    startEpochInclusive: currentEpoch,
                    endEpochExclusive: currentEpoch + 50,
                    nonce: RadixBridgeHelper.RandomNonce(),
                    notaryPublicKey: privateKey.PublicKey(),
                    notaryIsSignatory: true,
                    tipPercentage: 0
                ))
                .Manifest(manifest)
                .Message(new Message.None())
                .NotarizeWithPrivateKey(privateKey);

            // Submit transaction
            var submitData = new
            {
                network = _network,
                notarized_transaction_hex = Encoders.Hex.EncodeData(transaction.Compile()),
                force_recalculate = true
            };

            var response = await HttpClientHelper.PostAsync<object, TransactionSubmitResponse>(
                _httpClient,
                $"{_config.HostUri}/core/lts/transaction/submit",
                submitData,
                token);

            if (response.IsError)
            {
                result.IsError = true;
                result.Message = response.Message ?? "Failed to submit transaction";
                return result;
            }

            var transactionHash = transaction.IntentHash().AsStr();
            result.Result = transactionHash;
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            return OASISErrorHandling.HandleError<string>(ref result,
                $"Error calling component method {methodName}: {ex.Message}", ex);
        }
    }
}

