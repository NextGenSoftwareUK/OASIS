using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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

namespace NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS;

/// <summary>HEAD metadata for an exact catalogue in Oracle's public workshop bucket.</summary>
public sealed class OracleObjectStoragePublicMetadataQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "objectstorage.us-phoenix-1.oraclecloud.com/n/oraclebigdatadb/b/workshop-data/o";
    private static readonly Regex KeyPattern = new(@"\Abds-livelabs/[a-z0-9][a-z0-9-]{0,79}\.sh\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private readonly ObjectMetadataBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public OracleObjectStoragePublicMetadataQuerySource(Guid owner, IEnumerable<string> keys, string probe)
        : this(CreateBackend(owner, keys, probe, null), LogicalId(owner, probe)) { }

    internal OracleObjectStoragePublicMetadataQuerySource(Guid owner, IEnumerable<string> keys, string probe, HttpMessageHandler handler)
        : this(CreateBackend(owner, keys, probe, handler), LogicalId(owner, probe)) { }

    private OracleObjectStoragePublicMetadataQuerySource(ObjectMetadataBackend backend, Guid probeId) : base(backend, "oci-public-object:")
    {
        this.backend = backend;
        this.probeId = probeId;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Public Oracle Object Storage workshop metadata";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        query = new(QueryProviderType, backend.ReadAsync);
    }

    public static Guid LogicalId(Guid owner, string key)
    {
        if (!IsCanonicalKey(key)) throw new ArgumentException("A canonical Oracle workshop object key is required.", nameof(key));
        return NativeRecordHolonMapper.StableHolonId(owner, PType.OracleObjectStorageOASIS, Source, key);
    }

    private static bool IsCanonicalKey(string key) => key is { Length: >= 17 and <= 96 } && KeyPattern.IsMatch(key);

    private static ObjectMetadataBackend CreateBackend(Guid owner, IEnumerable<string> keys, string probe, HttpMessageHandler? handler)
    {
        var catalogue = keys?.Take(9).ToArray() ?? Array.Empty<string>();
        if (owner == Guid.Empty || catalogue.Length is < 1 or > 8 || catalogue.Any(key => !IsCanonicalKey(key)) ||
            catalogue.Distinct(StringComparer.Ordinal).Count() != catalogue.Length ||
            !catalogue.Contains(probe, StringComparer.Ordinal))
            throw new ArgumentException("A source owner, bounded Oracle Object Storage key catalogue and a catalogue probe are required.");
        if (handler == null)
        {
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseCookies = false;
            transport.UseProxy = false;
            handler = transport;
        }
        return new ObjectMetadataBackend(owner, catalogue, new HttpClient(handler));
    }

    public PType QueryProviderType => PType.OracleObjectStorageOASIS;
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

    private sealed class ObjectMetadataBackend(Guid owner, IEnumerable<string> catalogue, HttpClient client) : IKeyValueBackend, IDisposable
    {
        private readonly Dictionary<Guid, string> keys = catalogue.ToDictionary(key => LogicalId(owner, key));

        public async Task<string?> ReadAsync(Guid id, CancellationToken token)
        {
            if (!keys.TryGetValue(id, out var key))
                throw new InvalidDataException("The selected ID is outside the Oracle Object Storage workshop catalogue.");
            using var request = new HttpRequestMessage(HttpMethod.Head, $"https://{Source}/{key}");
            request.Headers.UserAgent.ParseAdd("OASISAPI/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            if (response.StatusCode != System.Net.HttpStatusCode.OK ||
                response.Content.Headers.ContentLength is not long bytes || bytes < 0 ||
                response.Content.Headers.LastModified is not DateTimeOffset modified ||
                response.Content.Headers.ContentType?.MediaType is not string mediaType ||
                !response.Headers.NonValidated.TryGetValues("ETag", out var tags) || tags.Count != 1 ||
                tags.Single() is not { Length: > 0 and <= 256 } etag || etag.Any(character => character <= ' ' || character > '~'))
                throw new InvalidDataException("Oracle Object Storage returned incomplete public HEAD metadata.");
            var observation = new NativeRecord(Source, key, new Dictionary<string, object>
            {
                ["name"] = key,
                ["description"] = "Oracle Object Storage public workshop object",
                ["OciWorkshopObjectKey"] = key,
                ["OciObjectBytes"] = bytes,
                ["OciObjectETag"] = etag,
                ["OciObjectLastModifiedUtc"] = modified.UtcDateTime.ToString("O"),
                ["OciObjectMediaType"] = mediaType
            });
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.OracleObjectStorageOASIS, observation, NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var holon = mapped.Result;
            holon.CreatedByAvatarId = owner;
            holon.IsPublic = true;
            holon.IsActive = true;
            holon.Children = Array.Empty<IHolon>();
            holon.ProviderUniqueStorageKey[PType.OracleObjectStorageOASIS] = key;
            return StoredRecordJson.Serialize<IHolon>(holon);
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted Oracle Object Storage CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Oracle Object Storage teaching scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("The Oracle Object Storage teaching source exposes exact CQL reads."));
        public void Dispose() => client.Dispose();
    }
}
