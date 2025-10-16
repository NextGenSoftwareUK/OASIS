using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class MapManager : OASISManager, IMapManager
    {
        private static MapManager _instance;
        private readonly object _lockObject = new object();
        //private readonly Dictionary<Guid, List<Mission>> _avatarMissions;
        //private readonly Dictionary<Guid, List<MissionProgress>> _missionProgress;
        private readonly Dictionary<Guid, List<MapVisit>> _mapVisits;

        //public static MapManager Instance
        //{
        //    get
        //    {
        //        if (_instance == null)
        //            _instance = new MapManager(ProviderManager.Instance.CurrentStorageProvider);

        //        return _instance;
        //    }
        //}

        public MapManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {

        }

        public MapManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {

        }

        public IOASISMapProvider CurrentMapProvider { get; set; }
        public MapProviderType CurrentMapProviderType { get; set; }

        public void SetCurrentMapProvider(MapProviderType mapProviderType)
        {
            CurrentMapProviderType = mapProviderType;
        }

        public void SetCurrentMapProvider(IOASISMapProvider mapProvider)
        {
            CurrentMapProvider = mapProvider;
            CurrentMapProviderType = mapProvider.MapProviderType;
        }

        public bool Draw3DObjectOnMap(object obj, float x, float y)
        {
            return CurrentMapProvider.Draw3DObjectOnMap(obj, x, y);
        }

        public bool Draw2DSpriteOnMap(object sprite, float x, float y)
        {
            return CurrentMapProvider.Draw2DSpriteOnMap(sprite, x, y);
        }

        public bool Draw2DSpriteOnHUD(object sprite, float x, float y)
        {
            return CurrentMapProvider.Draw2DSpriteOnHUD(sprite, x, y);
        }

        public bool HighlightBuildingOnMap(IBuilding building)
        {
            return true;
        }

        public bool PlaceHolonOnMap(IHolon holon, float x, float y)
        {
            return CurrentMapProvider.PlaceHolonOnMap(holon, x, y);
        }

        public bool PlaceBuildingOnMap(IBuilding building, float x, float y)
        {
            return CurrentMapProvider.PlaceBuildingOnMap(building, x, y);
        }

        public bool PlaceQuestOnMap(IQuest quest, float x, float y)
        {
            return CurrentMapProvider.PlaceQuestOnMap(quest, x, y);
        }

        public bool PlaceGeoNFTOnMap(IOASISGeoSpatialNFT geoNFT, float x, float y)
        {
            return CurrentMapProvider.PlaceGeoNFTOnMap(geoNFT, x, y);
        }

        public bool PlaceGeoHotSpotOnMap(IGeoHotSpot geoHotSpot, float x, float y)
        {
            return CurrentMapProvider.PlaceGeoHotSpotOnMap(geoHotSpot, x, y);
        }

        public bool PlaceOAPPOnMap(IOAPP OAPP, float x, float y)
        {
            return CurrentMapProvider.PlaceOAPPOnMap(OAPP, x, y);
        }

        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY, Color colour)
        {
            return CurrentMapProvider.DrawRouteOnMap(startX, startY, endX, endY, colour);
        }

        public bool CreateAndDrawRouteOnMapBeweenPoints(MapPoints points)
        {
            return CurrentMapProvider.CreateAndDrawRouteOnMapBeweenPoints(points);
        }

        public bool CreateAndDrawRouteOnMapBetweenHolons(IHolon fromHolon, IHolon toHolon)
        {
            return CurrentMapProvider.CreateAndDrawRouteOnMapBetweenHolons(fromHolon, toHolon);
        }

        public bool CreateAndDrawRouteOnMapBetweenHolons(Guid fromHolonId, Guid toHolonId)
        {
            return CurrentMapProvider.CreateAndDrawRouteOnMapBetweenHolons(fromHolonId, toHolonId);
        }

        public bool CreateAndDrawRouteOnMapBetweenQuests(IQuest fromQuest, IQuest toQuest)
        {
            return CurrentMapProvider.CreateAndDrawRouteOnMapBetweenQuests(fromQuest, toQuest);
        }

        public bool CreateAndDrawRouteOnMapBetweenQuests(Guid fromQuestId, Guid toQuestId)
        {
            return CurrentMapProvider.CreateAndDrawRouteOnMapBetweenQuests(fromQuestId, toQuestId);
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoNFTs(IOASISGeoSpatialNFT fromGeoNFT, IOASISGeoSpatialNFT toGeoNFT)
        {
            return CurrentMapProvider.CreateAndDrawRouteOnMapBetweenGeoNFTs(fromGeoNFT, toGeoNFT);
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoNFTs(Guid fromGeoNFTId, Guid toGeoNFTId)
        {
            return CurrentMapProvider.CreateAndDrawRouteOnMapBetweenGeoNFTs(fromGeoNFTId, toGeoNFTId);
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(IGeoHotSpot fromGeoHotSpot, IGeoHotSpot toGeoHotSpot)
        {
            return CurrentMapProvider.CreateAndDrawRouteOnMapBetweenGeoHotSpots(fromGeoHotSpot, toGeoHotSpot);
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(Guid fromGeoHotSpotId, Guid toGeoHotSpotId)
        {
            return CurrentMapProvider.CreateAndDrawRouteOnMapBetweenGeoHotSpots(fromGeoHotSpotId, toGeoHotSpotId);
        }

        public bool CreateAndDrawRouteOnMapBetweenOAPPs(IOAPP fromOAPP, IOAPP toOAPP)
        {
            return CurrentMapProvider.CreateAndDrawRouteOnMapBetweenOAPPs(fromOAPP, toOAPP);
        }

        public bool CreateAndDrawRouteOnMapBetweenOAPPs(Guid fromOAPPId, Guid toOAPPId)
        {
            return CurrentMapProvider.CreateAndDrawRouteOnMapBetweenOAPPs(fromOAPPId, toOAPPId);
        }

        public bool CreateAndDrawRouteOnMapBetweenBuildings(IBuilding fromBuilding, IBuilding toBuilding)
        {
            return CurrentMapProvider.CreateAndDrawRouteOnMapBetweenBuildings(fromBuilding, toBuilding);
        }

        public bool CreateAndDrawRouteOnMapBetweenBuildings(Guid fromBuildingId, Guid toBuildingId)
        {
            return CurrentMapProvider.CreateAndDrawRouteOnMapBetweenBuildings(fromBuildingId, toBuildingId);
        }

        public bool ZoomMapOut(float value)
        {
            return CurrentMapProvider.ZoomMapOut(value);
        }

        public bool ZoomMapIn(float value)
        {
            return CurrentMapProvider.ZoomMapIn(value);
        }

        public bool PanMapLeft(float value)
        {
            return CurrentMapProvider.PanMapLeft(value);
        }

        public bool PanMapRight(float value)
        {
            return CurrentMapProvider.PanMapRight(value);
        }

        public bool PanMapUp(float value)
        {
            return CurrentMapProvider.PanMapUp(value);
        }

        public bool PanMapDown(float value)
        {
            return CurrentMapProvider.PanMapDown(value);
        }

        //Select is same as Zoom so these functions are now redundant because zoom will zoom to and select the item on the map...
        public bool SelectHolonOnMap(IHolon holon)
        {
            return true;
        }

        public bool SelectHolonOnMap(Guid holonId)
        {
            return true;
        }

        public bool SelectQuestOnMap(IQuest quest)
        {
            return true;
        }

        public bool SelectQuestOnMap(Guid questId)
        {
            return true;
        }

        public bool SelectGeoNFTOnMap(IOASISGeoSpatialNFT geoNFT)
        {
            return true;
        }

        public bool SelectGeoNFTOnMap(Guid geoNFTId)
        {
            return true;
        }

        public bool SelectGeoHotSpotOnMap(IGeoHotSpot geoHotSpot)
        {
            return true;
        }

        public bool SelectGeoHotSpotOnMap(Guid geoHotSpotId)
        {
            return true;
        }

        public bool SelectOAPPOnMap(IOAPP oapp)
        {
            return true;
        }

        public bool SelectOAPPOnMap(Guid oappId)
        {
            return true;
        }

        public bool SelectBuildingOnMap(IBuilding building)
        {
            return true;
        }

        public bool ZoomToHolonOnMap(IHolon holon)
        {
            return CurrentMapProvider.ZoomToHolonOnMap(holon);
        }

        public bool ZoomToHolonOnMap(Guid holonId)
        {
            return CurrentMapProvider.ZoomToHolonOnMap(holonId);
        }

        public bool ZoomToQuestOnMap(IQuest quest)
        {
            return CurrentMapProvider.ZoomToQuestOnMap(quest);
        }

        public bool ZoomToQuestOnMap(Guid questId)
        {
            return CurrentMapProvider.ZoomToQuestOnMap(questId);
        }

        public bool ZoomToGeoNFTOnMap(IOASISGeoSpatialNFT geoNFT)
        {
            return CurrentMapProvider.ZoomToGeoNFTOnMap(geoNFT);
        }

        public bool ZoomToGeoNFTOnMap(Guid geoNFTId)
        {
            return CurrentMapProvider.ZoomToGeoNFTOnMap(geoNFTId);
        }

        public bool ZoomToGeoHotSpotOnMap(IGeoHotSpot geoHotSpot)
        {
            return CurrentMapProvider.ZoomToGeoHotSpotOnMap(geoHotSpot);
        }

        public bool ZoomToGeoHotSpotOnMap(Guid geoHotSpotId)
        {
            return CurrentMapProvider.ZoomToGeoHotSpotOnMap(geoHotSpotId);
        }

        public bool ZoomToOAPPOnMap(IOAPP oapp)
        {
            return CurrentMapProvider.ZoomToOAPPOnMap(oapp);
        }

        public bool ZoomToOAPPOnMap(Guid oappId)
        {
            return CurrentMapProvider.ZoomToOAPPOnMap(oappId);
        }

        public bool ZoomToBuildingOnMap(IBuilding building)
        {
            return CurrentMapProvider.ZoomToBuildingOnMap(building);
        }

        public bool ZoomToBuildingOnMap(Guid buildingId)
        {
            return CurrentMapProvider.ZoomToBuildingOnMap(buildingId);
        }

        public bool ZoomToCoOrdsOnMap(float x, float y)
        {
            return CurrentMapProvider.ZoomToCoOrdsOnMap(x, y);
        }

        public async Task<OASISResult<List<MapLocation>>> GetNearbyLocationsAsync(Guid avatarId, double latitude, double longitude, double radiusKm = 10.0)
        {
            var result = new OASISResult<List<MapLocation>>();
            try
            {
                var nearbyLocations = new List<MapLocation>();

                // In a real implementation, this would query a spatial database
                // For now, return mock locations within the radius
                var mockLocations = new List<MapLocation>
                {
                    new MapLocation
                    {
                        Id = Guid.NewGuid(),
                        Name = "OASIS Central Hub",
                        Description = "The main gathering place in the OASIS",
                        Latitude = latitude + 0.001,
                        Longitude = longitude + 0.001,
                        Type = LocationType.Hub,
                        IsActive = true
                    },
                    new MapLocation
                    {
                        Id = Guid.NewGuid(),
                        Name = "Quest Giver Station",
                        Description = "Where avatars can pick up new quests",
                        Latitude = latitude - 0.002,
                        Longitude = longitude + 0.003,
                        Type = LocationType.Quest,
                        IsActive = true
                    },
                    new MapLocation
                    {
                        Id = Guid.NewGuid(),
                        Name = "Social Plaza",
                        Description = "A place for avatars to meet and socialize",
                        Latitude = latitude + 0.005,
                        Longitude = longitude - 0.001,
                        Type = LocationType.Social,
                        IsActive = true
                    }
                };

                result.Result = mockLocations;
                result.Message = "Nearby locations retrieved successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving nearby locations: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<bool>> VisitLocationAsync(Guid avatarId, Guid locationId, string purpose = null)
        {
            var result = new OASISResult<bool>();
            try
            {
                var visit = new MapVisit
                {
                    Id = Guid.NewGuid(),
                    AvatarId = avatarId,
                    LocationId = locationId,
                    Purpose = purpose,
                    VisitedAt = DateTime.UtcNow
                };

                lock (_lockObject)
                {
                    if (!_mapVisits.ContainsKey(avatarId))
                    {
                        _mapVisits[avatarId] = new List<MapVisit>();
                    }
                    _mapVisits[avatarId].Add(visit);
                }

                // Update competition scores
                await UpdateMapCompetitionScoresAsync(avatarId, locationId);

                result.Result = true;
                result.Message = "Location visited successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error visiting location: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<List<MapVisit>>> GetVisitHistoryAsync(Guid avatarId, int limit = 50, int offset = 0)
        {
            var result = new OASISResult<List<MapVisit>>();
            try
            {
                if (_mapVisits.TryGetValue(avatarId, out var visits))
                {
                    result.Result = visits
                        .OrderByDescending(v => v.VisitedAt)
                        .Skip(offset)
                        .Take(limit)
                        .ToList();
                    result.Message = "Visit history retrieved successfully.";
                }
                else
                {
                    result.Result = new List<MapVisit>();
                    result.Message = "No visit history found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving visit history: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<List<MapLocation>>> SearchLocationsAsync(string query, LocationType? type = null, double? latitude = null, double? longitude = null, double? radiusKm = null)
        {
            var result = new OASISResult<List<MapLocation>>();
            try
            {
                // In a real implementation, this would search a spatial database
                var mockLocations = new List<MapLocation>
                {
                    new MapLocation
                    {
                        Id = Guid.NewGuid(),
                        Name = "OASIS Central Hub",
                        Description = "The main gathering place in the OASIS",
                        Latitude = 40.7128,
                        Longitude = -74.0060,
                        Type = LocationType.Hub,
                        IsActive = true
                    },
                    new MapLocation
                    {
                        Id = Guid.NewGuid(),
                        Name = "Quest Giver Station",
                        Description = "Where avatars can pick up new quests",
                        Latitude = 40.7130,
                        Longitude = -74.0058,
                        Type = LocationType.Quest,
                        IsActive = true
                    }
                };

                var filteredLocations = mockLocations.AsQueryable();

                if (!string.IsNullOrEmpty(query))
                {
                    filteredLocations = filteredLocations.Where(l =>
                        l.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        l.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
                }

                if (type.HasValue)
                {
                    filteredLocations = filteredLocations.Where(l => l.Type == type.Value);
                }

                result.Result = filteredLocations.ToList();
                result.Message = "Locations found successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error searching locations: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        #region Competition Tracking

        private async Task UpdateMapCompetitionScoresAsync(Guid avatarId, Guid locationId)
        {
            try
            {
                var competitionManager = CompetitionManager.Instance;

                // TODO: Implement competition score updates when CompetitionManager is properly integrated
                // For now, just log the activity
                Console.WriteLine($"Map activity recorded for avatar {avatarId} at location {locationId}");
                
                // Calculate score based on location type and exploration
                var score = CalculateMapScore(locationId);

                // TODO: Update competition scores when CompetitionManager is available
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Daily, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Weekly, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Monthly, score);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating map competition scores: {ex.Message}");
            }
        }

        private long CalculateMapScore(Guid locationId)
        {
            // Base score for visiting any location
            return 5;
        }

        public async Task<OASISResult<Dictionary<string, object>>> GetMapStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var visits = _mapVisits.GetValueOrDefault(avatarId, new List<MapVisit>());

                var totalVisits = visits.Count;
                var uniqueLocations = visits.Select(v => v.LocationId).Distinct().Count();
                var visitsByType = visits.GroupBy(v => v.Purpose).ToDictionary(g => g.Key ?? "Unknown", g => g.Count());

                var stats = new Dictionary<string, object>
                {
                    ["totalVisits"] = totalVisits,
                    ["uniqueLocations"] = uniqueLocations,
                    ["visitsByPurpose"] = visitsByType,
                    ["totalScore"] = totalVisits * 5,
                    ["averageVisitsPerDay"] = CalculateAverageVisitsPerDay(visits),
                    ["mostVisitedLocation"] = GetMostVisitedLocation(visits)
                };

                result.Result = stats;
                result.Message = "Map statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving map statistics: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        private double CalculateAverageVisitsPerDay(List<MapVisit> visits)
        {
            if (visits.Count == 0) return 0;

            var days = (DateTime.UtcNow - visits.Min(v => v.VisitedAt)).TotalDays;
            return days > 0 ? visits.Count / days : 0;
        }

        private string GetMostVisitedLocation(List<MapVisit> visits)
        {
            return visits.GroupBy(v => v.LocationId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key.ToString() ?? "None";
        }

        #endregion
    
    }

    public class MapLocation
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public LocationType Type { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class MapVisit
    {
        public Guid Id { get; set; }
        public Guid AvatarId { get; set; }
        public Guid LocationId { get; set; }
        public string Purpose { get; set; }
        public DateTime VisitedAt { get; set; }
    }

    public enum LocationType
    {
        Hub,
        Quest,
        Social,
        Shop,
        Arena,
        Dungeon,
        Custom
    }
}
