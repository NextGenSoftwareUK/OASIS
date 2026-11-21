using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

/// <summary>Recorded routes with immutable geometry and conditional visibility changes. Geometry loads only when a route is opened.
/// Public routes are visible to authenticated OASIS avatars; private routes remain owner-only.</summary>
[ApiController, Authorize, Route("api/map/routes")]
public sealed class MapRoutesController : OASISControllerBase
{
    private const string HeaderType = "OASIS.MapRoute.v1";
    private const string GeometryType = "OASIS.MapRouteGeometry.v1";
    private HolonManager data;
    private HolonManager Data
    {
        get
        {
            if (data != null) return data;
            if (!AdmittedStorageManagerAcquisition.TryCreate(
                () => OASISBootLoader.OASISBootLoader.OASISDNA,
                () => ProviderManager.Instance.CurrentStorageProvider,
                (provider, dna) => new HolonManager(null, dna, ProviderManager.Instance),
                out HolonManager admitted, admissionOnly: false))
                throw new InvalidOperationException("Recorded route storage requires HyperDrive V2.");
            return data = admitted;
        }
    }

    private static Guid Identity(string value)
        => new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(value)).Take(16).ToArray());
    private static Guid Catalogue(Guid owner) => Identity($"oasis:map-routes:{owner:D}");
    private static Guid Geometry(Guid route) => Identity($"oasis:map-route-geometry:{route:D}");
    private static OASISResult<T> Error<T>(string message)
        => new() { IsError = true, ErrorCode = "MAP_ROUTE_INVALID", Message = message };

    public sealed class Point
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime? Utc { get; set; }
    }
    public sealed class RecordedRoute
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        // Reported provenance does not authenticate an originating app or confer rewards.
        public string SourceApp { get; set; }
        public string SourceRouteId { get; set; }
        public bool IsPublic { get; set; }
        public List<List<Point>> Segments { get; set; }
    }
    public sealed class CreateRouteRequest : HolonWriteIntentRequest
    {
        public RecordedRoute Route { get; set; }
    }
    public sealed class RouteAccessRequest : HolonWriteIntentRequest
    {
        public string Name { get; set; }
        public bool? IsPublic { get; set; }
    }
    public sealed class Summary
    {
        public string SourceApp { get; set; }
        public string SourceRouteId { get; set; }
        public int PointCount { get; set; }
        public int SegmentCount { get; set; }
    }

    private static bool ValidText(string value) => !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 256 && !value.Any(char.IsControl);
    private static string Validate(RecordedRoute route)
    {
        if (route == null || route.Id == Guid.Empty || !ValidText(route.Name) ||
            !ValidText(route.SourceApp) || !ValidText(route.SourceRouteId))
            return "A stable route ID, name and reported source app/route identity are required (at most 256 characters each).";
        if (route.Segments == null || route.Segments.Count == 0 || route.Segments.Count > 50000 ||
            route.Segments.Any(s => s == null || s.Count < 2) || route.Segments.Sum(s => (long)s.Count) > 100000)
            return "A route requires connected segments of at least two points, with at most 100,000 total points.";
        if (route.Segments.SelectMany(s => s).Any(p => p == null || !double.IsFinite(p.Latitude) ||
            Math.Abs(p.Latitude) > 90 || !double.IsFinite(p.Longitude) || Math.Abs(p.Longitude) > 180 ||
            p.Utc.HasValue && p.Utc.Value.Kind != DateTimeKind.Utc))
            return "Route coordinates must be finite WGS84 degrees and optional timestamps must be UTC.";
        return null;
    }

    [HttpPost]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<OASISResult<IEnumerable<IHolon>>> Create([FromBody] CreateRouteRequest request)
    {
        var actor = AvatarId;
        if (actor == Guid.Empty) return Error<IEnumerable<IHolon>>("An authenticated avatar is required.");
        var invalid = Validate(request?.Route);
        if (invalid != null) return Error<IEnumerable<IHolon>>(invalid);
        var route = request.Route;
        var geometryId = Geometry(route.Id);
        // Create-only intent prevents replacing an existing route or an unrelated holon.
        if (request.ExpectedRevisions == null || request.ExpectedRevisions.Count != 2 ||
            !request.ExpectedRevisions.TryGetValue(route.Id, out var headerRevision) || headerRevision.HasValue ||
            !request.ExpectedRevisions.TryGetValue(geometryId, out var geometryRevision) || geometryRevision.HasValue)
            return Error<IEnumerable<IHolon>>("Supply create-only (null) revisions for both route and deterministic geometry IDs.");
        var geometryJson = JsonConvert.SerializeObject(route.Segments);
        if (Encoding.UTF8.GetByteCount(geometryJson) > 8 * 1024 * 1024)
            return Error<IEnumerable<IHolon>>("Serialized route geometry must not exceed 8 MB.");
        Holon Record(Guid id, Guid parent, string type, string payload) => new()
        {
            Id = id, ParentHolonId = parent, Name = route.Name, HolonType = HolonType.Custom,
            CustomHolonType = type, CreatedByAvatarId = actor, IsPublic = route.IsPublic, IsActive = true,
            MetaData = new Dictionary<string, object> { ["payload"] = payload }
        };
        var summary = new Summary { SourceApp = route.SourceApp, SourceRouteId = route.SourceRouteId,
            PointCount = route.Segments.Sum(s => s.Count), SegmentCount = route.Segments.Count };
        return await HolonWriteEndpoint.SaveBatchAsync(Data, new SaveHolonsRequest
        {
            Holons = new List<Holon> {
                Record(route.Id, Catalogue(actor), HeaderType, JsonConvert.SerializeObject(summary)),
                Record(geometryId, route.Id, GeometryType, geometryJson) },
            OperationId = request.OperationId, ExpectedRevisions = request.ExpectedRevisions,
            ProviderType = request.ProviderType, SetGlobally = request.SetGlobally,
            SaveChildren = false, Recursive = false, MaxChildDepth = 0
        }, actor, trustedWizard: false, Request.Headers[HolonWriteEndpoint.OperationHeader].ToString());
    }

    [HttpGet]
    public async Task<OASISResult<object>> List([FromQuery] int offset = 0, [FromQuery] int limit = 50)
    {
        var actor = AvatarId;
        if (actor == Guid.Empty || offset < 0 || limit < 1 || limit > 100)
            return Error<object>("An authenticated avatar and a valid page (1–100 routes) are required.");
        var loaded = await Data.LoadHolonsForParentAsync(Catalogue(actor), HolonType.Custom,
            loadChildren: false, recursive: false, avatarId: actor);
        var result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>, object>(loaded);
        if (loaded.IsError) return result;
        if (loaded.Result == null) return Error<object>("The route catalogue returned no collection.");
        var rows = loaded.Result.Where(h => h.CustomHolonType == HeaderType && h.CreatedByAvatarId == actor && !h.IsDeleted)
            .OrderByDescending(h => h.CreatedDate).ThenBy(h => h.Id).ToList();
        result.Result = new { Items = rows.Skip(offset).Take(limit), Total = rows.Count,
            NextOffset = offset + limit < rows.Count ? (int?)(offset + limit) : null };
        return result;
    }

    [HttpGet("{id:guid}")]
    public async Task<OASISResult<object>> Get(Guid id)
    {
        var actor = AvatarId;
        if (actor == Guid.Empty || id == Guid.Empty) return Error<object>("An authenticated avatar and route ID are required.");
        // Pin both observations to one authority before applying the route access policy.
        var header = await Data.LoadHolonForWriteAsync(id, actor);
        if (header.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<HolonAuthorityRead, object>(header);
        var row = header.Result.Record;
        if (row == null || row.IsDeleted || row.CustomHolonType != HeaderType ||
            row.ParentHolonId != Catalogue(row.CreatedByAvatarId) || row.CreatedByAvatarId != actor && !row.IsPublic)
            return Error<object>("This recorded route is unavailable to the current avatar.");
        var geometry = await Data.LoadHolonForWriteAsync(Geometry(id), actor, header.Result.Authority);
        if (geometry.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<HolonAuthorityRead, object>(geometry);
        var points = geometry.Result.Record;
        if (points == null || points.IsDeleted || points.CustomHolonType != GeometryType || points.ParentHolonId != id ||
            points.CreatedByAvatarId != row.CreatedByAvatarId || points.IsPublic != row.IsPublic)
            return Error<object>("The route summary and geometry do not agree.");
        var result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<HolonAuthorityRead, object>(geometry);
        result.Result = new { Header = row, Geometry = points, Authority = header.Result.Authority.ToString() };
        if (header.IsWarning) { result.IsWarning = true; result.Message = string.Join(" ", new[] { header.Message, result.Message }.Where(m => !string.IsNullOrWhiteSpace(m))); }
        return result;
    }

    [HttpPut("{id:guid}/access")]
    public async Task<OASISResult<IEnumerable<IHolon>>> UpdateAccess(Guid id, [FromBody] RouteAccessRequest request)
    {
        var actor = AvatarId;
        var geometryId = Geometry(id);
        if (actor == Guid.Empty || id == Guid.Empty || request?.IsPublic == null ||
            request.Name != null && !ValidText(request.Name))
            return Error<IEnumerable<IHolon>>("An authenticated owner, route ID, explicit visibility and optional valid name are required.");
        if (request.ExpectedRevisions == null || request.ExpectedRevisions.Count != 2 ||
            !request.ExpectedRevisions.TryGetValue(id, out var headerRevision) || !headerRevision.HasValue ||
            !request.ExpectedRevisions.TryGetValue(geometryId, out var geometryRevision) || !geometryRevision.HasValue)
            return Error<IEnumerable<IHolon>>("Supply the observed revisions for both route and geometry.");
        var provider = ProviderType.Default;
        if (!string.IsNullOrWhiteSpace(request.ProviderType) &&
            (!Enum.TryParse(request.ProviderType, true, out provider) || !Enum.IsDefined(typeof(ProviderType), provider)))
            return Error<IEnumerable<IHolon>>("A valid storage authority is required.");
        var header = await Data.LoadHolonForWriteAsync(id, actor, provider);
        if (header.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<HolonAuthorityRead, IEnumerable<IHolon>>(header);
        if (header.Result.Record is not Holon row || row.IsDeleted || row.CreatedByAvatarId != actor ||
            row.CustomHolonType != HeaderType || row.ParentHolonId != Catalogue(actor))
            return Error<IEnumerable<IHolon>>("Only the recorded route owner can change its access.");
        var geometry = await Data.LoadHolonForWriteAsync(geometryId, actor, header.Result.Authority);
        if (geometry.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<HolonAuthorityRead, IEnumerable<IHolon>>(geometry);
        if (geometry.Result.Record is not Holon points || points.IsDeleted || points.CreatedByAvatarId != actor ||
            points.CustomHolonType != GeometryType || points.ParentHolonId != id || points.IsPublic != row.IsPublic)
            return Error<IEnumerable<IHolon>>("The owned route summary and geometry do not agree.");
        row.IsPublic = points.IsPublic = request.IsPublic.Value;
        if (request.Name != null) row.Name = points.Name = request.Name;
        return await HolonWriteEndpoint.SaveBatchAsync(Data, new SaveHolonsRequest
        {
            Holons = new List<Holon> { row, points },
            OperationId = request.OperationId, ExpectedRevisions = request.ExpectedRevisions,
            ProviderType = header.Result.Authority.ToString(), SetGlobally = request.SetGlobally,
            SaveChildren = false, Recursive = false, MaxChildDepth = 0
        }, actor, trustedWizard: false, Request.Headers[HolonWriteEndpoint.OperationHeader].ToString());
    }
}
