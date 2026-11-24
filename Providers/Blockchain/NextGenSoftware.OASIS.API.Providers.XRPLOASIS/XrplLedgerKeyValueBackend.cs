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

namespace NextGenSoftware.OASIS.API.Providers.XRPLOASIS;

/// <summary>
/// Key-value storage on the XRP Ledger. Each value is an (already encrypted) envelope held by a Kubo node; the ledger is
/// the authority: every put or delete is a signed, validated <c>AccountSet</c> whose memo records the key and the
/// envelope's CID and SHA-256 (or a deletion). The newest validated entry for a key wins.
/// </summary>
internal sealed class XrplLedgerKeyValueBackend : IKeyValueBackend, IDisposable
{
    private const string MemoTypeText = "oasis-kv/v1";
    private static readonly byte[] MemoType = Encoding.UTF8.GetBytes(MemoTypeText);
    private const int MaximumEnvelopeBytes = 4 * 1024 * 1024;

    private readonly HttpClient _rpc;
    private readonly HttpClient _kubo;
    private readonly XrplEd25519Signer _signer;
    private readonly SemaphoreSlim _submission = new(1, 1);

    public XrplLedgerKeyValueBackend(string rpcEndpoint, string privateKeyHex, string kuboApi)
    {
        _signer = new XrplEd25519Signer(privateKeyHex);
        _rpc = new HttpClient { BaseAddress = new Uri(rpcEndpoint ?? throw new ArgumentNullException(nameof(rpcEndpoint))), Timeout = TimeSpan.FromSeconds(30) };
        _kubo = new HttpClient { BaseAddress = new Uri((kuboApi ?? throw new ArgumentNullException(nameof(kuboApi))).TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(60) };
    }

    public string Address => _signer.Address;

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        var info = await RpcAsync("server_info", new JsonObject(), cancellationToken);
        if (!info.TryGetProperty("info", out _))
            throw new InvalidDataException("XRPL server_info returned no server state.");
        await AccountSequenceAsync(cancellationToken);
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
    {
        var index = await ReadIndexAsync(cancellationToken);
        return index.Where(pair => !pair.Value.Deleted && pair.Key.StartsWith(prefix ?? string.Empty, StringComparison.Ordinal))
            .Select(pair => pair.Key).OrderBy(key => key, StringComparer.Ordinal).ToArray();
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        var bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length > MaximumEnvelopeBytes)
            throw new InvalidDataException("The XRPL record envelope exceeds the blob size limit.");
        using var form = new MultipartFormDataContent { { new ByteArrayContent(bytes), "file", "record" } };
        using var added = await _kubo.PostAsync("api/v0/add?cid-version=0&pin=true", form, cancellationToken);
        added.EnsureSuccessStatusCode();
        using var receipt = JsonDocument.Parse(await added.Content.ReadAsStringAsync(cancellationToken));
        var cid = receipt.RootElement.GetProperty("Hash").GetString();
        if (string.IsNullOrEmpty(cid)) throw new InvalidDataException("Kubo did not return the record CID.");
        var memo = new JsonObject { ["k"] = key, ["c"] = cid, ["h"] = Convert.ToHexString(SHA256.HashData(bytes)) };
        await SubmitMemoAsync(memo, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var index = await ReadIndexAsync(cancellationToken);
        if (!index.TryGetValue(key, out var entry) || entry.Deleted) return; // deleting a missing key succeeds
        await SubmitMemoAsync(new JsonObject { ["k"] = key, ["d"] = true }, cancellationToken);
        // The ledger now records the deletion; release the envelope so its content leaves the record store.
        using var unpinned = await _kubo.PostAsync($"api/v0/pin/rm?arg={Uri.EscapeDataString(entry.Cid)}&recursive=true", null, cancellationToken);
        if (!unpinned.IsSuccessStatusCode)
        {
            var body = await unpinned.Content.ReadAsStringAsync(cancellationToken);
            if (!body.Contains("not pinned", StringComparison.OrdinalIgnoreCase)) unpinned.EnsureSuccessStatusCode();
        }
    }

    private sealed record IndexEntry(string Cid, string Sha256, bool Deleted);

    private async Task<string> ReadEnvelopeAsync(IndexEntry entry, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v0/cat?arg={Uri.EscapeDataString(entry.Cid)}&length={MaximumEnvelopeBytes + 1}");
        using var response = await _kubo.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length > MaximumEnvelopeBytes || !string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), entry.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new CryptographicException("The XRPL record envelope does not match its ledger commitment.");
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>Replays the account's validated OASIS memos in ledger order; the newest entry for each key wins.</summary>
    private async Task<Dictionary<string, IndexEntry>> ReadIndexAsync(CancellationToken cancellationToken)
    {
        var index = new Dictionary<string, IndexEntry>(StringComparer.Ordinal);
        JsonNode marker = null;
        var expectedMemoType = Convert.ToHexString(MemoType);
        do
        {
            var parameters = new JsonObject
            {
                ["account"] = _signer.Address, ["ledger_index_min"] = -1, ["ledger_index_max"] = -1,
                ["forward"] = true, ["limit"] = 400, ["api_version"] = 1
            };
            if (marker != null) parameters["marker"] = marker.DeepClone();
            var page = await RpcAsync("account_tx", parameters, cancellationToken);
            foreach (var item in page.GetProperty("transactions").EnumerateArray())
            {
                if (!item.TryGetProperty("validated", out var validated) || validated.ValueKind != JsonValueKind.True) continue;
                var tx = item.GetProperty("tx");
                if (tx.GetProperty("TransactionType").GetString() != "AccountSet" || tx.GetProperty("Account").GetString() != _signer.Address) continue;
                if (item.GetProperty("meta").GetProperty("TransactionResult").GetString() != "tesSUCCESS") continue;
                if (!tx.TryGetProperty("Memos", out var memos) || memos.GetArrayLength() != 1) continue;
                var memo = memos[0].GetProperty("Memo");
                if (!string.Equals(memo.GetProperty("MemoType").GetString(), expectedMemoType, StringComparison.OrdinalIgnoreCase)) continue;
                using var data = JsonDocument.Parse(Convert.FromHexString(memo.GetProperty("MemoData").GetString()));
                var root = data.RootElement;
                var key = root.GetProperty("k").GetString();
                index[key] = root.TryGetProperty("d", out var deleted) && deleted.ValueKind == JsonValueKind.True
                    ? new IndexEntry(null, null, true)
                    : new IndexEntry(root.GetProperty("c").GetString(), root.GetProperty("h").GetString(), false);
            }
            marker = page.TryGetProperty("marker", out var next) ? JsonNode.Parse(next.GetRawText()) : null;
        } while (marker != null);
        return index;
    }

    private async Task SubmitMemoAsync(JsonObject memo, CancellationToken cancellationToken)
    {
        await _submission.WaitAsync(cancellationToken);
        try
        {
            var sequence = await AccountSequenceAsync(cancellationToken);
            var current = (await RpcAsync("ledger_current", new JsonObject(), cancellationToken)).GetProperty("ledger_current_index").GetUInt32();
            var fees = await RpcAsync("fee", new JsonObject(), cancellationToken);
            var fee = Math.Max(12UL, ulong.Parse(fees.GetProperty("drops").GetProperty("open_ledger_fee").GetString()!));
            var lastLedger = current + 20;
            var (blob, hash) = _signer.SignAccountSetWithMemo(sequence, lastLedger, fee, MemoType, Encoding.UTF8.GetBytes(memo.ToJsonString()));
            var submitted = await RpcAsync("submit", new JsonObject { ["tx_blob"] = blob }, cancellationToken);
            var engine = submitted.GetProperty("engine_result").GetString();
            if (engine != "tesSUCCESS" && engine != "terQUEUED")
                throw new InvalidOperationException($"XRPL rejected the storage transaction ({engine}).");
            await WaitForValidationAsync(hash, lastLedger, cancellationToken);
        }
        finally { _submission.Release(); }
    }

    /// <summary>A write is acknowledged only once its transaction is in a validated ledger with tesSUCCESS.</summary>
    private async Task WaitForValidationAsync(string hash, uint lastLedger, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await RpcRawAsync("tx", new JsonObject { ["transaction"] = hash, ["api_version"] = 1 }, cancellationToken);
            var result = response.GetProperty("result");
            if (result.TryGetProperty("validated", out var validated) && validated.ValueKind == JsonValueKind.True)
            {
                var outcome = result.GetProperty("meta").GetProperty("TransactionResult").GetString();
                if (outcome != "tesSUCCESS") throw new InvalidOperationException($"XRPL storage transaction failed ({outcome}).");
                return;
            }
            var validatedLedger = (await RpcAsync("ledger", new JsonObject { ["ledger_index"] = "validated" }, cancellationToken))
                .GetProperty("ledger_index").GetUInt32();
            if (validatedLedger > lastLedger)
                throw new InvalidOperationException("The XRPL storage transaction expired before validation.");
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException("The XRPL storage transaction was not validated in time.");
            await Task.Delay(250, cancellationToken);
        }
    }

    private async Task<uint> AccountSequenceAsync(CancellationToken cancellationToken)
    {
        var info = await RpcAsync("account_info", new JsonObject { ["account"] = _signer.Address, ["ledger_index"] = "current" }, cancellationToken);
        return info.GetProperty("account_data").GetProperty("Sequence").GetUInt32();
    }

    private async Task<JsonElement> RpcAsync(string method, JsonObject parameters, CancellationToken cancellationToken)
    {
        var result = (await RpcRawAsync(method, parameters, cancellationToken)).GetProperty("result");
        if (result.TryGetProperty("status", out var status) && status.GetString() == "error")
            throw new InvalidOperationException($"XRPL {method} failed: {(result.TryGetProperty("error", out var error) ? error.GetString() : "unknown error")}.");
        return result;
    }

    private async Task<JsonElement> RpcRawAsync(string method, JsonObject parameters, CancellationToken cancellationToken)
    {
        var body = new JsonObject { ["method"] = method, ["params"] = new JsonArray(parameters) }.ToJsonString();
        using var response = await _rpc.PostAsync("", new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.Clone();
    }

    public void Dispose()
    {
        _rpc.Dispose();
        _kubo.Dispose();
        _submission.Dispose();
    }
}
