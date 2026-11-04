using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Explicit public teaching-data provisioning, never user storage or a
/// recovery fallback. A partial original attempt requires operator reconciliation.</summary>
internal sealed class LocalFilePublicBootstrap : IHostedService
{
    private readonly LocalFilePublicCqlProfile source;
    private readonly (Guid Id, byte[] Bytes)[] records;
    private readonly string bundleHash;
    private LocalFilePublicBootstrap(LocalFilePublicCqlProfile source, (Guid Id, byte[] Bytes)[] records)
    {
        this.source = source;
        this.records = records;
        var identity = source.SourceDirectory + "\n" + source.SourceOwner.ToString("D") + "\n"
            + string.Join("\n", source.RecordHashes.OrderBy(pair => pair.Key)
                .Select(pair => pair.Key.ToString("D") + ":" + pair.Value));
        bundleHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))).ToLowerInvariant();
    }

    internal static LocalFilePublicBootstrap FromJson(LocalFilePublicCqlProfile source, string json)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (json == null || Encoding.UTF8.GetByteCount(json) > 131072)
            throw new ArgumentException("Bounded explicit public LocalFile bootstrap required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        Fields(root, "schema", "records");
        if (root.GetProperty("schema").ValueKind != JsonValueKind.Number
            || !root.GetProperty("schema").TryGetInt32(out var version) || version != 1
            || root.GetProperty("records").ValueKind != JsonValueKind.Array
            || root.GetProperty("records").GetArrayLength() != source.RecordHashes.Count)
            throw new ArgumentException("Bootstrap must match the entire declared source.");
        var rows = new List<(Guid Id, byte[] Bytes)>();
        var seen = new HashSet<Guid>();
        foreach (var item in root.GetProperty("records").EnumerateArray())
        {
            Fields(item, "id", "sha256", "contentBase64");
            if (item.GetProperty("id").ValueKind != JsonValueKind.String
                || !Guid.TryParseExact(item.GetProperty("id").GetString(), "D", out var id)
                || !seen.Add(id) || !source.RecordHashes.TryGetValue(id, out var expected)
                || item.GetProperty("sha256").ValueKind != JsonValueKind.String
                || !string.Equals(item.GetProperty("sha256").GetString(), expected, StringComparison.OrdinalIgnoreCase)
                || item.GetProperty("contentBase64").ValueKind != JsonValueKind.String)
                throw new ArgumentException("Bootstrap identity/hash differs from the source catalogue.");
            var encoded = item.GetProperty("contentBase64").GetString();
            var bytes = Convert.FromBase64String(encoded);
            if (bytes.Length is < 1 or > 32768 || Convert.ToBase64String(bytes) != encoded
                || !string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), expected, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Bootstrap content encoding/hash differs.");
            var row = OasisJson.DeserializeStored<Holon>(new UTF8Encoding(false, true).GetString(bytes));
            if (row == null || row.Id != id || !row.IsPublic || row.IsDeleted
                || row.CreatedByAvatarId != source.SourceOwner || row.Children?.Any() == true)
                throw new ArgumentException("Only the declared live public flat records may be provisioned.");
            rows.Add((id, bytes));
        }
        return new(source, rows.ToArray());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = source.SourceDirectory;
        var parent = Path.GetDirectoryName(directory);
        if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
            throw new DirectoryNotFoundException("Explicit existing source parent/volume required.");
        var lockPath = Path.Combine(parent, "." + Path.GetFileName(directory) + ".cql-bootstrap.lock");
        using var lease = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var intent = Path.Combine(directory, "cql-bootstrap.intent.json");
        var ack = Path.Combine(directory, "cql-bootstrap.acknowledged.json");
        if (Directory.Exists(directory))
        {
            if (!System.IO.File.Exists(intent) || !System.IO.File.Exists(ack))
                throw new InvalidDataException("Original source directory requires reconciliation;no bootstrap replay.");
            await VerifyManifest(intent, cancellationToken);
            await VerifyManifest(ack, cancellationToken);
            foreach (var record in records)
                await RequireBytes(Path.Combine(directory, "Holons", record.Id.ToString("D") + ".json"), record.Bytes, cancellationToken);
            return; // Verified acknowledged reuse performs no dataset writes.
        }

        Directory.CreateDirectory(directory);
        var manifest = JsonSerializer.SerializeToUtf8Bytes(new { schema = 1, bundleSha256 = bundleHash });
        await WriteNew(intent, manifest, cancellationToken);
        Directory.CreateDirectory(Path.Combine(directory, "Holons"));
        foreach (var record in records)
            await WriteNew(Path.Combine(directory, "Holons", record.Id.ToString("D") + ".json"), record.Bytes, cancellationToken);
        await WriteNew(ack, manifest, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task VerifyManifest(string path, CancellationToken token)
    {
        var expected = JsonSerializer.SerializeToUtf8Bytes(new { schema = 1, bundleSha256 = bundleHash });
        await RequireBytes(path, expected, token);
    }

    private static async Task RequireBytes(string path, byte[] expected, CancellationToken token)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous);
        // Read at most the admitted length plus one. A replaced huge file cannot
        // turn verification into an unbounded allocation or directory scan.
        var bytes = new byte[expected.Length + 1];
        int total = 0, count;
        while (total < bytes.Length && (count = await file.ReadAsync(bytes.AsMemory(total), token)) != 0)
            total += count;
        if (total != expected.Length || !bytes.AsSpan(0, total).SequenceEqual(expected))
            throw new InvalidDataException("Acknowledged source bytes differ;never overwrite or reseed.");
    }

    private static async Task WriteNew(string path, byte[] bytes, CancellationToken token)
    {
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous);
        await file.WriteAsync(bytes, token);
        file.Flush(flushToDisk: true);
    }

    private static void Fields(JsonElement value, params string[] expected)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new ArgumentException("Exact bootstrap objects required.");
        var names = value.EnumerateObject().Select(property => property.Name).ToArray();
        if (names.Length != expected.Length || names.Distinct(StringComparer.Ordinal).Count() != names.Length
            || names.Any(name => !expected.Contains(name, StringComparer.Ordinal)))
            throw new ArgumentException("Exact unique bootstrap fields required.");
    }
}
