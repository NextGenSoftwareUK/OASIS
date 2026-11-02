using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

namespace NextGenSoftware.OASIS.API.Providers.NostrOASIS;

/// <summary>
/// Verified immutable public kind-1 events selected by captured event IDs.
/// No login, publication, relay search, real-world identity or completeness claim.
/// </summary>
public sealed class NostrPublicEventMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "NostrEventId";
    private readonly EventsBackend events;
    private bool disposed;

    public NostrPublicEventMetadataSource(Uri relay, IEnumerable<string> eventIds, Guid sourceOwner)
        : this(new EventsBackend(relay, eventIds, sourceOwner)) { }

    internal NostrPublicEventMetadataSource(Uri relay, IEnumerable<string> eventIds, Guid sourceOwner,
        Func<IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<string>>> reader)
        : this(new EventsBackend(relay, eventIds, sourceOwner, reader)) { }

    private NostrPublicEventMetadataSource(EventsBackend backend) : base(backend, backend.Prefix)
    {
        events = backend;
        ProviderName = PType.NostrOASIS.ToString();
        ProviderDescription = "Verified public Nostr kind-1 event observations";
        ProviderType = new EnumValue<PType>(PType.NostrOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 64, 8);
    }

    public PType QueryProviderType => PType.NostrOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Nostr storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static NostrPublicEventMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Nostr public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "relayUrl", "eventIds", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("relayUrl").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("eventIds").ValueKind != JsonValueKind.Array ||
            root.GetProperty("eventIds").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Nostr source schema required; signer/billing fields forbidden.");
        return new NostrPublicEventMetadataSource(new Uri(root.GetProperty("relayUrl").GetString()!, UriKind.Absolute),
            root.GetProperty("eventIds").EnumerateArray().Select(value => value.GetString()!),
            root.GetProperty("sourceOwner").GetGuid());
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await events.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Nostr event source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Nostr source is required.");
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
            if (keys.Any(key => !events.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select event IDs from the configured catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await events.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Nostr query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Nostr event query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; 
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



    private sealed class EventsBackend : IKeyValueBackend
    {
        private const string SourceName = "nostr-public-events";
        private readonly Guid owner;
        private readonly Func<IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<string>>> read;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "nostr-public-events/";

        public EventsBackend(Uri relay, IEnumerable<string> ids, Guid owner,
            Func<IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<string>>>? reader = null)
        {
            if (relay == null || !relay.IsAbsoluteUri || relay.Scheme != "wss" || relay.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(relay.UserInfo) || !string.IsNullOrEmpty(relay.Query) ||
                !string.IsNullOrEmpty(relay.Fragment) || owner == Guid.Empty)
                throw new ArgumentException("Explicit anonymous WSS root and stable source owner required.");
            var keys = ids?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(ids));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length ||
                keys.Any(key => key == null || !Regex.IsMatch(key, "\\A[0-9a-f]{64}\\z")))
                throw new ArgumentException("One to eight unique canonical event IDs required.");
            this.owner = owner;
            read = reader ?? ((keys, token) => NostrPublicRelayRead.ReadAsync(relay, keys, token));
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.NostrOASIS, SourceName, key).ToString("N"), key => key);
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            var events = await read(keys, token);
            if (events.Count != keys.Count) throw new InvalidDataException("Complete signed event selection required.");
            var selected = new HashSet<string>(keys, StringComparer.Ordinal);
            var observed = new Dictionary<string, IHolon>(StringComparer.Ordinal);
            foreach (string raw in events)
            {
                token.ThrowIfCancellationRequested();
                var verified = NostrPublicEventVerification.Parse(raw);
                if (!selected.Contains(verified.Id) || observed.ContainsKey(verified.Id))
                    throw new InvalidDataException("Native event ID differs or repeats.");
                var fields = new Dictionary<string, object> {
                    [AddressMetadata] = verified.Id, ["NostrPublicKey"] = verified.PublicKey,
                    ["NostrCreatedAt"] = verified.CreatedAt.ToString(CultureInfo.InvariantCulture),
                    ["NostrKind"] = "1", ["NostrContent"] = verified.Content,
                    ["NostrTagsJson"] = verified.TagsJson, ["NostrSignature"] = verified.Signature,
                    ["NostrObservationPolicy"] = "signed-event-relay-observed",
                    ["name"] = "Nostr event " + verified.Id[..12], ["description"] = verified.Content
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.NostrOASIS,
                    new NativeRecord(SourceName, verified.Id, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.NostrOASIS] = verified.Id;
                observed.Add(verified.Id, row);
            }
            token.ThrowIfCancellationRequested();
            return Array.AsReadOnly(keys.Select(key => observed[key]).ToArray());
        }

        public async Task VerifyAsync(CancellationToken token = default)
        {
            var rows = await ReadAsync(Addresses, token);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured events missing.");
        }
        public async Task<string?> GetAsync(string key, CancellationToken token = default)
        {
            if (!records.TryGetValue(key, out var id)) return null;
            var rows = await ReadAsync(new[] { id }, token);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Nostr event source is read-only."));
        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Nostr event source is read-only."));
    }
}
