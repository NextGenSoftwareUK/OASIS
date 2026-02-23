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
    public partial class MapManager : OASISManager, IMapManager
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

        private bool WithMapProvider(Func<IOASISMapProvider, bool> action)
        {
            if (CurrentMapProvider == null) return false;
            return action(CurrentMapProvider);
        }

        public bool Draw3DObjectOnMap(object obj, float x, float y)
        {
            return WithMapProvider(p => p.Draw3DObjectOnMap(obj, x, y));
        }

        public bool Draw2DSpriteOnMap(object sprite, float x, float y)
        {
            return WithMapProvider(p => p.Draw2DSpriteOnMap(sprite, x, y));
        }

        public bool Draw2DSpriteOnHUD(object sprite, float x, float y)
        {
            return WithMapProvider(p => p.Draw2DSpriteOnHUD(sprite, x, y));
        }

        public bool HighlightBuildingOnMap(IBuilding building)
        {
            return true;
        }

        public bool PlaceHolonOnMap(IHolon holon, float x, float y)
        {
            return WithMapProvider(p => p.PlaceHolonOnMap(holon, x, y));
        }

        public bool PlaceBuildingOnMap(IBuilding building, float x, float y)
        {
            return WithMapProvider(p => p.PlaceBuildingOnMap(building, x, y));
        }

        public bool PlaceQuestOnMap(IQuest quest, float x, float y)
        {
            return WithMapProvider(p => p.PlaceQuestOnMap(quest, x, y));
        }

        public bool PlaceGeoNFTOnMap(IWeb4GeoSpatialNFT geoNFT, float x, float y)
        {
            return WithMapProvider(p => p.PlaceGeoNFTOnMap(geoNFT, x, y));
        }

        public bool PlaceGeoHotSpotOnMap(IGeoHotSpot geoHotSpot, float x, float y)
        {
            return WithMapProvider(p => p.PlaceGeoHotSpotOnMap(geoHotSpot, x, y));
        }

        public bool PlaceOAPPOnMap(IOAPP OAPP, float x, float y)
        {
            return WithMapProvider(p => p.PlaceOAPPOnMap(OAPP, x, y));
        }

        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY, Color colour)
        {
            return WithMapProvider(p => p.DrawRouteOnMap(startX, startY, endX, endY, colour));
        }

        public bool CreateAndDrawRouteOnMapBeweenPoints(MapPoints points)
        {
            return WithMapProvider(p => p.CreateAndDrawRouteOnMapBeweenPoints(points));
        }

        public bool CreateAndDrawRouteOnMapBetweenHolons(IHolon fromHolon, IHolon toHolon)
        {
            return WithMapProvider(p => p.CreateAndDrawRouteOnMapBetweenHolons(fromHolon, toHolon));
        }

        public bool CreateAndDrawRouteOnMapBetweenHolons(Guid fromHolonId, Guid toHolonId)
        {
            return WithMapProvider(p => p.CreateAndDrawRouteOnMapBetweenHolons(fromHolonId, toHolonId));
        }

        public bool CreateAndDrawRouteOnMapBetweenQuests(IQuest fromQuest, IQuest toQuest)
        {
            return WithMapProvider(p => p.CreateAndDrawRouteOnMapBetweenQuests(fromQuest, toQuest));
        }

        public bool CreateAndDrawRouteOnMapBetweenQuests(Guid fromQuestId, Guid toQuestId)
        {
            return WithMapProvider(p => p.CreateAndDrawRouteOnMapBetweenQuests(fromQuestId, toQuestId));
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoNFTs(IWeb4GeoSpatialNFT fromGeoNFT, IWeb4GeoSpatialNFT toGeoNFT)
        {
            return WithMapProvider(p => p.CreateAndDrawRouteOnMapBetweenGeoNFTs(fromGeoNFT, toGeoNFT));
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoNFTs(Guid fromGeoNFTId, Guid toGeoNFTId)
        {
            return WithMapProvider(p => p.CreateAndDrawRouteOnMapBetweenGeoNFTs(fromGeoNFTId, toGeoNFTId));
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(IGeoHotSpot fromGeoHotSpot, IGeoHotSpot toGeoHotSpot)
        {
            return WithMapProvider(p => p.CreateAndDrawRouteOnMapBetweenGeoHotSpots(fromGeoHotSpot, toGeoHotSpot));
        }

        public bool CreateAndDrawRouteOnMapBetweenGeoHotSpots(Guid fromGeoHotSpotId, Guid toGeoHotSpotId)
        {
            return WithMapProvider(p => p.CreateAndDrawRouteOnMapBetweenGeoHotSpots(fromGeoHotSpotId, toGeoHotSpotId));
        }

        public bool CreateAndDrawRouteOnMapBetweenOAPPs(IOAPP fromOAPP, IOAPP toOAPP)
        {
            return WithMapProvider(p => p.CreateAndDrawRouteOnMapBetweenOAPPs(fromOAPP, toOAPP));
        }

        public bool CreateAndDrawRouteOnMapBetweenOAPPs(Guid fromOAPPId, Guid toOAPPId)
        {
            return WithMapProvider(p => p.CreateAndDrawRouteOnMapBetweenOAPPs(fromOAPPId, toOAPPId));
        }

        public bool CreateAndDrawRouteOnMapBetweenBuildings(IBuilding fromBuilding, IBuilding toBuilding)
        {
            return WithMapProvider(p => p.CreateAndDrawRouteOnMapBetweenBuildings(fromBuilding, toBuilding));
        }

        public bool CreateAndDrawRouteOnMapBetweenBuildings(Guid fromBuildingId, Guid toBuildingId)
        {
            return WithMapProvider(p => p.CreateAndDrawRouteOnMapBetweenBuildings(fromBuildingId, toBuildingId));
        }

        public bool ZoomMapOut(float value)
        {
            return WithMapProvider(p => p.ZoomMapOut(value));
        }

        public bool ZoomMapIn(float value)
        {
            return WithMapProvider(p => p.ZoomMapIn(value));
        }

        public bool PanMapLeft(float value)
        {
            return WithMapProvider(p => p.PanMapLeft(value));
        }

        public bool PanMapRight(float value)
        {
            return WithMapProvider(p => p.PanMapRight(value));
        }

        public bool PanMapUp(float value)
        {
            return WithMapProvider(p => p.PanMapUp(value));
        }

        public bool PanMapDown(float value)
        {
            return WithMapProvider(p => p.PanMapDown(value));
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

        public bool SelectGeoNFTOnMap(IWeb4GeoSpatialNFT geoNFT)
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
            return WithMapProvider(p => p.ZoomToHolonOnMap(holon));
        }

        public bool ZoomToHolonOnMap(Guid holonId)
        {
            return WithMapProvider(p => p.ZoomToHolonOnMap(holonId));
        }

        public bool ZoomToQuestOnMap(IQuest quest)
        {
            return WithMapProvider(p => p.ZoomToQuestOnMap(quest));
        }

        public bool ZoomToQuestOnMap(Guid questId)
        {
            return WithMapProvider(p => p.ZoomToQuestOnMap(questId));
        }

        public bool ZoomToGeoNFTOnMap(IWeb4GeoSpatialNFT geoNFT)
        {
            return WithMapProvider(p => p.ZoomToGeoNFTOnMap(geoNFT));
        }

        public bool ZoomToGeoNFTOnMap(Guid geoNFTId)
        {
            return WithMapProvider(p => p.ZoomToGeoNFTOnMap(geoNFTId));
        }

        public bool ZoomToGeoHotSpotOnMap(IGeoHotSpot geoHotSpot)
        {
            return WithMapProvider(p => p.ZoomToGeoHotSpotOnMap(geoHotSpot));
        }

        public bool ZoomToGeoHotSpotOnMap(Guid geoHotSpotId)
        {
            return WithMapProvider(p => p.ZoomToGeoHotSpotOnMap(geoHotSpotId));
        }

        public bool ZoomToOAPPOnMap(IOAPP oapp)
        {
            return WithMapProvider(p => p.ZoomToOAPPOnMap(oapp));
        }

        public bool ZoomToOAPPOnMap(Guid oappId)
        {
            return WithMapProvider(p => p.ZoomToOAPPOnMap(oappId));
        }

        public bool ZoomToBuildingOnMap(IBuilding building)
        {
            return WithMapProvider(p => p.ZoomToBuildingOnMap(building));
        }

        public bool ZoomToBuildingOnMap(Guid buildingId)
        {
            return WithMapProvider(p => p.ZoomToBuildingOnMap(buildingId));
        }

        public bool ZoomToCoOrdsOnMap(float x, float y)
        {
            return WithMapProvider(p => p.ZoomToCoOrdsOnMap(x, y));
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

                // Implement real competition score updates
                Console.WriteLine($"Map activity recorded for avatar {avatarId} at location {locationId}");
                
                // Calculate score based on location type and exploration
                var score = CalculateMapScore(locationId);
                
                // Get additional score factors
                var explorationBonus = await CalculateExplorationBonusAsync(avatarId, locationId);
                var discoveryBonus = await CalculateDiscoveryBonusAsync(avatarId, locationId);
                var socialBonus = await CalculateSocialBonusAsync(avatarId, locationId);
                
                var totalScore = score + explorationBonus + discoveryBonus + socialBonus;
                
                // Update competition scores with real implementation
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

    public class MapActivity
    {
        public Guid AvatarId { get; set; }
        public Guid LocationId { get; set; }
        public string ActivityType { get; set; }
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

    /// <summary>
    /// Helper methods for map competition system
    /// </summary>
    public partial class MapManager
    {
        /// <summary>
        /// Calculate exploration bonus based on avatar's exploration history
        /// </summary>
        private async Task<int> CalculateExplorationBonusAsync(Guid avatarId, Guid locationId)
        {
            try
            {
                // Get avatar's exploration history
                var explorationHistory = await GetAvatarExplorationHistoryAsync(avatarId);
                
                // Calculate bonus based on:
                // 1. First time visiting this location
                // 2. Exploring new areas vs revisiting
                // 3. Exploration diversity
                
                var isFirstVisit = !explorationHistory.Any(h => h.LocationId == locationId);
                var explorationDiversity = explorationHistory.Select(h => h.LocationId).Distinct().Count();
                var recentExploration = explorationHistory.Count(h => h.VisitedAt > DateTime.UtcNow.AddDays(-7));
                
                var bonus = 0;
                
                if (isFirstVisit)
                    bonus += 50; // First visit bonus
                
                if (explorationDiversity > 10)
                    bonus += 25; // Diversity bonus
                
                if (recentExploration > 5)
                    bonus += 15; // Active exploration bonus
                
                return bonus;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating exploration bonus: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Calculate discovery bonus for finding new locations or items
        /// </summary>
        private async Task<int> CalculateDiscoveryBonusAsync(Guid avatarId, Guid locationId)
        {
            try
            {
                // Get location details to determine discovery potential
                var locationDetails = await GetLocationDetailsAsync(locationId);
                if (locationDetails == null)
                    return 0;
                
                var bonus = 0;
                
                // Bonus for discovering rare locations
                if (locationDetails.Rarity == LocationRarity.Rare)
                    bonus += 100;
                else if (locationDetails.Rarity == LocationRarity.Epic)
                    bonus += 200;
                else if (locationDetails.Rarity == LocationRarity.Legendary)
                    bonus += 500;
                
                // Bonus for discovering hidden locations
                if (locationDetails.IsHidden)
                    bonus += 150;
                
                // Bonus for discovering locations with special properties
                if (locationDetails.HasSpecialProperties)
                    bonus += 75;
                
                return bonus;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating discovery bonus: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Calculate social bonus for group activities and interactions
        /// </summary>
        private async Task<int> CalculateSocialBonusAsync(Guid avatarId, Guid locationId)
        {
            try
            {
                // Get social activity data
                var socialActivity = await GetAvatarSocialActivityAsync(avatarId, locationId);
                
                var bonus = 0;
                
                // Bonus for group activities
                if (socialActivity.IsGroupActivity)
                    bonus += 30;
                
                // Bonus for helping other players
                if (socialActivity.HelpedOtherPlayers > 0)
                    bonus += socialActivity.HelpedOtherPlayers * 10;
                
                // Bonus for social interactions
                if (socialActivity.SocialInteractions > 0)
                    bonus += socialActivity.SocialInteractions * 5;
                
                // Bonus for organizing events
                if (socialActivity.OrganizedEvents > 0)
                    bonus += socialActivity.OrganizedEvents * 25;
                
                return bonus;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating social bonus: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Get avatar's exploration history
        /// </summary>
        private async Task<List<MapActivity>> GetAvatarExplorationHistoryAsync(Guid avatarId)
        {
            try
            {
                // This would typically query the database for exploration history
                // For now, return mock data
                return new List<MapActivity>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting exploration history: {ex.Message}");
                return new List<MapActivity>();
            }
        }
        
        /// <summary>
        /// Get location details for discovery calculations
        /// </summary>
        private async Task<LocationDetails> GetLocationDetailsAsync(Guid locationId)
        {
            try
            {
                // This would typically query the database for location details
                // For now, return mock data
                return new LocationDetails
                {
                    Id = locationId,
                    Rarity = LocationRarity.Common,
                    IsHidden = false,
                    HasSpecialProperties = false
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting location details: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get avatar's social activity data
        /// </summary>
        private async Task<SocialActivity> GetAvatarSocialActivityAsync(Guid avatarId, Guid locationId)
        {
            try
            {
                // This would typically query the database for social activity
                // For now, return mock data
                return new SocialActivity
                {
                    AvatarId = avatarId,
                    LocationId = locationId,
                    IsGroupActivity = false,
                    HelpedOtherPlayers = 0,
                    SocialInteractions = 0,
                    OrganizedEvents = 0
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting social activity: {ex.Message}");
                return new SocialActivity();
            }
        }
    }
    
    /// <summary>
    /// Location details for discovery calculations
    /// </summary>
    public class LocationDetails
    {
        public Guid Id { get; set; }
        public LocationRarity Rarity { get; set; }
        public bool IsHidden { get; set; }
        public bool HasSpecialProperties { get; set; }
    }
    
    /// <summary>
    /// Location rarity levels
    /// </summary>
    public enum LocationRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    /// <summary>
    /// Social activity data
    /// </summary>
    public class SocialActivity
    {
        public Guid AvatarId { get; set; }
        public Guid LocationId { get; set; }
        public bool IsGroupActivity { get; set; }
        public int HelpedOtherPlayers { get; set; }
        public int SocialInteractions { get; set; }
        public int OrganizedEvents { get; set; }
    }
}
