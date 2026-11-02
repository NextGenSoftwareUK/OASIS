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

namespace NextGenSoftware.OASIS.API.Providers.MatrixOASIS;

/// <summary>
/// Anonymous room-alias directory observations over an explicit bounded catalogue.
/// Never logs in, joins rooms, reads messages or follows returned server addresses.
/// </summary>
public sealed class MatrixPublicRoomMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "MatrixRoomAlias";
    private readonly RoomsBackend rooms;
    private bool disposed;

    public MatrixPublicRoomMetadataSource(HttpClient client, Uri rest,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new RoomsBackend(client, rest, addresses, sourceOwner, ownsClient)) { }

    private MatrixPublicRoomMetadataSource(RoomsBackend backend) : base(backend, backend.Prefix)
    {
        rooms = backend;
        ProviderName = PType.MatrixOASIS.ToString();
        ProviderDescription = "Anonymous public Matrix room-alias observations";
        ProviderType = new EnumValue<PType>(PType.MatrixOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 255, 8);
    }

    public PType QueryProviderType => PType.MatrixOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// authenticated Matrix storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static MatrixPublicRoomMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Matrix public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "apiUrl", "aliases", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("apiUrl").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("aliases").ValueKind != JsonValueKind.Array ||
            root.GetProperty("aliases").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Matrix source schema required; signer/billing fields forbidden.");
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new MatrixPublicRoomMetadataSource(client, new Uri(root.GetProperty("apiUrl").GetString(), UriKind.Absolute),
                root.GetProperty("aliases").EnumerateArray().Select(value => value.GetString()),
                root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await rooms.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Matrix room-alias source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Matrix source is required.");
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
            if (keys.Any(key => !rooms.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select aliases from the configured alias catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await rooms.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Matrix query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Matrix room-alias query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; rooms.Dispose();
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



    private static void Alias(string value)
    {
        if (value == null || value.Length > 255 ||
            !Regex.IsMatch(value, @"\A#[A-Za-z0-9._=+\-]+:[a-z0-9.\-]+\z") ||
            Uri.CheckHostName(value[(value.IndexOf(':') + 1)..]) != UriHostNameType.Dns)
            throw new ArgumentException("Bounded ASCII alias with a DNS homeserver required.");
    }

    private static string Text(JsonElement value, int maximumBytes)
    {
        if (value.ValueKind != JsonValueKind.String) throw new InvalidDataException("Native string required.");
        string text = value.GetString();
        if (Encoding.UTF8.GetByteCount(text) > maximumBytes) throw new InvalidDataException("Native text exceeds its bound.");
        return text;
    }

    private sealed class RoomsBackend : IKeyValueBackend, IDisposable
    {
        private const string SourceName = "matrix-public-room-aliases";
        private readonly HttpClient client;
        private readonly Uri api;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "matrix-public-metadata/";

        public RoomsBackend(HttpClient client, Uri api, IEnumerable<string> aliases, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (api == null || api.Scheme != Uri.UriSchemeHttps || api.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(api.UserInfo) || !string.IsNullOrEmpty(api.Query) || !string.IsNullOrEmpty(api.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || client.DefaultRequestHeaders.Contains("Cookie") ||
                owner == Guid.Empty)
                throw new ArgumentException("Explicit anonymous HTTPS directory source required.");
            var keys = aliases?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(aliases));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique room aliases required.");
            foreach (string key in keys) Alias(key);
            this.client = client; this.api = api; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.MatrixOASIS, SourceName, key).ToString("N"), key => key);
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            var rows = new List<IHolon>();
            long totalBytes = 0;
            foreach (string alias in keys)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    new Uri(api, "_matrix/client/v3/directory/room/" + Uri.EscapeDataString(alias)));
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
                using var body = new MemoryStream(); var buffer = new byte[8192]; int count;
                while ((count = await stream.ReadAsync(buffer.AsMemory(), deadline.Token)) != 0)
                {
                    totalBytes += count;
                    if (body.Length + count > 65536 || totalBytes > 262144)
                        throw new InvalidDataException("Native response byte budget exceeded.");
                    body.Write(buffer, 0, count);
                }
                using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 8 });
                var root = document.RootElement;
                Unique(root);
                if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("errcode", out _))
                    throw new InvalidDataException("Complete room-alias response required.");
                string roomId = Text(root.GetProperty("room_id"), 255);
                // Matrix v12 room IDs omit the domain. Preserve IDs as opaque
                // identifiers, not network locations or parseable server names.
                if (!roomId.StartsWith('!') || roomId.Length < 2 || roomId.Any(c => c <= 32 || c > 126))
                    throw new InvalidDataException("Opaque visible ASCII room ID required.");
                var servers = root.GetProperty("servers");
                if (servers.ValueKind != JsonValueKind.Array || servers.GetArrayLength() is < 1 or > 4096)
                    throw new InvalidDataException("Bounded complete routing server list required.");
                foreach (var server in servers.EnumerateArray())
                {
                    string text = Text(server, 255);
                    if (text.Length == 0 || text.Any(c => c <= 32 || c > 126))
                        throw new InvalidDataException("Routing server text required.");
                }
                // Retain public routing data as JSON text, never fetch it. The
                // response binds the room to the requested alias, not a snapshot.
                var fields = new Dictionary<string, object> {
                    [AddressMetadata] = alias, ["MatrixRoomId"] = roomId,
                    ["MatrixServersJson"] = servers.GetRawText(),
                    ["MatrixObservationPolicy"] = "server-reported-alias-resolution",
                    ["name"] = alias, ["description"] = "Public Matrix room alias resolves to " + roomId
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.MatrixOASIS,
                    new NativeRecord(SourceName, alias, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.MatrixOASIS] = alias;
                rows.Add(row);
            }
            deadline.Token.ThrowIfCancellationRequested();
            return Array.AsReadOnly(rows.ToArray());
        }

        public async Task VerifyAsync(CancellationToken token = default)
        {
            var rows = await ReadAsync(Addresses, token);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured aliases are missing.");
        }
        public async Task<string> GetAsync(string key, CancellationToken token = default)
        {
            if (!records.TryGetValue(key, out var did)) return null;
            var rows = await ReadAsync(new[] { did }, token);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Matrix room-alias metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Matrix room-alias metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
