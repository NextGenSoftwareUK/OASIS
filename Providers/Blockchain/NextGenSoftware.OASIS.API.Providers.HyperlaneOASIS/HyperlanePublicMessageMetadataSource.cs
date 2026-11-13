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

namespace NextGenSoftware.OASIS.API.Providers.HyperlaneOASIS;

/// <summary>
/// Anonymous indexed message metadata over a bounded Hyperlane ID catalogue.
/// </summary>
public sealed class HyperlanePublicMessageMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string MessageIdMetadata = "HyperlaneMessageId";
    private readonly MessagesBackend messages;
    private bool disposed;

    public HyperlanePublicMessageMetadataSource(HttpClient client, Uri endpoint,
        IEnumerable<string> messageIds, Guid sourceOwner, bool ownsClient = false)
        : this(new MessagesBackend(client, endpoint, messageIds, sourceOwner, ownsClient)) { }

    private HyperlanePublicMessageMetadataSource(MessagesBackend backend) : base(backend, backend.Prefix)
    {
        messages = backend;
        ProviderName = PType.HyperlaneOASIS.ToString();
        ProviderDescription = "Anonymous Hyperlane Explorer message observations";
        ProviderType = new EnumValue<PType>(PType.HyperlaneOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(MessageIdMetadata, 66, 8);
    }

    public PType QueryProviderType => PType.HyperlaneOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// cross-chain tracker settings. Reject unknown fields rather than ignore keys.</summary>
    public static HyperlanePublicMessageMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Hyperlane public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "apiUrl", "messageIds", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("apiUrl").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("messageIds").ValueKind != JsonValueKind.Array ||
            root.GetProperty("messageIds").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Hyperlane source schema required; signer/billing fields forbidden.");
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new HyperlanePublicMessageMetadataSource(client, new Uri(root.GetProperty("apiUrl").GetString(), UriKind.Absolute),
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
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Hyperlane message source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Hyperlane source is required.");
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
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Hyperlane query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Hyperlane message query failed; no partial rows.", error); }
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
        private const string SourceName = "hyperlane-explorer-messages";
        private const string Endpoint = "https://explorer4.hasura.app/v1/graphql";
        private const int MaximumBytes = 65536;
        private readonly HttpClient client;
        private readonly Uri endpoint;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> MessageIds { get; }
        public string Prefix => "hyperlane-public-metadata/";

        public MessagesBackend(HttpClient client, Uri endpoint, IEnumerable<string> messageIds, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(messageIds);
            if (endpoint?.OriginalString != Endpoint || owner == Guid.Empty ||
                client.DefaultRequestHeaders.Authorization != null || client.DefaultRequestHeaders.Contains("Cookie"))
                throw new ArgumentException("The qualified anonymous Hyperlane Explorer endpoint and source owner are required.");
            var keys = messageIds.Take(9).ToArray();
            if (keys.Length is < 1 or > 8 || keys.Any(key => key == null || !Regex.IsMatch(key, @"\A0x[0-9a-f]{64}\z")) ||
                keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique canonical Hyperlane message IDs are required.");
            this.client = client;
            this.endpoint = endpoint;
            this.owner = owner;
            this.ownsClient = ownsClient;
            MessageIds = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.HyperlaneOASIS, SourceName, key).ToString("N"), key => key);
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var nativeKeys = keys.Select(key => @"\x" + key[2..]).ToArray();
            string query = "query { message_view(limit:" + keys.Count + ",where:{msg_id:{_in:" +
                JsonSerializer.Serialize(nativeKeys) + "}}) { msg_id origin_domain_id destination_domain_id is_delivered } }";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = new StringContent(JsonSerializer.Serialize(new { query }), Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var body = new MemoryStream();
            var buffer = new byte[4096];
            int count;
            while ((count = await stream.ReadAsync(buffer.AsMemory(), deadline.Token)) != 0)
            {
                if (body.Length + count > MaximumBytes)
                    throw new InvalidDataException("Explorer response exceeded its byte budget.");
                body.Write(buffer, 0, count);
            }
            using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            Unique(root);
            if (root.TryGetProperty("errors", out _))
                throw new InvalidDataException("Explorer returned a GraphQL error.");
            var selected = root.GetProperty("data").GetProperty("message_view");
            if (selected.GetArrayLength() != keys.Count)
                throw new InvalidDataException("The complete requested message set is required.");

            var rows = new Dictionary<string, IHolon>(StringComparer.Ordinal);
            foreach (var entry in selected.EnumerateArray())
            {
                string nativeId = entry.GetProperty("msg_id").GetString();
                if (!nativeKeys.Contains(nativeId, StringComparer.Ordinal))
                    throw new InvalidDataException("Explorer returned a foreign message.");
                string id = "0x" + nativeId[2..];
                var fields = new Dictionary<string, object>
                {
                    [MessageIdMetadata] = id,
                    ["HyperlaneOriginDomainId"] = (long)entry.GetProperty("origin_domain_id").GetUInt32(),
                    ["HyperlaneDestinationDomainId"] = (long)entry.GetProperty("destination_domain_id").GetUInt32(),
                    ["HyperlaneIsDelivered"] = entry.GetProperty("is_delivered").GetBoolean(),
                    ["name"] = id,
                    ["description"] = "Hyperlane Explorer indexed message metadata"
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.HyperlaneOASIS,
                    new NativeRecord(SourceName, id, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner;
                row.IsPublic = true;
                row.IsActive = true;
                row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.HyperlaneOASIS] = id;
                if (!rows.TryAdd(id, row))
                    throw new InvalidDataException("Explorer returned a duplicate message.");
            }
            deadline.Token.ThrowIfCancellationRequested();
            return Array.AsReadOnly(keys.Select(key => rows[key]).ToArray());
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
            => Task.FromException(new NotSupportedException("Public Hyperlane metadata is read-only."));

        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Hyperlane metadata is read-only."));

        public void Dispose()
        {
            if (ownsClient) client.Dispose();
        }
    }
}
