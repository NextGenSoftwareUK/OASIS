using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.StellarOASIS;

/// <summary>
/// Key-value storage on Stellar account data entries. A key's entry is named by the base64url SHA-256 of the key and its
/// 64-byte value commits the (already encrypted) envelope held by Kubo: CIDv0 multihash (34 bytes) plus the first 30
/// bytes of the envelope's SHA-256. The envelope carries the key too, so listing can recover key names. Every write or
/// removal is an Ed25519-signed ManageData transaction that Horizon returns only once it is in a closed ledger.
/// </summary>
internal sealed class StellarLedgerKeyValueBackend : IKeyValueBackend, IDisposable
{
    private const int MaximumEnvelopeBytes = 4 * 1024 * 1024;
    private const string Base58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    private readonly HttpClient _horizon;
    private readonly HttpClient _kubo;
    private readonly StellarEd25519Signer _signer;
    private readonly SemaphoreSlim _submission = new(1, 1);

    public StellarLedgerKeyValueBackend(string horizonUrl, string networkPassphrase, string privateKeyHex, string kuboApi)
    {
        _signer = new StellarEd25519Signer(privateKeyHex, networkPassphrase);
        _horizon = new HttpClient { BaseAddress = new Uri((horizonUrl ?? throw new ArgumentNullException(nameof(horizonUrl))).TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(60) };
        _kubo = new HttpClient { BaseAddress = new Uri((kuboApi ?? throw new ArgumentNullException(nameof(kuboApi))).TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(60) };
    }

    public string AccountId => _signer.AccountId;

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        await ReadAccountAsync(cancellationToken);
        using var kubo = await _kubo.PostAsync("api/v0/version", null, cancellationToken);
        kubo.EnsureSuccessStatusCode();
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var data = (await ReadAccountAsync(cancellationToken)).GetProperty("data");
        if (!data.TryGetProperty(EntryName(key), out var committed)) return null;
        var envelope = await ReadEnvelopeAsync(Convert.FromBase64String(committed.GetString()!), cancellationToken);
        if (envelope.Key != key) throw new InvalidDataException("The Stellar data entry commits an envelope for a different key.");
        return envelope.Value;
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var keys = new List<string>();
        foreach (var entry in (await ReadAccountAsync(cancellationToken)).GetProperty("data").EnumerateObject())
        {
            var envelope = await ReadEnvelopeAsync(Convert.FromBase64String(entry.Value.GetString()!), cancellationToken);
            if (EntryName(envelope.Key) != entry.Name) throw new InvalidDataException("A Stellar data entry name differs from its envelope key.");
            if (envelope.Key.StartsWith(prefix ?? string.Empty, StringComparison.Ordinal)) keys.Add(envelope.Key);
        }
        keys.Sort(StringComparer.Ordinal);
        return keys;
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        var bytes = Encoding.UTF8.GetBytes(new JsonObject { ["k"] = key, ["v"] = value }.ToJsonString());
        if (bytes.Length > MaximumEnvelopeBytes) throw new InvalidDataException("The Stellar record envelope exceeds the blob size limit.");
        using var form = new MultipartFormDataContent { { new ByteArrayContent(bytes), "file", "record" } };
        using var added = await _kubo.PostAsync("api/v0/add?cid-version=0&pin=true", form, cancellationToken);
        added.EnsureSuccessStatusCode();
        using var receipt = JsonDocument.Parse(await added.Content.ReadAsStringAsync(cancellationToken));
        var multihash = DecodeCid(receipt.RootElement.GetProperty("Hash").GetString());
        var commitment = multihash.Concat(SHA256.HashData(bytes).AsSpan(0, 30).ToArray()).ToArray();
        await SubmitAsync(EntryName(key), commitment, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var data = (await ReadAccountAsync(cancellationToken)).GetProperty("data");
        var name = EntryName(key);
        if (!data.TryGetProperty(name, out var committed)) return; // deleting a missing key succeeds
        await SubmitAsync(name, null, cancellationToken);
        var cid = EncodeCid(Convert.FromBase64String(committed.GetString()!).AsSpan(0, 34));
        using var unpinned = await _kubo.PostAsync($"api/v0/pin/rm?arg={cid}&recursive=true", null, cancellationToken);
        if (!unpinned.IsSuccessStatusCode)
        {
            var body = await unpinned.Content.ReadAsStringAsync(cancellationToken);
            if (!body.Contains("not pinned", StringComparison.OrdinalIgnoreCase)) unpinned.EnsureSuccessStatusCode();
        }
    }

    private static string EntryName(string key)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(key))).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private async Task<(string Key, string Value)> ReadEnvelopeAsync(byte[] commitment, CancellationToken cancellationToken)
    {
        if (commitment.Length != 64) throw new InvalidDataException("A Stellar data entry is not an OASIS record commitment.");
        var cid = EncodeCid(commitment.AsSpan(0, 34));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v0/cat?arg={cid}&length={MaximumEnvelopeBytes + 1}");
        using var response = await _kubo.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length > MaximumEnvelopeBytes || !SHA256.HashData(bytes).AsSpan(0, 30).SequenceEqual(commitment.AsSpan(34, 30)))
            throw new CryptographicException("The Stellar record envelope does not match its ledger commitment.");
        using var document = JsonDocument.Parse(bytes);
        return (document.RootElement.GetProperty("k").GetString()!, document.RootElement.GetProperty("v").GetString()!);
    }

    private async Task<JsonElement> ReadAccountAsync(CancellationToken cancellationToken)
    {
        using var response = await _horizon.GetAsync($"accounts/{_signer.AccountId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"The Stellar storage account {_signer.AccountId} does not exist; fund it before use.");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.Clone();
    }

    private async Task SubmitAsync(string name, byte[] value, CancellationToken cancellationToken)
    {
        await _submission.WaitAsync(cancellationToken);
        try
        {
            var account = await ReadAccountAsync(cancellationToken);
            var sequence = long.Parse(account.GetProperty("sequence").GetString()!) + 1;
            using var feeStats = JsonDocument.Parse(await _horizon.GetStringAsync("fee_stats", cancellationToken));
            var fee = Math.Max(100u, uint.Parse(feeStats.RootElement.GetProperty("last_ledger_base_fee").GetString()!));
            var (envelope, hash) = _signer.SignManageData(sequence, fee, name, value);
            // Horizon's synchronous submission returns once the transaction is in a closed ledger (or failed).
            using var response = await _horizon.PostAsync("transactions",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["tx"] = envelope }), cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Stellar rejected the storage transaction ({(int)response.StatusCode}): {body}");
            using var result = JsonDocument.Parse(body);
            if (!result.RootElement.GetProperty("successful").GetBoolean() || result.RootElement.GetProperty("hash").GetString() != hash)
                throw new InvalidOperationException("Stellar did not acknowledge the staged storage transaction.");
        }
        finally { _submission.Release(); }
    }

    private static byte[] DecodeCid(string cid)
    {
        if (cid == null || cid.Length != 46 || !cid.StartsWith("Qm", StringComparison.Ordinal))
            throw new FormatException("Kubo did not return a CIDv0 record identifier.");
        var value = System.Numerics.BigInteger.Zero;
        foreach (var c in cid) { var d = Base58.IndexOf(c); if (d < 0) throw new FormatException("Invalid CIDv0."); value = value * 58 + d; }
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (bytes.Length != 34 || bytes[0] != 0x12 || bytes[1] != 0x20) throw new FormatException("The record requires a SHA-256 CIDv0.");
        return bytes;
    }

    private static string EncodeCid(ReadOnlySpan<byte> multihash)
    {
        var value = new System.Numerics.BigInteger(multihash, isUnsigned: true, isBigEndian: true);
        Span<char> text = stackalloc char[46];
        for (var i = text.Length - 1; i >= 0; i--) { value = System.Numerics.BigInteger.DivRem(value, 58, out var d); text[i] = Base58[(int)d]; }
        return new string(text);
    }

    public void Dispose() { _horizon.Dispose(); _kubo.Dispose(); _submission.Dispose(); }
}
