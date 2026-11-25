using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.CelestiaOASIS;

/// <summary>
/// Append-only key-value storage on Celestia data availability.
///
/// Every <see cref="PutAsync"/> submits one share-version-1 blob <c>{"k":key,"v":value,"n":nonce}</c> to the configured
/// namespace and every <see cref="DeleteAsync"/> a tombstone <c>{"k":key,"v":null,"n":nonce}</c>. Celestia has no key index,
/// so the backend keeps one in memory: key → (height, commitment) of the newest record, ordered by (height, blob index).
/// The index is never authoritative state of its own: it is rebuilt from Celestia by scanning the namespace from the
/// configured start height up to the node's local head, and every operation first catches up with new heights, so a
/// restarted provider (or another instance writing with the same account) observes exactly what is on chain.
///
/// The namespace is public, so only blobs whose consensus-verified signer is the configured writer account are records;
/// blobs other accounts post into the namespace are not this store's data and are ignored. A malformed blob from the
/// writer account is corruption and fails the operation.
/// A write is acknowledged only after the node reports its inclusion height, has synced that height, and the scan of
/// that height returned the submitted blob.
/// </summary>
public sealed class CelestiaBlobKeyValueBackend : IKeyValueBackend, IDisposable
{
    /// <summary>Largest record blob this backend submits. Celestia's own per-transaction ceiling is several MiB; one MiB keeps
    /// each record within a single small PayForBlobs transaction on every network configuration.</summary>
    public const int MaxBlobDataBytes = 1024 * 1024;

    private const int NamespaceIdBytes = 10;
    private const int ScanParallelism = 8;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private readonly HttpClient _http;
    private readonly bool _ownsClient;
    private readonly string _namespaceBase64;
    private readonly ulong _startHeight;
    private readonly string? _configuredWriter;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, Entry> _index = new(StringComparer.Ordinal);
    private byte[]? _writerSigner;
    private ulong _scannedThrough;
    private long _rpcId;

    private readonly record struct Entry(ulong Height, string Commitment);

    /// <param name="client">HTTP client whose BaseAddress is the celestia-node JSON-RPC endpoint and which carries its auth token.</param>
    /// <param name="namespaceIdHex">10-byte (20 hex characters) version-0 namespace ID that holds this store's blobs.</param>
    /// <param name="startHeight">First height the namespace can contain this store's records; recovery scans from here.</param>
    /// <param name="writerAddress">Bech32 account whose blobs are records; defaults to the node's own submitting account.</param>
    public CelestiaBlobKeyValueBackend(HttpClient client, string namespaceIdHex, ulong startHeight, string? writerAddress = null, bool ownsClient = false)
    {
        _http = client ?? throw new ArgumentNullException(nameof(client));
        if (client.BaseAddress == null) throw new ArgumentException("The Celestia node client requires a BaseAddress.", nameof(client));
        if (startHeight == 0) throw new ArgumentOutOfRangeException(nameof(startHeight), "Celestia heights start at 1.");
        _namespaceBase64 = Convert.ToBase64String(NamespaceBytes(namespaceIdHex));
        _startHeight = startHeight;
        _scannedThrough = startHeight - 1;
        _configuredWriter = writerAddress;
        _ownsClient = ownsClient;
    }

    /// <summary>The 29-byte version-0 namespace (version byte, 18 zero bytes, 10-byte ID).</summary>
    public static byte[] NamespaceBytes(string namespaceIdHex)
    {
        if (string.IsNullOrWhiteSpace(namespaceIdHex) || namespaceIdHex.Length != NamespaceIdBytes * 2)
            throw new ArgumentException("A Celestia namespace ID is exactly 10 bytes (20 hex characters).", nameof(namespaceIdHex));
        var id = Convert.FromHexString(namespaceIdHex);
        var ns = new byte[29];
        id.CopyTo(ns, 29 - NamespaceIdBytes);
        return ns;
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { await CatchUpAsync(await LocalHeadAsync(cancellationToken), cancellationToken); }
        finally { _gate.Release(); }
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        Entry entry;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await CatchUpAsync(await LocalHeadAsync(cancellationToken), cancellationToken);
            if (!_index.TryGetValue(key, out entry)) return null!;
        }
        finally { _gate.Release(); }

        var blob = await CallAsync("blob.Get", new JsonArray(entry.Height, _namespaceBase64, entry.Commitment), cancellationToken);
        if (blob.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"Celestia did not return the indexed blob at height {entry.Height}.");
        var record = Decode(blob, entry.Height);
        if (record == null || !string.Equals(record.Value.Key, key, StringComparison.Ordinal) || record.Value.Value == null)
            throw new InvalidDataException($"The Celestia blob indexed for a key at height {entry.Height} is not that key's value.");
        return record.Value.Value;
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        prefix ??= string.Empty;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await CatchUpAsync(await LocalHeadAsync(cancellationToken), cancellationToken);
            return _index.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).OrderBy(k => k, StringComparer.Ordinal).ToArray();
        }
        finally { _gate.Release(); }
    }

    public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        return WriteAsync(key, value, cancellationToken);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        return WriteAsync(key, null, cancellationToken);
    }

    private async Task WriteAsync(string key, string? value, CancellationToken cancellationToken)
    {
        var nonce = Guid.NewGuid().ToString("N");
        var data = StrictUtf8.GetBytes(new JsonObject { ["k"] = key, ["v"] = value, ["n"] = nonce }.ToJsonString());
        if (data.Length > MaxBlobDataBytes)
            throw new InvalidDataException($"A Celestia record of {data.Length} bytes exceeds the {MaxBlobDataBytes}-byte blob limit.");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await CatchUpAsync(await LocalHeadAsync(cancellationToken), cancellationToken);
            if (value == null && !_index.ContainsKey(key)) return; // deleting a missing key succeeds without a tombstone
            var signer = await WriterSignerAsync(cancellationToken);

            var blob = new JsonObject
            {
                ["namespace"] = _namespaceBase64, ["data"] = Convert.ToBase64String(data),
                ["share_version"] = 1, ["signer"] = Convert.ToBase64String(signer)
            };
            var submitted = await CallAsync("blob.Submit", new JsonArray(new JsonArray(blob), new JsonObject()), cancellationToken);
            if (submitted.ValueKind != JsonValueKind.Number || !submitted.TryGetUInt64(out var height) || height <= _scannedThrough)
                throw new InvalidDataException($"Celestia blob.Submit returned no usable inclusion height ({submitted}).");

            await CallAsync("header.WaitForHeight", new JsonArray(height), cancellationToken);
            var observed = await CatchUpAsync(height, cancellationToken);
            if (!observed.Contains(nonce))
                throw new InvalidDataException($"The submitted Celestia record was not found in its reported inclusion height {height}.");
        }
        finally { _gate.Release(); }
    }

    /// <summary>Applies every record in heights (_scannedThrough, through]; returns the nonces of the records seen.</summary>
    private async Task<HashSet<string>> CatchUpAsync(ulong through, CancellationToken cancellationToken)
    {
        var nonces = new HashSet<string>(StringComparer.Ordinal);
        if (through <= _scannedThrough) return nonces;
        var signer = await WriterSignerAsync(cancellationToken);
        for (var from = _scannedThrough + 1; from <= through; from += ScanParallelism)
        {
            var heights = Enumerable.Range(0, (int)Math.Min((ulong)ScanParallelism, through - from + 1)).Select(i => from + (ulong)i).ToArray();
            var pages = await Task.WhenAll(heights.Select(h => CallAsync("blob.GetAll", new JsonArray(h, new JsonArray(_namespaceBase64)), cancellationToken)));
            for (var i = 0; i < heights.Length; i++)
            {
                if (pages[i].ValueKind == JsonValueKind.Null) { _scannedThrough = heights[i]; continue; }
                if (pages[i].ValueKind != JsonValueKind.Array)
                    throw new InvalidDataException($"Celestia blob.GetAll returned an unexpected result at height {heights[i]}.");
                var blobs = pages[i].EnumerateArray().OrderBy(b => b.GetProperty("index").GetInt64()).ToArray();
                foreach (var blob in blobs)
                {
                    if (!blob.TryGetProperty("signer", out var s) || s.ValueKind != JsonValueKind.String
                        || !Convert.FromBase64String(s.GetString()!).AsSpan().SequenceEqual(signer))
                        continue; // not written by this store's account: public namespace traffic, not a record
                    var record = Decode(blob, heights[i]) ?? throw new InvalidDataException($"A Celestia record at height {heights[i]} is malformed.");
                    if (record.Value == null) _index.Remove(record.Key);
                    else _index[record.Key] = new Entry(heights[i], blob.GetProperty("commitment").GetString()!);
                    nonces.Add(record.Nonce);
                }
                _scannedThrough = heights[i];
            }
        }
        return nonces;
    }

    private (string Key, string? Value, string Nonce)? Decode(JsonElement blob, ulong height)
    {
        if (blob.GetProperty("namespace").GetString() != _namespaceBase64 || blob.GetProperty("share_version").GetInt32() != 1)
            return null;
        try
        {
            using var doc = JsonDocument.Parse(StrictUtf8.GetString(Convert.FromBase64String(blob.GetProperty("data").GetString()!)));
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("k", out var k) || k.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("n", out var n) || n.ValueKind != JsonValueKind.String || !root.TryGetProperty("v", out var v)
                || v.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                return null;
            return (k.GetString()!, v.ValueKind == JsonValueKind.Null ? null : v.GetString(), n.GetString()!);
        }
        catch (Exception cause) when (cause is JsonException or FormatException or DecoderFallbackException)
        {
            throw new InvalidDataException($"A Celestia record at height {height} is not valid record JSON.", cause);
        }
    }

    private async Task<byte[]> WriterSignerAsync(CancellationToken cancellationToken)
    {
        if (_writerSigner != null) return _writerSigner;
        var address = _configuredWriter;
        if (address == null)
        {
            var reported = await CallAsync("state.AccountAddress", new JsonArray(), cancellationToken);
            address = reported.ValueKind == JsonValueKind.String ? reported.GetString() : null;
            if (string.IsNullOrEmpty(address)) throw new InvalidDataException("The Celestia node reported no submitting account.");
        }
        return _writerSigner = Bech32.DecodeAccount(address, "celestia");
    }

    private async Task<ulong> LocalHeadAsync(CancellationToken cancellationToken)
    {
        var head = await CallAsync("header.LocalHead", new JsonArray(), cancellationToken);
        var text = head.GetProperty("header").GetProperty("height").GetString();
        if (!ulong.TryParse(text, out var height)) throw new InvalidDataException("The Celestia node reported an invalid local head height.");
        return height;
    }

    private async Task<JsonElement> CallAsync(string method, JsonArray parameters, CancellationToken cancellationToken)
    {
        var request = new JsonObject
        {
            ["jsonrpc"] = "2.0", ["id"] = Interlocked.Increment(ref _rpcId), ["method"] = method, ["params"] = parameters
        };
        using var content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync(string.Empty, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Celestia {method} failed with HTTP {(int)response.StatusCode}: {Truncate(body)}", null, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null)
            throw new InvalidOperationException($"Celestia {method} failed: {Truncate(error.ToString())}");
        if (!doc.RootElement.TryGetProperty("result", out var result))
            throw new InvalidDataException($"Celestia {method} returned no result.");
        return result.Clone();
    }

    private static string Truncate(string text) => text.Length <= 500 ? text : text[..500];

    public void Dispose()
    {
        _gate.Dispose();
        if (_ownsClient) _http.Dispose();
    }
}

/// <summary>BIP-173 bech32 decoding for Cosmos account addresses (20-byte payload), checksum verified.</summary>
internal static class Bech32
{
    private const string Charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";

    public static byte[] DecodeAccount(string address, string expectedPrefix)
    {
        var lower = address.ToLowerInvariant();
        if (lower != address && address.ToUpperInvariant() != address) throw new FormatException("Bech32 address has mixed case.");
        var separator = lower.LastIndexOf('1');
        if (separator < 1 || lower.Length - separator < 7) throw new FormatException("Bech32 address is malformed.");
        var hrp = lower[..separator];
        if (hrp != expectedPrefix) throw new FormatException($"Expected a {expectedPrefix} account address.");
        var values = lower[(separator + 1)..].Select(c => Charset.IndexOf(c)).ToArray();
        if (values.Any(v => v < 0)) throw new FormatException("Bech32 address has an invalid character.");
        if (Polymod(ExpandHrp(hrp).Concat(values)) != 1) throw new FormatException("Bech32 address checksum is invalid.");

        var data = values[..^6];
        var bytes = new List<byte>();
        int accumulator = 0, bits = 0;
        foreach (var value in data)
        {
            accumulator = (accumulator << 5) | value;
            bits += 5;
            if (bits >= 8) { bits -= 8; bytes.Add((byte)((accumulator >> bits) & 0xff)); }
        }
        if (bits >= 5 || ((accumulator << (8 - bits)) & 0xff) != 0) throw new FormatException("Bech32 address has invalid padding.");
        if (bytes.Count != 20) throw new FormatException("A Celestia account address carries 20 bytes.");
        return bytes.ToArray();
    }

    private static IEnumerable<int> ExpandHrp(string hrp) => hrp.Select(c => c >> 5).Append(0).Concat(hrp.Select(c => c & 31));

    private static int Polymod(IEnumerable<int> values)
    {
        int[] generator = { 0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3 };
        var chk = 1;
        foreach (var value in values)
        {
            var top = chk >> 25;
            chk = ((chk & 0x1ffffff) << 5) ^ value;
            for (var i = 0; i < 5; i++) if (((top >> i) & 1) != 0) chk ^= generator[i];
        }
        return chk;
    }
}
