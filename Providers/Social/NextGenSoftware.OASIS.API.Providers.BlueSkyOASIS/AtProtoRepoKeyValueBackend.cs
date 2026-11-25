using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.BlueSkyOASIS;

/// <summary>
/// Key-value storage in the logged-in account's AT Protocol repository: one record per key in the
/// <see cref="Collection"/> collection, written with <c>com.atproto.repo.putRecord</c>, read with <c>getRecord</c>,
/// removed with <c>deleteRecord</c> and enumerated with cursor-paginated <c>listRecords</c>.
/// Every record stores its original key; reads verify it so a record can never be returned for another key.
/// Repository records are public, so callers wrap this backend in <see cref="PrivateKeyValueBackend"/>.
/// </summary>
internal sealed partial class AtProtoRepoKeyValueBackend : IKeyValueBackend
{
    public const string Collection = "one.oasis.record";

    /// <summary>The PDS accepts putRecord bodies up to 1,000,000 bytes; larger writes are rejected here, explicitly.</summary>
    public const int MaxPutRecordBodyBytes = 1_000_000;

    private const int MaxRecordKeyLength = 512;
    private const int ListPageSize = 100;
    private readonly AtProtoSessionClient _client;

    public AtProtoRepoKeyValueBackend(AtProtoSessionClient client) => _client = client ?? throw new ArgumentNullException(nameof(client));

    [GeneratedRegex("^[A-Za-z0-9._:~-]+$")]
    private static partial Regex RecordKeyCharacters();

    /// <summary>
    /// Keys that are already valid record keys (and do not start with '~') are used verbatim; every other key maps to
    /// '~' + SHA-256 hex of its UTF-8 bytes. The two forms cannot collide because only hashed keys start with '~'.
    /// </summary>
    public static string RecordKeyFor(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length is > 0 and <= MaxRecordKeyLength && key is not "." and not ".." && key[0] != '~' && RecordKeyCharacters().IsMatch(key))
            return key;
        return "~" + Convert.ToHexString(SHA256.HashData(new UTF8Encoding(false, true).GetBytes(key))).ToLowerInvariant();
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        var did = await _client.GetDidAsync(cancellationToken);
        var session = await _client.QueryAsync("com.atproto.server.getSession", string.Empty, cancellationToken);
        if (!session.TryGetProperty("did", out var sessionDid) || sessionDid.GetString() != did)
            throw new InvalidDataException("The PDS session does not belong to the logged-in account.");
        await _client.QueryAsync("com.atproto.repo.listRecords", Query(did, null, 1), cancellationToken);
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var did = await _client.GetDidAsync(cancellationToken);
        var rkey = RecordKeyFor(key);
        JsonElement response;
        try
        {
            response = await _client.QueryAsync("com.atproto.repo.getRecord",
                $"repo={Uri.EscapeDataString(did)}&collection={Collection}&rkey={Uri.EscapeDataString(rkey)}", cancellationToken);
        }
        catch (AtProtoXrpcException missing) when (missing.Error == "RecordNotFound")
        {
            return null!;
        }
        if (!response.TryGetProperty("value", out var value))
            throw new InvalidDataException("PDS getRecord returned no record value.");
        var (storedKey, storedValue) = ParseRecord(value);
        if (!string.Equals(storedKey, key, StringComparison.Ordinal))
            throw new InvalidDataException("PDS record identity mismatch: the stored key differs from the requested key.");
        return storedValue;
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        var did = await _client.GetDidAsync(cancellationToken);
        var body = new JsonObject
        {
            ["repo"] = did,
            ["collection"] = Collection,
            ["rkey"] = RecordKeyFor(key),
            ["validate"] = false, // no published lexicon for the custom collection; the PDS stores it as-is
            ["record"] = new JsonObject { ["$type"] = Collection, ["key"] = key, ["value"] = value }
        };
        var size = Encoding.UTF8.GetByteCount(body.ToJsonString());
        if (size > MaxPutRecordBodyBytes)
            throw new InvalidDataException($"The record for this key needs a {size}-byte putRecord body; the PDS accepts at most {MaxPutRecordBodyBytes} bytes.");
        var written = await _client.ProcedureAsync("com.atproto.repo.putRecord", body, cancellationToken);
        if (!written.TryGetProperty("uri", out var uri) || !written.TryGetProperty("cid", out _)
            || uri.GetString() != $"at://{did}/{Collection}/{RecordKeyFor(key)}")
            throw new InvalidDataException("PDS putRecord did not acknowledge the expected record.");
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var did = await _client.GetDidAsync(cancellationToken);
        await _client.ProcedureAsync("com.atproto.repo.deleteRecord", new JsonObject
        {
            ["repo"] = did, ["collection"] = Collection, ["rkey"] = RecordKeyFor(key)
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        prefix ??= string.Empty;
        var did = await _client.GetDidAsync(cancellationToken);
        var keys = new SortedSet<string>(StringComparer.Ordinal);
        string? cursor = null;
        var seenCursors = new HashSet<string>(StringComparer.Ordinal);
        do
        {
            var page = await _client.QueryAsync("com.atproto.repo.listRecords", Query(did, cursor, ListPageSize), cancellationToken);
            if (!page.TryGetProperty("records", out var records) || records.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("PDS listRecords returned no records array.");
            foreach (var record in records.EnumerateArray())
            {
                var (key, _) = ParseRecord(record.GetProperty("value"));
                var expectedUri = $"at://{did}/{Collection}/{RecordKeyFor(key)}";
                if (record.GetProperty("uri").GetString() != expectedUri)
                    throw new InvalidDataException("PDS record identity mismatch: a listed record is stored under another record key.");
                if (key.StartsWith(prefix, StringComparison.Ordinal) && !keys.Add(key))
                    throw new InvalidDataException("PDS listRecords returned a duplicate key.");
            }
            cursor = page.TryGetProperty("cursor", out var next) && next.ValueKind == JsonValueKind.String ? next.GetString() : null;
            if (cursor != null && !seenCursors.Add(cursor))
                throw new InvalidDataException("PDS listRecords repeated a pagination cursor.");
        }
        while (!string.IsNullOrEmpty(cursor));
        return keys.ToList();
    }

    private static string Query(string did, string? cursor, int limit)
        => $"repo={Uri.EscapeDataString(did)}&collection={Collection}&limit={limit}"
           + (cursor == null ? string.Empty : $"&cursor={Uri.EscapeDataString(cursor)}");

    private static (string Key, string Value) ParseRecord(JsonElement record)
    {
        if (record.ValueKind != JsonValueKind.Object
            || !record.TryGetProperty("$type", out var type) || type.GetString() != Collection
            || !record.TryGetProperty("key", out var key) || key.ValueKind != JsonValueKind.String
            || !record.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.String)
            throw new InvalidDataException($"PDS record is not a {Collection} record with string key/value fields.");
        return (key.GetString()!, value.GetString()!);
    }
}
