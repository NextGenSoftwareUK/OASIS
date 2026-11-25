using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS;

/// <summary>
/// Key-value storage committed to the Zcash ledger. Each value (already encrypted by the caller) is wrapped with its key
/// and added to Kubo as a single raw block, so its CIDv1 is exactly the SHA-256 of the wrapper bytes. Every put is a
/// transparent transaction authored by the storage address whose OP_RETURN commits
/// <c>"OASZK1P" || SHA-256(key) || SHA-256(wrapper)</c>; a delete commits <c>"OASZK1D" || SHA-256(key)</c> and unpins the
/// envelope. Only transactions that spend the storage address's own outputs are authoritative, replayed in block order;
/// the newest entry for each key wins. A write is acknowledged once its transaction is mined.
/// </summary>
/// <remarks>
/// Requires zcashd with <c>-txindex</c> and <c>-insightexplorer</c> (address index), and a wallet that holds the storage
/// address's key: inputs are chosen from that address only and the wallet signs (zcashd's v5 sighash).
/// </remarks>
internal sealed class ZcashLedgerKeyValueBackend : IKeyValueBackend, IDisposable
{
    private static readonly byte[] PutMagic = Encoding.ASCII.GetBytes("OASZK1P");
    private static readonly byte[] DeleteMagic = Encoding.ASCII.GetBytes("OASZK1D");
    private const int HashLength = 32;
    private const int MaximumEnvelopeBytes = 1024 * 1024; // one raw Kubo block (chunker size-1048576)
    private const long ZatoshisPerZec = 100_000_000;
    private const long MarginalFee = 5_000; // ZIP-317
    private const long DustThreshold = 5_460;
    private const long CoinbaseMaturity = 100;

    private readonly HttpClient _rpc;
    private readonly HttpClient _kubo;
    private readonly string _address;
    private readonly SemaphoreSlim _submission = new(1, 1);
    private readonly ConcurrentDictionary<string, JsonElement> _transactions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string[]> _blockOrder = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, (string Key, string Value)> _envelopes = new(StringComparer.Ordinal);

    public ZcashLedgerKeyValueBackend(string rpcUrl, string rpcUser, string rpcPassword, string storageAddress, string kuboApi)
    {
        _address = storageAddress ?? throw new ArgumentNullException(nameof(storageAddress));
        _rpc = new HttpClient { BaseAddress = new Uri(rpcUrl ?? throw new ArgumentNullException(nameof(rpcUrl))), Timeout = TimeSpan.FromSeconds(60) };
        _rpc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{rpcUser}:{rpcPassword}")));
        _kubo = new HttpClient { BaseAddress = new Uri((kuboApi ?? throw new ArgumentNullException(nameof(kuboApi))).TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(60) };
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        await RpcAsync("getblockchaininfo", new JsonArray(), cancellationToken);
        var validated = await RpcAsync("validateaddress", new JsonArray(_address), cancellationToken);
        if (!validated.GetProperty("isvalid").GetBoolean())
            throw new InvalidDataException("The Zcash storage address is not a valid transparent address.");
        if (!validated.TryGetProperty("ismine", out var mine) || !mine.GetBoolean())
            throw new InvalidOperationException("The zcashd wallet does not hold the Zcash storage address key.");
        // Proves the address index is enabled; storage cannot be listed without it.
        await RpcAsync("getaddresstxids", new JsonArray(new JsonObject { ["addresses"] = new JsonArray(_address) }), cancellationToken);
        using var kubo = await _kubo.PostAsync("api/v0/version", null, cancellationToken);
        kubo.EnsureSuccessStatusCode();
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var index = await ReadIndexAsync(cancellationToken);
        if (!index.TryGetValue(KeyHash(key), out var entry) || entry.Deleted) return null;
        var envelope = await ReadEnvelopeAsync(entry.EnvelopeHash, cancellationToken);
        if (!string.Equals(envelope.Key, key, StringComparison.Ordinal))
            throw new CryptographicException("The Zcash record envelope belongs to a different key.");
        return envelope.Value;
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var index = await ReadIndexAsync(cancellationToken);
        var live = index.Values.Where(entry => !entry.Deleted).ToArray();
        var envelopes = await Task.WhenAll(live.Select(entry => ReadEnvelopeAsync(entry.EnvelopeHash, cancellationToken)));
        return envelopes.Select(envelope => envelope.Key)
            .Where(key => key.StartsWith(prefix ?? string.Empty, StringComparison.Ordinal))
            .OrderBy(key => key, StringComparer.Ordinal).ToArray();
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        var wrapper = Encoding.UTF8.GetBytes(new JsonObject { ["k"] = key, ["v"] = value }.ToJsonString());
        if (wrapper.Length > MaximumEnvelopeBytes)
            throw new InvalidDataException("The Zcash record envelope exceeds the single-block size limit.");
        var envelopeHash = SHA256.HashData(wrapper);
        var expectedCid = RawCid(envelopeHash);
        using var form = new MultipartFormDataContent { { new ByteArrayContent(wrapper), "file", "record" } };
        using var added = await _kubo.PostAsync("api/v0/add?cid-version=1&raw-leaves=true&chunker=size-1048576&pin=true", form, cancellationToken);
        added.EnsureSuccessStatusCode();
        using (var receipt = JsonDocument.Parse(await added.Content.ReadAsStringAsync(cancellationToken)))
        {
            if (!string.Equals(receipt.RootElement.GetProperty("Hash").GetString(), expectedCid, StringComparison.Ordinal))
                throw new InvalidDataException("Kubo did not store the record envelope as the expected raw block.");
        }
        _envelopes[Convert.ToHexString(envelopeHash)] = (key, value);
        await SubmitCommitmentAsync([.. PutMagic, .. KeyHashBytes(key), .. envelopeHash], cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var index = await ReadIndexAsync(cancellationToken);
        if (!index.TryGetValue(KeyHash(key), out var entry) || entry.Deleted) return; // deleting a missing key succeeds
        await SubmitCommitmentAsync([.. DeleteMagic, .. KeyHashBytes(key)], cancellationToken);
        // The ledger now records the deletion; release the envelope so its content leaves the record store.
        var cid = RawCid(Convert.FromHexString(entry.EnvelopeHash));
        using var unpinned = await _kubo.PostAsync($"api/v0/pin/rm?arg={cid}&recursive=true", null, cancellationToken);
        if (!unpinned.IsSuccessStatusCode)
        {
            var body = await unpinned.Content.ReadAsStringAsync(cancellationToken);
            if (!body.Contains("not pinned", StringComparison.OrdinalIgnoreCase)) unpinned.EnsureSuccessStatusCode();
        }
        _envelopes.TryRemove(entry.EnvelopeHash, out _);
    }

    private sealed record IndexEntry(string EnvelopeHash, bool Deleted);

    private static byte[] KeyHashBytes(string key) => SHA256.HashData(Encoding.UTF8.GetBytes(key));
    private static string KeyHash(string key) => Convert.ToHexString(KeyHashBytes(key));

    /// <summary>CIDv1, raw codec, sha2-256 multihash, base32 multibase: the CID Kubo assigns a single raw block.</summary>
    internal static string RawCid(byte[] sha256)
    {
        byte[] bytes = [0x01, 0x55, 0x12, 0x20, .. sha256];
        const string alphabet = "abcdefghijklmnopqrstuvwxyz234567";
        var output = new StringBuilder("b");
        int buffer = 0, bits = 0;
        foreach (var b in bytes)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                output.Append(alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }
        if (bits > 0) output.Append(alphabet[(buffer << (5 - bits)) & 31]);
        return output.ToString();
    }

    private async Task<(string Key, string Value)> ReadEnvelopeAsync(string envelopeHash, CancellationToken cancellationToken)
    {
        if (_envelopes.TryGetValue(envelopeHash, out var cached)) return cached;
        var hash = Convert.FromHexString(envelopeHash);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v0/cat?arg={RawCid(hash)}&length={MaximumEnvelopeBytes + 1}");
        using var response = await _kubo.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length > MaximumEnvelopeBytes || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(bytes), hash))
            throw new CryptographicException("The Zcash record envelope does not match its ledger commitment.");
        using var document = JsonDocument.Parse(bytes);
        var envelope = (document.RootElement.GetProperty("k").GetString()!, document.RootElement.GetProperty("v").GetString()!);
        _envelopes[envelopeHash] = envelope;
        return envelope;
    }

    /// <summary>Replays the storage address's mined commitments in block order; the newest entry for each key wins.</summary>
    private async Task<Dictionary<string, IndexEntry>> ReadIndexAsync(CancellationToken cancellationToken)
    {
        var index = new Dictionary<string, IndexEntry>(StringComparer.Ordinal);
        var txids = await RpcAsync("getaddresstxids", new JsonArray(new JsonObject { ["addresses"] = new JsonArray(_address) }), cancellationToken);
        var commitments = new List<(long Height, int Position, byte[] Payload)>();
        foreach (var txid in txids.EnumerateArray().Select(item => item.GetString()!).Distinct(StringComparer.Ordinal))
        {
            var transaction = await TransactionAsync(txid, cancellationToken);
            if (!transaction.TryGetProperty("blockhash", out var blockHash) || blockHash.ValueKind != JsonValueKind.String) continue;
            var payload = CommitmentPayload(transaction);
            if (payload == null || !await SpendsOnlyStorageOutputsAsync(transaction, cancellationToken)) continue;
            var order = await BlockOrderAsync(blockHash.GetString()!, cancellationToken);
            var position = Array.IndexOf(order.Transactions, txid);
            if (position < 0) throw new InvalidDataException("A Zcash storage transaction is missing from its block.");
            commitments.Add((order.Height, position, payload));
        }
        foreach (var (_, _, payload) in commitments.OrderBy(item => item.Height).ThenBy(item => item.Position))
        {
            var keyHash = Convert.ToHexString(payload, PutMagic.Length, HashLength);
            index[keyHash] = payload.AsSpan(0, PutMagic.Length).SequenceEqual(PutMagic)
                ? new IndexEntry(Convert.ToHexString(payload, PutMagic.Length + HashLength, HashLength), false)
                : new IndexEntry(null, true);
        }
        return index;
    }

    /// <summary>The single OASIS OP_RETURN of a transaction, or null when it carries none (or a malformed one).</summary>
    private static byte[] CommitmentPayload(JsonElement transaction)
    {
        byte[] found = null;
        foreach (var output in transaction.GetProperty("vout").EnumerateArray())
        {
            var script = output.GetProperty("scriptPubKey");
            if (!script.TryGetProperty("type", out var type) || type.GetString() != "nulldata") continue;
            var data = NullDataPayload(Convert.FromHexString(script.GetProperty("hex").GetString()!));
            if (data == null) continue;
            var isPut = data.Length == PutMagic.Length + 2 * HashLength && data.AsSpan(0, PutMagic.Length).SequenceEqual(PutMagic);
            var isDelete = data.Length == DeleteMagic.Length + HashLength && data.AsSpan(0, DeleteMagic.Length).SequenceEqual(DeleteMagic);
            if (!isPut && !isDelete) continue;
            if (found != null) return null; // ambiguous: more than one commitment
            found = data;
        }
        return found;
    }

    /// <summary>Extracts the pushed bytes of <c>OP_RETURN &lt;push&gt;</c>.</summary>
    internal static byte[] NullDataPayload(byte[] script)
    {
        if (script.Length < 2 || script[0] != 0x6a) return null;
        int length, offset;
        if (script[1] <= 75) { length = script[1]; offset = 2; }
        else if (script[1] == 0x4c && script.Length >= 3) { length = script[2]; offset = 3; }
        else return null;
        return script.Length == offset + length ? script[offset..] : null;
    }

    /// <summary>A commitment is authoritative only when every input spends an output of the storage address.</summary>
    private async Task<bool> SpendsOnlyStorageOutputsAsync(JsonElement transaction, CancellationToken cancellationToken)
    {
        var inputs = transaction.GetProperty("vin").EnumerateArray().ToArray();
        if (inputs.Length == 0) return false;
        foreach (var input in inputs)
        {
            if (!input.TryGetProperty("txid", out var previousId)) return false; // coinbase
            var previous = await TransactionAsync(previousId.GetString()!, cancellationToken);
            var spent = previous.GetProperty("vout")[input.GetProperty("vout").GetInt32()].GetProperty("scriptPubKey");
            if (!spent.TryGetProperty("addresses", out var addresses) ||
                addresses.GetArrayLength() != 1 || addresses[0].GetString() != _address)
                return false;
        }
        return true;
    }

    private async Task<JsonElement> TransactionAsync(string txid, CancellationToken cancellationToken)
    {
        if (_transactions.TryGetValue(txid, out var cached)) return cached;
        var transaction = await RpcAsync("getrawtransaction", new JsonArray(txid, 1), cancellationToken);
        // Only mined transactions are immutable enough to cache.
        if (transaction.TryGetProperty("blockhash", out var blockHash) && blockHash.ValueKind == JsonValueKind.String)
            _transactions[txid] = transaction;
        return transaction;
    }

    private async Task<(long Height, string[] Transactions)> BlockOrderAsync(string blockHash, CancellationToken cancellationToken)
    {
        var block = await RpcAsync("getblock", new JsonArray(blockHash, 1), cancellationToken);
        // A block can be reorganised away, so its height is re-read; its transaction order is fixed by its hash.
        var order = _blockOrder.GetOrAdd(blockHash, _ => block.GetProperty("tx").EnumerateArray().Select(tx => tx.GetString()!).ToArray());
        if (block.GetProperty("confirmations").GetInt64() < 1)
            throw new InvalidDataException("A Zcash storage transaction's block is no longer on the main chain.");
        return (block.GetProperty("height").GetInt64(), order);
    }

    private async Task SubmitCommitmentAsync(byte[] payload, CancellationToken cancellationToken)
    {
        if (payload.Length > 75) throw new InvalidDataException("The Zcash commitment exceeds a single direct push.");
        await _submission.WaitAsync(cancellationToken);
        try
        {
            var unspent = await RpcAsync("listunspent", new JsonArray(0, 9_999_999, new JsonArray(_address)), cancellationToken);
            var candidate = unspent.EnumerateArray()
                .Where(item => item.TryGetProperty("spendable", out var spendable) && spendable.GetBoolean())
                // Consensus forbids spending coinbase before COINBASE_MATURITY (100) confirmations.
                .Where(item => !(item.TryGetProperty("generated", out var generated) && generated.GetBoolean()) ||
                    (item.TryGetProperty("confirmations", out var depth) && depth.GetInt64() >= CoinbaseMaturity))
                .OrderByDescending(item => ToZatoshis(item.GetProperty("amount")))
                .Select(item => (Txid: item.GetProperty("txid").GetString()!, Vout: item.GetProperty("vout").GetInt32(),
                    Amount: ToZatoshis(item.GetProperty("amount"))))
                .FirstOrDefault();
            var fee = Zip317Fee(inputCount: 1, outputScriptLengths: [P2pkhScriptLength, 2 + payload.Length]);
            if (candidate.Txid == null || candidate.Amount - fee < DustThreshold)
                throw new InvalidOperationException("The Zcash storage address has no spendable balance to pay for the commitment.");

            var script = Convert.ToHexString([0x6a, (byte)payload.Length, .. payload]).ToLowerInvariant();
            var inputs = new JsonArray(new JsonObject { ["txid"] = candidate.Txid, ["vout"] = candidate.Vout });
            // zcashd's createrawtransaction only accepts address outputs, so the OP_RETURN output is appended to the
            // unsigned transaction's transparent bundle before the wallet signs it.
            var outputs = new JsonObject { [_address] = JsonValue.Create(FromZatoshis(candidate.Amount - fee)) };
            var changeOnly = (await RpcAsync("createrawtransaction", new JsonArray(inputs, outputs), cancellationToken)).GetString()!;
            var unsigned = ZcashTransparentBundle.AppendNullDataOutput(changeOnly, Convert.FromHexString(script));
            var signed = await RpcAsync("signrawtransaction", new JsonArray(unsigned), cancellationToken);
            if (!signed.GetProperty("complete").GetBoolean())
                throw new InvalidOperationException("The zcashd wallet could not sign the Zcash storage commitment.");
            var hex = signed.GetProperty("hex").GetString()!;
            var decoded = await RpcAsync("decoderawtransaction", new JsonArray(hex), cancellationToken);
            if (!decoded.GetProperty("vout").EnumerateArray().Any(output =>
                    string.Equals(output.GetProperty("scriptPubKey").GetProperty("hex").GetString(), script, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("The signed Zcash transaction does not carry the storage commitment.");
            var txid = (await RpcAsync("sendrawtransaction", new JsonArray(hex), cancellationToken)).GetString()!;
            await WaitForConfirmationAsync(txid, cancellationToken);
        }
        finally { _submission.Release(); }
    }

    /// <summary>A write is acknowledged only once its transaction is mined on the main chain.</summary>
    private async Task WaitForConfirmationAsync(string txid, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(180);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var transaction = await RpcAsync("getrawtransaction", new JsonArray(txid, 1), cancellationToken);
            if (transaction.TryGetProperty("confirmations", out var confirmations) && confirmations.GetInt64() >= 1)
                return;
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException("The Zcash storage transaction was not mined in time.");
            await Task.Delay(500, cancellationToken);
        }
    }

    private const int P2pkhScriptLength = 25;
    private const int P2pkhInputSize = 150; // ZIP-317 P2PKH standard input size
    private const int P2pkhOutputSize = 34; // ZIP-317 P2PKH standard output size

    /// <summary>
    /// ZIP-317 conventional fee for a transparent-only transaction. Transparent logical actions are measured by size:
    /// max(ceil(total input bytes / 150), ceil(total output bytes / 34)); the fee is 5000 zatoshis per logical action,
    /// with a grace floor of two actions. An OP_RETURN output is larger than a P2PKH output and counts accordingly.
    /// </summary>
    internal static long Zip317Fee(int inputCount, int[] outputScriptLengths)
    {
        var inputBytes = (long)inputCount * P2pkhInputSize;
        var outputBytes = outputScriptLengths.Sum(length => 8L + CompactSizeLength(length) + length);
        var logicalActions = Math.Max((inputBytes + P2pkhInputSize - 1) / P2pkhInputSize,
            (outputBytes + P2pkhOutputSize - 1) / P2pkhOutputSize);
        return MarginalFee * Math.Max(2, logicalActions);
    }

    private static int CompactSizeLength(long value) => value < 0xFD ? 1 : value <= 0xFFFF ? 3 : value <= 0xFFFF_FFFF ? 5 : 9;

    private static long ToZatoshis(JsonElement amount) =>
        (long)decimal.Round(decimal.Parse(amount.GetRawText(), NumberStyles.Float, CultureInfo.InvariantCulture) * ZatoshisPerZec);

    private static decimal FromZatoshis(long zatoshis) => zatoshis / (decimal)ZatoshisPerZec;

    private async Task<JsonElement> RpcAsync(string method, JsonArray parameters, CancellationToken cancellationToken)
    {
        var body = new JsonObject { ["jsonrpc"] = "1.0", ["id"] = "oasis", ["method"] = method, ["params"] = parameters }.ToJsonString();
        using var response = await _rpc.PostAsync("", new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        var root = document.RootElement;
        if (root.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null)
            throw new InvalidOperationException($"zcashd {method} failed: {error.GetRawText()}");
        response.EnsureSuccessStatusCode();
        return root.GetProperty("result").Clone();
    }

    public void Dispose()
    {
        _rpc.Dispose();
        _kubo.Dispose();
        _submission.Dispose();
    }
}
