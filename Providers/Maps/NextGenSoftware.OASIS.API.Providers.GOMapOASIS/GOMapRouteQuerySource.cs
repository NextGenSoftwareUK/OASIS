#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Directions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.GOMapOASIS.UnitTests")]
namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS;

/// <summary>Read-only fixed public Berlin driving routes; never uses visitor coordinates.</summary>
public sealed class GOMapRouteQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "gomap/osrm/berlin/driving";
    private readonly RouteBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    public GOMapRouteQuerySource(Guid owner, IEnumerable<string> routes) : this(owner, routes, null) { }
    public static GOMapRouteQuerySource ForReviewedTeachingCatalogue(Guid owner) => new(owner, new[] { "west-north", "north-east", "east-west" });
    public static GOMapRouteQuerySource ForHostedTeachingCatalogue(Guid owner, Uri origin) =>
        new(new RouteBackend(owner, new[] { "west-north", "north-east", "east-west" }, null, origin));

    internal GOMapRouteQuerySource(Guid owner, IEnumerable<string> routes, HttpMessageHandler? handler)
        : this(new RouteBackend(owner, routes, handler, null)) { }
    private GOMapRouteQuerySource(RouteBackend backend) : base(backend, Source + ":")
    {
        this.backend = backend;
        ProviderType = new EnumValue<PType>(PType.GOMapOASIS);
        ProviderName = PType.GOMapOASIS.ToString();
        ProviderDescription = "Fixed public Berlin OSRM driving route observations";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Map);
        query = new(PType.GOMapOASIS, backend.ReadAsync, maximumConcurrentReads: 1, admittedRecordIds: backend.RecordIds);
    }
    public PType QueryProviderType => PType.GOMapOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public static Guid LogicalId(Guid owner, string key)
    {
        if (owner == Guid.Empty || key is not ("west-north" or "north-east" or "east-west"))
            throw new ArgumentException("A source owner and exact reviewed route key are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.GOMapOASIS, Source, key);
    }
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

    private sealed class RouteBackend : IKeyValueBackend, IDisposable
    {
        private readonly Guid owner;
        private readonly Dictionary<Guid, string> routes;
        private readonly HttpClient client;
        private readonly OSRMDirectionsProvider sdk;
        public Guid Probe { get; }
        public IReadOnlySet<Guid> RecordIds => routes.Keys.ToHashSet();
        public RouteBackend(Guid owner, IEnumerable<string> configured, HttpMessageHandler? handler, Uri? origin)
        {
            if (configured is null) throw new ArgumentNullException(nameof(configured));
            var keys = configured.Take(4).ToArray();
            if (keys.Length is < 1 or > 3 || keys.Distinct().Count() != keys.Length)
                throw new ArgumentException("One to three distinct reviewed route keys are required.");
            routes = keys.ToDictionary(key => LogicalId(owner, key));
            this.owner = owner; Probe = LogicalId(owner, keys[0]);
            if (handler is null)
            {
                var guarded = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
                guarded.AllowAutoRedirect = false; guarded.UseProxy = false; guarded.UseCookies = false;
                handler = guarded;
            }
            client = new HttpClient(handler);
            sdk = new OSRMDirectionsProvider(client);
            if (origin != null) sdk.BaseUrl = origin.AbsoluteUri;
        }
        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!routes.TryGetValue(id, out var key)) throw new InvalidDataException("Route identity is outside the configured teaching catalogue.");
            var points = new[] { new Geolocation(52.517037, 13.388860), new Geolocation(52.529407, 13.397634), new Geolocation(52.523219, 13.428555) };
            var index = key == "west-north" ? 0 : key == "north-east" ? 1 : 2;
            var result = await sdk.GetRouteAsync(points[index], points[(index + 1) % 3], RoutingType.Driving, token);
            if (result.IsError || result.Result is null) throw new InvalidDataException("Reviewed GOMap route unavailable: " + result.Message);
            var route = result.Result;
            var fields = new Dictionary<string, object> {
                ["name"] = "Berlin driving route " + key,
                ["description"] = "Fixed public teaching coordinates; current OSRM observation, not live traffic or a visitor journey.",
                ["GOMapRouteKey"] = key, ["GOMapRouteProfile"] = "driving", ["GOMapEndpoint"] = OSRMDirectionsProvider.DefaultBaseUrl,
                ["GOMapRouteJson"] = route.RawJson, ["GOMapResponseSha256"] = route.ResponseSha256,
                ["GOMapDistanceMetres"] = route.DistanceMetres, ["GOMapDurationSeconds"] = route.DurationSeconds,
                ["GOMapCoordinateCount"] = route.Geometry.Count, ["GOMapObservedUtc"] = route.ObservedUtc.ToString("O"),
                ["GOMapAttribution"] = "© OpenStreetMap contributors", ["GOMapAttributionUrl"] = "https://www.openstreetmap.org/copyright",
                ["GOMapFixMapUrl"] = "https://www.openstreetmap.org/fixthemap"
            };
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.GOMapOASIS,
                new NativeRecord(Source, key, fields), NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result; row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true;
            row.Children = Array.Empty<IHolon>(); row.ProviderUniqueStorageKey[PType.GOMapOASIS] = key;
            return StoredRecordJson.Serialize<IHolon>(row);
        }
        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(new NotSupportedException("Use an admitted fixed-route CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Route scans are not supported."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Reviewed public routes are read-only."));
        public void Dispose() => client.Dispose();
    }
}
