using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.CardanoOASIS;

/// <summary>
/// Key-value storage on Cardano through a Blockfrost-compatible API. Each value is an (already encrypted) envelope held by
/// a Kubo node; every put or delete is an Ed25519-signed transaction from the storage address back to itself whose
/// metadata (label 1967) commits the key and the envelope's CID and SHA-256, or a deletion. A write is acknowledged once
/// the transaction is on chain; the newest on-chain entry for a key wins.
/// </summary>
internal sealed class CardanoLedgerKeyValueBackend : IKeyValueBackend, IDisposable
{
    public const ulong MetadataLabel = 1967;
    private const int MaximumEnvelopeBytes = 4 * 1024 * 1024;
    private readonly HttpClient _api;
    private readonly HttpClient _kubo;
    private readonly CardanoEd25519Signer _signer;
    private readonly SemaphoreSlim _submission = new(1, 1);

    public CardanoLedgerKeyValueBackend(string blockfrostUrl, string projectId, bool mainnet, string privateKeyHex, string kuboApi)
    {
        _signer = new CardanoEd25519Signer(privateKeyHex, mainnet);
        _api = new HttpClient { BaseAddress = new Uri((blockfrostUrl ?? throw new ArgumentNullException(nameof(blockfrostUrl))).TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(60) };
        if (!string.IsNullOrEmpty(projectId)) _api.DefaultRequestHeaders.Add("project_id", projectId);
        _kubo = new HttpClient { BaseAddress = new Uri((kuboApi ?? throw new ArgumentNullException(nameof(kuboApi))).TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(60) };
    }

    public string Address => _signer.Address;

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        using var latest = await GetJsonAsync("blocks/latest", cancellationToken);
        if (!latest.RootElement.TryGetProperty("slot", out _)) throw new InvalidDataException("The Cardano API returned no latest block.");
        using var kubo = await _kubo.PostAsync("api/v0/version", null, cancellationToken);
        kubo.EnsureSuccessStatusCode();
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var index = await ReadIndexAsync(cancellationToken);
        return index.TryGetValue(key, out var entry) && !entry.Deleted ? await ReadEnvelopeAsync(entry, cancellationToken) : null;
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        => (await ReadIndexAsync(cancellationToken)).Where(p => !p.Value.Deleted && p.Key.StartsWith(prefix ?? string.Empty, StringComparison.Ordinal))
            .Select(p => p.Key).OrderBy(k => k, StringComparer.Ordinal).ToArray();

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        var bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length > MaximumEnvelopeBytes) throw new InvalidDataException("The Cardano record envelope exceeds the blob size limit.");
        using var form = new MultipartFormDataContent { { new ByteArrayContent(bytes), "file", "record" } };
        using var added = await _kubo.PostAsync("api/v0/add?cid-version=0&pin=true", form, cancellationToken);
        added.EnsureSuccessStatusCode();
        using var receipt = JsonDocument.Parse(await added.Content.ReadAsStringAsync(cancellationToken));
        var cid = receipt.RootElement.GetProperty("Hash").GetString() ?? throw new InvalidDataException("Kubo returned no CID.");
        await SubmitAsync(Metadatum(key, (CborValue.Text("c"), CborValue.Text(cid)),
            (CborValue.Text("h"), CborValue.Text(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()))), cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var index = await ReadIndexAsync(cancellationToken);
        if (!index.TryGetValue(key, out var entry) || entry.Deleted) return; // deleting a missing key succeeds
        await SubmitAsync(Metadatum(key, (CborValue.Text("d"), CborValue.UInt(1))), cancellationToken);
        using var unpinned = await _kubo.PostAsync($"api/v0/pin/rm?arg={Uri.EscapeDataString(entry.Cid)}&recursive=true", null, cancellationToken);
        if (!unpinned.IsSuccessStatusCode && !(await unpinned.Content.ReadAsStringAsync(cancellationToken)).Contains("not pinned", StringComparison.OrdinalIgnoreCase))
            unpinned.EnsureSuccessStatusCode();
    }

    /// <summary>Metadata strings are at most 64 bytes, so the key travels as a list of UTF-8 chunks.</summary>
    private static CborValue Metadatum(string key, params (CborValue, CborValue)[] fields)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var chunks = Enumerable.Range(0, (keyBytes.Length + 63) / 64)
            .Select(i => CborValue.Bytes(keyBytes.Skip(i * 64).Take(64).ToArray())).ToArray();
        return CborValue.Map(new[] { (CborValue.Text("k"), CborValue.Array(chunks)) }.Concat(fields).ToArray());
    }

    private sealed record IndexEntry(string Cid, string Sha256, bool Deleted);

    private async Task<string> ReadEnvelopeAsync(IndexEntry entry, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v0/cat?arg={Uri.EscapeDataString(entry.Cid)}&length={MaximumEnvelopeBytes + 1}");
        using var response = await _kubo.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length > MaximumEnvelopeBytes || !string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), entry.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new CryptographicException("The Cardano record envelope does not match its on-chain commitment.");
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>Replays the address's on-chain OASIS metadata in chain order (block height, then transaction index).</summary>
    private async Task<Dictionary<string, IndexEntry>> ReadIndexAsync(CancellationToken cancellationToken)
    {
        var transactions = new List<(long Height, int Index, string Hash)>();
        for (var page = 1; ; page++)
        {
            using var doc = await GetJsonAsync($"addresses/{_signer.Address}/transactions?order=asc&count=100&page={page}", cancellationToken, allowNotFound: true);
            if (doc == null) break;
            var rows = doc.RootElement.EnumerateArray().ToArray();
            transactions.AddRange(rows.Select(r => (r.GetProperty("block_height").GetInt64(), r.GetProperty("tx_index").GetInt32(), r.GetProperty("tx_hash").GetString()!)));
            if (rows.Length < 100) break;
        }
        var index = new Dictionary<string, IndexEntry>(StringComparer.Ordinal);
        foreach (var (_, _, hash) in transactions.OrderBy(t => t.Height).ThenBy(t => t.Index))
        {
            using var metadata = await GetJsonAsync($"txs/{hash}/metadata", cancellationToken);
            foreach (var item in metadata.RootElement.EnumerateArray())
            {
                if (item.GetProperty("label").GetString() != MetadataLabel.ToString()) continue;
                var json = item.GetProperty("json_metadata");
                var key = Encoding.UTF8.GetString(json.GetProperty("k").EnumerateArray().SelectMany(chunk => HexOrText(chunk)).ToArray());
                index[key] = json.TryGetProperty("d", out _) ? new IndexEntry(null, null, true)
                    : new IndexEntry(json.GetProperty("c").GetString(), json.GetProperty("h").GetString(), false);
            }
        }
        return index;
    }

    /// <summary>Blockfrost renders metadata byte strings as "0x"-prefixed hex.</summary>
    private static byte[] HexOrText(JsonElement chunk)
    {
        var text = chunk.GetString()!;
        return text.StartsWith("0x", StringComparison.Ordinal) ? Convert.FromHexString(text[2..]) : Encoding.UTF8.GetBytes(text);
    }

    private async Task SubmitAsync(CborValue metadatum, CancellationToken cancellationToken)
    {
        await _submission.WaitAsync(cancellationToken);
        try
        {
            using var utxos = await GetJsonAsync($"addresses/{_signer.Address}/utxos", cancellationToken, allowNotFound: true)
                ?? throw new InvalidOperationException($"The Cardano storage address {_signer.Address} has no funds.");
            var input = utxos.RootElement.EnumerateArray()
                .Where(u => u.GetProperty("amount").GetArrayLength() == 1 && u.GetProperty("amount")[0].GetProperty("unit").GetString() == "lovelace")
                .Select(u => (Hash: u.GetProperty("tx_hash").GetString()!, Index: u.GetProperty("output_index").GetUInt32(),
                    Lovelace: ulong.Parse(u.GetProperty("amount")[0].GetProperty("quantity").GetString()!)))
                .OrderByDescending(u => u.Lovelace).FirstOrDefault();
            if (input.Hash == null) throw new InvalidOperationException("The Cardano storage address has no ADA-only UTxO.");
            using var parameters = await GetJsonAsync("epochs/latest/parameters", cancellationToken);
            var a = parameters.RootElement.GetProperty("min_fee_a").GetUInt64();
            var b = parameters.RootElement.GetProperty("min_fee_b").GetUInt64();
            using var latest = await GetJsonAsync("blocks/latest", cancellationToken);
            var ttl = latest.RootElement.GetProperty("slot").GetUInt64() + 3600;

            // The fee depends on the signed size; rebuild until it covers the size it pays for.
            ulong fee = 200_000;
            byte[] transaction; string id;
            while (true)
            {
                (transaction, id) = _signer.Build(input.Hash, input.Index, input.Lovelace, fee, ttl, MetadataLabel, metadatum);
                var required = a * (ulong)transaction.Length + b;
                if (required <= fee) break;
                fee = required + 1_000;
            }

            using var content = new ByteArrayContent(transaction);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/cbor");
            using var submitted = await _api.PostAsync("tx/submit", content, cancellationToken);
            var body = await submitted.Content.ReadAsStringAsync(cancellationToken);
            if (!submitted.IsSuccessStatusCode) throw new InvalidOperationException($"Cardano rejected the storage transaction ({(int)submitted.StatusCode}): {body}");
            if (!body.Contains(id, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Cardano acknowledged a different transaction id.");
            await WaitForConfirmationAsync(id, cancellationToken);
        }
        finally { _submission.Release(); }
    }

    private async Task WaitForConfirmationAsync(string id, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(120);
        while (true)
        {
            using var tx = await GetJsonAsync($"txs/{id}", cancellationToken, allowNotFound: true);
            if (tx != null) return;
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("The Cardano storage transaction was not confirmed in time.");
            await Task.Delay(500, cancellationToken);
        }
    }

    private async Task<JsonDocument> GetJsonAsync(string path, CancellationToken cancellationToken, bool allowNotFound = false)
    {
        using var response = await _api.GetAsync(path, cancellationToken);
        if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound) return null;
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Cardano API {path} returned {(int)response.StatusCode}: {text}");
        return JsonDocument.Parse(text);
    }

    public void Dispose() { _api.Dispose(); _kubo.Dispose(); _submission.Dispose(); }
}
