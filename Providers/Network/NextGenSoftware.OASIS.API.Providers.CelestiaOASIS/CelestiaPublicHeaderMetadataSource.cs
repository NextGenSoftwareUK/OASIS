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
using System.Text.RegularExpressions;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.CelestiaOASIS;

/// <summary>
/// Anonymous mainnet extended-header observations over a bounded height catalogue.
/// Pins node-reported block IDs; never submits blobs or claims independent consensus.
/// </summary>
public sealed class CelestiaPublicHeaderMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "CelestiaHeight";
    private readonly HeadersBackend headers;
    private bool disposed;

    public CelestiaPublicHeaderMetadataSource(HttpClient client, Uri rest,
        IReadOnlyDictionary<string, string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new HeadersBackend(client, rest, addresses, sourceOwner, ownsClient)) { }

    private CelestiaPublicHeaderMetadataSource(HeadersBackend backend) : base(backend, backend.Prefix)
    {
        headers = backend;
        ProviderName = PType.CelestiaOASIS.ToString();
        ProviderDescription = "Read-only pinned Celestia mainnet extended-header observations";
        ProviderType = new EnumValue<PType>(PType.CelestiaOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 20, 8);
    }

    public PType QueryProviderType => PType.CelestiaOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Explicit read profile, separate from node authentication/blob settings.</summary>
    public static CelestiaPublicHeaderMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Celestia source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        Unique(root);
        var fields = new[] { "schema", "rpcUrl", "chainId", "headers", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(p => !fields.Contains(p.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("rpcUrl").ValueKind != JsonValueKind.String ||
            root.GetProperty("chainId").ValueKind != JsonValueKind.String || root.GetProperty("chainId").GetString() != "celestia" ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String ||
            root.GetProperty("headers").ValueKind != JsonValueKind.Object ||
            root.GetProperty("headers").EnumerateObject().Count() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous mainnet Celestia source schema required.");
        var catalogue = root.GetProperty("headers").EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString(), StringComparer.Ordinal);
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new CelestiaPublicHeaderMetadataSource(client,
                new Uri(root.GetProperty("rpcUrl").GetString(), UriKind.Absolute), catalogue,
                root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await headers.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Celestia header source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Celestia source is required.");
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
            if (keys.Any(key => !headers.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select heights from the configured header catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await headers.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Celestia query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Celestia header query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; headers.Dispose();
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



    private static ulong Height(string text)
    {
        if (text == null || text.Length > 20 ||
            !ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out ulong height) ||
            height == 0 || height.ToString(CultureInfo.InvariantCulture) != text)
            throw new ArgumentException("Canonical positive uint64 height text required.");
        return height;
    }

    private static string Hash(JsonElement value) => Hash(value.GetString());
    private static string Hash(string text)
    {
        if (text == null || !Regex.IsMatch(text, @"\A[0-9A-F]{64}\z"))
            throw new InvalidDataException("Canonical uppercase32byte node-reported hash required.");
        return text;
    }

    private static Dictionary<string, object> Decode(JsonElement root, string height, string pinnedHash)
    {
        var header = root.GetProperty("header");
        if (header.GetProperty("chain_id").GetString() != "celestia" ||
            header.GetProperty("height").GetString() != height ||
            root.GetProperty("commit").GetProperty("height").GetString() != height)
            throw new InvalidDataException("Exact mainnet header/commit height required.");
        string blockHash = Hash(root.GetProperty("commit").GetProperty("block_id").GetProperty("hash"));
        if (blockHash != pinnedHash) throw new InvalidDataException("Configured reported block identity changed.");
        string time = header.GetProperty("time").GetString();
        if (time == null || time.Length > 40 || !time.EndsWith("Z", StringComparison.Ordinal) ||
            !DateTimeOffset.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
            throw new InvalidDataException("Bounded UTC header timestamp required.");
        var dah = root.GetProperty("dah");
        var rows = dah.GetProperty("row_roots");
        var columns = dah.GetProperty("column_roots");
        if (rows.ValueKind != JsonValueKind.Array || columns.ValueKind != JsonValueKind.Array ||
            rows.GetArrayLength() is < 2 or > 512 || rows.GetArrayLength() != columns.GetArrayLength() ||
            (rows.GetArrayLength() & (rows.GetArrayLength() - 1)) != 0)
            throw new InvalidDataException("Bounded square extended data-root dimensions required.");
        foreach (var entry in rows.EnumerateArray().Concat(columns.EnumerateArray()))
        {
            string text = entry.GetString();
            if (text == null || text.Length > 172) throw new InvalidDataException("Bounded namespaced root required.");
            var bytes = Convert.FromBase64String(text);
            if (bytes.Length is < 1 or > 128 || Convert.ToBase64String(bytes) != text)
                throw new InvalidDataException("Canonical bounded base64 root required.");
        }
        return new Dictionary<string, object>
        {
            [AddressMetadata] = height, ["CelestiaChainId"] = "celestia", ["CelestiaBlockHash"] = blockHash,
            ["CelestiaDataHash"] = Hash(header.GetProperty("data_hash")),
            ["CelestiaPreviousBlockHash"] = Hash(header.GetProperty("last_block_id").GetProperty("hash")),
            ["CelestiaTimestamp"] = time,
            ["CelestiaExtendedSquareWidth"] = rows.GetArrayLength().ToString(CultureInfo.InvariantCulture),
            ["CelestiaObservationPolicy"] = "node-reported-pinned-header",
            ["CelestiaConsensusVerified"] = "false", ["CelestiaDataAvailabilitySampled"] = "false",
            ["name"] = height, ["description"] = $"Celestia mainnet header {height} ({blockHash})"
        };
    }

    private sealed class HeadersBackend : IKeyValueBackend, IDisposable
    {
        private const string SourceName = "celestia-public-pinned-headers";
        private readonly HttpClient client;
        private readonly Uri api;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> catalogue;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "celestia-public-metadata/";

        public HeadersBackend(HttpClient client, Uri api, IReadOnlyDictionary<string, string> headers, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (api == null || api.Scheme != Uri.UriSchemeHttps || api.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(api.UserInfo) || !string.IsNullOrEmpty(api.Query) || !string.IsNullOrEmpty(api.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || client.DefaultRequestHeaders.Contains("Cookie") || owner == Guid.Empty)
                throw new ArgumentException("Explicit anonymous HTTPS mainnet header source required.");
            if (headers == null || headers.Count is < 1 or > 8)
                throw new ArgumentException("One to eight unique pinned headers required.");
            catalogue = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in headers) { Height(pair.Key); Hash(pair.Value); catalogue.Add(pair.Key, pair.Value); }
            this.client = client; this.api = api; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(catalogue.Keys.ToArray());
            records = Addresses.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.CelestiaOASIS, SourceName, key).ToString("N"), key => key);
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            var rows = new List<IHolon>();
            long totalBytes = 0;
            foreach (string height in keys)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, api)
                {
                    Content = System.Net.Http.Json.JsonContent.Create(new
                    {
                        jsonrpc = "2.0", id = 1, method = "header.GetByHeight", @params = new[] { Height(height) }
                    })
                };
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
                using var body = new MemoryStream(); var buffer = new byte[8192]; int count;
                while ((count = await stream.ReadAsync(buffer.AsMemory(), deadline.Token)) != 0)
                {
                    totalBytes += count;
                    if (body.Length + count > 1048576 || totalBytes > 4194304)
                        throw new InvalidDataException("Native response byte budget exceeded.");
                    body.Write(buffer, 0, count);
                }
                using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
                var root = document.RootElement;
                Unique(root);
                if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("error", out _) ||
                    root.GetProperty("jsonrpc").GetString() != "2.0" || root.GetProperty("id").GetInt32() != 1)
                    throw new InvalidDataException("Exact successful JSON-RPC response required.");
                var fields = Decode(root.GetProperty("result"), height, catalogue[height]);
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.CelestiaOASIS,
                    new NativeRecord(SourceName, height, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.CelestiaOASIS] = height;
                rows.Add(row);
            }
            deadline.Token.ThrowIfCancellationRequested();
            return Array.AsReadOnly(rows.ToArray());
        }

        public async Task VerifyAsync(CancellationToken token = default)
        {
            var rows = await ReadAsync(Addresses, token);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured headers are missing.");
        }
        public async Task<string> GetAsync(string key, CancellationToken token = default)
        {
            if (!records.TryGetValue(key, out var height)) return null;
            var rows = await ReadAsync(new[] { height }, token);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Celestia headers are read-only."));
        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Celestia headers are read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
