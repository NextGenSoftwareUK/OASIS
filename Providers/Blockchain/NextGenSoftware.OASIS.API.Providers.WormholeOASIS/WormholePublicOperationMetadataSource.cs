using System;
using System.Collections.Generic;
using System.Globalization;
using System.Buffers.Binary;
using System.Security.Cryptography;
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

namespace NextGenSoftware.OASIS.API.Providers.WormholeOASIS;

/// <summary>
/// Anonymous indexed operations bound to configured VAA body fingerprints.
/// No cross-chain dispatch, trusted guardian verification or source writes.
/// </summary>
public sealed class WormholePublicOperationMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "WormholeOperationId";
    private readonly OperationsBackend operations;
    private bool disposed;

    public WormholePublicOperationMetadataSource(HttpClient client, Uri rest,
        IEnumerable<KeyValuePair<string, string>> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new OperationsBackend(client, rest, addresses, sourceOwner, ownsClient)) { }

    private WormholePublicOperationMetadataSource(OperationsBackend backend) : base(backend, backend.Prefix)
    {
        operations = backend;
        ProviderName = PType.WormholeOASIS.ToString();
        ProviderDescription = "Anonymous public Wormhole operation observations";
        ProviderType = new EnumValue<PType>(PType.WormholeOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 128, 8);
    }

    public PType QueryProviderType => PType.WormholeOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// cross-chain tracker settings. Reject unknown fields rather than ignore keys.</summary>
    public static WormholePublicOperationMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Wormhole public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "apiUrl", "messages", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("apiUrl").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("messages").ValueKind != JsonValueKind.Object ||
            root.GetProperty("messages").EnumerateObject().Count() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Wormhole source schema required; signer/billing fields forbidden.");
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new WormholePublicOperationMetadataSource(client, new Uri(root.GetProperty("apiUrl").GetString(), UriKind.Absolute),
                root.GetProperty("messages").EnumerateObject().Select(value => new KeyValuePair<string, string>(value.Name, value.Value.GetString())),
                root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await operations.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Wormhole operation source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Wormhole source is required.");
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
            if (keys.Any(key => !operations.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select operations from the configured catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await operations.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Wormhole query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Wormhole operation query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; operations.Dispose();
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



    private static void Identity(string value)
    {
        if (value == null || value.Length > 128) throw new ArgumentException("Bounded operation tuple required.");
        var parts = value.Split('/');
        if (parts.Length != 3 || !ushort.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var chain) || chain == 0 ||
            chain.ToString(CultureInfo.InvariantCulture) != parts[0] || !Regex.IsMatch(parts[1], @"\A[0-9a-f]{64}\z") ||
            !ulong.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence) ||
            sequence.ToString(CultureInfo.InvariantCulture) != parts[2])
            throw new ArgumentException("Canonical uint16/emitter32/uint64 tuple required.");
    }

    private static Dictionary<string, object> Decode(JsonElement root, string identity, string expectedBody)
    {
        if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("error", out _) || Text(root.GetProperty("id"), 128) != identity)
            throw new InvalidDataException("Exact native operation required.");
        var parts = identity.Split('/');
        if (root.GetProperty("emitterChain").GetUInt16().ToString(CultureInfo.InvariantCulture) != parts[0] ||
            Text(root.GetProperty("emitterAddress").GetProperty("hex"), 64) != parts[1] ||
            Text(root.GetProperty("sequence"), 20) != parts[2])
            throw new InvalidDataException("Native operation tuple differs.");
        var envelope = root.GetProperty("vaa");
        string encoded = Text(envelope.GetProperty("raw"), 32768);
        byte[] bytes = Convert.FromBase64String(encoded);
        if (Convert.ToBase64String(bytes) != encoded || bytes.Length < 57 || bytes[0] != 1)
            throw new InvalidDataException("Canonical version1 VAA required.");
        int count = bytes[5], offset = 6 + 66 * count;
        if (count == 0 || bytes.Length < offset + 51 || envelope.GetProperty("isDuplicated").ValueKind != JsonValueKind.False)
            throw new InvalidDataException("Complete unambiguous VAA framing required.");
        for (int index = 1; index < count; index++)
            if (bytes[6 + 66 * index] <= bytes[6 + 66 * (index - 1)])
                throw new InvalidDataException("Strictly increasing guardian indices required.");
        uint guardian = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(1, 4));
        if (envelope.GetProperty("guardianSetIndex").GetUInt32() != guardian)
            throw new InvalidDataException("Guardian-set framing differs.");
        var body = bytes.AsSpan(offset);
        string tuple = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(8, 2)).ToString(CultureInfo.InvariantCulture) + "/" +
            Convert.ToHexString(body.Slice(10, 32)).ToLowerInvariant() + "/" +
            BinaryPrimitives.ReadUInt64BigEndian(body.Slice(42, 8)).ToString(CultureInfo.InvariantCulture);
        string fingerprint = Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant();
        if (tuple != identity || fingerprint != expectedBody)
            throw new InvalidDataException("VAA body differs from the configured observation.");
        // Fingerprint and framing bind this indexed observation. No trusted
        // guardian set is supplied, so this does not authenticate signatures.
        return new Dictionary<string, object> {
            [AddressMetadata] = identity, ["WormholeEmitterChain"] = parts[0], ["WormholeEmitterAddress"] = parts[1],
            ["WormholeSequence"] = parts[2], ["WormholeGuardianSetIndex"] = guardian.ToString(CultureInfo.InvariantCulture),
            ["WormholeSignatureCount"] = count.ToString(CultureInfo.InvariantCulture),
            ["WormholeTimestamp"] = BinaryPrimitives.ReadUInt32BigEndian(body[..4]).ToString(CultureInfo.InvariantCulture),
            ["WormholeNonce"] = BinaryPrimitives.ReadUInt32BigEndian(body.Slice(4, 4)).ToString(CultureInfo.InvariantCulture),
            ["WormholeConsistencyLevel"] = body[50].ToString(CultureInfo.InvariantCulture),
            ["WormholePayloadBytes"] = (body.Length - 51).ToString(CultureInfo.InvariantCulture),
            ["WormholeBodySha256"] = fingerprint, ["WormholePayloadSha256"] = Convert.ToHexString(SHA256.HashData(body[51..])).ToLowerInvariant(),
            ["WormholeObservationPolicy"] = "indexer-reported-pinned-vaa-body", ["WormholeSignaturesVerified"] = "false",
            ["name"] = identity, ["description"] = "Indexed Wormhole message with pinned VAA body " + fingerprint
        };
    }

    private static string Text(JsonElement value, int maximumBytes)
    {
        if (value.ValueKind != JsonValueKind.String) throw new InvalidDataException("Native string required.");
        string text = value.GetString();
        if (Encoding.UTF8.GetByteCount(text) > maximumBytes) throw new InvalidDataException("Native text exceeds its bound.");
        return text;
    }

    private sealed class OperationsBackend : IKeyValueBackend, IDisposable
    {
        private const string SourceName = "wormhole-public-operations-v1";
        private readonly HttpClient client;
        private readonly Uri api;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        private readonly Dictionary<string, string> fingerprints;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "wormhole-public-metadata/";

        public OperationsBackend(HttpClient client, Uri api, IEnumerable<KeyValuePair<string, string>> messages, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (api == null || api.Scheme != Uri.UriSchemeHttps || api.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(api.UserInfo) || !string.IsNullOrEmpty(api.Query) || !string.IsNullOrEmpty(api.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || client.DefaultRequestHeaders.Contains("Cookie") ||
                owner == Guid.Empty)
                throw new ArgumentException("Explicit anonymous HTTPS operation source required.");
            var entries = messages?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(messages));
            var keys = entries.Select(pair => pair.Key).ToArray();
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique operation identities required.");
            foreach (string key in keys) Identity(key);
            if (entries.Any(pair => pair.Value == null || !Regex.IsMatch(pair.Value, @"\A[0-9a-f]{64}\z")))
                throw new ArgumentException("Exact VAA body SHA256 fingerprints required.");
            fingerprints = entries.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            this.client = client; this.api = api; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.WormholeOASIS, SourceName, key).ToString("N"), key => key);
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            var rows = new List<IHolon>();
            long totalBytes = 0;
            foreach (string identity in keys)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    new Uri(api, "api/v1/operations/" + identity));
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
                using var body = new MemoryStream(); var buffer = new byte[8192]; int count;
                while ((count = await stream.ReadAsync(buffer.AsMemory(), deadline.Token)) != 0)
                {
                    totalBytes += count;
                    if (body.Length + count > 262144 || totalBytes > 1048576)
                        throw new InvalidDataException("Native response byte budget exceeded.");
                    body.Write(buffer, 0, count);
                }
                using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
                var root = document.RootElement;
                Unique(root);
                if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("error", out _))
                    throw new InvalidDataException("Complete operation response required.");
                var fields = Decode(root, identity, fingerprints[identity]);
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.WormholeOASIS,
                    new NativeRecord(SourceName, identity, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.WormholeOASIS] = identity;
                rows.Add(row);
            }
            deadline.Token.ThrowIfCancellationRequested();
            return Array.AsReadOnly(rows.ToArray());
        }

        public async Task VerifyAsync(CancellationToken token = default)
        {
            var rows = await ReadAsync(Addresses, token);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured messages are missing.");
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
            => Task.FromException(new NotSupportedException("Public Wormhole operation metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Wormhole operation metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
