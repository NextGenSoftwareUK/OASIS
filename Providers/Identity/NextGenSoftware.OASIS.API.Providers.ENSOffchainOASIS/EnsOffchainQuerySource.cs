using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
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

namespace NextGenSoftware.OASIS.API.Providers.ENSOffchainOASIS;

/// <summary>Read-only observations from a bounded catalogue on the public Web3.bio ENS index.</summary>
public sealed class EnsOffchainQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "web3.bio/ens";
    private static readonly Regex NamePattern = new(@"^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+eth$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly Regex AddressPattern = new(@"^0x[0-9a-fA-F]{40}$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private readonly IndexBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public EnsOffchainQuerySource(Guid owner, IEnumerable<string> names, string probe)
        : this(owner, names, probe, apiKey: null) { }

    public EnsOffchainQuerySource(Guid owner, IEnumerable<string> names, string probe, string apiKey)
        : this(owner, names, probe, handler: null, apiKey: apiKey) { }

    internal EnsOffchainQuerySource(Guid owner, IEnumerable<string> names, string probe, HttpMessageHandler handler, string apiKey = null)
        : this(CreateBackend(owner, names, probe, handler, apiKey), LogicalId(owner, probe)) { }

    private EnsOffchainQuerySource(IndexBackend backend, Guid probeId) : base(backend, "ens-offchain:")
    {
        this.backend = backend;
        this.probeId = probeId;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Public Web3.bio ENS name observations";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Identity);
        query = new(QueryProviderType, backend.ReadAsync);
    }

    public static Guid LogicalId(Guid owner, string name)
    {
        if (!IsCanonicalName(name)) throw new ArgumentException("A canonical ASCII ENS name is required.", nameof(name));
        return NativeRecordHolonMapper.StableHolonId(owner, PType.ENSOffchainOASIS, Source, name);
    }

    private static bool IsCanonicalName(string name) => name is { Length: > 0 and <= 253 } &&
        name.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '.') &&
        NamePattern.IsMatch(name);

    private static IndexBackend CreateBackend(Guid owner, IEnumerable<string> names, string probe, HttpMessageHandler handler, string apiKey)
    {
        var catalogue = names?.Take(9).ToArray() ?? Array.Empty<string>();
        if (owner == Guid.Empty || catalogue.Length is < 1 or > 8 || catalogue.Any(name => !IsCanonicalName(name)) ||
            catalogue.Distinct(StringComparer.Ordinal).Count() != catalogue.Length ||
            !catalogue.Contains(probe, StringComparer.Ordinal))
            throw new ArgumentException("A source owner, bounded ENS name catalogue and a catalogue probe are required.");
        if (apiKey != null && (apiKey.Length is < 1 or > 1024 ||
            apiKey.Any(character => character is < '!' or > '~' || character == ',')))
            throw new ArgumentException("A valid Web3.bio API key is required.", nameof(apiKey));
        if (handler == null)
        {
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseCookies = false;
            transport.UseProxy = false;
            handler = transport;
        }
        return new IndexBackend(owner, catalogue, new HttpClient(handler), apiKey);
    }

    public PType QueryProviderType => PType.ENSOffchainOASIS;
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

    private sealed class IndexBackend(Guid owner, IEnumerable<string> catalogue, HttpClient client, string apiKey) : IKeyValueBackend, IDisposable
    {
        private readonly Dictionary<Guid, string> names = catalogue.ToDictionary(name => LogicalId(owner, name));

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!names.TryGetValue(id, out var name))
                throw new InvalidDataException("The selected ID is outside the ENS offchain catalogue.");
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.web3.bio/ns/ens/{name}");
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.UserAgent.ParseAdd("OASISAPI/1.0");
            if (apiKey != null)
                request.Headers.Add("X-API-KEY", "Bearer " + apiKey);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            if (response.StatusCode != System.Net.HttpStatusCode.OK ||
                response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("Unexpected ENS index response.");
            await response.Content.LoadIntoBufferAsync(128 * 1024, token);
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token),
                new JsonDocumentOptions { MaxDepth = 16 });
            var record = document.RootElement;
            var fields = new HashSet<string>(StringComparer.Ordinal);
            if (record.ValueKind != JsonValueKind.Object || record.EnumerateObject().Any(field => !fields.Add(field.Name)) ||
                record.GetProperty("identity").GetString() != name || record.GetProperty("platform").GetString() != "ens" ||
                record.GetProperty("address").GetString() is not { Length: 42 } address || !AddressPattern.IsMatch(address))
                throw new InvalidDataException("The index response did not match the requested ENS identity.");
            var observation = new NativeRecord(Source, name, new Dictionary<string, object>
            {
                ["name"] = name,
                ["EnsOffchainAddress"] = address.ToLowerInvariant(),
                ["EnsObservationPolicy"] = "web3.bio-index"
            });
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.ENSOffchainOASIS, observation, NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var holon = mapped.Result;
            holon.CreatedByAvatarId = owner;
            holon.IsPublic = true;
            holon.IsActive = true;
            holon.Children = Array.Empty<IHolon>();
            holon.ProviderUniqueStorageKey[PType.ENSOffchainOASIS] = name;
            return StoredRecordJson.Serialize<IHolon>(holon);
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted ENS offchain CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("ENS offchain scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("The ENS offchain source exposes exact CQL reads."));
        public void Dispose() => client.Dispose();
    }
}
