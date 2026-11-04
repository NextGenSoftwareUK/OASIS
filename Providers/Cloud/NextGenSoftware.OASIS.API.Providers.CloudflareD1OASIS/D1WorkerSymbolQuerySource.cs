using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.CloudflareD1OASIS;

/// <summary>Read-only CQL projection of an explicitly enrolled D1 Worker symbol table.</summary>
public sealed class D1WorkerSymbolQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string SymbolMetadata = "D1Symbol";
    private readonly SymbolBackend backend;
    private bool disposed;
    private HttpClient? ownedClient;

    public D1WorkerSymbolQuerySource(HttpClient http, Uri endpoint, Guid owner, IEnumerable<string> symbols)
        : this(new SymbolBackend(http, endpoint, owner, symbols)) { }

    private D1WorkerSymbolQuerySource(SymbolBackend backend) : base(backend, SymbolBackend.Prefix)
    {
        this.backend = backend;
        ProviderType = new EnumValue<ProviderType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Read-only D1 Worker symbol metadata";
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
    }

    public static D1WorkerSymbolQuerySource FromJson(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded D1 Worker symbol configuration required.", nameof(json));
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        var fields = new HashSet<string>(new[] { "schema", "endpoint", "sourceOwner", "symbols" }, StringComparer.Ordinal);
        if (root.ValueKind != JsonValueKind.Object ||
            root.EnumerateObject().Any(property => !fields.Remove(property.Name)) || fields.Count != 0 ||
            root.GetProperty("schema").GetInt32() != 1)
            throw new ArgumentException("Exact D1 Worker symbol configuration required.", nameof(json));

        var endpoint = new Uri(root.GetProperty("endpoint").GetString()!, UriKind.Absolute);
        if (endpoint.Scheme != Uri.UriSchemeHttps || endpoint.IsLoopback)
            throw new ArgumentException("Hosted D1 Worker symbols require a public HTTPS endpoint.", nameof(json));
        var owner = root.GetProperty("sourceOwner").GetGuid();
        var keys = root.GetProperty("symbols").EnumerateArray().Select(item => item.GetString()!).Take(9).ToArray();
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler());
        try
        {
            return new D1WorkerSymbolQuerySource(client, endpoint, owner, keys) { ownedClient = client };
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await backend.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "D1 Worker symbol activation failed.", Exception = error };
        }
    }

    public void Dispose()
    {
        disposed = true;
        IsProviderActivated = false;
        ownedClient?.Dispose();
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.CloudflareD1OASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; } =
        CosmicQueryCapabilities.ForStringMetadataKeyLookup(SymbolMetadata, 33, 8);

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
            return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit D1 Worker symbol source is required.");
        if (maximumResults is < 1 or > 10000)
            return Failure("CQL_QUERY_LIMIT", "A bounded result budget is required.");
        if (disposed || !IsProviderActivated)
            return Failure("CQL_PROVIDER_INACTIVE", "Activate the D1 Worker symbol profile first.");
        var admitted = QueryCapabilities.AdmitPlan(plan);
        if (admitted.IsError)
            return Failure(admitted.ErrorCode, admitted.Message);

        IReadOnlyList<string> keys;
        if (QueryCapabilities.StringKeySetLookup.IsKeySetQuery(plan.Query))
        {
            var selected = QueryCapabilities.StringKeySetLookup.SelectKeys(plan.Query);
            if (selected.IsError) return Failure(selected.ErrorCode, selected.Message);
            keys = selected.Result;
        }
        else
        {
            var selected = QueryCapabilities.StringPointLookup.SelectKey(plan.Query);
            if (selected.IsError) return Failure(selected.ErrorCode, selected.Message);
            keys = new[] { selected.Result };
        }
        if (keys.Any(key => !backend.Keys.Contains(key, StringComparer.Ordinal)))
            return Failure("CQL_INVALID_SOURCE_KEY", "Select symbols from the enrolled D1 Worker catalogue.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var symbols = await backend.ReadSymbolsAsync(keys, cancellationToken);
            var rows = symbols.Select(backend.MapSymbol)
                .Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? symbols.Count).ToArray();
            cancellationToken.ThrowIfCancellationRequested();
            return rows.Length > maximumResults
                ? Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.")
                : new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error)
        {
            return Failure("CQL_QUERY_CANCELLED", "D1 Worker symbol query cancelled.", error);
        }
        catch (Exception error)
        {
            return Failure("CQL_QUERY_FAILED", "D1 Worker symbol query failed.", error);
        }
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception? error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };
    private sealed class SymbolBackend : IKeyValueBackend
    {
        public const string Prefix = "d1-worker-symbols/";
        private readonly Guid owner;
        private readonly string sourceName;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> Keys { get; }
        private readonly HttpClient http;
        private readonly Uri endpoint;

        public SymbolBackend(HttpClient http, Uri endpoint, Guid owner, IEnumerable<string> symbols)
        {
            ArgumentNullException.ThrowIfNull(http);
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(symbols);
            if (!endpoint.IsAbsoluteUri || endpoint.AbsolutePath != "/symbols" ||
                (endpoint.Scheme != Uri.UriSchemeHttps && !(endpoint.Scheme == Uri.UriSchemeHttp && endpoint.IsLoopback)) ||
                endpoint.UserInfo.Length != 0 || endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0 || owner == Guid.Empty)
                throw new ArgumentException("An explicit D1 Worker SQL endpoint and owner are required.");
            var enrolled = symbols.Take(9).ToArray();
            if (enrolled.Length is < 1 or > 8 || enrolled.Distinct(StringComparer.Ordinal).Count() != enrolled.Length ||
                enrolled.Any(key => string.IsNullOrWhiteSpace(key) || key.Length > 33))
                throw new ArgumentException("One to eight distinct bounded symbols are required.", nameof(symbols));
            Keys = Array.AsReadOnly(enrolled);
            this.owner = owner;
            sourceName = $"d1-worker-symbols:{endpoint.AbsoluteUri}";
            this.http = http;
            this.endpoint = endpoint;
            records = Keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, Core.Enums.ProviderType.CloudflareD1OASIS, sourceName, key).ToString("N"),
                StringComparer.Ordinal);
        }

        public async Task<IReadOnlyList<string>> ReadSymbolsAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var token = deadline.Token;
            var uri = new UriBuilder(endpoint) { Query = string.Join("&", keys.Select(key => "symbol=" + Uri.EscapeDataString(key))) }.Uri;
            using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            var buffer = new byte[4097];
            var size = await stream.ReadAtLeastAsync(buffer, buffer.Length, false, token);
            if (size == buffer.Length) throw new InvalidDataException("D1 response exceeded the symbol query budget.");
            using var json = JsonDocument.Parse(buffer.AsMemory(0, size));
            var properties = json.RootElement.EnumerateObject().ToArray();
            if (properties.Length != 1 || properties[0].Name != "symbols")
                throw new InvalidDataException("D1 returned an unexpected symbol envelope.");
            var symbols = properties[0].Value.EnumerateArray().Select(row => row.GetString()).ToArray();
            if (!symbols.SequenceEqual(keys.OrderBy(key => key, StringComparer.Ordinal), StringComparer.Ordinal))
                throw new InvalidDataException("D1 returned unexpected or duplicate symbols.");
            return keys.ToArray();
        }

        public IHolon MapSymbol(string symbol)
        {
            var fields = new Dictionary<string, object> { ["name"] = symbol, [SymbolMetadata] = symbol };
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, Core.Enums.ProviderType.CloudflareD1OASIS,
                new NativeRecord(sourceName, symbol, fields), NativeDataMapping.ForSource(sourceName));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner;
            row.IsPublic = true;
            row.IsActive = true;
            row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[Core.Enums.ProviderType.CloudflareD1OASIS] = symbol;
            return row;
        }
        public async Task VerifyAsync(CancellationToken cancellationToken = default)
            => await ReadSymbolsAsync(Keys, cancellationToken);

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!records.TryGetValue(key, out var symbol)) return null!;
            var native = await ReadSymbolsAsync(new[] { symbol }, cancellationToken);
            return StoredRecordJson.Serialize<IHolon>(MapSymbol(native[0]));
        }

        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray());
        }

        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("D1 Worker symbols are read-only."));

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("D1 Worker symbols are read-only."));
    }
}
