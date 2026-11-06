using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
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

namespace NextGenSoftware.OASIS.API.Providers.SafeOASIS;

/// <summary>Exact-key public observations of Safe Transaction Service indexing status.</summary>
public sealed class SafeIndexingQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "safe-transaction-service-indexing";
    private static readonly Dictionary<string, int> Chains = new(StringComparer.Ordinal)
    {
        ["eth"] = 1, ["base"] = 8453, ["arb1"] = 42161
    };
    private readonly IndexBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public SafeIndexingQuerySource(Guid owner, IEnumerable<string> chains, string probe, string? apiKey = null)
        : this(owner, chains, probe, apiKey, handler: null) { }

    internal SafeIndexingQuerySource(Guid owner, IEnumerable<string> chains, string probe, string? apiKey, HttpMessageHandler? handler)
        : this(new IndexBackend(owner, chains, probe, apiKey, handler), LogicalId(owner, probe)) { }

    private SafeIndexingQuerySource(IndexBackend backend, Guid probeId) : base(backend, "safe-indexing:")
    {
        this.backend = backend;
        this.probeId = probeId;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Public Safe Transaction Service indexing observations";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Blockchain);
        query = new(QueryProviderType, backend.ReadAsync);
    }

    public static Guid LogicalId(Guid owner, string chain)
    {
        if (owner == Guid.Empty || string.IsNullOrEmpty(chain) || !Chains.ContainsKey(chain))
            throw new ArgumentException("A source owner and reviewed Safe network are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.SafeOASIS, Source, chain);
    }

    public PType QueryProviderType => PType.SafeOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(probeId, cancellationToken);
        IsProviderActivated = !result.IsError && result.Result;
        return result;
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        query.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    public void Dispose()
    {
        IsProviderActivated = false;
        backend.Dispose();
    }

    private sealed class IndexBackend : IKeyValueBackend, IDisposable
    {
        private static readonly SemaphoreSlim AnonymousSlot = new(1);
        private readonly Guid owner;
        private readonly Dictionary<Guid, string> chains;
        private readonly HttpClient client;
        private readonly string? apiKey;

        public IndexBackend(Guid owner, IEnumerable<string> selected, string probe, string? apiKey, HttpMessageHandler? handler)
        {
            var catalogue = selected?.Take(4).ToArray() ?? Array.Empty<string>();
            if (owner == Guid.Empty || catalogue.Length is < 1 or > 3 ||
                catalogue.Any(chain => string.IsNullOrEmpty(chain) || !Chains.ContainsKey(chain)) ||
                catalogue.Distinct(StringComparer.Ordinal).Count() != catalogue.Length ||
                !catalogue.Contains(probe, StringComparer.Ordinal))
                throw new ArgumentException("A source owner, distinct reviewed Safe networks and a catalogue probe are required.");
            if (apiKey != null && (apiKey.Length is < 1 or > 4096 ||
                apiKey.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('.' or '-' or '_'))))
                throw new ArgumentException("A valid Safe API key is required.", nameof(apiKey));

            this.owner = owner;
            this.apiKey = apiKey;
            chains = catalogue.ToDictionary(chain => LogicalId(owner, chain));
            if (handler is null)
            {
                var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
                transport.UseCookies = false;
                transport.UseProxy = false;
                handler = transport;
            }
            client = new HttpClient(handler);
        }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!chains.TryGetValue(id, out var chain))
                throw new InvalidDataException("The selected ID is outside the Safe indexing catalogue.");
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.safe.global/tx-service/{chain}/api/v1/about/indexing/");
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.UserAgent.ParseAdd("OASISAPI/1.0");
            if (apiKey is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await SendAsync(request, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("Unexpected Safe indexing content type.");
            await response.Content.LoadIntoBufferAsync(8192, token);
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token),
                new JsonDocumentOptions { MaxDepth = 8 });
            var record = document.RootElement;
            var fields = new HashSet<string>(StringComparer.Ordinal);
            if (record.ValueKind != JsonValueKind.Object || record.EnumerateObject().Any(field => !fields.Add(field.Name)))
                throw new InvalidDataException("The Safe indexing response must be a unique-field JSON object.");

            var head = Height(record, "currentBlockNumber");
            var tokens = Height(record, "erc20BlockNumber");
            var contracts = Height(record, "masterCopiesBlockNumber");
            var observation = new NativeRecord(Source, chain, new Dictionary<string, object>
            {
                ["name"] = chain,
                ["SafeChainId"] = Chains[chain],
                ["SafeCurrentBlockNumber"] = head,
                ["SafeErc20BlockNumber"] = tokens,
                ["SafeErc20Synced"] = record.GetProperty("erc20Synced").GetBoolean(),
                ["SafeMasterCopiesBlockNumber"] = contracts,
                ["SafeMasterCopiesSynced"] = record.GetProperty("masterCopiesSynced").GetBoolean(),
                ["SafeSynced"] = record.GetProperty("synced").GetBoolean()
            });
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.SafeOASIS, observation, NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var holon = mapped.Result;
            holon.CreatedByAvatarId = owner;
            holon.IsPublic = true;
            holon.IsActive = true;
            holon.Children = Array.Empty<IHolon>();
            holon.ProviderUniqueStorageKey[PType.SafeOASIS] = chain;
            return StoredRecordJson.Serialize<IHolon>(holon);
        }

        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (apiKey is not null)
                return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

            // Safe documents two requests per second for anonymous exploration.
            await AnonymousSlot.WaitAsync(token);
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(550), token);
                return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            }
            finally { AnonymousSlot.Release(); }
        }

        private static long Height(JsonElement record, string field) =>
            record.GetProperty(field).TryGetInt64(out var height) && height >= 0
                ? height : throw new InvalidDataException("Safe block positions must be non-negative integers.");

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted Safe indexing CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Safe indexing scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Safe indexing exposes exact public CQL reads."));
        public void Dispose() => client.Dispose();
    }
}
