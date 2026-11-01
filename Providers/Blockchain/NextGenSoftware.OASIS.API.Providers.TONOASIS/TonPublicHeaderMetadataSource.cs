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

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS;

/// <summary>Explicit public Ton header representation; never signed storage.</summary>
public sealed class TonPublicHeaderMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "TonBlockRootHash";
    private readonly HeadersBackend headers;
    private bool disposed;

    public TonPublicHeaderMetadataSource(HttpClient client, Uri rest,
        IEnumerable<TonPublicHeaderReference> blocks, Guid sourceOwner, bool ownsClient = false)
        : this(new HeadersBackend(client, rest, blocks, sourceOwner, ownsClient)) { }

    private TonPublicHeaderMetadataSource(HeadersBackend backend) : base(backend, backend.Prefix)
    {
        headers = backend;
        ProviderName = PType.TONOASIS.ToString();
        ProviderDescription = "Read-only explicit historical Ton gateway headers";
        ProviderType = new EnumValue<PType>(PType.TONOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 44, 8);
    }

    public PType QueryProviderType => PType.TONOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// signed Ton storage settings. Reject unknown fields rather than ignore keys.</summary>
    public static TonPublicHeaderMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit TON source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 6 });
        Unique(document.RootElement);
        var root = document.RootElement;
        string[] fields = { "schema", "restUrl", "globalId", "blocks", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(value => !fields.Contains(value.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").GetRawText() != "1" || root.GetProperty("globalId").GetRawText() != "-239" ||
            root.GetProperty("blocks").ValueKind != JsonValueKind.Array || root.GetProperty("blocks").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous TON source schema required; signing fields forbidden.");
        var blocks = root.GetProperty("blocks").EnumerateArray().Select(value => {
            if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Count() != 3 ||
                value.EnumerateObject().Any(field => field.Name is not ("seqno" or "rootHash" or "fileHash")))
                throw new ArgumentException("Exact TON descriptor required.");
            return new TonPublicHeaderReference(TonPublicHeaderReference.UInt32Token(value.GetProperty("seqno")),
                value.GetProperty("rootHash").GetString(), value.GetProperty("fileHash").GetString());
        }).ToArray();
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()) { Timeout = TimeSpan.FromSeconds(15) };
        try { return new TonPublicHeaderMetadataSource(client, new Uri(root.GetProperty("restUrl").GetString(), UriKind.Absolute),
            blocks, root.GetProperty("sourceOwner").GetGuid(), true); }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await headers.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Ton header source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Ton source is required.");
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
            if (keys.Any(key => !headers.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select addresses from the configured header catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await headers.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Ton query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Ton header query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; headers.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public const string PublicEndpoint = "https://toncenter.com/api/v2/";

    private static void Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate TON configuration property.");
                Unique(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) Unique(item);
    }

    private sealed class HeadersBackend : IKeyValueBackend, IDisposable
    {
        private readonly HttpClient client;
        private readonly Uri endpoint;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, TonPublicHeaderReference> blocks;
        private readonly Dictionary<string, string> records;
        private const string SourceName = "ton-public-masterchain:-239";
        public string Prefix => "ton-public-metadata/";
        public IReadOnlyList<string> Addresses { get; }

        // Anonymous TON Center permits one request/second. Measure from the
        // preceding completion: connection setup/latency must not consume the
        // next admission interval. This is pacing, never an error-driven retry.
        private static readonly SemaphoreSlim EndpointGate = new(1, 1);
        private static long previousCompletion;

        public HeadersBackend(HttpClient client, Uri endpoint, IEnumerable<TonPublicHeaderReference> selections,
            Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (endpoint == null || endpoint.AbsoluteUri != PublicEndpoint ||
                client.DefaultRequestHeaders.Authorization != null || owner == Guid.Empty)
                throw new ArgumentException("Explicit anonymous TON endpoint and stable source owner required.");

            var selected = selections?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(selections));
            if (selected.Length is < 1 or > 8 || selected.Any(value => value == null) ||
                selected.Select(value => value.Sequence).Distinct().Count() != selected.Length ||
                selected.Select(value => value.RootHash).Distinct(StringComparer.Ordinal).Count() != selected.Length)
                throw new ArgumentException("One to eight unique TON block descriptors required.");

            this.client = client; this.endpoint = endpoint; this.owner = owner; this.ownsClient = ownsClient;
            blocks = selected.ToDictionary(value => value.RootHash, StringComparer.Ordinal);
            Addresses = Array.AsReadOnly(selected.Select(value => value.RootHash).ToArray());
            records = Addresses.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.TONOASIS, SourceName, key).ToString("N"), key => key);
        }

        private async Task<JsonElement> RequestAsync(string route, CancellationToken token)
        {
            await EndpointGate.WaitAsync(token);
            bool dispatched = false;
            try
            {
                if (previousCompletion != 0)
                {
                    var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(previousCompletion);
                    var wait = TimeSpan.FromMilliseconds(1100) - elapsed;
                    if (wait > TimeSpan.Zero) await Task.Delay(wait, token);
                }
                token.ThrowIfCancellationRequested();
                using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(endpoint, route));
                dispatched = true;
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(token);
                using var body = new MemoryStream();
                var buffer = new byte[8192]; int count;
                while ((count = await stream.ReadAsync(buffer.AsMemory(), token)) != 0)
                {
                    if (body.Length + count > 262144) throw new InvalidDataException("TON response exceeds256KiB.");
                    body.Write(buffer, 0, count);
                }
                using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
                token.ThrowIfCancellationRequested();
                return document.RootElement.Clone();
            }
            finally
            {
                if (dispatched) previousCompletion = System.Diagnostics.Stopwatch.GetTimestamp();
                EndpointGate.Release();
            }
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            var identity = await RequestAsync("getMasterchainInfo", token);
            TonPublicHeaderReference.ValidateMainnetIdentity(identity);
            var rows = new List<IHolon>();
            foreach (string key in keys)
            {
                var reference = blocks[key];
                string route = "getBlockHeader?workchain=-1&shard=" + TonPublicHeaderReference.MasterchainShard +
                    "&seqno=" + reference.Sequence.ToString(CultureInfo.InvariantCulture) +
                    "&root_hash=" + Uri.EscapeDataString(reference.RootHash) +
                    "&file_hash=" + Uri.EscapeDataString(reference.FileHash);
                var envelope = await RequestAsync(route, token);
                var header = reference.ValidateHeader(envelope);
                var fields = new Dictionary<string, object> {
                    ["name"] = "TON masterchain block " + reference.Sequence.ToString(CultureInfo.InvariantCulture),
                    ["description"] = "Public historical TON RPC header; consensus not independently verified.",
                    [AddressMetadata] = key, ["TonBlockFileHash"] = reference.FileHash,
                    ["TonSequence"] = reference.Sequence.ToString(CultureInfo.InvariantCulture),
                    ["TonGlobalId"] = "-239", ["TonMasterchainShard"] = TonPublicHeaderReference.MasterchainShard,
                    ["TonStartLogicalTime"] = header.GetProperty("start_lt").GetString(),
                    ["TonEndLogicalTime"] = header.GetProperty("end_lt").GetString(),
                    ["TonTimestamp"] = header.GetProperty("gen_utime").GetRawText(),
                    ["TonParentRootHash"] = header.GetProperty("prev_blocks")[0].GetProperty("root_hash").GetString(),
                    ["TonParentFileHash"] = header.GetProperty("prev_blocks")[0].GetProperty("file_hash").GetString()
                };
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.TONOASIS,
                    new NativeRecord(SourceName, key, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.TONOASIS] = key;
                rows.Add(row);
            }
            token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }

        public async Task VerifyAsync(CancellationToken token = default)
        {
            var rows = await ReadAsync(Addresses, token);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Missing selected TON headers.");
        }

        public async Task<string> GetAsync(string key, CancellationToken token = default)
        {
            if (!records.TryGetValue(key, out string native)) return null;
            var rows = await ReadAsync(new[] { native }, token);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }

        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys
                .Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }

        public Task PutAsync(string key, string value, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public TON metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public TON metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}

