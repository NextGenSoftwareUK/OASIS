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
using NextGenSoftware.OASIS.Common;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.NostrOASIS;

/// <summary>
/// Verified immutable public kind-1 events selected by captured event IDs.
/// No login, publication, relay search, real-world identity or completeness claim.
/// </summary>
public partial class NostrOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "NostrEventId";
    private readonly PublicEvents? publicEvents;
    private bool disposed;

    private static readonly CosmicQueryCapabilities PublicEventCapabilities =
        CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 64, 8);

    internal NostrOASIS(Uri relay, IEnumerable<string> eventIds, Guid sourceOwner,
        Func<IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<string>>>? reader = null)
        : this(new[] { relay.AbsoluteUri })
    {
        publicEvents = new PublicEvents(relay, eventIds, sourceOwner, reader);
        QueryCapabilities = PublicEventCapabilities;
    }

    public PType QueryProviderType => PType.NostrOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; } = new(CosmicQueryFeatures.None);

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Nostr storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static NostrOASIS FromPublicQueryConfiguration(string json)
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
        return new NostrOASIS(new Uri(root.GetProperty("relayUrl").GetString()!, UriKind.Absolute),
            root.GetProperty("eventIds").EnumerateArray().Select(value => value.GetString()!),
            root.GetProperty("sourceOwner").GetGuid());
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        if (publicEvents == null)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_SOURCE_UNAVAILABLE",
                Message = "Configure the public event catalogue before activating Nostr queries." };
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await publicEvents.VerifyAsync(cancellationToken);
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
            if (publicEvents == null)
                return Failure("CQL_SOURCE_UNAVAILABLE", "Configure the public event catalogue before querying Nostr.");
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
            if (keys.Any(key => !publicEvents.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select event IDs from the configured catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await publicEvents.ReadAsync(keys, cancellationToken);
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
        disposed = true;
        IsProviderActivated = false;
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



    private sealed class PublicEvents
    {
        private const string SourceName = "nostr-public-events";
        private readonly Guid owner;
        private readonly Func<IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<string>>> read;
        public IReadOnlyList<string> Addresses { get; }

        public PublicEvents(Uri relay, IEnumerable<string> ids, Guid owner,
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
    }
}
