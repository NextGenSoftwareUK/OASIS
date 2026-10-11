using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;
using NextGenSoftware.OASIS.Common;
using RadixEngineToolkit;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;

/// <summary>
/// Client for the on-ledger OasisStorage component (contracts/src/oasis_storage.rs) through the Radix Gateway API.
///
/// Reads: the component's key-value stores are looked up with <c>/state/entity/details</c> (store addresses, cached)
/// and <c>/state/key-value-store/data</c> (entries). Writes: a V1 transaction that locks the fee from the technical
/// account, proves the owner badge and calls the component; it is submitted to <c>/transaction/submit</c> and polled
/// with <c>/transaction/status</c> until committed.
/// </summary>
public sealed class RadixOasisStorageClient
{
    private static readonly TimeSpan CommitTimeout = TimeSpan.FromSeconds(90);

    private readonly RadixOASISConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, string> _storeAddresses = new();

    public RadixOasisStorageClient(RadixOASISConfig config, HttpClient httpClient)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>True when a component is configured (reads). Writes also need the owner badge and technical account.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_config.OasisBlueprintAddress);

    public bool CanWrite => IsConfigured && !string.IsNullOrWhiteSpace(_config.OasisStorageOwnerBadgeAddress) &&
                            !string.IsNullOrWhiteSpace(_config.AccountAddress) && !string.IsNullOrWhiteSpace(_config.PrivateKey);

    // ------------------------------------------------------------------------------------------------ reads

    public Task<OASISResult<string>> GetAvatarJsonAsync(Guid id, CancellationToken token = default) =>
        GetEntryAsync("avatars", Key(id), token);

    public Task<OASISResult<string>> GetAvatarJsonByUsernameAsync(string username, CancellationToken token = default) =>
        GetIndexedAsync("username_to_avatar_id", username, "avatars", token);

    public Task<OASISResult<string>> GetAvatarJsonByEmailAsync(string email, CancellationToken token = default) =>
        GetIndexedAsync("email_to_avatar_id", email, "avatars", token);

    public Task<OASISResult<string>> GetAvatarDetailJsonAsync(Guid id, CancellationToken token = default) =>
        GetEntryAsync("avatar_details", Key(id), token);

    public Task<OASISResult<string>> GetHolonJsonAsync(Guid id, CancellationToken token = default) =>
        GetEntryAsync("holons", Key(id), token);

    public Task<OASISResult<string>> GetHolonJsonByProviderKeyAsync(string providerKey, CancellationToken token = default) =>
        GetIndexedAsync("provider_key_to_holon_id", providerKey, "holons", token);

    // ----------------------------------------------------------------------------------------------- writes

    public Task<OASISResult<string>> SaveAvatarAsync(Guid id, string json, string username, string email, CancellationToken token = default) =>
        CallAsync("save_avatar", token, Key(id), json, username ?? string.Empty, email ?? string.Empty);

    public Task<OASISResult<string>> DeleteAvatarAsync(Guid id, CancellationToken token = default) =>
        CallAsync("delete_avatar", token, Key(id));

    public Task<OASISResult<string>> SaveAvatarDetailAsync(Guid id, string json, CancellationToken token = default) =>
        CallAsync("save_avatar_detail", token, Key(id), json);

    public Task<OASISResult<string>> SaveHolonAsync(Guid id, string json, string providerKey, CancellationToken token = default) =>
        CallAsync("save_holon", token, Key(id), json, providerKey ?? string.Empty);

    public Task<OASISResult<string>> DeleteHolonAsync(Guid id, CancellationToken token = default) =>
        CallAsync("delete_holon", token, Key(id));

    /// <summary>Entity keys are the OASIS GUID in its default "D" format, lower case.</summary>
    public static string Key(Guid id) => id.ToString("D");

    // ----------------------------------------------------------------------------------------- read helpers

    private async Task<OASISResult<string>> GetIndexedAsync(string indexStore, string indexKey, string store, CancellationToken token)
    {
        var result = new OASISResult<string>();
        if (string.IsNullOrEmpty(indexKey))
        {
            OASISErrorHandling.HandleError(ref result, $"A {indexStore} key is required.");
            return result;
        }
        var id = await GetEntryAsync(indexStore, indexKey, token).ConfigureAwait(false);
        if (id.IsError || id.Result == null) return id;
        return await GetEntryAsync(store, id.Result, token).ConfigureAwait(false);
    }

    /// <summary>Returns the string stored under <paramref name="key"/>, or a null Result (not an error) if absent.</summary>
    private async Task<OASISResult<string>> GetEntryAsync(string store, string key, CancellationToken token)
    {
        var result = new OASISResult<string>();
        try
        {
            var storeAddress = await GetStoreAddressAsync(store, token).ConfigureAwait(false);
            if (storeAddress == null)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"OasisStorage component {_config.OasisBlueprintAddress} has no '{store}' store. Is OasisBlueprintAddress the OasisStorage component?");
                return result;
            }

            var request = new JsonObject
            {
                ["key_value_store_address"] = storeAddress,
                ["keys"] = new JsonArray(new JsonObject
                {
                    ["key_json"] = new JsonObject { ["kind"] = "String", ["value"] = key }
                })
            };
            var response = await PostAsync("/state/key-value-store/data", request, token).ConfigureAwait(false);
            result.Result = ParseKeyValueStoreString(response);
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error reading '{store}' from Radix: {ex.Message}", ex);
            return result;
        }
    }

    private async Task<string?> GetStoreAddressAsync(string store, CancellationToken token)
    {
        if (_storeAddresses.TryGetValue(store, out var cached)) return cached;

        var request = new JsonObject { ["addresses"] = new JsonArray(_config.OasisBlueprintAddress) };
        var response = await PostAsync("/state/entity/details", request, token).ConfigureAwait(false);
        foreach (var (name, address) in ParseOwnedStores(response))
            _storeAddresses[name] = address;

        return _storeAddresses.TryGetValue(store, out var found) ? found : null;
    }

    /// <summary>
    /// Field name -> owned key-value store address, from a <c>/state/entity/details</c> response
    /// (<c>items[0].details.state</c> is the component's programmatic SBOR tuple).
    /// </summary>
    public static IEnumerable<(string Name, string Address)> ParseOwnedStores(JsonNode? response)
    {
        var fields = First(response?["items"])?["details"]?["state"]?["fields"] as JsonArray;
        if (fields == null) yield break;
        foreach (var field in fields)
        {
            if ((string?)field?["kind"] != "Own") continue;
            var name = (string?)field?["field_name"];
            var address = (string?)field?["value"];
            if (!string.IsNullOrEmpty(name) && address != null && address.StartsWith("internal_keyvaluestore", StringComparison.Ordinal))
                yield return (name, address);
        }
    }

    /// <summary>The string value of the first entry in a <c>/state/key-value-store/data</c> response, or null.</summary>
    public static string? ParseKeyValueStoreString(JsonNode? response)
    {
        var value = First(response?["entries"])?["value"]?["programmatic_json"];
        return (string?)value?["kind"] == "String" ? (string?)value["value"] : null;
    }

    /// <summary>First element of a JSON array, or null when it is missing or empty (the Gateway's "not found").</summary>
    private static JsonNode? First(JsonNode? array) => array is JsonArray { Count: > 0 } items ? items[0] : null;

    // ---------------------------------------------------------------------------------------- write helpers

    private async Task<OASISResult<string>> CallAsync(string method, CancellationToken token, params string[] args)
    {
        var result = new OASISResult<string>();
        if (!CanWrite)
        {
            OASISErrorHandling.HandleError(ref result,
                "Radix on-ledger storage writes need OasisBlueprintAddress, OasisStorageOwnerBadgeAddress, AccountAddress and PrivateKey.");
            return result;
        }

        try
        {
            using PrivateKey signer = RadixBridgeHelper.GetPrivateKeyFromHex(_config.PrivateKey);
            var epoch = await GetCurrentEpochAsync(token).ConfigureAwait(false);

            using TransactionManifestV1 manifest = new ManifestV1Builder()
                .AccountLockFee(new Address(_config.AccountAddress), new RadixEngineToolkit.Decimal("10"))
                .AccountCreateProofOfAmount(new Address(_config.AccountAddress),
                    new Address(_config.OasisStorageOwnerBadgeAddress!), new RadixEngineToolkit.Decimal("1"))
                .CallMethod(new ManifestBuilderAddress.Static(new Address(_config.OasisBlueprintAddress!)), method,
                    args.Select(a => (ManifestBuilderValue)new ManifestBuilderValue.StringValue(a)).ToArray())
                .Build(_config.NetworkId);
            manifest.StaticallyValidate(_config.NetworkId);

            var header = new TransactionHeaderV1(
                networkId: _config.NetworkId,
                startEpochInclusive: epoch,
                endEpochExclusive: epoch + 10,
                nonce: RadixBridgeHelper.RandomNonce(),
                notaryPublicKey: signer.PublicKey(),
                notaryIsSignatory: true,
                tipPercentage: 0);

            using IntentV1 intent = new(header, manifest, new MessageV1.None());
            using SignedTransactionIntentV1 signedIntent = new(intent, Array.Empty<SignatureWithPublicKeyV1>());
            var notarySignature = signer.SignToSignature(signedIntent.SignedIntentHash().AsHash());
            using NotarizedTransactionV1 transaction = new(signedIntent, notarySignature);
            transaction.StaticallyValidate(_config.NetworkId);

            var intentHash = transaction.IntentHash().AsStr();
            await PostAsync("/transaction/submit", new JsonObject
            {
                ["notarized_transaction_hex"] = Convert.ToHexString(transaction.ToPayloadBytes()).ToLowerInvariant()
            }, token).ConfigureAwait(false);

            var status = await WaitForCommitAsync(intentHash, token).ConfigureAwait(false);
            if (status != "CommittedSuccess")
            {
                OASISErrorHandling.HandleError(ref result, $"Radix transaction {intentHash} ({method}) ended as {status}.");
                return result;
            }

            result.Result = intentHash;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error calling OasisStorage.{method} on Radix: {ex.Message}", ex);
            return result;
        }
    }

    private async Task<ulong> GetCurrentEpochAsync(CancellationToken token)
    {
        var status = await PostAsync("/status/gateway-status", new JsonObject(), token).ConfigureAwait(false);
        return (ulong?)status?["ledger_state"]?["epoch"] ?? throw new InvalidOperationException("The Radix Gateway returned no epoch.");
    }

    /// <summary>Polls the intent status until it is committed or rejected. Returns the final status name.</summary>
    private async Task<string> WaitForCommitAsync(string intentHash, CancellationToken token)
    {
        var deadline = DateTime.UtcNow + CommitTimeout;
        string status = "Unknown";
        while (DateTime.UtcNow < deadline)
        {
            var response = await PostAsync("/transaction/status", new JsonObject { ["intent_hash"] = intentHash }, token)
                .ConfigureAwait(false);
            status = (string?)response?["intent_status"] ?? status;
            if (status is "CommittedSuccess" or "CommittedFailure" or "PermanentlyRejected") return status;
            await Task.Delay(TimeSpan.FromSeconds(2), token).ConfigureAwait(false);
        }
        return $"{status} (timed out after {CommitTimeout.TotalSeconds:0}s)";
    }

    private async Task<JsonNode?> PostAsync(string path, JsonNode body, CancellationToken token)
    {
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync($"{_config.HostUri.TrimEnd('/')}{path}", content, token).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Radix Gateway {path} returned {(int)response.StatusCode}: {text}");
        return JsonNode.Parse(text);
    }
}
