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

namespace NextGenSoftware.OASIS.API.Providers.SynapseOASIS;

/// <summary>
/// Anonymous indexed transaction metadata over a bounded Synapse ID catalogue.
/// </summary>
public sealed class SynapsePublicTransactionMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string TransactionIdMetadata = "SynapseTransactionId";
    private readonly MessagesBackend messages;
    private bool disposed;

    public SynapsePublicTransactionMetadataSource(HttpMessageHandler handler,
        IEnumerable<string> messageIds, Guid sourceOwner, bool ownsHandler = false)
        : this(new MessagesBackend(handler, messageIds, sourceOwner, ownsHandler)) { }

    private SynapsePublicTransactionMetadataSource(MessagesBackend backend) : base(backend, backend.Prefix)
    {
        messages = backend;
        ProviderName = PType.SynapseOASIS.ToString();
        ProviderDescription = "Anonymous Synapse RFQ transaction observations";
        ProviderType = new EnumValue<PType>(PType.SynapseOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(TransactionIdMetadata, 66, 8);
    }

    public PType QueryProviderType => PType.SynapseOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// cross-chain tracker settings. Reject unknown fields rather than ignore keys.</summary>
    public static SynapsePublicTransactionMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Synapse public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "messageIds", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("messageIds").ValueKind != JsonValueKind.Array ||
            root.GetProperty("messageIds").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Synapse source schema required; signer/billing fields forbidden.");
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        try
        {
            return new SynapsePublicTransactionMetadataSource(handler,
                root.GetProperty("messageIds").EnumerateArray().Select(value => value.GetString()),
                root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { handler.Dispose(); throw; }
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
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Synapse message source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Synapse source is required.");
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
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Synapse query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Synapse message query failed; no partial rows.", error); }
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
        private const string SourceName = "synapse-rfq-transactions";
        private readonly SynapseOASIS client;
        private readonly HttpMessageHandler handler;
        private readonly Guid owner;
        private readonly bool ownsHandler;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> MessageIds { get; }
        public string Prefix => "synapse-public-metadata/";

        public MessagesBackend(HttpMessageHandler handler, IEnumerable<string> messageIds, Guid owner, bool ownsHandler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            ArgumentNullException.ThrowIfNull(messageIds);
            if (owner == Guid.Empty)
                throw new ArgumentException("An explicit Synapse source owner is required.");
            var keys = messageIds.Take(9).ToArray();
            if (keys.Length is < 1 or > 8 || keys.Any(key => key == null || !Regex.IsMatch(key, @"\A0x[0-9a-f]{64}\z")) ||
                keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique canonical Synapse message IDs are required.");
            this.handler = handler;
            client = new SynapseOASIS("https://api.synapseprotocol.com", handler);
            client.ConfigureBoundedPublicReads();
            this.owner = owner;
            this.ownsHandler = ownsHandler;
            MessageIds = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.SynapseOASIS, SourceName, key).ToString("N"), key => key);
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var rows = new List<IHolon>(keys.Count);
            foreach (var id in keys)
            {
                var response = await client.GetIndexedTransactionAsync(id, deadline.Token);
                deadline.Token.ThrowIfCancellationRequested();
                if (response.IsError)
                    throw new InvalidDataException("Synapse RFQ indexer did not return the requested transaction.");

                var bridge = response.Result.GetProperty("Bridge");
                var origin = bridge.GetProperty("originChainId").GetUInt32();
                var destination = bridge.GetProperty("destChainId").GetUInt32();
                if (bridge.GetProperty("transactionId").GetString() != id || origin == 0 || destination == 0)
                    throw new InvalidDataException("The exact Synapse transaction and chain identities are required.");

                var fields = new Dictionary<string, object>
                {
                    [TransactionIdMetadata] = id,
                    ["SynapseOriginChainId"] = (long)origin,
                    ["SynapseDestinationChainId"] = (long)destination,
                    ["name"] = id,
                    ["description"] = "Synapse RFQ indexed transaction metadata"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.SynapseOASIS,
                    new NativeRecord(SourceName, id, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner;
                row.IsPublic = true;
                row.IsActive = true;
                row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.SynapseOASIS] = id;
                rows.Add(row);
            }
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
            => Task.FromException(new NotSupportedException("Public Synapse metadata is read-only."));

        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Synapse metadata is read-only."));

        public void Dispose()
        {
            client.DisposePublicReadClient();
            if (ownsHandler) handler.Dispose();
        }
    }
}
