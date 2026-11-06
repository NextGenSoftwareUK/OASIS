#nullable enable
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

namespace NextGenSoftware.OASIS.API.Providers.DecentralandOASIS;

/// <summary>Reviewed public Decentraland Genesis City parcel scene metadata.</summary>
public sealed class DecentralandSceneQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "decentraland-genesis-city-parcel";
    private const string Probe = "0,0";
    private static readonly HashSet<string> Reviewed = new(StringComparer.Ordinal)
    {
        Probe,
        "-53,71",
        "-100,-50"
    };

    private readonly SceneBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public DecentralandSceneQuerySource(Guid owner) : this(owner, handler: null) { }

    internal DecentralandSceneQuerySource(Guid owner, HttpMessageHandler? handler)
        : this(new SceneBackend(owner, handler), owner) { }

    private DecentralandSceneQuerySource(SceneBackend backend, Guid owner) : base(backend, "decentraland-scene:")
    {
        this.backend = backend;
        probeId = LogicalId(owner, Probe);
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Reviewed public Decentraland Genesis City scenes";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Spatial);
        query = new(QueryProviderType, backend.ReadAsync, maximumConcurrentReads: 3);
    }

    public static Guid LogicalId(Guid owner, string parcel)
    {
        if (owner == Guid.Empty || parcel is null || !Reviewed.Contains(parcel))
            throw new ArgumentException("A teaching owner and reviewed Decentraland parcel are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.DecentralandOASIS, Source, parcel);
    }

    public PType QueryProviderType => PType.DecentralandOASIS;
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

    private sealed class SceneBackend : IKeyValueBackend, IDisposable
    {
        private const string Route = "https://peer.decentraland.org/content/entities/scene?pointer=";
        private readonly Guid owner;
        private readonly Dictionary<Guid, string> parcels;
        private readonly HttpClient client;

        public SceneBackend(Guid owner, HttpMessageHandler? handler)
        {
            this.owner = owner;
            parcels = Reviewed.ToDictionary(parcel => LogicalId(owner, parcel));
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
            if (!parcels.TryGetValue(id, out var parcel))
                throw new InvalidDataException("The selected ID is outside the reviewed Decentraland parcel catalogue.");

            using var request = new HttpRequestMessage(HttpMethod.Get, Route + Uri.EscapeDataString(parcel));
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.UserAgent.ParseAdd("OASISAPI/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("Catalyst must return a JSON scene response.");

            await response.Content.LoadIntoBufferAsync(256 * 1024, token);
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token),
                new JsonDocumentOptions { MaxDepth = 16 });
            var entities = document.RootElement;
            if (entities.GetArrayLength() != 1)
                throw new InvalidDataException("Catalyst must return one scene for the reviewed parcel.");

            var scene = RequireObject(entities[0]);
            var pointers = scene.GetProperty("pointers");
            var entityId = scene.GetProperty("id").GetString();
            var timestamp = scene.GetProperty("timestamp").GetInt64();
            if (scene.GetProperty("type").GetString() != "scene" || timestamp <= 0 ||
                entityId is null || !EntityId.IsMatch(entityId) ||
                pointers.GetArrayLength() is < 1 or > 2048)
                throw new InvalidDataException("Catalyst returned an invalid scene identity.");

            var exactPointers = pointers.EnumerateArray().Select(pointer => pointer.GetString()).ToArray();
            if (exactPointers.Any(pointer => pointer is null || !Coordinate.IsMatch(pointer)) ||
                exactPointers.Distinct(StringComparer.Ordinal).Count() != exactPointers.Length ||
                !exactPointers.Contains(parcel, StringComparer.Ordinal))
                throw new InvalidDataException("Catalyst did not bind the scene to the requested parcel.");

            var metadata = RequireObject(scene.GetProperty("metadata"));
            var baseParcel = RequireObject(metadata.GetProperty("scene")).GetProperty("base").GetString();
            var title = RequireObject(metadata.GetProperty("display")).GetProperty("title").GetString();
            if (!exactPointers.Contains(baseParcel, StringComparer.Ordinal) ||
                string.IsNullOrWhiteSpace(title) || title.Length > 128 || title.Any(char.IsControl))
                throw new InvalidDataException("Catalyst returned invalid public scene metadata.");

            var record = new NativeRecord(Source, parcel, new Dictionary<string, object>
            {
                ["name"] = title,
                ["DecentralandParcel"] = parcel,
                ["DecentralandBaseParcel"] = baseParcel!,
                ["DecentralandSceneEntityId"] = entityId,
                ["DecentralandSceneTimestamp"] = timestamp,
                ["DecentralandSceneParcelCount"] = exactPointers.Length
            });
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.DecentralandOASIS, record, NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner;
            row.IsPublic = true;
            row.IsActive = true;
            row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.DecentralandOASIS] = parcel;
            return StoredRecordJson.Serialize<IHolon>(row);
        }

        private static readonly Regex EntityId = new(
            @"\A(?:bafkrei[a-z2-7]{52}|Qm[1-9A-HJ-NP-Za-km-z]{44})\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
        private static readonly Regex Coordinate = new(
            @"\A(?:0|-?[1-9][0-9]{0,2}),(?:0|-?[1-9][0-9]{0,2})\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

        private static JsonElement RequireObject(JsonElement value)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Any(property => !names.Add(property.Name)))
                throw new InvalidDataException("Catalyst must return a unique-field scene object.");
            return value;
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact Decentraland parcel CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Decentraland parcel scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Decentraland exposes exact public scene reads."));
        public void Dispose() => client.Dispose();
    }
}
