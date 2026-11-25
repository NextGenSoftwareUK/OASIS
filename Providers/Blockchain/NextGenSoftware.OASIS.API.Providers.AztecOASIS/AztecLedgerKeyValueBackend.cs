using System;
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

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS;

/// <summary>
/// Key-value storage committed to Aztec public state. Each value (already encrypted by
/// <see cref="PrivateKeyValueBackend"/>) is wrapped with its key as {k, v} and held on a Kubo node as one raw block, so
/// its CIDv1 follows from the block's SHA-256. The OasisKv Noir contract keeps, under the record id
/// (first 31 bytes of SHA-256(key)), that SHA-256 as two 128-bit Fields; zero means absent. Writes are owner-only public
/// transactions acknowledged once their receipt reports success. Listing walks the contract's append-only id index.
/// </summary>
internal sealed class AztecLedgerKeyValueBackend : IKeyValueBackend, IDisposable
{
    private const int MaximumEnvelopeBytes = 1024 * 1024; // one raw Kubo block (chunker size-1048576)
    private readonly IAztecSdkBridge _sdk;
    private readonly string _nodeUrl;
    private readonly string _contract;
    private readonly string _secret;
    private readonly string _salt;
    private readonly string _signingKey;
    private readonly HttpClient _kubo;
    private readonly SemaphoreSlim _initialise = new(1, 1);
    private string _account;

    public AztecLedgerKeyValueBackend(IAztecSdkBridge sdk, string nodeUrl, string contract, string secret, string salt, string signingKey,
        string kuboApi)
    {
        _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        _nodeUrl = nodeUrl ?? throw new ArgumentNullException(nameof(nodeUrl));
        _contract = contract ?? throw new ArgumentNullException(nameof(contract));
        _secret = secret ?? throw new ArgumentNullException(nameof(secret));
        _salt = salt ?? throw new ArgumentNullException(nameof(salt));
        _signingKey = signingKey ?? throw new ArgumentNullException(nameof(signingKey));
        _kubo = new HttpClient { BaseAddress = new Uri((kuboApi ?? throw new ArgumentNullException(nameof(kuboApi))).TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(60) };
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        await InitialiseAsync(cancellationToken);
        var owner = await _sdk.InvokeAsync("owner", null, cancellationToken);
        if (!SameAddress(owner.GetProperty("owner").GetString(), _account))
            throw new InvalidOperationException($"The OasisKv contract {_contract} is owned by another account; this provider's account {_account} cannot write to it.");
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
            throw new InvalidDataException("The Aztec commitment belongs to a different key (record id collision).");
        return envelope.Value;
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        await InitialiseAsync(cancellationToken);
        var listed = await _sdk.InvokeAsync("list", null, cancellationToken);
        var keys = new List<string>();
        foreach (var id in listed.GetProperty("ids").EnumerateArray())
        {
            var hash = await ReadCommitmentAsync(id.GetString(), cancellationToken);
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
            throw new InvalidDataException("The Aztec record envelope exceeds the single-block size limit.");
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
        var (hi, lo) = Halves(hash);
        await _sdk.InvokeAsync("put", new { id, hi, lo }, cancellationToken);
        if (replaced != null && !replaced.AsSpan().SequenceEqual(hash)) await UnpinAsync(replaced, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var id = RecordId(key);
        var hash = await ReadCommitmentAsync(id, cancellationToken);
        if (hash == null) return; // deleting a missing key succeeds
        await _sdk.InvokeAsync("remove", new { id }, cancellationToken);
        await UnpinAsync(hash, cancellationToken); // the ledger records the removal; release the envelope content
    }

    /// <summary>A Field holds 254 bits, so the record id is the first 31 bytes of SHA-256(key).</summary>
    internal static string RecordId(string key) => "0x" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)), 0, 31).ToLowerInvariant();

    /// <summary>Splits a SHA-256 into two Fields of 16 bytes each (high half first).</summary>
    internal static (string Hi, string Lo) Halves(byte[] sha256) =>
        ("0x" + Convert.ToHexString(sha256, 0, 16).ToLowerInvariant(), "0x" + Convert.ToHexString(sha256, 16, 16).ToLowerInvariant());

    /// <summary>Joins the two 128-bit Fields back into the SHA-256; null when both are zero (no record).</summary>
    internal static byte[] Join(string hi, string lo)
    {
        var high = FieldBytes(hi);
        var low = FieldBytes(lo);
        if (high.All(b => b == 0) && low.All(b => b == 0)) return null;
        return [.. high, .. low];
    }

    private static byte[] FieldBytes(string field)
    {
        var text = field.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? field[2..] : field;
        var bytes = Convert.FromHexString(text.PadLeft(64, '0'));
        if (bytes.Take(16).Any(b => b != 0)) throw new InvalidDataException("An OasisKv hash half exceeds 128 bits.");
        return bytes[16..];
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

    private async Task<byte[]> ReadCommitmentAsync(string id, CancellationToken cancellationToken)
    {
        await InitialiseAsync(cancellationToken);
        var read = await _sdk.InvokeAsync("get", new { id }, cancellationToken);
        return Join(read.GetProperty("hi").GetString()!, read.GetProperty("lo").GetString()!);
    }

    private async Task<(string Key, string Value)> ReadEnvelopeAsync(byte[] hash, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v0/cat?arg={RawCid(hash)}&length={MaximumEnvelopeBytes + 1}");
        using var response = await _kubo.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length > MaximumEnvelopeBytes || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(bytes), hash))
            throw new CryptographicException("The Aztec record envelope does not match its on-chain commitment.");
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
        if (_account != null) return;
        await _initialise.WaitAsync(cancellationToken);
        try
        {
            if (_account != null) return;
            var initialised = await _sdk.InvokeAsync("init", new { nodeUrl = _nodeUrl, secret = _secret, salt = _salt, signingKey = _signingKey, contract = _contract }, cancellationToken);
            _account = initialised.GetProperty("address").GetString() ?? throw new InvalidDataException("The Aztec worker returned no account address.");
        }
        finally { _initialise.Release(); }
    }

    private static bool SameAddress(string a, string b) =>
        a != null && b != null && string.Equals(a.TrimStart('0', 'x').PadLeft(64, '0'), b.TrimStart('0', 'x').PadLeft(64, '0'), StringComparison.OrdinalIgnoreCase);

    public void Dispose()
    {
        _kubo.Dispose();
        _initialise.Dispose();
        _sdk.Dispose();
    }
}
