using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MidenOASIS;

/// <summary>
/// Key-value storage committed to a public Miden account. Each value (already encrypted by
/// <see cref="PrivateKeyValueBackend"/>) is wrapped with its key as {k, v} and held on a Kubo node as one raw block, so its
/// CIDv1 follows from the block's SHA-256. The account's <c>oasis::kv</c> map holds that SHA-256 as two entries keyed by
/// [id0, id1, id2, half] (id = the first 12 bytes of SHA-256(key) as 32-bit limbs), each value four 32-bit limbs of one
/// half; zero words mean absent. Writes are account transactions acknowledged once the committed state reads back.
/// </summary>
internal sealed class MidenLedgerKeyValueBackend : IKeyValueBackend, IDisposable
{
    private const int MaximumEnvelopeBytes = 1024 * 1024; // one raw Kubo block (chunker size-1048576)
    private static readonly string[] Zero = ["0", "0", "0", "0"];
    private readonly IMidenSdkBridge _sdk;
    private readonly string _rpcUrl;
    private readonly string _accountId;
    private readonly string _keySeed;
    private readonly HttpClient _kubo;
    private readonly SemaphoreSlim _initialise = new(1, 1);
    private bool _initialised;

    public MidenLedgerKeyValueBackend(IMidenSdkBridge sdk, string rpcUrl, string accountId, string keySeed, string kuboApi)
    {
        _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        _rpcUrl = rpcUrl ?? throw new ArgumentNullException(nameof(rpcUrl));
        _accountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
        _keySeed = keySeed ?? throw new ArgumentNullException(nameof(keySeed));
        _kubo = new HttpClient { BaseAddress = new Uri((kuboApi ?? throw new ArgumentNullException(nameof(kuboApi))).TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(60) };
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        await InitialiseAsync(cancellationToken);
        await _sdk.InvokeAsync("list", null, cancellationToken);
        using var kubo = await _kubo.PostAsync("api/v0/version", null, cancellationToken);
        kubo.EnsureSuccessStatusCode();
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var hash = await ReadCommitmentAsync(RecordId(key), cancellationToken);
        if (hash == null) return null;
        var envelope = await ReadEnvelopeAsync(hash, cancellationToken);
        if (!string.Equals(envelope.Key, key, StringComparison.Ordinal))
            throw new InvalidDataException("The Miden commitment belongs to a different key (record id collision).");
        return envelope.Value;
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        await InitialiseAsync(cancellationToken);
        var listed = await _sdk.InvokeAsync("list", null, cancellationToken);
        var halves = new Dictionary<string, string[][]>(StringComparer.Ordinal);
        foreach (var entry in listed.GetProperty("entries").EnumerateArray())
        {
            var limbs = Limbs(entry.GetProperty("key"));
            var half = limbs[3];
            if (half is not ("0" or "1")) continue; // not an OASIS record entry
            var id = string.Join(",", limbs.Take(3));
            if (!halves.TryGetValue(id, out var pair)) halves[id] = pair = new string[2][];
            pair[half == "0" ? 0 : 1] = Limbs(entry.GetProperty("value"));
        }
        var keys = new List<string>();
        foreach (var pair in halves.Values)
        {
            var hash = Join(pair[0] ?? Zero, pair[1] ?? Zero);
            if (hash == null) continue; // removed
            var envelope = await ReadEnvelopeAsync(hash, cancellationToken);
            if (envelope.Key.StartsWith(prefix ?? string.Empty, StringComparison.Ordinal)) keys.Add(envelope.Key);
        }
        return keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        var wrapper = Encoding.UTF8.GetBytes(new JsonObject { ["k"] = key, ["v"] = value }.ToJsonString());
        if (wrapper.Length > MaximumEnvelopeBytes)
            throw new InvalidDataException("The Miden record envelope exceeds the single-block size limit.");
        var hash = SHA256.HashData(wrapper);
        using var form = new MultipartFormDataContent { { new ByteArrayContent(wrapper), "file", "record" } };
        using var added = await _kubo.PostAsync("api/v0/add?cid-version=1&raw-leaves=true&chunker=size-1048576&pin=true", form, cancellationToken);
        added.EnsureSuccessStatusCode();
        using (var receipt = JsonDocument.Parse(await added.Content.ReadAsStringAsync(cancellationToken)))
        {
            if (!string.Equals(receipt.RootElement.GetProperty("Hash").GetString(), RawCid(hash), StringComparison.Ordinal))
                throw new InvalidDataException("Kubo did not store the record envelope as the expected raw block.");
        }
        var id = RecordId(key);
        var replaced = await ReadCommitmentAsync(id, cancellationToken);
        await WriteAsync(id, ValueLimbs(hash, 0), ValueLimbs(hash, 1), cancellationToken);
        if (replaced != null && !replaced.AsSpan().SequenceEqual(hash)) await UnpinAsync(replaced, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var id = RecordId(key);
        var hash = await ReadCommitmentAsync(id, cancellationToken);
        if (hash == null) return; // deleting a missing key succeeds
        await WriteAsync(id, Zero, Zero, cancellationToken);
        await UnpinAsync(hash, cancellationToken); // the ledger records the removal; release the envelope content
    }

    /// <summary>The record id: the first 12 bytes of SHA-256(key) as three big-endian 32-bit limbs.</summary>
    internal static string[] RecordId(string key)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Enumerable.Range(0, 3).Select(i => BinaryPrimitives.ReadUInt32BigEndian(digest.AsSpan(i * 4)).ToString()).ToArray();
    }

    internal static string[] MapKey(string[] id, int half) => [.. id, half.ToString()];

    /// <summary>One 16-byte half of a SHA-256 as four big-endian 32-bit limbs.</summary>
    internal static string[] ValueLimbs(byte[] sha256, int half) =>
        Enumerable.Range(0, 4).Select(i => BinaryPrimitives.ReadUInt32BigEndian(sha256.AsSpan(half * 16 + i * 4)).ToString()).ToArray();

    /// <summary>Rebuilds the SHA-256 from its two halves; null when both are zero (no record).</summary>
    internal static byte[] Join(string[] first, string[] second)
    {
        var limbs = first.Concat(second).Select(limb => ulong.Parse(limb, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        if (limbs.Length != 8) throw new InvalidDataException("A Miden hash half must be four limbs.");
        if (limbs.All(limb => limb == 0)) return null;
        var hash = new byte[32];
        for (var i = 0; i < 8; i++)
        {
            if (limbs[i] > uint.MaxValue) throw new InvalidDataException("A Miden hash limb exceeds 32 bits.");
            BinaryPrimitives.WriteUInt32BigEndian(hash.AsSpan(i * 4), (uint)limbs[i]);
        }
        return hash;
    }

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

    private static string[] Limbs(JsonElement word) => word.EnumerateArray().Select(limb => limb.GetString()!).ToArray();

    private async Task WriteAsync(string[] id, string[] first, string[] second, CancellationToken cancellationToken)
    {
        await InitialiseAsync(cancellationToken);
        await _sdk.InvokeAsync("set", new { entries = new[] { new { key = MapKey(id, 0), value = first }, new { key = MapKey(id, 1), value = second } } }, cancellationToken);
    }

    private async Task<byte[]> ReadCommitmentAsync(string[] id, CancellationToken cancellationToken)
    {
        await InitialiseAsync(cancellationToken);
        var read = await _sdk.InvokeAsync("get", new { keys = new[] { MapKey(id, 0), MapKey(id, 1) } }, cancellationToken);
        var values = read.GetProperty("values").EnumerateArray().Select(Limbs).ToArray();
        return Join(values[0], values[1]);
    }

    private async Task<(string Key, string Value)> ReadEnvelopeAsync(byte[] hash, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v0/cat?arg={RawCid(hash)}&length={MaximumEnvelopeBytes + 1}");
        using var response = await _kubo.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length > MaximumEnvelopeBytes || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(bytes), hash))
            throw new CryptographicException("The Miden record envelope does not match its on-chain commitment.");
        using var document = JsonDocument.Parse(bytes);
        return (document.RootElement.GetProperty("k").GetString()!, document.RootElement.GetProperty("v").GetString()!);
    }

    private async Task UnpinAsync(byte[] hash, CancellationToken cancellationToken)
    {
        using var unpinned = await _kubo.PostAsync($"api/v0/pin/rm?arg={RawCid(hash)}&recursive=true", null, cancellationToken);
        if (unpinned.IsSuccessStatusCode) return;
        var body = await unpinned.Content.ReadAsStringAsync(cancellationToken);
        if (!body.Contains("not pinned", StringComparison.OrdinalIgnoreCase)) unpinned.EnsureSuccessStatusCode();
    }

    private async Task InitialiseAsync(CancellationToken cancellationToken)
    {
        if (_initialised && _sdk.IsInitialized) return;
        await _initialise.WaitAsync(cancellationToken);
        try
        {
            if (_initialised && _sdk.IsInitialized) return;
            await _sdk.InvokeAsync("init", new { rpcUrl = _rpcUrl, accountId = _accountId, keySeed = _keySeed }, cancellationToken);
            _initialised = true;
        }
        finally { _initialise.Release(); }
    }

    public void Dispose()
    {
        _kubo.Dispose();
        _initialise.Dispose();
        _sdk.Dispose();
    }
}
