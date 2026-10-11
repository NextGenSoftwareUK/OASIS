using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

/// <summary>Reads indexed TRC20/TRC721 history, not a current NFT inventory or proof of complete ledger coverage.</summary>
public sealed class TronGridTokenHistoryClient
{
    private readonly HttpClient _http;
    private readonly string _genesis;

    public TronGridTokenHistoryClient(HttpClient http, string expectedGenesisBlockId)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        if (_http.BaseAddress == null) throw new ArgumentException("An explicit TronGrid base URI is required.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(expectedGenesisBlockId ?? "", "\\A[0-9a-fA-F]{64}\\z"))
            throw new ArgumentException("Expected genesis block ID must contain 32 hexadecimal bytes.");
        _genesis = expectedGenesisBlockId;
    }

    public async Task<IReadOnlyList<JsonElement>> ReadAsync(string accountAddress, long maximumTimestamp,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(accountAddress) || maximumTimestamp < 0)
            throw new ArgumentException("Account address and nonnegative fixed maximum timestamp are required.");
        using (var request = new HttpRequestMessage(HttpMethod.Post, "wallet/getblockbynum"))
        {
            request.Content = new StringContent("{\"num\":0}", Encoding.UTF8, "application/json");
            using var response = await _http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var genesis = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!genesis.RootElement.TryGetProperty("blockID", out var id)
                || !string.Equals(id.GetString(), _genesis, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("TronGrid genesis differs from the configured SDK network.");
        }
        var records = new List<JsonElement>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string fingerprint = null;
        var path = "v1/accounts/" + Uri.EscapeDataString(accountAddress) + "/transactions/trc20"
            + "?only_confirmed=true&limit=200&order_by=block_timestamp,asc&min_timestamp=0&max_timestamp="
            + maximumTimestamp.ToString(CultureInfo.InvariantCulture);
        for (var page = 0; page < 5000; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var url = path + (fingerprint == null ? "" : "&fingerprint=" + Uri.EscapeDataString(fingerprint));
            using var response = await _http.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;
            if (!root.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True
                || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array
                || !root.TryGetProperty("meta", out var meta) || meta.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("TronGrid history returned an invalid or unsuccessful response.");
            if (data.GetArrayLength() > 200) throw new InvalidOperationException("TronGrid exceeded the requested page size.");
            foreach (var record in data.EnumerateArray())
            {
                if (record.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("TronGrid history returned a non-record item.");
                if (!record.TryGetProperty("block_timestamp", out var timestamp) || !timestamp.TryGetInt64(out var time)
                    || time < 0 || time > maximumTimestamp)
                    throw new InvalidOperationException("TronGrid history record falls outside the fixed time bounds.");
                records.Add(record.Clone());
            }
            fingerprint = meta.TryGetProperty("fingerprint", out var cursor) ? cursor.GetString() : null;
            if (string.IsNullOrEmpty(fingerprint)) return records;
            if (data.GetArrayLength() == 0 || !seen.Add(fingerprint))
                throw new InvalidOperationException("TronGrid history pagination did not advance.");
        }
        throw new InvalidOperationException("TronGrid history exceeds the explicit page budget; no truncated result is returned.");
    }
}
