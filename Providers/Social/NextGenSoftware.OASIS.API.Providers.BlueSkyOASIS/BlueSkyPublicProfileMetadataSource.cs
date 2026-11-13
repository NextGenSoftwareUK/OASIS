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

namespace NextGenSoftware.OASIS.API.Providers.BlueSkyOASIS;

/// <summary>
/// Anonymous cached public AppView profile observations. Uses explicit stable
/// DIDs and never creates PDS sessions, signs writes or fetches remote images.
/// </summary>
public sealed class BlueSkyPublicProfileMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "BlueSkyActorDid";
    private readonly ProfilesBackend profiles;
    private bool disposed;

    public BlueSkyPublicProfileMetadataSource(HttpClient client, Uri rest,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new ProfilesBackend(client, rest, addresses, sourceOwner, ownsClient)) { }

    private BlueSkyPublicProfileMetadataSource(ProfilesBackend backend) : base(backend, backend.Prefix)
    {
        profiles = backend;
        ProviderName = PType.BlueSkyOASIS.ToString();
        ProviderDescription = "Anonymous cached public BlueSky profile observations";
        ProviderType = new EnumValue<PType>(PType.BlueSkyOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 32, 8);
    }

    public PType QueryProviderType => PType.BlueSkyOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed BlueSky storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static BlueSkyPublicProfileMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit BlueSky public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "apiUrl", "actors", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("apiUrl").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("actors").ValueKind != JsonValueKind.Array ||
            root.GetProperty("actors").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous BlueSky source schema required; signer/billing fields forbidden.");
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new BlueSkyPublicProfileMetadataSource(client, new Uri(root.GetProperty("apiUrl").GetString(), UriKind.Absolute),
                root.GetProperty("actors").EnumerateArray().Select(value => value.GetString()),
                root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await profiles.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public BlueSky profile source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public BlueSky source is required.");
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
            if (keys.Any(key => !profiles.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select addresses from the configured DID catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await profiles.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "BlueSky query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public BlueSky profile query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; profiles.Dispose();
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



    private static void Did(string value)
    {
        if (value == null || !Regex.IsMatch(value, "\\Adid:plc:[a-z2-7]{24}\\z"))
            throw new ArgumentException("Canonical configured did:plc identifier required.");
    }

    private static string Text(JsonElement value, int maximumBytes)
    {
        if (value.ValueKind != JsonValueKind.String) throw new InvalidDataException("Native string required.");
        string text = value.GetString();
        if (Encoding.UTF8.GetByteCount(text) > maximumBytes) throw new InvalidDataException("Native text exceeds its bound.");
        return text;
    }

    private sealed class ProfilesBackend : IKeyValueBackend, IDisposable
    {
        private const string SourceName = "bluesky-public-profiles";
        private readonly HttpClient client;
        private readonly Uri api;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "bluesky-public-metadata/";

        public ProfilesBackend(HttpClient client, Uri api, IEnumerable<string> actors, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (api == null || api.Scheme != Uri.UriSchemeHttps || api.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(api.UserInfo) || !string.IsNullOrEmpty(api.Query) || !string.IsNullOrEmpty(api.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || client.DefaultRequestHeaders.Contains("Cookie") ||
                owner == Guid.Empty)
                throw new ArgumentException("Explicit anonymous HTTPS AppView source required.");
            var keys = actors?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(actors));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique stable DIDs required.");
            foreach (string key in keys) Did(key);
            this.client = client; this.api = api; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.BlueSkyOASIS, SourceName, key).ToString("N"), key => key);
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            string query = string.Join("&", keys.Select(key => "actors=" + Uri.EscapeDataString(key)));
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(api, "xrpc/app.bsky.actor.getProfiles?" + query));
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var body = new MemoryStream(); var buffer = new byte[8192]; int count;
            while ((count = await stream.ReadAsync(buffer.AsMemory(), token)) != 0)
            {
                if (body.Length + count > 262144) throw new InvalidDataException("Native response exceeds256KiB.");
                body.Write(buffer, 0, count);
            }
            using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 24 });
            var root = document.RootElement;
            Unique(root);
            if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("error", out _) ||
                !root.TryGetProperty("profiles", out var profiles) || profiles.ValueKind != JsonValueKind.Array ||
                profiles.GetArrayLength() != keys.Count)
                throw new InvalidDataException("Complete native profile selection required.");
            var selected = new HashSet<string>(keys, StringComparer.Ordinal);
            var observed = new Dictionary<string, IHolon>(StringComparer.Ordinal);
            foreach (var profile in profiles.EnumerateArray())
            {
                string did = Text(profile.GetProperty("did"), 32);
                Did(did);
                if (!selected.Contains(did) || observed.ContainsKey(did))
                    throw new InvalidDataException("Native DID selection differs or repeats.");
                string handle = Text(profile.GetProperty("handle"), 253);
                if (handle.Length == 0 || handle.Any(c => c < 33 || c > 126))
                    throw new InvalidDataException("Bounded public handle text required.");
                var fields = new Dictionary<string, object> {
                    [AddressMetadata] = did, ["BlueSkyHandle"] = handle,
                    ["BlueSkyObservationPolicy"] = "cached-public-appview"
                };
                string name = null;
                if (profile.TryGetProperty("displayName", out var display))
                {
                    name = Text(display, 4096);
                    fields["BlueSkyDisplayName"] = name;
                }
                // Optional display names map to the handle when absent/blank.
                // Preserve the original optional value in metadata, never invent it.
                fields["name"] = string.IsNullOrWhiteSpace(name) ? handle : name;
                if (profile.TryGetProperty("description", out var description))
                    fields["description"] = Text(description, 16384);
                foreach (var pair in new[] {
                    ("followersCount", "BlueSkyFollowersCount"), ("followsCount", "BlueSkyFollowsCount"), ("postsCount", "BlueSkyPostsCount") })
                {
                    if (!profile.TryGetProperty(pair.Item1, out var value)) continue;
                    if (value.ValueKind != JsonValueKind.Number || !Regex.IsMatch(value.GetRawText(), "\\A(?:0|[1-9][0-9]*)\\z") ||
                        !ulong.TryParse(value.GetRawText(), NumberStyles.None, CultureInfo.InvariantCulture, out _))
                        throw new InvalidDataException("Exact nonnegative native64-bit count required.");
                    fields[pair.Item2] = value.GetRawText();
                }
                foreach (var pair in new[] { ("createdAt", "BlueSkyCreatedAt"), ("indexedAt", "BlueSkyIndexedAt") })
                {
                    if (!profile.TryGetProperty(pair.Item1, out var value)) continue;
                    string text = Text(value, 64);
                    if (!Regex.IsMatch(text, "\\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(?:\\.[0-9]{1,9})?(?:Z|[+-][0-9]{2}:[0-9]{2})\\z") ||
                        !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                        throw new InvalidDataException("Native RFC3339 timestamp required.");
                    fields[pair.Item2] = text;
                }
                // Avatar/banner URLs and unrelated view fields are not fetched or projected.
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.BlueSkyOASIS,
                    new NativeRecord(SourceName, did, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.BlueSkyOASIS] = did;
                observed.Add(did, row);
            }
            token.ThrowIfCancellationRequested();
            return Array.AsReadOnly(keys.Select(key => observed[key]).ToArray());
        }

        public async Task VerifyAsync(CancellationToken token = default)
        {
            var rows = await ReadAsync(Addresses, token);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured profiles are missing.");
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
            => Task.FromException(new NotSupportedException("Public BlueSky profile metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public BlueSky profile metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
