using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

/// <summary>
/// Stores records in one <c>OasisKeyValueStore</c> Scrypto component. Writes are notarised manifests calling
/// <c>put</c>/<c>delete</c>, acknowledged only on CommittedSuccess; reads come from the committed substate through the
/// node's Engine State API (<c>kv-store/entry</c>, <c>kv-store/iterator</c>).
/// </summary>
public sealed class RadixKeyValueBackend : IKeyValueBackend
{
    private const string StoreBlueprint = "OasisKeyValueStore";
    private const string EntriesField = "entries";
    private readonly RadixLedgerClient client;
    private readonly string componentAddress;
    private string entriesStoreAddress;

    public RadixKeyValueBackend(RadixLedgerClient client, string componentAddress)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        if (string.IsNullOrWhiteSpace(componentAddress) || !componentAddress.StartsWith("component_", StringComparison.Ordinal))
            throw new ArgumentException("An OasisKeyValueStore component address is required.", nameof(componentAddress));
        this.componentAddress = componentAddress;
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var store = await EntriesStoreAsync(cancellationToken).ConfigureAwait(false);
        var response = await client.EngineStateAsync("kv-store/entry", new JsonObject
        {
            ["entity_address"] = store,
            ["key"] = new JsonObject { ["programmatic_json"] = StringValue(key) }
        }, cancellationToken, allowError: true).ConfigureAwait(false);
        if (response["details"]?["error_type"]?.GetValue<string>() == "RequestedItemNotFound"
            && response["details"]?["item_type"]?.GetValue<string>() == "EntryKey")
            return null;
        return ReadString(response["content"]?["programmatic_json"], response);
    }

    public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        return CallAsync("put", cancellationToken, key, value);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => CallAsync("delete", cancellationToken, key);

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        prefix ??= string.Empty;
        var store = await EntriesStoreAsync(cancellationToken).ConfigureAwait(false);
        var keys = new List<string>();
        string continuation = null;
        do
        {
            var request = new JsonObject { ["entity_address"] = store, ["max_page_size"] = 100 };
            if (continuation != null) request["continuation_token"] = continuation;
            var page = await client.EngineStateAsync("kv-store/iterator", request, cancellationToken).ConfigureAwait(false);
            foreach (var item in page["page"]?.AsArray() ?? throw new HttpRequestException("Radix kv-store iterator returned no page."))
            {
                var key = ReadString(item?["key"]?["programmatic_json"], page);
                if (key.StartsWith(prefix, StringComparison.Ordinal)) keys.Add(key);
            }
            continuation = page["continuation_token"]?.GetValue<string>();
        } while (continuation != null);
        return keys;
    }

    /// <summary>Proves the node answers, the component is an OasisKeyValueStore and the signing account exists, without a paid write.</summary>
    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        entriesStoreAddress = null;
        await EntriesStoreAsync(cancellationToken).ConfigureAwait(false);
        await client.CoreAsync("lts/state/account-all-fungible-resource-balances",
            new JsonObject { ["account_address"] = client.AccountAddress }, cancellationToken).ConfigureAwait(false);
    }

    private async Task CallAsync(string method, CancellationToken cancellationToken, params string[] arguments)
    {
        foreach (var argument in arguments)
            if (string.IsNullOrEmpty(argument)) throw new ArgumentException("Radix record keys must not be empty.", nameof(arguments));
        var manifest =
            $"CALL_METHOD Address(\"{client.AccountAddress}\") \"lock_fee\" Decimal(\"25\");\n" +
            $"CALL_METHOD Address(\"{componentAddress}\") \"{method}\" {string.Join(" ", Array.ConvertAll(arguments, RadixLedgerClient.ManifestString))};\n";
        await client.SubmitManifestAsync(manifest, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> EntriesStoreAsync(CancellationToken cancellationToken)
    {
        if (entriesStoreAddress != null) return entriesStoreAddress;
        var state = await client.EngineStateAsync("object/field", new JsonObject
        {
            ["entity_address"] = componentAddress,
            ["field_index"] = 0
        }, cancellationToken).ConfigureAwait(false);
        var tuple = state["content"]?["programmatic_json"];
        if (tuple?["type_name"]?.GetValue<string>() != StoreBlueprint)
            throw new InvalidOperationException($"{componentAddress} is not an {StoreBlueprint} component: {state.ToJsonString()}");
        foreach (var field in tuple["fields"]?.AsArray() ?? new JsonArray())
            if (field?["field_name"]?.GetValue<string>() == EntriesField && field["kind"]?.GetValue<string>() == "Own")
                return entriesStoreAddress = field["value"]!.GetValue<string>();
        throw new InvalidOperationException($"{componentAddress} has no owned '{EntriesField}' key-value store.");
    }

    private static JsonObject StringValue(string value)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentException("Radix record keys must not be empty.", nameof(value));
        return new JsonObject { ["kind"] = "String", ["value"] = value };
    }

    private static string ReadString(JsonNode value, JsonNode response)
    {
        if (value?["kind"]?.GetValue<string>() != "String")
            throw new HttpRequestException($"Radix node returned an unexpected key-value entry: {response.ToJsonString()}");
        return value["value"]!.GetValue<string>();
    }
}
