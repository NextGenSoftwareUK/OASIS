using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class MapGrpcService : MapService.MapServiceBase
    {
        private static MapManager CreateMapManager(Guid avatarId = default)
            => new MapManager(avatarId);

        public override async Task<JsonResponse> SearchMapLocations(MapSearchRequest request, ServerCallContext context)
        {
            try
            {
                double? lat = request.HasLocation ? request.Latitude : (double?)null;
                double? lon = request.HasLocation ? request.Longitude : (double?)null;
                double? radius = request.RadiusKm > 0 ? request.RadiusKm : (double?)null;
                NextGenSoftware.OASIS.API.ONODE.Core.Managers.LocationType? locType = null;
                if (!string.IsNullOrWhiteSpace(request.LocationType) && System.Enum.TryParse<NextGenSoftware.OASIS.API.ONODE.Core.Managers.LocationType>(request.LocationType, true, out var lt))
                    locType = lt;
                var result = await CreateMapManager().SearchLocationsAsync(request.Query, locType, lat, lon, radius);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetNearbyLocations(GetNearbyLocationsRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await CreateMapManager(avatarId).GetNearbyLocationsAsync(avatarId, request.Latitude, request.Longitude, request.RadiusKm > 0 ? request.RadiusKm : 10.0);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetVisitHistory(MapByAvatarRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await CreateMapManager(avatarId).GetVisitHistoryAsync(avatarId, 50, 0);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetMapStats(MapByAvatarRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var result = await CreateMapManager(avatarId).GetMapStatsAsync(avatarId);
                return result.IsError ? new JsonResponse { IsError = true, Message = result.Message } : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> VisitLocation(VisitLocationRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId) || !Guid.TryParse(request.LocationId, out var locationId))
                    return new OASISGrpcResponse { IsError = true, Message = "Invalid avatar or location ID." };
                var result = await CreateMapManager(avatarId).VisitLocationAsync(avatarId, locationId, request.Purpose);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        public override Task<OASISGrpcResponse> CreateAndDrawRouteOnMapBetweenHolons(DrawRouteBetweenHolonsRequest request, ServerCallContext context)
        {
            try
            {
                var result = CreateMapManager().CreateAndDrawRouteOnMapBetweenHolons(
                    new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = Guid.TryParse(request.FromHolonId, out var f) ? f : Guid.Empty },
                    new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = Guid.TryParse(request.ToHolonId, out var t) ? t : Guid.Empty });
                return Task.FromResult(result ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed to draw route." });
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> CreateAndDrawRouteOnMapBeweenPoints(DrawRouteBetweenPointsRequest request, ServerCallContext context)
        {
            try
            {
                var points = new NextGenSoftware.OASIS.API.Core.Objects.MapPoints
                {
                    FromX = request.FromX, FromY = request.FromY, ToX = request.ToX, ToY = request.ToY
                };
                var result = CreateMapManager().CreateAndDrawRouteOnMapBeweenPoints(points);
                return Task.FromResult(result ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed to draw route." });
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> Draw2DSpriteOnHUD(DrawSpriteRequest request, ServerCallContext context)
        {
            try
            {
                var result = CreateMapManager().Draw2DSpriteOnHUD(request.Sprite, request.X, request.Y);
                return Task.FromResult(result ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed." });
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> Draw2DSpriteOnMap(DrawSpriteRequest request, ServerCallContext context)
        {
            try
            {
                var result = CreateMapManager().Draw2DSpriteOnMap(request.Sprite, request.X, request.Y);
                return Task.FromResult(result ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed." });
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> Draw3DObjectOnMap(DrawSpriteRequest request, ServerCallContext context)
        {
            try
            {
                var result = CreateMapManager().Draw3DObjectOnMap(request.Sprite, request.X, request.Y);
                return Task.FromResult(result ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed." });
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> HighlightBuildingOnMap(BuildingJsonRequest request, ServerCallContext context)
        {
            try
            {
                var building = JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.ONODE.Core.Holons.Building>(request.BuildingJson);
                if (building == null) return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = "Invalid building JSON." });
                var result = CreateMapManager().HighlightBuildingOnMap(building);
                return Task.FromResult(result ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed." });
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> SelectBuildingOnMap(BuildingJsonRequest request, ServerCallContext context)
        {
            try
            {
                var building = JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.ONODE.Core.Holons.Building>(request.BuildingJson);
                if (building == null) return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = "Invalid building JSON." });
                var result = CreateMapManager().SelectBuildingOnMap(building);
                return Task.FromResult(result ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed." });
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> SelectHolonOnMap(HolonJsonRequest request, ServerCallContext context)
        {
            try
            {
                var holon = JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.Core.Holons.Holon>(request.HolonJson);
                if (holon == null) return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = "Invalid holon JSON." });
                var result = CreateMapManager().SelectHolonOnMap(holon);
                return Task.FromResult(result ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed." });
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> SelectQuestOnMap(QuestJsonRequest request, ServerCallContext context)
        {
            try
            {
                var quest = JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.ONODE.Core.Holons.Quest>(request.QuestJson);
                if (quest == null) return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = "Invalid quest JSON." });
                var result = CreateMapManager().SelectQuestOnMap(quest);
                return Task.FromResult(result ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed." });
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> ZoomMapIn(MapFloatRequest request, ServerCallContext context)
        {
            try { var r = CreateMapManager().ZoomMapIn(request.Value); return Task.FromResult(r ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed." }); }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> ZoomMapOut(MapFloatRequest request, ServerCallContext context)
        {
            try { var r = CreateMapManager().ZoomMapOut(request.Value); return Task.FromResult(r ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed." }); }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> PanMapUp(MapFloatRequest request, ServerCallContext context)
        {
            try { var r = CreateMapManager().PanMapUp(request.Value); return Task.FromResult(r ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed." }); }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> PanMapDown(MapFloatRequest request, ServerCallContext context)
        {
            try { var r = CreateMapManager().PanMapDown(request.Value); return Task.FromResult(r ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed." }); }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> PanMapLeft(MapFloatRequest request, ServerCallContext context)
        {
            try { var r = CreateMapManager().PanMapLeft(request.Value); return Task.FromResult(r ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed." }); }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> PanMapRight(MapFloatRequest request, ServerCallContext context)
        {
            try { var r = CreateMapManager().PanMapRight(request.Value); return Task.FromResult(r ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed." }); }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> ZoomToHolonOnMap(HolonJsonRequest request, ServerCallContext context)
        {
            try
            {
                var holon = JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.Core.Holons.Holon>(request.HolonJson);
                if (holon == null) return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = "Invalid holon JSON." });
                var result = CreateMapManager().ZoomToHolonOnMap(holon);
                return Task.FromResult(result ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed." });
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }

        public override Task<OASISGrpcResponse> ZoomToQuestOnMap(QuestJsonRequest request, ServerCallContext context)
        {
            try
            {
                var quest = JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.ONODE.Core.Holons.Quest>(request.QuestJson);
                if (quest == null) return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = "Invalid quest JSON." });
                var result = CreateMapManager().ZoomToQuestOnMap(quest);
                return Task.FromResult(result ? new OASISGrpcResponse() : new OASISGrpcResponse { IsError = true, Message = "Failed." });
            }
            catch (Exception ex) { return Task.FromResult(new OASISGrpcResponse { IsError = true, Message = ex.Message }); }
        }
    }
}
