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

namespace NextGenSoftware.OASIS.API.Providers.DruidOASIS;

/// <summary>
/// Key-value storage on one append-only Druid datasource. Every put or delete appends a row {__time, k, v, d, s}
/// through a native batch task with appendToExisting=true; the newest row per key wins (LATEST_BY over __time, which this
/// writer keeps strictly increasing). A write is acknowledged only when its task reports SUCCESS and a SQL read on the
/// broker returns that row's sequence id.
/// </summary>
internal sealed class DruidKeyValueBackend : IKeyValueBackend, IDisposable
{
    private const int MaximumValueBytes = 4 * 1024 * 1024;
    private static readonly TimeSpan WriteDeadline = TimeSpan.FromMinutes(5);
    private readonly HttpClient _http;
    private readonly string _datasource;
    private readonly SemaphoreSlim _writes = new(1, 1);
    private DateTime _lastTime = DateTime.MinValue;

    public DruidKeyValueBackend(string routerUrl, string datasource)
    {
        _http = new HttpClient { BaseAddress = new Uri((routerUrl ?? throw new ArgumentNullException(nameof(routerUrl))).TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(60) };
        _datasource = string.IsNullOrWhiteSpace(datasource) || !datasource.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')
            ? throw new ArgumentException("A Druid datasource name of letters, digits and underscores is required.", nameof(datasource)) : datasource;
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        using var rows = await SqlAsync("SELECT 1 AS ok", Array.Empty<string>(), cancellationToken);
        if (rows == null || rows.RootElement.GetArrayLength() != 1) throw new IOException("The Druid broker did not answer SELECT 1.");
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var latest = await LatestAsync(key, cancellationToken);
        return latest is { Deleted: false } ? latest.Value.Value : null;
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        if (Encoding.UTF8.GetByteCount(value) > MaximumValueBytes) throw new InvalidDataException("The Druid record exceeds the value size limit.");
        await AppendAsync(key, value, deleted: false, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var latest = await LatestAsync(key, cancellationToken);
        if (latest is null or { Deleted: true }) return; // deleting a missing key succeeds
        await AppendAsync(key, string.Empty, deleted: true, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        using var rows = await SqlAsync(
            $"SELECT k FROM (SELECT k, LATEST_BY(d, __time, 8) AS d FROM \"{_datasource}\" WHERE k LIKE ? ESCAPE '\\' GROUP BY k) WHERE d = '0'",
            new[] { LikePrefix(prefix ?? string.Empty) }, cancellationToken);
        if (rows == null) return Array.Empty<string>();
        return rows.RootElement.EnumerateArray().Select(r => r.GetProperty("k").GetString()!)
            .Where(k => k.StartsWith(prefix ?? string.Empty, StringComparison.Ordinal)).OrderBy(k => k, StringComparer.Ordinal).ToArray();
    }

    private async Task<(string Value, bool Deleted, string Sequence)?> LatestAsync(string key, CancellationToken cancellationToken)
    {
        using var rows = await SqlAsync(
            $"SELECT LATEST_BY(v, __time, {MaximumValueBytes}) AS v, LATEST_BY(d, __time, 8) AS d, LATEST_BY(s, __time, 64) AS s, COUNT(*) AS n FROM \"{_datasource}\" WHERE k = ?",
            new[] { key }, cancellationToken);
        if (rows == null) return null;
        var row = rows.RootElement[0];
        if (row.GetProperty("n").GetInt64() == 0) return null;
        return (row.GetProperty("v").GetString() ?? string.Empty, row.GetProperty("d").GetString() == "1", row.GetProperty("s").GetString());
    }

    private async Task AppendAsync(string key, string value, bool deleted, CancellationToken cancellationToken)
    {
        await _writes.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            _lastTime = now > _lastTime ? now : _lastTime.AddMilliseconds(1); // LATEST_BY needs strictly increasing __time
            var sequence = Guid.NewGuid().ToString("N");
            var row = new JsonObject
            {
                ["ts"] = _lastTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), ["k"] = key, ["v"] = value, ["d"] = deleted ? "1" : "0", ["s"] = sequence
            };
            var spec = new JsonObject
            {
                ["type"] = "index_parallel",
                ["spec"] = new JsonObject
                {
                    ["dataSchema"] = new JsonObject
                    {
                        ["dataSource"] = _datasource,
                        ["timestampSpec"] = new JsonObject { ["column"] = "ts", ["format"] = "iso" },
                        ["dimensionsSpec"] = new JsonObject { ["dimensions"] = new JsonArray("k", "v", "d", "s") },
                        ["granularitySpec"] = new JsonObject { ["segmentGranularity"] = "day", ["queryGranularity"] = "none", ["rollup"] = false }
                    },
                    ["ioConfig"] = new JsonObject
                    {
                        ["type"] = "index_parallel",
                        ["inputSource"] = new JsonObject { ["type"] = "inline", ["data"] = row.ToJsonString() },
                        ["inputFormat"] = new JsonObject { ["type"] = "json" },
                        ["appendToExisting"] = true
                    },
                    ["tuningConfig"] = new JsonObject { ["type"] = "index_parallel", ["maxNumConcurrentSubTasks"] = 1 }
                }
            };
            using var submitted = await _http.PostAsync("druid/indexer/v1/task", Json(spec), cancellationToken);
            var body = await submitted.Content.ReadAsStringAsync(cancellationToken);
            if (!submitted.IsSuccessStatusCode) throw new HttpRequestException($"Druid rejected the ingestion task ({(int)submitted.StatusCode}): {body}");
            using var receipt = JsonDocument.Parse(body);
            var taskId = receipt.RootElement.GetProperty("task").GetString()!;

            var deadline = DateTime.UtcNow + WriteDeadline;
            while (true)
            {
                using var status = JsonDocument.Parse(await _http.GetStringAsync($"druid/indexer/v1/task/{Uri.EscapeDataString(taskId)}/status", cancellationToken));
                var state = status.RootElement.GetProperty("status").GetProperty("status").GetString();
                if (state == "SUCCESS") break;
                if (state == "FAILED")
                    throw new IOException($"Druid ingestion task {taskId} failed: {status.RootElement.GetProperty("status").GetRawText()}");
                if (DateTime.UtcNow > deadline) throw new TimeoutException($"Druid ingestion task {taskId} did not finish in time.");
                await Task.Delay(500, cancellationToken);
            }
            // The task is done once its segment is published; the write counts only once the broker serves it.
            while ((await LatestAsync(key, cancellationToken))?.Sequence != sequence)
            {
                if (DateTime.UtcNow > deadline) throw new TimeoutException($"Druid did not serve the segment for task {taskId} in time.");
                await Task.Delay(500, cancellationToken);
            }
        }
        finally { _writes.Release(); }
    }

    /// <summary>Runs parameterised SQL; returns null only when the datasource does not exist yet (nothing written).</summary>
    private async Task<JsonDocument> SqlAsync(string sql, IReadOnlyList<string> parameters, CancellationToken cancellationToken)
    {
        var request = new JsonObject
        {
            ["query"] = sql, ["resultFormat"] = "object",
            ["parameters"] = new JsonArray(parameters.Select(p => (JsonNode)new JsonObject { ["type"] = "VARCHAR", ["value"] = p }).ToArray())
        };
        using var response = await _http.PostAsync("druid/v2/sql", Json(request), cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode) return JsonDocument.Parse(text);
        if (text.Contains($"Object '{_datasource}' not found", StringComparison.Ordinal)) return null;
        throw new HttpRequestException($"Druid SQL returned {(int)response.StatusCode}: {text}");
    }

    private static string LikePrefix(string prefix)
        => prefix.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

    private static StringContent Json(JsonNode node) => new(node.ToJsonString(), Encoding.UTF8, "application/json");

    public void Dispose() { _http.Dispose(); _writes.Dispose(); }
}
