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

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.MapLibreOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.MapLibreOASIS;

/// <summary>
/// Public MapLibre demo style documents at three explicitly reviewed HTTPS paths.
/// Documents are read as data; no tile resources or map rendering operations are executed.
/// </summary>
public sealed class MapLibreStyleQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "maplibre/demo-styles";
    private readonly StyleBackend backend;
    private readonly BoundedPublicKeyValueQuery query;

    public MapLibreStyleQuerySource(Guid owner, IEnumerable<string> styles)
        : this(owner, styles, null) { }

    public static MapLibreStyleQuerySource ForReviewedTeachingCatalogue(Guid owner) =>
        new(owner, new[] { "world", "bright", "terrain" });

    internal MapLibreStyleQuerySource(Guid owner, IEnumerable<string> styles, HttpMessageHandler? handler)
        : this(new StyleBackend(owner, styles, handler)) { }

    private MapLibreStyleQuerySource(StyleBackend backend) : base(backend, Source + ":")
    {
        this.backend = backend;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Public MapLibre demo style documents";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Map);
        query = new(QueryProviderType, backend.ReadAsync, maximumConcurrentReads: 3,
            admittedRecordIds: backend.RecordIds);
    }

    public PType QueryProviderType => PType.MapLibreOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public static Guid LogicalId(Guid owner, string styleKey)
    {
        if (owner == Guid.Empty || styleKey is not ("world" or "bright" or "terrain"))
            throw new ArgumentException("A source owner and reviewed public style key are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.MapLibreOASIS, Source,
            styleKey);
    }

    public static string ReviewedStylePath(string key) => key switch
    {
        "world" => "style.json",
        "bright" => "styles/osm-bright-gl-style/style.json",
        "terrain" => "styles/osm-bright-gl-terrain/style.json",
        _ => throw new ArgumentException("A reviewed MapLibre style key is required.")
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

    private sealed class StyleBackend : IKeyValueBackend, IDisposable
    {
        private static readonly Uri Endpoint = new("https://demotiles.maplibre.org/");
        private readonly Guid owner;
        private readonly Dictionary<Guid, string> styles;
        private readonly HttpClient client;
        public Guid Probe { get; }
        public IReadOnlySet<Guid> RecordIds => styles.Keys.ToHashSet();

        public StyleBackend(Guid owner, IEnumerable<string> configured, HttpMessageHandler? handler)
        {
            if (configured is null) throw new ArgumentNullException(nameof(configured));
            var selected = configured.Take(4).ToArray();
            if (selected.Length is < 1 or > 3 || selected.Distinct().Count() != selected.Length)
                throw new ArgumentException("One to three distinct reviewed style keys are required.");
            styles = selected.ToDictionary(style => LogicalId(owner, style));
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
            if (!styles.TryGetValue(id, out var style))
                throw new InvalidDataException("The ID is outside the configured MapLibre style catalogue.");
            var key = style;
            var uri = new Uri(Endpoint, ReviewedStylePath(key));
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.UserAgent.ParseAdd("OASISAPI/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new HttpRequestException("The exact MapLibre style GET must return HTTP200.", null, response.StatusCode);
            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("The MapLibre style must be JSON.");
            await response.Content.LoadIntoBufferAsync(262144, token);
            var bytes = await response.Content.ReadAsByteArrayAsync(token);
            var text = new UTF8Encoding(false, true).GetString(bytes);
            using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 48 });
            UniqueFields(document.RootElement);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.GetProperty("version").GetInt32() != 8)
                throw new InvalidDataException("A version8 MapLibre style is required.");
            var name = root.GetProperty("name").GetString();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 256 || name.Any(char.IsControl))
                throw new InvalidDataException("A bounded native style name is required.");
            var sources = root.GetProperty("sources"); var layers = root.GetProperty("layers");
            if (sources.ValueKind != JsonValueKind.Object || sources.EnumerateObject().Count() is < 1 or > 32 ||
                layers.ValueKind != JsonValueKind.Array || layers.GetArrayLength() is < 1 or > 512)
                throw new InvalidDataException("Bounded nonempty style sources and layers are required.");
            var sourceIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in sources.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(source.Name) || source.Name.Length > 128 ||
                    source.Value.GetProperty("type").GetString() is not ("vector" or "raster" or "raster-dem" or "geojson" or "image" or "video" or "canvas"))
                    throw new InvalidDataException("A supported named style source is required.");
                sourceIds.Add(source.Name);
            }
            var layerIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var layer in layers.EnumerateArray())
            {
                var idText = layer.GetProperty("id").GetString();
                if (string.IsNullOrWhiteSpace(idText) || idText.Length > 128 || !layerIds.Add(idText) ||
                    layer.GetProperty("type").GetString() is not ("background" or "fill" or "line" or "symbol" or "raster" or "circle" or "fill-extrusion" or "heatmap" or "hillshade" or "color-relief"))
                    throw new InvalidDataException("Distinct bounded layer IDs and supported layer types are required.");
                if (layer.TryGetProperty("source", out var selected) && !sourceIds.Contains(selected.GetString()!))
                    throw new InvalidDataException("A layer references a source outside its style document.");
            }
            var compactStyle = JsonSerializer.Serialize(root);
            var fields = new Dictionary<string, object>
            {
                ["name"] = name,
                ["MapLibreStyleKey"] = key,
                ["MapLibreStyleUrl"] = uri.AbsoluteUri,
                ["MapLibreStyleVersion"] = 8,
                ["MapLibreSourceCount"] = sourceIds.Count,
                ["MapLibreLayerCount"] = layerIds.Count,
                ["MapLibreContentSha256"] = Convert.ToHexStringLower(SHA256.HashData(bytes)),
                ["MapLibreContentBytes"] = bytes.Length,
                ["MapLibreStyleJsonSha256"] = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(compactStyle))),
                ["MapLibreStyleJson"] = compactStyle
            };
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.MapLibreOASIS,
                new NativeRecord(Source, key, fields), NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.MapLibreOASIS] = key;
            return StoredRecordJson.Serialize<IHolon>(row);
        }

        private static void UniqueFields(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in value.EnumerateObject())
                {
                    if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate native style property.");
                    UniqueFields(property.Value);
                }
            }
            else if (value.ValueKind == JsonValueKind.Array)
                foreach (var item in value.EnumerateArray()) UniqueFields(item);
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact MapLibre style CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("MapLibre style scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("MapLibre style observations are read-only."));
        public void Dispose() => client.Dispose();
    }
}
