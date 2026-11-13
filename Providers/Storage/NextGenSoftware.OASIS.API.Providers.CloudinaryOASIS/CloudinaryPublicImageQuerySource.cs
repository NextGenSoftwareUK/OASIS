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

namespace NextGenSoftware.OASIS.API.Providers.CloudinaryOASIS;

/// <summary>Metadata for a bounded catalogue of images in Cloudinary's public demo cloud.</summary>
public sealed class CloudinaryPublicImageQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "res.cloudinary.com/demo/image/upload";
    private static readonly Regex AssetPattern = new(@"\A[a-z0-9][a-z0-9_-]{0,79}\.jpg\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private readonly ImageMetadataBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public CloudinaryPublicImageQuerySource(Guid owner, IEnumerable<string> assets, string probe)
        : this(CreateBackend(owner, assets, probe, null), LogicalId(owner, probe)) { }

    internal CloudinaryPublicImageQuerySource(Guid owner, IEnumerable<string> assets, string probe, HttpMessageHandler handler)
        : this(CreateBackend(owner, assets, probe, handler), LogicalId(owner, probe)) { }

    private CloudinaryPublicImageQuerySource(ImageMetadataBackend backend, Guid probeId) : base(backend, "cloudinary-image:")
    {
        this.backend = backend;
        this.probeId = probeId;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Public Cloudinary demo image metadata";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        query = new(QueryProviderType, backend.ReadAsync);
    }

    public static Guid LogicalId(Guid owner, string asset)
    {
        if (!IsCanonicalAsset(asset)) throw new ArgumentException("A canonical Cloudinary demo JPEG asset is required.", nameof(asset));
        return NativeRecordHolonMapper.StableHolonId(owner, PType.CloudinaryOASIS, Source, asset);
    }

    private static bool IsCanonicalAsset(string asset) => asset is { Length: >= 5 and <= 84 } && AssetPattern.IsMatch(asset);

    private static ImageMetadataBackend CreateBackend(Guid owner, IEnumerable<string> assets, string probe, HttpMessageHandler? handler)
    {
        var catalogue = assets?.Take(9).ToArray() ?? Array.Empty<string>();
        if (owner == Guid.Empty || catalogue.Length is < 1 or > 8 || catalogue.Any(asset => !IsCanonicalAsset(asset)) ||
            catalogue.Distinct(StringComparer.Ordinal).Count() != catalogue.Length ||
            !catalogue.Contains(probe, StringComparer.Ordinal))
            throw new ArgumentException("A source owner, bounded Cloudinary asset catalogue and a catalogue probe are required.");
        if (handler == null)
        {
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseCookies = false;
            transport.UseProxy = false;
            handler = transport;
        }
        return new ImageMetadataBackend(owner, catalogue, new HttpClient(handler));
    }

    public PType QueryProviderType => PType.CloudinaryOASIS;
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

    private sealed class ImageMetadataBackend(Guid owner, IEnumerable<string> catalogue, HttpClient client) : IKeyValueBackend, IDisposable
    {
        private readonly Dictionary<Guid, string> assets = catalogue.ToDictionary(asset => LogicalId(owner, asset));

        public async Task<string?> ReadAsync(Guid id, CancellationToken token)
        {
            if (!assets.TryGetValue(id, out var asset))
                throw new InvalidDataException("The selected ID is outside the Cloudinary demo catalogue.");
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://{Source}/fl_getinfo/{asset}");
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.UserAgent.ParseAdd("OASISAPI/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            if (response.StatusCode != System.Net.HttpStatusCode.OK ||
                response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("Unexpected Cloudinary metadata response.");
            await response.Content.LoadIntoBufferAsync(8192, token);
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token),
                new JsonDocumentOptions { MaxDepth = 4 });
            var record = document.RootElement;
            RequireUniqueObject(record);
            var input = record.GetProperty("input");
            var output = record.GetProperty("output");
            RequireUniqueObject(input);
            RequireUniqueObject(output);
            var width = input.GetProperty("width").GetInt32();
            var height = input.GetProperty("height").GetInt32();
            var bytes = input.GetProperty("bytes").GetInt64();
            if (width <= 0 || height <= 0 || bytes <= 0 || output.GetProperty("format").GetString() != "jpg" ||
                output.GetProperty("width").GetInt32() != width || output.GetProperty("height").GetInt32() != height ||
                output.GetProperty("bytes").GetInt64() <= 0)
                throw new InvalidDataException("Cloudinary metadata differs from the reviewed JPEG delivery shape.");
            var observation = new NativeRecord(Source, asset, new Dictionary<string, object>
            {
                ["name"] = asset,
                ["description"] = $"Cloudinary public image, {width} × {height} pixels",
                ["CloudinaryAsset"] = asset,
                ["CloudinaryOriginalWidth"] = width,
                ["CloudinaryOriginalHeight"] = height,
                ["CloudinaryOriginalBytes"] = bytes,
                ["CloudinaryDeliveryFormat"] = "jpg"
            });
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.CloudinaryOASIS, observation, NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var holon = mapped.Result;
            holon.CreatedByAvatarId = owner;
            holon.IsPublic = true;
            holon.IsActive = true;
            holon.Children = Array.Empty<IHolon>();
            holon.ProviderUniqueStorageKey[PType.CloudinaryOASIS] = asset;
            return StoredRecordJson.Serialize<IHolon>(holon);
        }

        private static void RequireUniqueObject(JsonElement value)
        {
            var fields = new HashSet<string>(StringComparer.Ordinal);
            if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Any(field => !fields.Add(field.Name)))
                throw new InvalidDataException("Unexpected Cloudinary metadata object.");
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted Cloudinary CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Cloudinary teaching scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("The Cloudinary teaching source exposes exact CQL reads."));
        public void Dispose() => client.Dispose();
    }
}
