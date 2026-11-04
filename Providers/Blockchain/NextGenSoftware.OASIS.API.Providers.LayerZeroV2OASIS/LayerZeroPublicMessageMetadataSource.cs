using System;
using System.Collections.Generic;
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

namespace NextGenSoftware.OASIS.API.Providers.LayerZeroV2OASIS;

/// <summary>
/// Anonymous indexed message metadata over a bounded LayerZero ID catalogue.
/// </summary>
public sealed class LayerZeroPublicMessageMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string MessageIdMetadata = "LayerZeroMessageId";
    private readonly MessagesBackend messages;
    private bool disposed;

    public LayerZeroPublicMessageMetadataSource(HttpClient client,
        IEnumerable<string> messageIds, Guid sourceOwner, bool ownsClient = false)
        : this(new MessagesBackend(client, messageIds, sourceOwner, ownsClient)) { }

    private LayerZeroPublicMessageMetadataSource(MessagesBackend backend) : base(backend, backend.Prefix)
    {
        messages = backend;
        ProviderName = PType.LayerZeroV2OASIS.ToString();
        ProviderDescription = "Anonymous LayerZero Scan message observations";
        ProviderType = new EnumValue<PType>(PType.LayerZeroV2OASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(MessageIdMetadata, 66, 8);
    }

    public PType QueryProviderType => PType.LayerZeroV2OASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// cross-chain tracker settings. Reject unknown fields rather than ignore keys.</summary>
    public static LayerZeroPublicMessageMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit LayerZero public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "messageIds", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("messageIds").ValueKind != JsonValueKind.Array ||
            root.GetProperty("messageIds").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous LayerZero source schema required; signer/billing fields forbidden.");
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new LayerZeroPublicMessageMetadataSource(client,
                root.GetProperty("messageIds").EnumerateArray().Select(value => value.GetString()),
                root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await messages.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public LayerZero message source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public LayerZero source is required.");
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
            if (keys.Any(key => !messages.MessageIds.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select messageIds from the configured message catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await messages.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "LayerZero query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public LayerZero message query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; messages.Dispose();
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



    private sealed class MessagesBackend : IKeyValueBackend, IDisposable
    {
        private const string SourceName = "layerzero-scan-messages";
        private readonly HttpClient client;
        private readonly LayerZeroScanMessageClient scan;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> MessageIds { get; }
        public string Prefix => "layerzero-public-metadata/";

        public MessagesBackend(HttpClient client, IEnumerable<string> messageIds, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(messageIds);
            if (owner == Guid.Empty ||
                client.DefaultRequestHeaders.Authorization != null || client.DefaultRequestHeaders.Contains("Cookie"))
                throw new ArgumentException("The qualified anonymous LayerZero Scan endpoint and source owner are required.");
            var keys = messageIds.Take(9).ToArray();
            if (keys.Length is < 1 or > 8 || keys.Any(key => key == null || !Regex.IsMatch(key, @"\A0x[0-9a-f]{64}\z")) ||
                keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique canonical LayerZero message IDs are required.");
            this.client = client;
            scan = new LayerZeroScanMessageClient(client);
            this.owner = owner;
            this.ownsClient = ownsClient;
            MessageIds = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.LayerZeroV2OASIS, SourceName, key).ToString("N"), key => key);
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var rows = new List<IHolon>(keys.Count);
            foreach (var key in keys)
            {
                var observation = await scan.ReadAsync(key, deadline.Token);
                var fields = new Dictionary<string, object>
                {
                    [MessageIdMetadata] = observation.Guid,
                    ["LayerZeroSourceEndpoint"] = (long)observation.SourceEndpoint,
                    ["LayerZeroDestinationEndpoint"] = (long)observation.DestinationEndpoint,
                    ["LayerZeroNonce"] = observation.Nonce.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["LayerZeroScanStatus"] = observation.Status,
                    ["name"] = observation.Guid,
                    ["description"] = "LayerZero Scan indexed message metadata"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.LayerZeroV2OASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner;
                row.IsPublic = true;
                row.IsActive = true;
                row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.LayerZeroV2OASIS] = key;
                rows.Add(row);
            }
            deadline.Token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }

        public async Task VerifyAsync(CancellationToken token) => await ReadAsync(MessageIds, token);

        public async Task<string> GetAsync(string key, CancellationToken token = default)
        {
            if (!records.TryGetValue(key, out var id)) return null;
            var rows = await ReadAsync(new[] { id }, token);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }

        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys
                .Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }

        public Task PutAsync(string key, string value, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public LayerZero metadata is read-only."));

        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public LayerZero metadata is read-only."));

        public void Dispose()
        {
            if (ownsClient) client.Dispose();
        }
    }
}
