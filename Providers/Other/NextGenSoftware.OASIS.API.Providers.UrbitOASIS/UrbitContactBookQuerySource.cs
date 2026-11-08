using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.UrbitOASIS;

/// <summary>Owner-only, bounded virtual book-page reads. Never imports or writes native pages.</summary>
public sealed class UrbitContactBookQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "contacts/v1/book";
    private readonly UrbitContactsV1Client client;
    private readonly Guid owner;
    private readonly Guid connection;
    private readonly Dictionary<Guid, string> ids;
    private readonly bool ownsClient;
    private bool disposed;

    /// <summary>Owner and persistent connection identity are selected by trusted host configuration,
    /// independently of the current URL/port and caller-supplied query data.</summary>
    public UrbitContactBookQuerySource(UrbitContactsV1Client client, Guid owner, Guid connection,
        IEnumerable<string> pageIds, bool ownsClient = false) : base(new ReadOnlyBackend(), "urbit-contacts-book:")
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        if (owner == Guid.Empty || connection == Guid.Empty || pageIds is null)
            throw new ArgumentException("A private source owner, stable connection and explicit catalogue are required.");
        var keys = pageIds.Take(33).ToArray();
        if (keys.Length is < 1 or > 32 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length
            || keys.Any(key => !UrbitContactsV1Client.IsBookPageId(key)))
            throw new ArgumentException("One to32distinct path-safe native book CIDs are required.");
        this.owner = owner;
        this.connection = connection;
        this.ownsClient = ownsClient;
        ids = keys.ToDictionary(key => LogicalId(owner, connection, key));
        ProviderType = new EnumValue<PType>(PType.UrbitOASIS);
        ProviderName = PType.UrbitOASIS.ToString();
        ProviderDescription = "Private owner-bound contacts-v1 native book pages";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
    }

    public PType QueryProviderType => PType.UrbitOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public static Guid LogicalId(Guid owner, Guid connection, string cid)
    {
        if (owner == Guid.Empty || connection == Guid.Empty || !UrbitContactsV1Client.IsBookPageId(cid))
            throw new ArgumentException("An owner, stable connection identity and native book CID are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.UrbitOASIS, Source, cid, connection.ToString("D"));
    }

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();
    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            var probe = await client.ReadBookPageAsync(ids.Values.First(), cancellationToken);
            if (probe.IsError || probe.Result is null)
                return Failure<bool>(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", "Native private book probe failed: " + probe.ErrorCode, probe.Exception);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        { return Failure<bool>(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", "Private book activation failed.", error); }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? error = null) =>
            Failure<IReadOnlyList<IHolon>>(code, message, error);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan is null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Urbit book target is required.");
            if (maximumResults is < 1 or > 10000) return Error("CQL_QUERY_LIMIT", "A bounded result budget is required.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "Historical book reads are not admitted.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Error("CQL_PROVIDER_INACTIVE", "Activate the configured private book first.");
            // An authoritative ID catalogue does not confer ownership of native credentials.
            if (plan.Query.ReadAccess.ActorId != owner)
                return new(Array.AsReadOnly(Array.Empty<IHolon>())) { IsLoaded = true, LoadedCount = 0 };
            if (selected.Result.Any(id => !ids.ContainsKey(id)))
                return Error("CQL_QUERY_FAILED", "Selected identity is outside the configured private source.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var rows = new List<IHolon>();
            var bytes = 0;
            foreach (var id in selected.Result)
            {
                var native = await client.ReadBookPageAsync(ids[id], deadline.Token);
                if (native.IsError || native.Result is null)
                    return Error(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : deadline.IsCancellationRequested
                        ? "CQL_QUERY_TIMEOUT" : "CQL_QUERY_FAILED", "Native private book read failed: " + native.ErrorCode + "; no partial result published.", native.Exception);
                var page = native.Result;
                var fields = new Dictionary<string, object> {
                    ["name"] = "Urbit contact page " + page.RequestedId,
                    ["UrbitBookPageId"] = page.RequestedId,
                    ["ContactNickname"] = page.Contact.Nickname!, ["ContactBio"] = page.Contact.Bio!, ["ContactStatus"] = page.Contact.Status!,
                    ["OverlayNickname"] = page.Overlay.Nickname!, ["OverlayBio"] = page.Overlay.Bio!, ["OverlayStatus"] = page.Overlay.Status!
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, QueryProviderType,
                    new NativeRecord(Source, page.RequestedId, fields), NativeDataMapping.ForSource(Source), connection.ToString("D"));
                if (mapped.IsError) return Error(mapped.ErrorCode, mapped.Message);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = false; row.IsActive = true;
                row.Children = Array.Empty<IHolon>(); row.ProviderUniqueStorageKey[QueryProviderType] = page.RequestedId;
                if (row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                    return Error("CQL_ACCESS_INVALID", "Native book mapping differs from the admitted owner/source identity.");
                bytes += Encoding.UTF8.GetByteCount(StoredRecordJson.Serialize<IHolon>(row));
                if (bytes > 4 * 1024 * 1024) return Error("CQL_QUERY_LIMIT", "Complete private book projection exceeds4MiB.");
                rows.Add(row);
            }
            var result = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (result.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "Complete page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(result)) { IsLoaded = true, LoadedCount = result.Length };
        }
        catch (Exception error)
        { return Error(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_FAILED", "Private native book query failed; no partial rows published.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false;
        if (ownsClient) client.Dispose();
    }

    private static OASISResult<T> Failure<T>(string code, string message, Exception? error = null) =>
        new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    private sealed class ReadOnlyBackend : IKeyValueBackend
    {
        private static NotSupportedException Unsupported() => new("This source admits only bounded owner-authorized CQL reads; canonical CRUD and native writes are not supported.");
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(Unsupported());
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Task.FromException(Unsupported());
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.FromException(Unsupported());
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(Unsupported());
        public Task VerifyAsync(CancellationToken cancellationToken = default) => Task.FromException(Unsupported());
    }
}
