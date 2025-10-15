//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.OASIS.API.DNA;
//using NextGenSoftware.Utilities;

//namespace NextGenSoftware.OASIS.API.Core.Managers
//{
//    public partial class MapManager : OASISManager
//    {
//        private static MapManager _instance;
//        private readonly object _lockObject = new object();
//        private readonly Dictionary<Guid, List<MapLocation>> _avatarLocations;
//        private readonly Dictionary<Guid, List<MapVisit>> _mapVisits;

//        public static MapManager Instance
//        {
//            get
//            {
//                if (_instance == null)
//                    _instance = new MapManager(ProviderManager.Instance.CurrentStorageProvider);
//                return _instance;
//            }
//        }

//        public MapManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
//        {
//            _avatarLocations = new Dictionary<Guid, List<MapLocation>>();
//            _mapVisits = new Dictionary<Guid, List<MapVisit>>();
//        }

//        public async Task<OASISResult<List<MapLocation>>> GetNearbyLocationsAsync(Guid avatarId, double latitude, double longitude, double radiusKm = 10.0)
//        {
//            var result = new OASISResult<List<MapLocation>>();
//            try
//            {
//                var nearbyLocations = new List<MapLocation>();
                
//                // In a real implementation, this would query a spatial database
//                // For now, return mock locations within the radius
//                var mockLocations = new List<MapLocation>
//                {
//                    new MapLocation
//                    {
//                        Id = Guid.NewGuid(),
//                        Name = "OASIS Central Hub",
//                        Description = "The main gathering place in the OASIS",
//                        Latitude = latitude + 0.001,
//                        Longitude = longitude + 0.001,
//                        Type = LocationType.Hub,
//                        IsActive = true
//                    },
//                    new MapLocation
//                    {
//                        Id = Guid.NewGuid(),
//                        Name = "Quest Giver Station",
//                        Description = "Where avatars can pick up new quests",
//                        Latitude = latitude - 0.002,
//                        Longitude = longitude + 0.003,
//                        Type = LocationType.Quest,
//                        IsActive = true
//                    },
//                    new MapLocation
//                    {
//                        Id = Guid.NewGuid(),
//                        Name = "Social Plaza",
//                        Description = "A place for avatars to meet and socialize",
//                        Latitude = latitude + 0.005,
//                        Longitude = longitude - 0.001,
//                        Type = LocationType.Social,
//                        IsActive = true
//                    }
//                };

//                result.Result = mockLocations;
//                result.Message = "Nearby locations retrieved successfully.";
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Message = $"Error retrieving nearby locations: {ex.Message}";
//                result.Exception = ex;
//            }
//            return await Task.FromResult(result);
//        }

//        public async Task<OASISResult<bool>> VisitLocationAsync(Guid avatarId, Guid locationId, string purpose = null)
//        {
//            var result = new OASISResult<bool>();
//            try
//            {
//                var visit = new MapVisit
//                {
//                    Id = Guid.NewGuid(),
//                    AvatarId = avatarId,
//                    LocationId = locationId,
//                    Purpose = purpose,
//                    VisitedAt = DateTime.UtcNow
//                };

//                lock (_lockObject)
//                {
//                    if (!_mapVisits.ContainsKey(avatarId))
//                    {
//                        _mapVisits[avatarId] = new List<MapVisit>();
//                    }
//                    _mapVisits[avatarId].Add(visit);
//                }

//                // Update competition scores
//                await UpdateMapCompetitionScoresAsync(avatarId, locationId);

//                result.Result = true;
//                result.Message = "Location visited successfully.";
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Result = false;
//                result.Message = $"Error visiting location: {ex.Message}";
//                result.Exception = ex;
//            }
//            return await Task.FromResult(result);
//        }

//        public async Task<OASISResult<List<MapVisit>>> GetVisitHistoryAsync(Guid avatarId, int limit = 50, int offset = 0)
//        {
//            var result = new OASISResult<List<MapVisit>>();
//            try
//            {
//                if (_mapVisits.TryGetValue(avatarId, out var visits))
//                {
//                    result.Result = visits
//                        .OrderByDescending(v => v.VisitedAt)
//                        .Skip(offset)
//                        .Take(limit)
//                        .ToList();
//                    result.Message = "Visit history retrieved successfully.";
//                }
//                else
//                {
//                    result.Result = new List<MapVisit>();
//                    result.Message = "No visit history found for this avatar.";
//                }
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Message = $"Error retrieving visit history: {ex.Message}";
//                result.Exception = ex;
//            }
//            return await Task.FromResult(result);
//        }

//        public async Task<OASISResult<List<MapLocation>>> SearchLocationsAsync(string query, LocationType? type = null, double? latitude = null, double? longitude = null, double? radiusKm = null)
//        {
//            var result = new OASISResult<List<MapLocation>>();
//            try
//            {
//                // In a real implementation, this would search a spatial database
//                var mockLocations = new List<MapLocation>
//                {
//                    new MapLocation
//                    {
//                        Id = Guid.NewGuid(),
//                        Name = "OASIS Central Hub",
//                        Description = "The main gathering place in the OASIS",
//                        Latitude = 40.7128,
//                        Longitude = -74.0060,
//                        Type = LocationType.Hub,
//                        IsActive = true
//                    },
//                    new MapLocation
//                    {
//                        Id = Guid.NewGuid(),
//                        Name = "Quest Giver Station",
//                        Description = "Where avatars can pick up new quests",
//                        Latitude = 40.7130,
//                        Longitude = -74.0058,
//                        Type = LocationType.Quest,
//                        IsActive = true
//                    }
//                };

//                var filteredLocations = mockLocations.AsQueryable();

//                if (!string.IsNullOrEmpty(query))
//                {
//                    filteredLocations = filteredLocations.Where(l => 
//                        l.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
//                        l.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
//                }

//                if (type.HasValue)
//                {
//                    filteredLocations = filteredLocations.Where(l => l.Type == type.Value);
//                }

//                result.Result = filteredLocations.ToList();
//                result.Message = "Locations found successfully.";
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Message = $"Error searching locations: {ex.Message}";
//                result.Exception = ex;
//            }
//            return await Task.FromResult(result);
//        }

//        #region Competition Tracking

//        private async Task UpdateMapCompetitionScoresAsync(Guid avatarId, Guid locationId)
//        {
//            try
//            {
//                var competitionManager = CompetitionManager.Instance;
                
//                // Calculate score based on location type and exploration
//                var score = CalculateMapScore(locationId);
                
//                // Update social activity competition scores
//                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Daily, score);
//                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Weekly, score);
//                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Monthly, score);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error updating map competition scores: {ex.Message}");
//            }
//        }

//        private long CalculateMapScore(Guid locationId)
//        {
//            // Base score for visiting any location
//            return 5;
//        }

//        public async Task<OASISResult<Dictionary<string, object>>> GetMapStatsAsync(Guid avatarId)
//        {
//            var result = new OASISResult<Dictionary<string, object>>();
//            try
//            {
//                var visits = _mapVisits.GetValueOrDefault(avatarId, new List<MapVisit>());
                
//                var totalVisits = visits.Count;
//                var uniqueLocations = visits.Select(v => v.LocationId).Distinct().Count();
//                var visitsByType = visits.GroupBy(v => v.Purpose).ToDictionary(g => g.Key ?? "Unknown", g => g.Count());

//                var stats = new Dictionary<string, object>
//                {
//                    ["totalVisits"] = totalVisits,
//                    ["uniqueLocations"] = uniqueLocations,
//                    ["visitsByPurpose"] = visitsByType,
//                    ["totalScore"] = totalVisits * 5,
//                    ["averageVisitsPerDay"] = CalculateAverageVisitsPerDay(visits),
//                    ["mostVisitedLocation"] = GetMostVisitedLocation(visits)
//                };

//                result.Result = stats;
//                result.Message = "Map statistics retrieved successfully.";
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Message = $"Error retrieving map statistics: {ex.Message}";
//                result.Exception = ex;
//            }
//            return await Task.FromResult(result);
//        }

//        private double CalculateAverageVisitsPerDay(List<MapVisit> visits)
//        {
//            if (visits.Count == 0) return 0;

//            var days = (DateTime.UtcNow - visits.Min(v => v.VisitedAt)).TotalDays;
//            return days > 0 ? visits.Count / days : 0;
//        }

//        private string GetMostVisitedLocation(List<MapVisit> visits)
//        {
//            return visits.GroupBy(v => v.LocationId)
//                .OrderByDescending(g => g.Count())
//                .FirstOrDefault()?.Key.ToString() ?? "None";
//        }

//        #endregion
//    }

//    public class MapLocation
//    {
//        public Guid Id { get; set; }
//        public string Name { get; set; }
//        public string Description { get; set; }
//        public double Latitude { get; set; }
//        public double Longitude { get; set; }
//        public LocationType Type { get; set; }
//        public bool IsActive { get; set; }
//        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
//    }

//    public class MapVisit
//    {
//        public Guid Id { get; set; }
//        public Guid AvatarId { get; set; }
//        public Guid LocationId { get; set; }
//        public string Purpose { get; set; }
//        public DateTime VisitedAt { get; set; }
//    }

//    public enum LocationType
//    {
//        Hub,
//        Quest,
//        Social,
//        Shop,
//        Arena,
//        Dungeon,
//        Custom
//    }
//}
