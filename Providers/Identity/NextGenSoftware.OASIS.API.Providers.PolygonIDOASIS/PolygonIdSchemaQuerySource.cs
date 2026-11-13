#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
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

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.PolygonIDOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.PolygonIDOASIS;

/// <summary>
/// Public iden3 credential schema definitions at three reviewed commit-pinned HTTPS paths.
/// Definitions are read as data; no credentials, proofs, contexts or circuits are fetched or executed.
/// </summary>
public sealed class PolygonIdSchemaQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "polygon-id/credential-schemas";
    private readonly SchemaBackend backend;
    private readonly BoundedPublicKeyValueQuery query;

    public PolygonIdSchemaQuerySource(Guid owner, IEnumerable<string> schemas)
        : this(owner, schemas, null) { }

    public static PolygonIdSchemaQuerySource ForReviewedTeachingCatalogue(Guid owner) =>
        new(owner, new[] { "age-v3", "country-v2", "auth-v1" });

    internal PolygonIdSchemaQuerySource(Guid owner, IEnumerable<string> schemas, HttpMessageHandler? handler)
        : this(new SchemaBackend(owner, schemas, handler)) { }

    private PolygonIdSchemaQuerySource(SchemaBackend backend) : base(backend, Source + ":")
    {
        this.backend = backend;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Public Polygon ID credential schema definitions";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Identity);
        query = new(QueryProviderType, backend.ReadAsync, maximumConcurrentReads: 3,
            admittedRecordIds: backend.RecordIds);
    }

    public PType QueryProviderType => PType.PolygonIDOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public static Guid LogicalId(Guid owner, string schemaKey)
    {
        if (owner == Guid.Empty || schemaKey is not ("age-v3" or "country-v2" or "auth-v1"))
            throw new ArgumentException("A source owner and reviewed public schema key are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.PolygonIDOASIS, Source,
            schemaKey);
    }

    public const string SourceCommit = "0711b5845db5f57ac8957eda0574196371535af2";

    public static string ReviewedSchemaPath(string key) => key switch
    {
        "age-v3" => "schemas/json/KYCAgeCredential-v3.json",
        "country-v2" => "schemas/json/KYCCountryOfResidenceCredential-v2.json",
        "auth-v1" => "schemas/json/auth-v1.json",
        _ => throw new ArgumentException("A reviewed Polygon ID schema key is required.")
    };

    public static string ReviewedSha256(string key) => key switch
    {
        "age-v3" => "73b9da01a67461ea71405933b4e4d1ae4928fdebe17212c12265e9a72d21fdaf",
        "country-v2" => "3e25b0ecdf2c805eac3b81de676737a084b8fda1989c78162ff517a97a157eda",
        "auth-v1" => "aa91049d1370cb12dd853f8524c922a02c178c7a4794e370fe9f4aa2d7806738",
        _ => throw new ArgumentException("A reviewed Polygon ID schema key is required.")
    };

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(backend.Probe, cancellationToken);
        IsProviderActivated = !result.IsError && result.Result;
        return result;
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        query.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    public void Dispose() { IsProviderActivated = false; backend.Dispose(); }

    private sealed class SchemaBackend : IKeyValueBackend, IDisposable
    {
        private static readonly Uri Endpoint = new("https://raw.githubusercontent.com/iden3/claim-schema-vocab/0711b5845db5f57ac8957eda0574196371535af2/");
        private readonly Guid owner;
        private readonly Dictionary<Guid, string> schemas;
        private readonly HttpClient client;
        public Guid Probe { get; }
        public IReadOnlySet<Guid> RecordIds => schemas.Keys.ToHashSet();

        public SchemaBackend(Guid owner, IEnumerable<string> configured, HttpMessageHandler? handler)
        {
            if (configured is null) throw new ArgumentNullException(nameof(configured));
            var selected = configured.Take(4).ToArray();
            if (selected.Length is < 1 or > 3 || selected.Distinct().Count() != selected.Length)
                throw new ArgumentException("One to three distinct reviewed schema keys are required.");
            schemas = selected.ToDictionary(schema => LogicalId(owner, schema));
            this.owner = owner;
            Probe = LogicalId(owner, selected[0]);
            if (handler is null)
            {
                var guarded = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
                guarded.AllowAutoRedirect = false; guarded.UseProxy = false; guarded.UseCookies = false;
                handler = guarded;
            }
            client = new HttpClient(handler);
        }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!schemas.TryGetValue(id, out var schema))
                throw new InvalidDataException("The ID is outside the configured Polygon ID schema catalogue.");
            var key = schema;
            var uri = new Uri(Endpoint, ReviewedSchemaPath(key));
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.ParseAdd("text/plain");
            request.Headers.UserAgent.ParseAdd("OASISAPI/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new HttpRequestException("The exact Polygon ID schema GET must return HTTP200.", null, response.StatusCode);
            if (response.Content.Headers.ContentType?.MediaType != "text/plain")
                throw new InvalidDataException("The Polygon ID schema must be JSON.");
            await response.Content.LoadIntoBufferAsync(65536, token);
            var bytes = await response.Content.ReadAsByteArrayAsync(token);
            var text = new UTF8Encoding(false, true).GetString(bytes);
            using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 32 });
            UniqueFields(document.RootElement);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                root.GetProperty("$schema").GetString() != "http://json-schema.org/draft-07/schema#" ||
                root.GetProperty("type").GetString() != "object" ||
                root.GetProperty("properties").ValueKind != JsonValueKind.Object ||
                root.GetProperty("required").ValueKind != JsonValueKind.Array ||
                root.GetProperty("$metadata").GetProperty("uris").ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("The complete reviewed draft-07 credential schema is required.");
            var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            if (hash != ReviewedSha256(key))
                throw new InvalidDataException("The schema differs from the exact reviewed source commit.");
            var compact = JsonSerializer.Serialize(root);
            var fields = new Dictionary<string, object>
            {
                ["name"] = "Polygon ID schema " + key,
                ["PolygonIdSchemaKey"] = key,
                ["PolygonIdSchemaUrl"] = uri.AbsoluteUri,
                ["PolygonIdSourceCommit"] = SourceCommit,
                ["PolygonIdSchemaDialect"] = root.GetProperty("$schema").GetString()!,
                ["PolygonIdContentSha256"] = hash,
                ["PolygonIdContentBytes"] = bytes.Length,
                ["PolygonIdSchemaJsonSha256"] = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(compact))),
                ["PolygonIdSchemaJson"] = compact
            };
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.PolygonIDOASIS,
                new NativeRecord(Source, key, fields), NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.PolygonIDOASIS] = key;
            return StoredRecordJson.Serialize<IHolon>(row);
        }

        private static void UniqueFields(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in value.EnumerateObject())
                {
                    if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate native schema property.");
                    UniqueFields(property.Value);
                }
            }
            else if (value.ValueKind == JsonValueKind.Array)
                foreach (var item in value.EnumerateArray()) UniqueFields(item);
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact Polygon ID schema CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Polygon ID schema scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Polygon ID schema observations are read-only."));
        public void Dispose() => client.Dispose();
    }
}
