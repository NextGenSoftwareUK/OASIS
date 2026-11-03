using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;

/// <summary>
/// Explicit hosted public CQL representation over existing canonical LocalFile
/// records. Register only in the query-source registry, never as writable authority.
/// The ordinary LocalFile writer and its storage format remain unchanged.
/// </summary>
public sealed class LocalFilePublicCqlProfile : LocalFileOASIS,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly string holons;
    private readonly Guid sourceOwner;
    private readonly IReadOnlyDictionary<Guid, string> records;
    public string SourceDirectory { get; }
    public Guid SourceOwner => sourceOwner;
    public IReadOnlyDictionary<Guid, string> RecordHashes => records;

    public LocalFilePublicCqlProfile(string directory, Guid sourceOwner,
        IReadOnlyDictionary<Guid, string> recordHashes) : base(storageDirectory: RequireDirectory(directory))
    {
        if (sourceOwner == Guid.Empty || recordHashes == null || recordHashes.Count is < 1 or > 100
            || recordHashes.Any(pair => pair.Key == Guid.Empty || pair.Value == null
                || pair.Value.Length != 64 || pair.Value.Any(c => !Uri.IsHexDigit(c))))
            throw new ArgumentException("Explicit owner and bounded canonical-ID/hash catalogue required.");
        this.sourceOwner = sourceOwner;
        records = new ReadOnlyDictionary<Guid, string>(recordHashes.ToDictionary(pair => pair.Key, pair => pair.Value.ToLowerInvariant()));
        SourceDirectory = Path.GetFullPath(directory);
        holons = Path.Combine(SourceDirectory, "Holons");
    }

    /// <summary>Metadata-only host configuration. Parsing never touches source files.</summary>
    public static LocalFilePublicCqlProfile FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 32768)
            throw new ArgumentException("Bounded public LocalFile configuration required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        Fields(root, "schema", "directory", "sourceOwner", "records");
        if (root.GetProperty("schema").ValueKind != JsonValueKind.Number
            || !root.GetProperty("schema").TryGetInt32(out var schema) || schema != 1
            || root.GetProperty("directory").ValueKind != JsonValueKind.String
            || !GuidValue(root.GetProperty("sourceOwner"), out var owner)
            || root.GetProperty("records").ValueKind != JsonValueKind.Array
            || root.GetProperty("records").GetArrayLength() is < 1 or > 100)
            throw new ArgumentException("Exact public LocalFile schema1 required.");
        var hashes = new Dictionary<Guid, string>();
        foreach (var record in root.GetProperty("records").EnumerateArray())
        {
            Fields(record, "id", "sha256");
            if (!GuidValue(record.GetProperty("id"), out var id)
                || record.GetProperty("sha256").ValueKind != JsonValueKind.String
                || !hashes.TryAdd(id, record.GetProperty("sha256").GetString()!))
                throw new ArgumentException("Unique canonical source records required.");
        }
        return new(root.GetProperty("directory").GetString()!, owner, hashes);
    }

    private static bool GuidValue(JsonElement value, out Guid id)
    {
        id = Guid.Empty;
        return value.ValueKind == JsonValueKind.String
            && Guid.TryParseExact(value.GetString(), "D", out id) && id != Guid.Empty;
    }

    private static void Fields(JsonElement value, params string[] expected)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Source configuration objects required.");
        var names = value.EnumerateObject().Select(property => property.Name).ToArray();
        if (names.Length != expected.Length || names.Distinct(StringComparer.Ordinal).Count() != names.Length
            || names.Any(name => !expected.Contains(name, StringComparer.Ordinal)))
            throw new ArgumentException("Exact unique configuration fields required;unknown credentials/options rejected.");
    }

    private static string RequireDirectory(string directory)
        => !string.IsNullOrWhiteSpace(directory) && Path.IsPathFullyQualified(directory)
            ? directory : throw new ArgumentException("Explicit absolute native source directory required.");

    private static OASISResult<T> Failure<T>(string code, string message, Exception? error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(holons)) throw new DirectoryNotFoundException();
            long total = 0;
            foreach (var expected in records)
            {
                // Health verification opens only declared GUID filenames. No
                // directory creation/enumeration, graph expansion or native write.
                using var file = new FileStream(Path.Combine(holons, expected.Key.ToString("D") + ".json"),
                    FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous);
                using var bytes = new MemoryStream();
                var buffer = new byte[8192];
                int count;
                while ((count = await file.ReadAsync(buffer, cancellationToken)) != 0)
                {
                    total = checked(total + count);
                    if (total > 4 * 1024 * 1024) throw new InvalidDataException("Source health budget exceeded.");
                    bytes.Write(buffer, 0, count);
                }
                var raw = bytes.ToArray();
                if (!string.Equals(Convert.ToHexString(SHA256.HashData(raw)), expected.Value, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Configured source content differs.");
                var row = OasisJson.DeserializeStored<Holon>(new UTF8Encoding(false, true).GetString(raw));
                if (row == null || row.Id != expected.Key || !PublicRow(row))
                    throw new InvalidDataException("Configured record is not the declared public source.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (OperationCanceledException error) when (cancellationToken.IsCancellationRequested)
        { return Failure<bool>("CQL_QUERY_CANCELLED", "Public LocalFile activation cancelled.", error); }
        catch (Exception error)
        { return Failure<bool>("CQL_SOURCE_UNAVAILABLE", "Configured public LocalFile activation failed.", error); }
    }

    private bool PublicRow(IHolon row) => row.IsPublic && row.CreatedByAvatarId == sourceOwner;

    public new async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
            return Failure<IReadOnlyList<IHolon>>("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit public LocalFile target required.");
        var keys = LocalFileCqlCompiler.Compile(plan.Query, QueryProviderType);
        if (keys.IsError) return Failure<IReadOnlyList<IHolon>>(keys.ErrorCode, keys.Message);
        if (keys.Result.Any(id => !records.ContainsKey(id)))
            return Failure<IReadOnlyList<IHolon>>("CQL_INVALID_SOURCE_KEY", "Select only configured public source identities.");

        // Use the existing native filename, bounded stream and canonical decoder.
        // The server catalogue cannot turn a subsequently private/foreign row
        // into public data. Fail the whole response instead of returning a subset.
        var result = await base.ExecuteCosmicQueryAsync(plan, maximumResults, cancellationToken);
        if (!result.IsError && (result.Result == null || result.Result.Any(row => !PublicRow(row))))
            return Failure<IReadOnlyList<IHolon>>("CQL_SOURCE_VISIBILITY_CHANGED", "Configured source no longer contains only declared public rows.");
        return result;
    }

    public void Dispose() => IsProviderActivated = false;
}
