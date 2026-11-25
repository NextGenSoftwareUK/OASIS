using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS;

/// <summary>
/// Append-only key-value storage on Arweave. Every Put is a new transaction signed by the configured wallet, tagged
/// <c>App-Name: OASIS-KV</c>, <c>OASIS-Key: key</c> and <c>OASIS-Seq: n</c> (strictly increasing for this writer).
/// A Delete appends a tombstone transaction (<c>OASIS-Deleted: 1</c>) for a key that currently exists.
/// Reads are owner-pinned to the configured wallet (a copied tag set from another wallet is never seen) and the
/// revision with the highest OASIS-Seq wins; a winning tombstone means the key is absent, so a later Put revives it.
/// </summary>
internal sealed class ArweaveKeyValueBackend : IKeyValueBackend
{
    internal const string AppName = "OASIS-KV";
    private const string KeyTag = "OASIS-Key", SeqTag = "OASIS-Seq", DeletedTag = "OASIS-Deleted";
    private static readonly Regex TransactionId = new("^[A-Za-z0-9_-]{43}$", RegexOptions.CultureInvariant);
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly IArweaveService _service;
    private readonly SemaphoreSlim _writes = new(1, 1);
    private long _lastSeq = -1;

    public ArweaveKeyValueBackend(IArweaveService service) => _service = service ?? throw new ArgumentNullException(nameof(service));

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        await _service.ProbeAsync();
        await _service.QueryOwnedTaggedAsync(new() { ["App-Name"] = AppName, [KeyTag] = "oasis/verify" });
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var latest = Latest(await _service.QueryOwnedTaggedAsync(new() { ["App-Name"] = AppName, [KeyTag] = key }));
        if (!latest.TryGetValue(key, out var head) || head.Deleted) return null;
        cancellationToken.ThrowIfCancellationRequested();
        return StrictUtf8.GetString(await _service.GetTransactionDataAsync(head.Id));
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var latest = Latest(await _service.QueryOwnedTaggedAsync(new() { ["App-Name"] = AppName }));
        return latest.Where(pair => !pair.Value.Deleted && pair.Key.StartsWith(prefix ?? string.Empty, StringComparison.Ordinal))
            .Select(pair => pair.Key).OrderBy(key => key, StringComparer.Ordinal).ToArray();
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        await AppendAsync(key, StrictUtf8.GetBytes(value), deleted: false, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var latest = Latest(await _service.QueryOwnedTaggedAsync(new() { ["App-Name"] = AppName, [KeyTag] = key }));
        if (!latest.TryGetValue(key, out var head) || head.Deleted) return; // deleting a missing key succeeds
        await AppendAsync(key, new[] { (byte)'0' }, deleted: true, cancellationToken);
    }

    private async Task AppendAsync(string key, byte[] data, bool deleted, CancellationToken cancellationToken)
    {
        await _writes.WaitAsync(cancellationToken);
        try
        {
            // The first write continues after the highest sequence this wallet has already published,
            // so revisions from earlier processes can never outrank a new one.
            if (_lastSeq < 0)
                _lastSeq = Latest(await _service.QueryOwnedTaggedAsync(new() { ["App-Name"] = AppName }))
                    .Values.Select(head => head.Seq).DefaultIfEmpty(0).Max();
            var seq = Math.Max(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), _lastSeq + 1);
            var tags = new Dictionary<string, string>
            {
                ["App-Name"] = AppName, [KeyTag] = key, [SeqTag] = seq.ToString(CultureInfo.InvariantCulture)
            };
            if (deleted) tags[DeletedTag] = "1";
            var id = await _service.PostTransactionAsync(data, "application/octet-stream", tags);
            if (string.IsNullOrEmpty(id) || !TransactionId.IsMatch(id))
                throw new InvalidDataException("Arweave post did not acknowledge a valid transaction identity.");
            _lastSeq = seq;
        }
        finally { _writes.Release(); }
    }

    private readonly record struct Head(string Id, long Seq, bool Deleted);

    private static Dictionary<string, Head> Latest(IEnumerable<ArweaveTaggedTransaction> transactions)
    {
        var heads = new Dictionary<string, Head>(StringComparer.Ordinal);
        foreach (var transaction in transactions)
        {
            if (!transaction.Tags.TryGetValue("App-Name", out var app) || app != AppName ||
                !transaction.Tags.TryGetValue(KeyTag, out var key) ||
                !transaction.Tags.TryGetValue(SeqTag, out var seqText) ||
                !long.TryParse(seqText, NumberStyles.None, CultureInfo.InvariantCulture, out var seq))
                throw new InvalidDataException("An owner-pinned Arweave storage transaction has malformed storage tags.");
            var deleted = transaction.Tags.TryGetValue(DeletedTag, out var flag)
                ? flag == "1" ? true : throw new InvalidDataException("An Arweave storage tombstone tag is malformed.")
                : false;
            if (heads.TryGetValue(key, out var current))
            {
                if (current.Seq == seq)
                    throw new InvalidDataException("Two Arweave storage revisions of one key share a sequence number.");
                if (current.Seq > seq) continue;
            }
            heads[key] = new Head(transaction.Id, seq, deleted);
        }
        return heads;
    }
}
