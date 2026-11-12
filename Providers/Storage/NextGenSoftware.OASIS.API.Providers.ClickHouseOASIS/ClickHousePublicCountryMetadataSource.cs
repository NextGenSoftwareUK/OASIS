using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS;

/// <summary>Explicit public ClickHouse country-summary projection; never owned database or native storage.</summary>
public sealed class ClickHousePublicCountryMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "ClickHouseCountryName";
    private readonly CountriesBackend countries;
    private bool disposed;

    public ClickHousePublicCountryMetadataSource(HttpClient client, Uri rest,
        IEnumerable<string> keys, Guid sourceOwner, bool ownsClient = false, TimeProvider timeProvider = null)
        : this(new CountriesBackend(client, rest, keys, sourceOwner, ownsClient, timeProvider)) { }

    private ClickHousePublicCountryMetadataSource(CountriesBackend backend) : base(backend, backend.Prefix)
    {
        countries = backend;
        ProviderName = PType.ClickHouseOASIS.ToString();
        ProviderDescription = "Read-only public ClickHouse country summaries";
        ProviderType = new EnumValue<PType>(PType.ClickHouseOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 128, 8);
    }

    public PType QueryProviderType => PType.ClickHouseOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    // Reject mutations before accessing native data, including unknown IDs.
    private static OASISResult<T> ReadOnly<T>() => new()
    {
        IsError = true, ErrorCount = 1, ErrorCode = "CQL_READ_ONLY_SOURCE",
        Message = "ClickHouse public countries cannot be saved or deleted."
    };

    public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon,
        bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
        bool continueOnError = true, bool saveChildrenOnProvider = false)
        => Task.FromResult(ReadOnly<IHolon>());

    public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
        bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        => Task.FromResult(ReadOnly<IEnumerable<IHolon>>());

    public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        => Task.FromResult(ReadOnly<IHolon>());

    public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        => Task.FromResult(ReadOnly<IHolon>());

    /// <summary>Separate host query-profile configuration, never the existing
    /// owned database/ADO settings. Reject unknown fields rather than ignore keys.</summary>
    public static ClickHousePublicCountryMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit public ClickHouse source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 5 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "apiUrl", "countries", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("countries").ValueKind != JsonValueKind.Array || root.GetProperty("countries").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous ClickHouse countries source schema required.");
        var keys = root.GetProperty("countries").EnumerateArray().Select(item => item.GetString()).ToArray();
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new ClickHousePublicCountryMetadataSource(client, new Uri(root.GetProperty("apiUrl").GetString(), UriKind.Absolute),
                keys, root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await countries.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public ClickHouse country-summary source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public ClickHouse source is required.");
            if (maximumResults is < 1 or > 10000) return Failure("CQL_QUERY_LIMIT", "A bounded result budget is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Failure(admitted.ErrorCode, admitted.Message);
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
            if (keys.Any(key => !countries.Keys.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select country names from the configured catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await countries.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "ClickHouse query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public ClickHouse country-summary query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; countries.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    private static void Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!seen.Add(property.Name)) throw new InvalidDataException("Duplicate native/configuration property.");
                Unique(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var entry in value.EnumerateArray()) Unique(entry);
    }

    private sealed class RpcRateGate
    {
        private readonly SemaphoreSlim mutex = new(1, 1);
        private readonly TimeProvider clock;
        private long? previous;

        public RpcRateGate(TimeProvider clock) => this.clock = clock;

        public async Task EnterAsync(CancellationToken token)
        {
            await mutex.WaitAsync(token);
            try
            {
                if (previous.HasValue)
                {
                    var remaining = TimeSpan.FromSeconds(1) - clock.GetElapsedTime(previous.Value);
                    if (remaining > TimeSpan.Zero)
                        await Task.Delay(remaining, clock, token);
                }
                token.ThrowIfCancellationRequested();
                previous = clock.GetTimestamp();
            }
            catch { mutex.Release(); throw; }
        }

        public void Exit() => mutex.Release();
    }

    private sealed class CountriesBackend : IKeyValueBackend, IDisposable
    {
        private readonly HttpClient client;
        private readonly Uri api;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly RpcRateGate rate;
        private static readonly RpcRateGate PublicRate = new(TimeProvider.System);
        private readonly Dictionary<string, string> records;
        private readonly string sourceName;
        public IReadOnlyList<string> Keys { get; }
        public string Prefix => "clickhouse-public-countries/";

        public CountriesBackend(HttpClient client, Uri api, IEnumerable<string> keys,
            Guid owner, bool ownsClient, TimeProvider clock)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (api == null || api.Scheme != Uri.UriSchemeHttps || api.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(api.UserInfo) || !string.IsNullOrEmpty(api.Query) || !string.IsNullOrEmpty(api.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty)
                throw new ArgumentException("Explicit public HTTPS ClickHouse source required.");
            var selected = keys?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(keys));
            if (selected.Length is < 1 or > 8 || selected.Any(key => string.IsNullOrEmpty(key) || key.Length > 128 || key.Any(c => c < 33 || c > 126)) ||
                selected.Distinct(StringComparer.Ordinal).Count() != selected.Length)
                throw new ArgumentException("One to eight unique printable ASCII country keys without spaces are required by the enrolled key strategy.");
            this.client = client; this.api = api; this.owner = owner; this.ownsClient = ownsClient;
            Keys = Array.AsReadOnly(selected);
            rate = clock == null ? PublicRate : new RpcRateGate(clock);
            sourceName = "clickhouse-public-countries:" + api.AbsoluteUri + ":blogs.countries";
            records = selected.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.ClickHouseOASIS, sourceName, key).ToString("N"), key => key);
        }

        // HTTP String parameters use ClickHouse's escaped representation. Values
        // never enter SQL text; only the bounded placeholder count is generated.
        private static string Parameter(string value) => value.Replace("\\", "\\\\");

        private async Task<JsonDocument> QueryAsync(string sql, IReadOnlyList<string> keys, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            token = deadline.Token;
            await rate.EnterAsync(token);
            try
            {
                var query = "?user=explorer";
                for (int i = 0; i < keys.Count; i++)
                    query += "&param_c" + i.ToString(CultureInfo.InvariantCulture) + "=" + Uri.EscapeDataString(Parameter(keys[i]));
                using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(api, query))
                { Content = new StringContent(sql, Encoding.UTF8, "text/plain") };
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();
                if (response.Headers.Contains("X-ClickHouse-Exception-Code")) throw new InvalidDataException("Native query exception.");
                await using var stream = await response.Content.ReadAsStreamAsync(token);
                using var body = new MemoryStream(); var buffer = new byte[8192]; int count;
                while ((count = await stream.ReadAsync(buffer.AsMemory(), token)) != 0)
                {
                    if (body.Length + count > 262144) throw new InvalidDataException("Native response exceeds256KiB.");
                    body.Write(buffer, 0, count);
                }
                var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 12 });
                try
                {
                    var root = document.RootElement; Unique(root);
                    if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("exception", out _) ||
                        !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array ||
                        !root.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Number ||
                        rows.GetInt32() != data.GetArrayLength()) throw new InvalidDataException("Successful complete native JSON required.");
                    token.ThrowIfCancellationRequested();
                    return document;
                }
                catch { document.Dispose(); throw; }
            }
            finally { rate.Exit(); }
        }

        private async Task IdentityAsync(CancellationToken token)
        {
            using var identity = await QueryAsync("SELECT version() AS version, currentUser() AS user, toString(getSetting('readonly')) AS readonly FORMAT JSON", Array.Empty<string>(), token);
            var rows = identity.RootElement.GetProperty("data");
            if (rows.GetArrayLength() != 1 || rows[0].GetProperty("user").GetString() != "explorer" ||
                rows[0].GetProperty("readonly").GetString() is not ("1" or "2") ||
                string.IsNullOrEmpty(rows[0].GetProperty("version").GetString()))
                throw new InvalidDataException("Public readonly ClickHouse user required.");
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            await IdentityAsync(token);
            var placeholders = string.Join(",", Enumerable.Range(0, keys.Count).Select(i => "{c" + i.ToString(CultureInfo.InvariantCulture) + ":String}"));
            using var document = await QueryAsync("SELECT name AS country,toString(length(coordinates)) AS polygons FROM blogs.countries WHERE name IN (" +
                placeholders + ") ORDER BY name LIMIT 9 FORMAT JSON", keys, token);
            var data = document.RootElement.GetProperty("data");
            if (data.GetArrayLength() != keys.Count) throw new InvalidDataException("Complete configured country selection required.");
            var found = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var item in data.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object || item.EnumerateObject().Count() != 2)
                    throw new InvalidDataException("Exact public country projection required.");
                string key = item.GetProperty("country").GetString();
                string count = item.GetProperty("polygons").GetString();
                if (key == null || !keys.Contains(key, StringComparer.Ordinal) ||
                    !ulong.TryParse(count, NumberStyles.None, CultureInfo.InvariantCulture, out ulong polygons) || polygons == 0 ||
                    polygons.ToString(CultureInfo.InvariantCulture) != count || !found.TryAdd(key, count))
                    throw new InvalidDataException("Unexpected country identity or noncanonical UInt64 count.");
            }
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                var fields = new Dictionary<string, object>
                {
                    ["name"] = key, ["description"] = "Public ClickHouse country polygon summary; raw geometry excluded.",
                    [AddressMetadata] = key, ["ClickHousePolygonCount"] = found[key],
                    ["ClickHouseTable"] = "blogs.countries", ["ClickHouseObservationPolicy"] = "operator-reported-public-country-summary"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.ClickHouseOASIS,
                    new NativeRecord(sourceName, key, fields), NativeDataMapping.ForSource(sourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result; row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true;
                row.Children = Array.Empty<IHolon>(); row.ProviderUniqueStorageKey[PType.ClickHouseOASIS] = key; rows.Add(row);
            }
            token.ThrowIfCancellationRequested(); return rows.AsReadOnly();
        }

        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            using var schema = await QueryAsync("SELECT name,type FROM system.columns WHERE database='blogs' AND table='countries' ORDER BY position LIMIT 3 FORMAT JSON", Array.Empty<string>(), cancellationToken);
            var rows = schema.RootElement.GetProperty("data");
            if (rows.GetArrayLength() != 2 || rows[0].GetProperty("name").GetString() != "name" || rows[0].GetProperty("type").GetString() != "String" ||
                rows[1].GetProperty("name").GetString() != "coordinates" || rows[1].GetProperty("type").GetString() != "MultiPolygon")
                throw new InvalidDataException("Qualified country table schema differs.");
            await ReadAsync(Keys, cancellationToken);
        }
        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!records.TryGetValue(key, out var native)) return null;
            return StoredRecordJson.Serialize<IHolon>((await ReadAsync(new[] { native }, cancellationToken))[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public country summaries are read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public country summaries are read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
