using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

/// <summary>Conditional opaque cells backed exclusively by the pinned native contract SDK.
/// Wrap with the existing private-record backend before storing OASIS records.</summary>
public sealed class PolkadotRecordBackend : IConditionalKeyValueBackend
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private static readonly Regex Hash = new("^0x[0-9a-fA-F]{64}$", RegexOptions.CultureInvariant);
    private readonly string _endpoint, _genesis, _contract, _code, _sender, _privateKey;
    private readonly Func<object, CancellationToken, PreparedSubmissionServer, Task<JsonElement>> _invoke;
    private readonly OwnedPreparationScope _owner = new();

    public PolkadotRecordBackend(string endpoint, string genesisHash, string contractAccountId,
        string contractCodeHash, string senderAccountId, string privateKey,
        Func<object, CancellationToken, PreparedSubmissionServer, Task<JsonElement>> invoke)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("Explicit HTTP(S) RPC without embedded credentials required.", nameof(endpoint));
        foreach (var identity in new[] { genesisHash, contractAccountId, contractCodeHash, senderAccountId })
            if (identity == null || !Hash.IsMatch(identity)) throw new ArgumentException("Pinned genesis/code and canonical32byte account identities required.");
        _endpoint = endpoint; _genesis = genesisHash.ToLowerInvariant(); _contract = contractAccountId.ToLowerInvariant();
        _code = contractCodeHash.ToLowerInvariant(); _sender = senderAccountId.ToLowerInvariant(); _privateKey = privateKey;
        _invoke = invoke ?? throw new ArgumentNullException(nameof(invoke));
    }

    /// <summary>Reuse the application's admitted durable owner; detached flows cannot outlive it.
    /// The factory must validate and durably persist each exact signed original before ACK.</summary>
    public Task<T> WithPreparationAsync<T>(Func<Task<T>> action,
        Func<string, string, PreparedSubmissionServer> factory, CancellationToken token = default)
        => _owner.RunAsync(action, factory, token);

    private Dictionary<string, object> Request(string operation) => new()
    {
        ["operation"] = operation, ["endpoint"] = _endpoint, ["genesisHash"] = _genesis,
        ["contractAddress"] = _contract, ["contractCodeHash"] = _code, ["origin"] = _sender
    };
    private static string Encode(string value, int maximum, bool empty)
    {
        ArgumentNullException.ThrowIfNull(value);
        var bytes = Utf8.GetBytes(value);
        if (bytes.Length > maximum || (!empty && bytes.Length == 0)) throw new ArgumentException("Native record byte bound exceeded or key empty.");
        return Convert.ToBase64String(bytes);
    }
    private static string Decode(string value, int maximum)
    {
        if (value == null) throw new InvalidDataException("Native binary result is missing.");
        var bytes = Convert.FromBase64String(value);
        if (bytes.Length > maximum || Convert.ToBase64String(bytes) != value) throw new InvalidDataException("Native binary result is not canonical/bounded.");
        return Utf8.GetString(bytes);
    }
    private void Identity(JsonElement value)
    {
        if (!Hash.IsMatch(value.GetProperty("blockHash").GetString() ?? "")
            || value.GetProperty("contractAddress").GetString() != _contract || value.GetProperty("codeHash").GetString() != _code)
            throw new InvalidDataException("SDK observation does not bind the configured contract/finalized snapshot.");
    }
    private static string Revision(string revision)
    {
        if (!ulong.TryParse(revision, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            || parsed.ToString(CultureInfo.InvariantCulture) != revision)
            throw new ArgumentException("Canonical unsigned64bit native revision required.", nameof(revision));
        return revision;
    }

    public async Task<KeyValueObservation> ReadObservedAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = Request("recordRead"); request["action"] = "get"; request["keyBase64"] = Encode(key, 1024, false);
        var response = await _invoke(request, cancellationToken, null).ConfigureAwait(false);
        Identity(response);
        var observation = response.GetProperty("observation");
        if (observation.ValueKind == JsonValueKind.Null) return null;
        return new KeyValueObservation(Decode(observation.GetProperty("valueBase64").GetString(), 8192),
            Revision(observation.GetProperty("revision").GetString()));
    }
    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        => (await ReadObservedAsync(key, cancellationToken).ConfigureAwait(false))?.Value;

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = Request("recordRead"); request["action"] = "list"; request["keyBase64"] = Encode(prefix, 1024, true);
        var response = await _invoke(request, cancellationToken, null).ConfigureAwait(false); Identity(response);
        Revision(response.GetProperty("generation").GetString());
        var keys = response.GetProperty("keysBase64").EnumerateArray().Select(entry => Decode(entry.GetString(), 1024)).ToArray();
        if (keys.Any(key => key.Length == 0 || !key.StartsWith(prefix, StringComparison.Ordinal))
            || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
            throw new InvalidDataException("SDK returned duplicate/invalid/out-of-prefix keys.");
        Array.Sort(keys, StringComparer.Ordinal); // Native dense slots have no lexical-order promise.
        return keys;
    }
    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = Request("recordRead"); request["action"] = "verify";
        var response = await _invoke(request, cancellationToken, null).ConfigureAwait(false); Identity(response);
        if (!response.GetProperty("verified").GetBoolean()) throw new InvalidDataException("Native contract verification was not acknowledged.");
    }

    /// <summary>Read-only recovery of a retained signed original; absence in a bounded
    /// scan is not failure/expiry and never authorizes signing or resubmission.</summary>
    public async Task<JsonElement> ReadOriginalOutcomeAsync(JsonElement preparation, string fromBlockNumber = null,
        int maximumBlocks = 32, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var original = preparation.Clone();
        if (maximumBlocks is < 1 or > 128) throw new ArgumentOutOfRangeException(nameof(maximumBlocks));
        if (fromBlockNumber != null) Revision(fromBlockNumber);
        if (original.GetProperty("chainId").GetString() != _genesis || original.GetProperty("sender").GetString() != _sender
            || original.GetProperty("contractAddress").GetString() != _contract || original.GetProperty("contractCodeHash").GetString() != _code
            || !Hash.IsMatch(original.GetProperty("transactionHash").GetString() ?? ""))
            throw new ArgumentException("Retained original belongs to a different record backend.", nameof(preparation));
        var lookup = Request("recordOutcome"); lookup["preparation"] = original; lookup["maximumBlocks"] = maximumBlocks;
        if (fromBlockNumber != null) lookup["fromBlockNumber"] = fromBlockNumber;
        var response = await _invoke(lookup, cancellationToken, null).ConfigureAwait(false);
        if (response.GetProperty("found").GetBoolean())
        {
            var receipt = response.GetProperty("receipt");
            if (receipt.GetProperty("transactionHash").GetString() != original.GetProperty("transactionHash").GetString()
                || !receipt.GetProperty("finalized").GetBoolean() || receipt.GetProperty("contractAddress").GetString() != _contract
                || receipt.GetProperty("action").GetString() != original.GetProperty("action").GetString())
                throw new InvalidDataException("Recovered receipt differs from the retained original.");
        }
        return response.Clone();
    }
    public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        => Mutate("put", key, value, "Any", null, cancellationToken);
    public Task CreateOnlyAsync(string key, string value, CancellationToken cancellationToken = default)
        => Mutate("put", key, value, "Missing", null, cancellationToken);
    public Task ReplaceUnchangedAsync(string key, string value, string revision, CancellationToken cancellationToken = default)
        => Mutate("put", key, value, "Revision", Revision(revision), cancellationToken);
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => Mutate("delete", key, null, "Any", null, cancellationToken);
    public Task DeleteUnchangedAsync(string key, string revision, CancellationToken cancellationToken = default)
        => Mutate("delete", key, null, "Revision", Revision(revision), cancellationToken);

    private async Task Mutate(string action, string key, string value, string expectation, string revision, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var encodedKey = Encode(key, 1024, false);
        var encodedValue = action == "put" ? Encode(value, 8192, true) : null;
        if (string.IsNullOrWhiteSpace(_privateKey)) throw new InvalidOperationException("Explicit contract-owner signing material required.");
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _owner.CurrentToken);
        linked.CancelAfter(TimeSpan.FromMinutes(2));
        using var server = _owner.CreateCurrent(action, key)
            ?? throw new InvalidOperationException("Record writes require an admitted durable preparation owner.");
        string original = null;
        try
        {
            var request = Request("prepareRecordMutation");
            request["action"] = action; request["sender"] = _sender; request["recipient"] = _contract;
            request["privateKey"] = _privateKey; request["keyBase64"] = encodedKey; request["expectation"] = expectation;
            request["operationTagBase64"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            if (action == "put") request["valueBase64"] = encodedValue;
            if (revision != null) request["revision"] = revision;
            var prepared = await _invoke(request, linked.Token, null).ConfigureAwait(false);
            var hash = prepared.GetProperty("transactionHash").GetString();
            if (!Hash.IsMatch(hash ?? "")) throw new InvalidDataException("Prepared original identity is invalid.");
            var submit = Request("submitRecordPreparation"); submit["preparation"] = prepared.Clone();
            var accepted = await _invoke(submit, linked.Token, server).ConfigureAwait(false);
            original = server.PreparedTransactionId;
            if (original != hash || accepted.GetProperty("transactionHash").GetString() != hash
                || !accepted.GetProperty("persisted").GetBoolean() || !accepted.GetProperty("submitted").GetBoolean()
                || !accepted.GetProperty("poolAccepted").GetBoolean() || !accepted.GetProperty("reconciliationRequired").GetBoolean()
                || accepted.GetProperty("finalized").GetBoolean()) throw new InvalidDataException("Submission reply does not bind durable original/pool-only acknowledgement.");
            string cursor = null;
            while (true)
            {
                linked.Token.ThrowIfCancellationRequested();
                var outcome = await ReadOriginalOutcomeAsync(prepared, cursor, 32, linked.Token).ConfigureAwait(false);
                if (outcome.GetProperty("found").GetBoolean())
                {
                    var receipt = outcome.GetProperty("receipt");
                    if (receipt.GetProperty("transactionHash").GetString() != original
                        || !receipt.GetProperty("finalized").GetBoolean() || !receipt.GetProperty("dispatchSucceeded").GetBoolean()
                        || !receipt.GetProperty("recordEffectMatchesRequest").GetBoolean()
                        || receipt.GetProperty("contractAddress").GetString() != _contract || receipt.GetProperty("action").GetString() != action)
                        throw new InvalidDataException("Original native receipt/effect is not confirmed.");
                    return;
                }
                cursor = outcome.GetProperty("nextBlockNumber").GetString();
                if (outcome.GetProperty("searchedThroughSnapshot").GetBoolean())
                    await Task.Delay(TimeSpan.FromSeconds(1), linked.Token).ConfigureAwait(false);
            }
        }
        catch (Exception failure)
        {
            original ??= server.PreparedTransactionId;
            if (original != null && failure is not INativeMutationReconciliationRequired)
            {
                if (failure is OperationCanceledException cancelled)
                    throw new PolkadotNativeMutationCanceledException(original, cancelled, cancelled.CancellationToken);
                throw new PolkadotNativeMutationReconciliationException(original, failure);
            }
            throw;
        }
    }
}
