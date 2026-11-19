using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
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

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests")]

namespace NextGenSoftware.OASIS.API.Providers.ConnextOASIS;

/// <summary>
/// Anonymous indexed order metadata over a bounded Connext ID catalogue.
/// </summary>
public partial class ConnextOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string TransferIdMetadata = "ConnextTransferId";
    private readonly PublicMessages? publicMessages;
    private bool disposed;

    internal ConnextOASIS(HttpClient client,
        IEnumerable<ConnextOriginEventClient.Identity> events, Guid sourceOwner, bool ownsClient = false)
        : this()
    {
        publicMessages = new PublicMessages(client, events, sourceOwner, ownsClient);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(TransferIdMetadata, 66, 8);
    }

    public PType QueryProviderType => PType.ConnextOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; } = new(CosmicQueryFeatures.None);

    private static OASISResult<T> PublicProfileWriteRejected<T>()
        => new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_READ_ONLY_SOURCE",
            Message = "The public Connext event profile is read-only." };

    /// <summary>Separate host query-profile configuration, never the existing
    /// cross-chain tracker settings. Reject unknown fields rather than ignore keys.</summary>
    public static ConnextOASIS FromPublicQueryConfiguration(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Connext public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "events", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("events").ValueKind != JsonValueKind.Array ||
            root.GetProperty("events").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Connext source schema required; signer/billing fields forbidden.");
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new ConnextOASIS(client,
                root.GetProperty("events").EnumerateArray().Select(value => new ConnextOriginEventClient.Identity(
                    value.GetProperty("transferId").GetString(), value.GetProperty("transactionHash").GetString(),
                    value.GetProperty("blockHash").GetString(), value.GetProperty("blockNumber").GetInt64(),
                    value.GetProperty("logIndex").GetInt32())),
                root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        if (publicMessages == null)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_SOURCE_UNAVAILABLE",
                Message = "Configure the public event catalogue before activating Connext queries." };
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await publicMessages.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Connext message source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (publicMessages == null)
                return Failure("CQL_SOURCE_UNAVAILABLE", "Configure the public event catalogue before querying Connext.");
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Connext source is required.");
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
            if (keys.Any(key => !publicMessages.MessageIds.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select transfer IDs from the configured event catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await publicMessages.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Connext query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Connext message query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        IsProviderActivated = false;
        publicMessages?.Dispose();
        _http.Dispose();
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



    private sealed class PublicMessages : IDisposable
    {
        private const string SourceName = "connext-ethereum-origin";
        private readonly HttpClient client;
        private readonly ConnextOriginEventClient origin;
        private readonly Dictionary<string, ConnextOriginEventClient.Identity> catalogue;
        private readonly Guid owner;
        private readonly bool ownsClient;
        public IReadOnlyList<string> MessageIds { get; }

        public PublicMessages(HttpClient client, IEnumerable<ConnextOriginEventClient.Identity> events, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(events);
            if (owner == Guid.Empty)
                throw new ArgumentException("An explicit Connext source owner is required.");
            var captured = events.Take(9).ToArray();
            var keys = captured.Select(entry => entry.TransferId).ToArray();
            if (keys.Length is < 1 or > 8 || keys.Any(key => key == null || !Regex.IsMatch(key, @"\A0x[0-9a-f]{64}\z")) ||
                keys.Distinct(StringComparer.Ordinal).Count() != keys.Length ||
                captured.Select(entry => (entry.TransactionHash, entry.LogIndex)).Distinct().Count() != keys.Length)
                throw new ArgumentException("One to eight distinct canonical Connext transfer events are required.");
            if (client.DefaultRequestHeaders.Authorization != null || client.DefaultRequestHeaders.Contains("Cookie"))
                throw new ArgumentException("Connext public event reads require an anonymous client.");
            this.client = client;
            origin = new ConnextOriginEventClient(client);
            catalogue = captured.ToDictionary(entry => entry.TransferId, StringComparer.Ordinal);
            this.owner = owner;
            this.ownsClient = ownsClient;
            MessageIds = Array.AsReadOnly(keys);
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var rows = new List<IHolon>(keys.Count);
            foreach (var id in keys)
            {
                var observation = await origin.ReadAsync(catalogue[id], deadline.Token);
                var fields = new Dictionary<string, object>
                {
                    [TransferIdMetadata] = id,
                    ["ConnextOriginChainId"] = 1L,
                    ["ConnextOriginTransactionHash"] = observation.TransactionHash,
                    ["ConnextOriginBlockHash"] = observation.BlockHash,
                    ["ConnextOriginBlockNumber"] = observation.BlockNumber,
                    ["ConnextOriginLogIndex"] = (long)observation.LogIndex,
                    ["name"] = id,
                    ["description"] = "Connext indexed Ethereum origin event"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.ConnextOASIS,
                    new NativeRecord(SourceName, id, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner;
                row.IsPublic = true;
                row.IsActive = true;
                row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.ConnextOASIS] = id;
                rows.Add(row);
            }
            deadline.Token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }

        public async Task VerifyAsync(CancellationToken token) => await ReadAsync(MessageIds, token);

        public void Dispose()
        {
            if (ownsClient) client.Dispose();
        }
    }
}
