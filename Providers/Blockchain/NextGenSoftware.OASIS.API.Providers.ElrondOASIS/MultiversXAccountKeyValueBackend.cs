using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS;

/// <summary>
/// Key-value storage in a MultiversX account's own key-value store. Each value is an (already encrypted) envelope held by
/// a Kubo node; the account stores, under "oasis:" + sha256(key), the commitment {k: key, c: CID, h: SHA-256}, written by a
/// signed SaveKeyValue transaction to itself (an empty value deletes the entry). A write is acknowledged only once the
/// transaction status is "success".
/// </summary>
internal sealed class MultiversXAccountKeyValueBackend : IKeyValueBackend, IDisposable
{
    private const int MaximumEnvelopeBytes = 4 * 1024 * 1024;
    private static readonly byte[] Namespace = Encoding.ASCII.GetBytes("oasis:");
    private static readonly TimeSpan WriteDeadline = TimeSpan.FromMinutes(2);
    private readonly HttpClient _proxy;
    private readonly HttpClient _kubo;
    private readonly MultiversXTransactionSigner _signer;
    private readonly string _chainId;
    private readonly SemaphoreSlim _submission = new(1, 1);

    public MultiversXAccountKeyValueBackend(string proxyUrl, string chainId, string secretKeyHex, string kuboApi)
    {
        _proxy = new HttpClient { BaseAddress = new Uri((proxyUrl ?? throw new ArgumentNullException(nameof(proxyUrl))).TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(60) };
        _kubo = new HttpClient { BaseAddress = new Uri((kuboApi ?? throw new ArgumentNullException(nameof(kuboApi))).TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(60) };
        _chainId = string.IsNullOrWhiteSpace(chainId) ? throw new ArgumentException("An explicit MultiversX chain id is required.", nameof(chainId)) : chainId;
        _signer = new MultiversXTransactionSigner(secretKeyHex);
    }

    public string Address => _signer.Address;

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        using var config = await GetDataAsync("network/config", cancellationToken);
        var chain = config.RootElement.GetProperty("config").GetProperty("erd_chain_id").GetString();
        if (!string.Equals(chain, _chainId, StringComparison.Ordinal))
            throw new InvalidOperationException($"The MultiversX proxy serves chain '{chain}', not the configured '{_chainId}'.");
        using var account = await GetDataAsync($"address/{Address}", cancellationToken);
        using var kubo = await _kubo.PostAsync("api/v0/version", null, cancellationToken);
        kubo.EnsureSuccessStatusCode();
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var commitment = await ReadCommitmentAsync(key, cancellationToken);
        return commitment == null ? null : await ReadEnvelopeAsync(commitment.Value.Cid, commitment.Value.Sha256, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        using var pairs = await GetDataAsync($"address/{Address}/keys", cancellationToken);
        var keys = new List<string>();
        foreach (var pair in pairs.RootElement.GetProperty("pairs").EnumerateObject())
        {
            var storageKey = Convert.FromHexString(pair.Name);
            if (!storageKey.AsSpan().StartsWith(Namespace)) continue; // account keys OASIS does not own (e.g. ESDT metadata)
            var value = pair.Value.GetString();
            if (string.IsNullOrEmpty(value)) continue; // deleted entry
            var (k, _, _) = ParseCommitment(Convert.FromHexString(value));
            if (!storageKey.AsSpan().SequenceEqual(StorageKey(k)))
                throw new InvalidDataException("A MultiversX OASIS entry is stored under a key that does not match its commitment.");
            if (k.StartsWith(prefix ?? string.Empty, StringComparison.Ordinal)) keys.Add(k);
        }
        return keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        var bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length > MaximumEnvelopeBytes) throw new InvalidDataException("The MultiversX record envelope exceeds the blob size limit.");
        using var form = new MultipartFormDataContent { { new ByteArrayContent(bytes), "file", "record" } };
        using var added = await _kubo.PostAsync("api/v0/add?cid-version=0&pin=true", form, cancellationToken);
        added.EnsureSuccessStatusCode();
        using var receipt = JsonDocument.Parse(await added.Content.ReadAsStringAsync(cancellationToken));
        var cid = receipt.RootElement.GetProperty("Hash").GetString() ?? throw new InvalidDataException("Kubo returned no CID.");
        var commitment = JsonSerializer.SerializeToUtf8Bytes(new { k = key, c = cid, h = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() });
        await SaveKeyValueAsync(StorageKey(key), commitment, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var commitment = await ReadCommitmentAsync(key, cancellationToken);
        if (commitment == null) return; // deleting a missing key succeeds
        await SaveKeyValueAsync(StorageKey(key), Array.Empty<byte>(), cancellationToken);
        using var unpinned = await _kubo.PostAsync($"api/v0/pin/rm?arg={Uri.EscapeDataString(commitment.Value.Cid)}&recursive=true", null, cancellationToken);
        if (!unpinned.IsSuccessStatusCode && !(await unpinned.Content.ReadAsStringAsync(cancellationToken)).Contains("not pinned", StringComparison.OrdinalIgnoreCase))
            unpinned.EnsureSuccessStatusCode();
    }

    internal static byte[] StorageKey(string key) => Namespace.Concat(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToArray();

    private async Task<(string Key, string Cid, string Sha256)?> ReadCommitmentAsync(string key, CancellationToken cancellationToken)
    {
        using var entry = await GetDataAsync($"address/{Address}/key/{Convert.ToHexString(StorageKey(key)).ToLowerInvariant()}", cancellationToken);
        var value = entry.RootElement.GetProperty("value").GetString();
        if (string.IsNullOrEmpty(value)) return null;
        var commitment = ParseCommitment(Convert.FromHexString(value));
        if (!string.Equals(commitment.Key, key, StringComparison.Ordinal))
            throw new InvalidDataException("The MultiversX commitment belongs to a different key (key hash collision).");
        return commitment;
    }

    private static (string Key, string Cid, string Sha256) ParseCommitment(byte[] json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return (root.GetProperty("k").GetString()!, root.GetProperty("c").GetString()!, root.GetProperty("h").GetString()!);
    }

    private async Task<string> ReadEnvelopeAsync(string cid, string sha256, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v0/cat?arg={Uri.EscapeDataString(cid)}&length={MaximumEnvelopeBytes + 1}");
        using var response = await _kubo.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length > MaximumEnvelopeBytes || !string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), sha256, StringComparison.OrdinalIgnoreCase))
            throw new CryptographicException("The MultiversX record envelope does not match its on-chain commitment.");
        return Encoding.UTF8.GetString(bytes);
    }

    private async Task SaveKeyValueAsync(byte[] storageKey, byte[] value, CancellationToken cancellationToken)
    {
        var data = Encoding.ASCII.GetBytes($"SaveKeyValue@{Convert.ToHexString(storageKey).ToLowerInvariant()}@{Convert.ToHexString(value).ToLowerInvariant()}");
        await _submission.WaitAsync(cancellationToken); // one account nonce stream
        try
        {
            using var config = await GetDataAsync("network/config", cancellationToken);
            var gasPrice = config.RootElement.GetProperty("config").GetProperty("erd_min_gas_price").GetUInt64();
            using var account = await GetDataAsync($"address/{Address}", cancellationToken);
            var nonce = account.RootElement.GetProperty("account").GetProperty("nonce").GetUInt64();

            using var costBody = new ByteArrayContent(MultiversXTransactionSigner.Canonical(nonce, Address, Address, gasPrice, 0, data, _chainId));
            costBody.Headers.ContentType = new("application/json");
            using var cost = await PostDataAsync("transaction/cost", costBody, cancellationToken);
            var gasLimit = cost.RootElement.GetProperty("txGasUnits").GetUInt64();

            using var sendBody = new StringContent(_signer.Sign(nonce, gasPrice, gasLimit, data, _chainId), Encoding.UTF8, "application/json");
            using var sent = await PostDataAsync("transaction/send", sendBody, cancellationToken);
            var hash = sent.RootElement.GetProperty("txHash").GetString() ?? throw new InvalidDataException("MultiversX returned no transaction hash.");

            var deadline = DateTime.UtcNow + WriteDeadline;
            while (true)
            {
                using var status = await GetDataAsync($"transaction/{hash}/status", cancellationToken, allowNotFound: true);
                var state = status?.RootElement.GetProperty("status").GetString();
                if (state == "success") return;
                if (state is "fail" or "invalid")
                    throw new IOException($"MultiversX storage transaction {hash} ended with status '{state}'.");
                if (DateTime.UtcNow > deadline) throw new TimeoutException($"MultiversX storage transaction {hash} was not processed in time.");
                await Task.Delay(500, cancellationToken);
            }
        }
        finally { _submission.Release(); }
    }

    private async Task<JsonDocument> GetDataAsync(string path, CancellationToken cancellationToken, bool allowNotFound = false)
    {
        using var response = await _proxy.GetAsync(path, cancellationToken);
        if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound) return null;
        return await DataAsync(path, response, cancellationToken);
    }

    private async Task<JsonDocument> PostDataAsync(string path, HttpContent body, CancellationToken cancellationToken)
    {
        using var response = await _proxy.PostAsync(path, body, cancellationToken);
        return await DataAsync(path, response, cancellationToken);
    }

    /// <summary>MultiversX proxy envelope: {"data": ..., "error": "", "code": "successful"}.</summary>
    private static async Task<JsonDocument> DataAsync(string path, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        using var envelope = JsonDocument.Parse(text);
        var root = envelope.RootElement;
        if (!response.IsSuccessStatusCode || root.GetProperty("code").GetString() != "successful")
            throw new HttpRequestException($"MultiversX {path} failed ({(int)response.StatusCode}): {text}");
        return JsonDocument.Parse(root.GetProperty("data").GetRawText());
    }

    public void Dispose() { _proxy.Dispose(); _kubo.Dispose(); _submission.Dispose(); }
}
