using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PinotOASIS;

/// <summary>
/// Key-value storage on one append-only Pinot OFFLINE table {k, v, d, s, ts}. Every put or delete uploads a one-row
/// segment through the controller's ingestFromFile; the newest row per key wins (LASTWITHTIME over ts, which this writer
/// keeps strictly increasing). A write is acknowledged only when the controller accepts the segment and the broker
/// serves that row's sequence id.
/// </summary>
internal sealed class PinotKeyValueBackend : IKeyValueBackend, IDisposable
{
    private const int MaximumValueChars = 2 * 1024 * 1024;
    private static readonly TimeSpan WriteDeadline = TimeSpan.FromMinutes(2);
    private readonly HttpClient _broker;
    private readonly HttpClient _controller;
    private readonly string _table;
    private readonly SemaphoreSlim _writes = new(1, 1);
    private long _lastTs;

    public PinotKeyValueBackend(string brokerUrl, string controllerUrl, string table)
    {
        _broker = new HttpClient { BaseAddress = new Uri((brokerUrl ?? throw new ArgumentNullException(nameof(brokerUrl))).TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(60) };
        _controller = new HttpClient { BaseAddress = new Uri((controllerUrl ?? throw new ArgumentNullException(nameof(controllerUrl))).TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(120) };
        _table = string.IsNullOrWhiteSpace(table) || !table.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')
            ? throw new ArgumentException("A Pinot table name of letters, digits and underscores is required.", nameof(table)) : table;
    }

    /// <summary>Creates the schema and OFFLINE table when missing, then proves the broker answers.</summary>
    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        using var existing = await _controller.GetAsync($"tables/{_table}", cancellationToken);
        if (existing.StatusCode != HttpStatusCode.NotFound) existing.EnsureSuccessStatusCode();
        if (existing.StatusCode == HttpStatusCode.NotFound || (await existing.Content.ReadAsStringAsync(cancellationToken)).Trim() is "{}" or "")
        {
            static JsonObject Dimension(string name, int maxLength) => new() { ["name"] = name, ["dataType"] = "STRING", ["maxLength"] = maxLength };
            var schema = new JsonObject
            {
                ["schemaName"] = _table,
                ["dimensionFieldSpecs"] = new JsonArray(Dimension("k", 4096), Dimension("v", MaximumValueChars), Dimension("d", 1), Dimension("s", 32)),
                ["dateTimeFieldSpecs"] = new JsonArray(new JsonObject { ["name"] = "ts", ["dataType"] = "LONG", ["format"] = "1:MILLISECONDS:EPOCH", ["granularity"] = "1:MILLISECONDS" })
            };
            await PostControllerAsync("schemas", schema, cancellationToken);
            var table = new JsonObject
            {
                ["tableName"] = _table, ["tableType"] = "OFFLINE",
                ["segmentsConfig"] = new JsonObject { ["schemaName"] = _table, ["timeColumnName"] = "ts", ["replication"] = "1" },
                ["tableIndexConfig"] = new JsonObject { ["noDictionaryColumns"] = new JsonArray("v") },
                ["tenants"] = new JsonObject(), ["metadata"] = new JsonObject()
            };
            await PostControllerAsync("tables", table, cancellationToken);
        }
        using var rows = await QueryAsync($"SELECT COUNT(*) FROM {_table}", cancellationToken);
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
        if (value.Length > MaximumValueChars) throw new InvalidDataException("The Pinot record exceeds the value size limit.");
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
        prefix ??= string.Empty;
        using var rows = await QueryAsync(
            $"SELECT k, LASTWITHTIME(d, ts, 'STRING') FROM {_table} WHERE REGEXP_LIKE(k, {Literal("^" + Regex.Escape(prefix))}) GROUP BY k LIMIT 1000000",
            cancellationToken);
        return Rows(rows).Where(r => r[1].GetString() == "0").Select(r => r[0].GetString()!)
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).OrderBy(k => k, StringComparer.Ordinal).ToArray();
    }

    private async Task<(string Value, bool Deleted, string Sequence)?> LatestAsync(string key, CancellationToken cancellationToken)
    {
        using var rows = await QueryAsync(
            $"SELECT LASTWITHTIME(v, ts, 'STRING'), LASTWITHTIME(d, ts, 'STRING'), LASTWITHTIME(s, ts, 'STRING'), COUNT(*) FROM {_table} WHERE k = {Literal(key)}",
            cancellationToken);
        var row = Rows(rows).FirstOrDefault();
        if (row == null || row[3].GetInt64() == 0) return null;
        return (row[0].GetString() ?? string.Empty, row[1].GetString() == "1", row[2].GetString());
    }

    private async Task AppendAsync(string key, string value, bool deleted, CancellationToken cancellationToken)
    {
        await _writes.WaitAsync(cancellationToken);
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _lastTs = now > _lastTs ? now : _lastTs + 1; // LASTWITHTIME needs strictly increasing ts
            var sequence = Guid.NewGuid().ToString("N");
            var row = new JsonObject { ["k"] = key, ["v"] = value, ["d"] = deleted ? "1" : "0", ["s"] = sequence, ["ts"] = _lastTs };
            using var form = new MultipartFormDataContent { { new StringContent(row.ToJsonString(), Encoding.UTF8), "file", "row.json" } };
            var batch = Uri.EscapeDataString("{\"inputFormat\":\"json\"}");
            using var response = await _controller.PostAsync($"ingestFromFile?tableNameWithType={_table}_OFFLINE&batchConfigMapStr={batch}", form, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Pinot rejected the segment upload ({(int)response.StatusCode}): {body}");

            var deadline = DateTime.UtcNow + WriteDeadline;
            while ((await LatestAsync(key, cancellationToken))?.Sequence != sequence)
            {
                if (DateTime.UtcNow > deadline) throw new TimeoutException("Pinot did not serve the uploaded segment in time.");
                await Task.Delay(250, cancellationToken);
            }
        }
        finally { _writes.Release(); }
    }

    private async Task<JsonDocument> QueryAsync(string sql, CancellationToken cancellationToken)
    {
        using var response = await _broker.PostAsync("query/sql", new StringContent(new JsonObject { ["sql"] = sql }.ToJsonString(), Encoding.UTF8, "application/json"), cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Pinot query returned {(int)response.StatusCode}: {text}");
        var doc = JsonDocument.Parse(text);
        if (doc.RootElement.TryGetProperty("exceptions", out var errors) && errors.GetArrayLength() > 0)
        {
            var message = errors.GetRawText();
            doc.Dispose();
            throw new IOException($"Pinot query failed: {message}");
        }
        return doc;
    }

    private static IEnumerable<JsonElement[]> Rows(JsonDocument doc)
        => doc.RootElement.GetProperty("resultTable").GetProperty("rows").EnumerateArray().Select(r => r.EnumerateArray().ToArray());

    /// <summary>Pinot SQL string literal: single quotes doubled.</summary>
    private static string Literal(string value) => "'" + value.Replace("'", "''") + "'";

    private async Task PostControllerAsync(string path, JsonObject body, CancellationToken cancellationToken)
    {
        using var response = await _controller.PostAsync(path, new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"), cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Conflict)
            throw new HttpRequestException($"Pinot controller {path} returned {(int)response.StatusCode}: {text}");
    }

    public void Dispose() { _broker.Dispose(); _controller.Dispose(); _writes.Dispose(); }
}
