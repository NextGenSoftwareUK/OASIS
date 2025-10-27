using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;
// Removed invalid ONODE.Core references - these will be implemented when needed

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class EggsManager : OASISManager
    {
        private static EggsManager _instance;
        private readonly object _lockObject = new object();
        private readonly Dictionary<Guid, List<Egg>> _avatarEggs;
        private readonly Dictionary<Guid, List<EggQuest>> _eggQuests;
        private readonly Dictionary<Guid, List<EggQuestLeaderboard>> _eggQuestLeaderboards;

        public static EggsManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new EggsManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public EggsManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            _avatarEggs = new Dictionary<Guid, List<Egg>>();
            _eggQuests = new Dictionary<Guid, List<EggQuest>>();
            _eggQuestLeaderboards = new Dictionary<Guid, List<EggQuestLeaderboard>>();
        }

        public async Task<OASISResult<List<Egg>>> GetAllEggsAsync(Guid avatarId)
        {
            var result = new OASISResult<List<Egg>>();
            try
            {
                // Load eggs from persistent storage using HolonManager
                var eggsHolon = await HolonManager.Instance.LoadHolonAsync($"eggs_{avatarId}");
                
                if (eggsHolon.IsError || eggsHolon.Result == null)
                {
                    result.Result = new List<Egg>();
                    result.Message = "No hidden eggs discovered yet. Keep exploring the OASIS!";
                }
                else
                {
                    // Deserialize eggs from holon metadata
                    var eggsData = eggsHolon.Result.MetaData.GetValueOrDefault("eggs", new List<Dictionary<string, object>>());
                    var eggs = new List<Egg>();
                    
                    if (eggsData is List<object> eggsList)
                    {
                        foreach (var eggData in eggsList)
                        {
                            if (eggData is Dictionary<string, object> eggDict)
                            {
                                var egg = DeserializeEgg(eggDict);
                                if (egg != null)
                                    eggs.Add(egg);
                            }
                        }
                    }
                    
                    result.Result = eggs;
                    result.Message = "Hidden eggs discovered by this avatar retrieved successfully.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving discovered eggs: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<List<EggQuest>>> GetCurrentEggQuestsAsync(Guid avatarId)
        {
            var result = new OASISResult<List<EggQuest>>();
            try
            {
                if (_eggQuests.TryGetValue(avatarId, out var quests))
                {
                    result.Result = quests.Where(q => q.IsActive).ToList();
                    result.Message = "Current egg quests retrieved successfully.";
                }
                else
                {
                    result.Result = new List<EggQuest>();
                    result.Message = "No current egg quests found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving current egg quests: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<List<EggQuestLeaderboard>>> GetCurrentEggQuestLeaderboardAsync(Guid avatarId)
        {
            var result = new OASISResult<List<EggQuestLeaderboard>>();
            try
            {
                if (_eggQuestLeaderboards.TryGetValue(avatarId, out var leaderboards))
                {
                    result.Result = leaderboards.OrderByDescending(l => l.Score).ToList();
                    result.Message = "Egg quest leaderboard retrieved successfully.";
                }
                else
                {
                    result.Result = new List<EggQuestLeaderboard>();
                    result.Message = "No egg quest leaderboard found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving egg quest leaderboard: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<Egg>> HatchEggAsync(Guid avatarId, Guid eggId)
        {
            var result = new OASISResult<Egg>();
            try
            {
                if (_avatarEggs.TryGetValue(avatarId, out var eggs))
                {
                    var egg = eggs.FirstOrDefault(e => e.Id == eggId);
                    if (egg != null && !egg.IsHatched)
                    {
                        lock (_lockObject)
                        {
                            egg.IsHatched = true;
                            egg.HatchedAt = DateTime.UtcNow;
                        }
                        result.Result = egg;
                        result.Message = "Egg hatched successfully.";
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = "Egg not found or already hatched.";
                    }
                }
                else
                {
                    result.IsError = true;
                    result.Message = "No eggs found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error hatching egg: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        //public async Task<OASISResult<Egg>> DiscoverEggAsync(Guid avatarId, EggType eggType, string name, string location, EggDiscoveryMethod discoveryMethod, EggRarity rarity = EggRarity.Common, int rarityLevel = 1)
        public async Task<OASISResult<Egg>> DiscoverEggAsync(Guid avatarId, EggType eggType, string name, Guid locationId, string location, EggDiscoveryMethod discoveryMethod, EggRarity rarity = EggRarity.Common)
        {
            var result = new OASISResult<Egg>();
            try
            {
                var newEgg = new Egg
                {
                    Id = Guid.NewGuid(),
                    AvatarId = avatarId,
                    EggType = eggType,
                    Name = name,
                    Rarity = rarity,
                    //RarityLevel = rarityLevel,
                    Location = location,
                    LocationId = locationId,
                    DiscoveryMethod = discoveryMethod,
                    DiscoveredAt = DateTime.UtcNow,
                    IsDisplayed = true,
                    GalleryPosition = GalleryPosition.Hidden, // Will be positioned automatically in gallery
                    Tags = new List<string> { eggType.ToString().ToLower(), rarity.ToString().ToLower(), discoveryMethod.ToString().ToLower() }
                };

                // Load existing eggs and add new one
                var existingEggs = await GetAllEggsAsync(avatarId);
                var eggs = existingEggs.Result ?? new List<Egg>();
                eggs.Add(newEgg);
                
                // Save to persistent storage
                await SaveEggsAsync(avatarId, eggs);

                result.Result = newEgg;
                result.Message = $"🎉 Congratulations! You discovered a {rarity} {name} egg!";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error discovering egg: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        /*
        public async Task<OASISResult<Egg>> DiscoverEggFromQuestAsync(Guid avatarId, Guid questId, EggType eggType, string name, EggRarity rarity = EggRarity.Rare, int rarityLevel = 3)
        {
            var result = new OASISResult<Egg>();
            try
            {
                var newEgg = new Egg
                {
                    Id = Guid.NewGuid(),
                    AvatarId = avatarId,
                    EggType = eggType,
                    Name = name,
                    Rarity = rarity,
                    //RarityLevel = rarityLevel,
                    Location = "Quest Reward",
                    DiscoveryMethod = EggDiscoveryMethod.QuestCompletion,
                    DiscoveredAt = DateTime.UtcNow,
                    IsDisplayed = true,
                    GalleryPosition = GalleryPosition.Hidden,
                    Tags = new List<string> { eggType.ToString().ToLower(), rarity.ToString().ToLower(), "quest", "reward" }
                };

                lock (_lockObject)
                {
                    if (!_avatarEggs.ContainsKey(avatarId))
                    {
                        _avatarEggs[avatarId] = new List<Egg>();
                    }
                    _avatarEggs[avatarId].Add(newEgg);
                }

                result.Result = newEgg;
                result.Message = $"🏆 Quest completed! You earned a {rarity} {name} egg!";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error discovering quest egg: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<Egg>> DiscoverEggFromPuzzleAsync(Guid avatarId, string puzzleName, EggType eggType, string name, EggRarity rarity = EggRarity.Epic, int rarityLevel = 5)
        {
            var result = new OASISResult<Egg>();
            try
            {
                var newEgg = new Egg
                {
                    Id = Guid.NewGuid(),
                    AvatarId = avatarId,
                    EggType = eggType,
                    Name = name,
                    Rarity = rarity,
                    RarityLevel = rarityLevel,
                    Location = puzzleName,
                    DiscoveryMethod = EggDiscoveryMethod.PuzzleSolved,
                    DiscoveredAt = DateTime.UtcNow,
                    IsDisplayed = true,
                    GalleryPosition = GalleryPosition.Hidden,
                    Tags = new List<string> { eggType.ToString().ToLower(), rarity.ToString().ToLower(), "puzzle", "solved" }
                };

                lock (_lockObject)
                {
                    if (!_avatarEggs.ContainsKey(avatarId))
                    {
                        _avatarEggs[avatarId] = new List<Egg>();
                    }
                    _avatarEggs[avatarId].Add(newEgg);
                }

                result.Result = newEgg;
                result.Message = $"🧩 Puzzle solved! You unlocked a {rarity} {name} egg!";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error discovering puzzle egg: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<Egg>> DiscoverHiddenEggAsync(Guid avatarId, string secretLocation, EggType eggType, string name, EggRarity rarity = EggRarity.Legendary, int rarityLevel = 8)
        {
            var result = new OASISResult<Egg>();
            try
            {
                var newEgg = new Egg
                {
                    Id = Guid.NewGuid(),
                    AvatarId = avatarId,
                    EggType = eggType,
                    Name = name,
                    Rarity = rarity,
                    RarityLevel = rarityLevel,
                    Location = secretLocation,
                    DiscoveryMethod = EggDiscoveryMethod.SecretLocation,
                    DiscoveredAt = DateTime.UtcNow,
                    IsDisplayed = true,
                    GalleryPosition = GalleryPosition.Hidden,
                    Tags = new List<string> { eggType.ToString().ToLower(), rarity.ToString().ToLower(), "hidden", "secret" }
                };

                lock (_lockObject)
                {
                    if (!_avatarEggs.ContainsKey(avatarId))
                    {
                        _avatarEggs[avatarId] = new List<Egg>();
                    }
                    _avatarEggs[avatarId].Add(newEgg);
                }

                result.Result = newEgg;
                result.Message = $"🔍 Secret discovered! You found a {rarity} {name} egg in a hidden location!";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error discovering hidden egg: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }*/


        #region Competition Tracking

        private async Task UpdateCompetitionScoresAsync(Guid avatarId, Egg egg)
        {
            try
            {
                // Update egg collection competition scores
                var competitionManager = CompetitionManager.Instance;
                
                // Calculate score based on egg rarity and type
                var score = CalculateEggScore(egg);
                
                // Update daily, weekly, monthly, quarterly, and yearly scores
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.EggCollection, SeasonType.Daily, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.EggCollection, SeasonType.Weekly, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.EggCollection, SeasonType.Monthly, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.EggCollection, SeasonType.Quarterly, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.EggCollection, SeasonType.Yearly, score);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the egg discovery
                Console.WriteLine($"Error updating competition scores: {ex.Message}");
            }
        }

        private long CalculateEggScore(Egg egg)
        {
            // Base score calculation based on rarity and type
            var baseScore = egg.Rarity switch
            {
                EggRarity.Common => 10,
                EggRarity.Uncommon => 25,
                EggRarity.Rare => 50,
                EggRarity.Epic => 100,
                EggRarity.Legendary => 250,
                EggRarity.Mythic => 500,
                EggRarity.Divine => 700,
                EggRarity.Celestial => 800,
                EggRarity.Transcendent => 900,
                EggRarity.Omnipotent => 1000,
                _ => 10
            };

            // Complete mapping of all egg types to scores
            var typeBonus = egg.EggType switch
            {
                // Basic metals
                EggType.Bronze => 10,
                EggType.Silver => 25,
                EggType.Gold => 50,
                EggType.Platinum => 75,
                EggType.Diamond => 100,
                
                // Dragons and mythical creatures
                EggType.Dragon => 150,
                
                // Elemental types
                EggType.Fire => 60,
                EggType.Ice => 60,
                EggType.Lightning => 70,
                EggType.Storm => 80,
                EggType.Wind => 50,
                EggType.Earth => 55,
                EggType.Water => 55,
                EggType.Air => 50,
                
                // Spiritual and cosmic types
                EggType.Spirit => 90,
                EggType.Cosmic => 120,
                EggType.Celestial => 130,
                EggType.Mystic => 100,
                EggType.Ancient => 110,
                EggType.Legendary => 140,
                EggType.Mythic => 150,
                EggType.Divine => 160,
                
                // Dark and light types
                EggType.Infernal => 85,
                EggType.Void => 95,
                EggType.Shadow => 80,
                EggType.Light => 90,
                
                // Gem types
                EggType.Crystal => 70,
                EggType.Obsidian => 75,
                EggType.Ruby => 80,
                EggType.Sapphire => 80,
                EggType.Emerald => 80,
                EggType.Amethyst => 75,
                EggType.Pearl => 70,
                EggType.Coral => 65,
                EggType.Jade => 75,
                EggType.Onyx => 75,
                EggType.Topaz => 70,
                EggType.Garnet => 75,
                EggType.Aquamarine => 70,
                EggType.Peridot => 65,
                EggType.Citrine => 70,
                EggType.Moonstone => 85,
                EggType.Sunstone => 85,
                EggType.Starstone => 90,
                
                // Celestial objects
                EggType.Comet => 100,
                EggType.Meteor => 95,
                EggType.Asteroid => 90,
                EggType.Nebula => 110,
                EggType.Galaxy => 120,
                EggType.Universe => 130,
                EggType.Multiverse => 140,
                EggType.Omniverse => 150,
                
                _ => 0
            };

            // Discovery method bonus
            var discoveryBonus = egg.DiscoveryMethod switch
            {
                EggDiscoveryMethod.SecretLocation => 100,
                EggDiscoveryMethod.PuzzleSolved => 75,
                EggDiscoveryMethod.QuestCompletion => 50,
                EggDiscoveryMethod.Exploration => 25,
                _ => 10
            };

            return baseScore + typeBonus + discoveryBonus;
        }

        public async Task<OASISResult<Dictionary<string, object>>> GetEggCollectionStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                if (_avatarEggs.TryGetValue(avatarId, out var eggs))
                {
                    var stats = new Dictionary<string, object>
                    {
                        ["totalEggs"] = eggs.Count,
                        ["uniqueTypes"] = eggs.Select(e => e.EggType).Distinct().Count(),
                        ["rarityDistribution"] = eggs.GroupBy(e => e.Rarity).ToDictionary(g => g.Key.ToString(), g => g.Count()),
                        ["typeDistribution"] = eggs.GroupBy(e => e.EggType).ToDictionary(g => g.Key.ToString(), g => g.Count()),
                        ["discoveryMethods"] = eggs.GroupBy(e => e.DiscoveryMethod).ToDictionary(g => g.Key.ToString(), g => g.Count()),
                        ["totalScore"] = eggs.Sum(e => CalculateEggScore(e)),
                        ["averageRarity"] = eggs.Average(e => e.RarityLevel),
                        ["mostCommonType"] = eggs.GroupBy(e => e.EggType).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key.ToString(),
                        ["rarestEgg"] = eggs.OrderByDescending(e => e.RarityLevel).FirstOrDefault()?.Name
                    };

                    result.Result = stats;
                    result.Message = "Egg collection statistics retrieved successfully.";
                }
                else
                {
                    result.Result = new Dictionary<string, object>();
                    result.Message = "No eggs found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving egg collection statistics: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Serialize an egg to dictionary for storage
        /// </summary>
        private Dictionary<string, object> SerializeEgg(Egg egg)
        {
            return new Dictionary<string, object>
            {
                ["id"] = egg.Id,
                ["name"] = egg.Name,
                ["description"] = egg.Description,
                ["eggType"] = egg.EggType.ToString(),
                ["rarity"] = egg.Rarity.ToString(),
                ["rarityLevel"] = egg.RarityLevel,
                ["score"] = egg.Score,
                ["location"] = egg.Location,
                ["locationId"] = egg.LocationId,
                ["discoveryMethod"] = egg.DiscoveryMethod.ToString(),
                ["discoveredAt"] = egg.DiscoveredAt,
                ["isDisplayed"] = egg.IsDisplayed,
                ["galleryPosition"] = egg.GalleryPosition.ToString(),
                ["tags"] = egg.Tags,
                ["avatarId"] = egg.AvatarId,
                ["isHatched"] = egg.IsHatched,
                ["hatchedAt"] = egg.HatchedAt,
                ["hatchedDate"] = egg.HatchedDate,
                ["discoveredDate"] = egg.DiscoveredDate,
                ["eggCategory"] = egg.EggCategory.ToString(),
                ["isHatchable"] = egg.IsHatchable,
                ["stats"] = egg.Stats,
                ["metadata"] = egg.Metadata,
                ["unlockedQuests"] = egg.UnlockedQuests,
                ["unlockedAreas"] = egg.UnlockedAreas,
                ["avatarUpgrades"] = egg.AvatarUpgrades.Select(upgrade => new Dictionary<string, object>
                {
                    ["upgradeType"] = upgrade.UpgradeType,
                    ["value"] = upgrade.Value,
                    ["description"] = upgrade.Description,
                    ["isPermanent"] = upgrade.IsPermanent,
                    ["expiresAt"] = upgrade.ExpiresAt
                }).ToList(),
                ["hatchedPets"] = egg.HatchedPets.Select(pet => new Dictionary<string, object>
                {
                    ["id"] = pet.Id,
                    ["name"] = pet.Name,
                    ["species"] = pet.Species,
                    ["element"] = pet.Element,
                    ["level"] = pet.Level,
                    ["experience"] = pet.Experience,
                    ["stats"] = pet.Stats,
                    ["abilities"] = pet.Abilities,
                    ["isActive"] = pet.IsActive,
                    ["hatchedAt"] = pet.HatchedAt,
                    ["metadata"] = pet.Metadata
                }).ToList()
            };
        }

        /// <summary>
        /// Deserialize dictionary to egg object
        /// </summary>
        private Egg DeserializeEgg(Dictionary<string, object> data)
        {
            return new Egg
            {
                Id = Guid.Parse(data["id"].ToString()),
                Name = data["name"].ToString(),
                Description = data["description"].ToString(),
                EggType = Enum.Parse<EggType>(data["eggType"].ToString()),
                Rarity = Enum.Parse<EggRarity>(data["rarity"].ToString()),
                // RarityLevel is read-only, calculated from Rarity
                Score = Convert.ToInt32(data.GetValueOrDefault("score", 0)),
                Location = data["location"].ToString(),
                LocationId = Guid.Parse(data["locationId"].ToString()),
                DiscoveryMethod = Enum.Parse<EggDiscoveryMethod>(data["discoveryMethod"].ToString()),
                DiscoveredAt = Convert.ToDateTime(data["discoveredAt"]),
                IsDisplayed = Convert.ToBoolean(data["isDisplayed"]),
                GalleryPosition = Enum.TryParse<GalleryPosition>(data.GetValueOrDefault("galleryPosition", "Hidden").ToString(), out var galleryPosition) ? galleryPosition : GalleryPosition.Hidden,
                Tags = ((List<object>)data["tags"]).Cast<string>().ToList(),
                AvatarId = Guid.Parse(data["avatarId"].ToString()),
                IsHatched = Convert.ToBoolean(data.GetValueOrDefault("isHatched", false)),
                HatchedAt = data.GetValueOrDefault("hatchedAt") != null ? Convert.ToDateTime(data["hatchedAt"]) : (DateTime?)null,
                HatchedDate = data.GetValueOrDefault("hatchedDate") != null ? Convert.ToDateTime(data["hatchedDate"]) : (DateTime?)null,
                DiscoveredDate = data.GetValueOrDefault("discoveredDate") != null ? Convert.ToDateTime(data["discoveredDate"]) : (DateTime?)null,
                EggCategory = Enum.TryParse<EggCategory>(data.GetValueOrDefault("eggCategory", "Trophy").ToString(), out var category) ? category : EggCategory.Trophy,
                IsHatchable = Convert.ToBoolean(data.GetValueOrDefault("isHatchable", true)),
                Stats = data.GetValueOrDefault("stats") as Dictionary<string, object> ?? new Dictionary<string, object>(),
                Metadata = data.GetValueOrDefault("metadata") as Dictionary<string, object> ?? new Dictionary<string, object>(),
                UnlockedQuests = ((List<object>)data.GetValueOrDefault("unlockedQuests", new List<object>())).Cast<string>().ToList(),
                UnlockedAreas = ((List<object>)data.GetValueOrDefault("unlockedAreas", new List<object>())).Cast<string>().ToList(),
                AvatarUpgrades = ((List<object>)data.GetValueOrDefault("avatarUpgrades", new List<object>())).Select(upgradeData =>
                {
                    var upgradeDict = (Dictionary<string, object>)upgradeData;
                    return new AvatarUpgrade
                    {
                        UpgradeType = upgradeDict["upgradeType"].ToString(),
                        Value = Convert.ToInt32(upgradeDict["value"]),
                        Description = upgradeDict["description"].ToString(),
                        IsPermanent = Convert.ToBoolean(upgradeDict["isPermanent"]),
                        ExpiresAt = upgradeDict["expiresAt"] != null ? DateTime.Parse(upgradeDict["expiresAt"].ToString()) : (DateTime?)null
                    };
                }).ToList(),
                HatchedPets = ((List<object>)data.GetValueOrDefault("hatchedPets", new List<object>())).Select(petData =>
                {
                    var petDict = (Dictionary<string, object>)petData;
                    return new Pet
                    {
                        Id = Guid.Parse(petDict["id"].ToString()),
                        Name = petDict["name"].ToString(),
                        Species = petDict["species"].ToString(),
                        Element = petDict["element"].ToString(),
                        Level = Convert.ToInt32(petDict["level"]),
                        Experience = Convert.ToInt32(petDict["experience"]),
                        Stats = petDict["stats"] as Dictionary<string, int> ?? new Dictionary<string, int>(),
                        Abilities = ((List<object>)petDict["abilities"]).Cast<string>().ToList(),
                        IsActive = Convert.ToBoolean(petDict["isActive"]),
                        HatchedAt = Convert.ToDateTime(petDict["hatchedAt"]),
                        Metadata = petDict["metadata"] as Dictionary<string, object> ?? new Dictionary<string, object>()
                    };
                }).ToList()
            };
        }

        /// <summary>
        /// Save eggs to storage
        /// </summary>
        private async Task SaveEggsAsync(Guid avatarId, List<Egg> eggs)
        {
            try
            {
                var eggsData = eggs.Select(SerializeEgg).ToList();
                
                // Create eggs holon with eggs data in MetaData
                var eggsHolon = new Holon
                {
                    Id = Guid.NewGuid(),
                    Name = $"Eggs_{avatarId}",
                    Description = $"Eggs discovered by avatar {avatarId}",
                    CreatedByAvatarId = avatarId,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    MetaData = new Dictionary<string, object> { ["eggs"] = eggsData }
                };
                
                await HolonManager.Instance.SaveHolonAsync(eggsHolon);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving eggs: {ex.Message}");
            }
        }

        #endregion
    }

    public class Egg
    {
        public Guid Id { get; set; }
        public Guid AvatarId { get; set; }
        public EggType EggType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public EggRarity Rarity { get; set; }
        public int RarityLevel
        {
            get
            {
                switch (Rarity)
                {
                    case EggRarity.Common:
                        return 1;

                    case EggRarity.Uncommon:
                        return 2;

                    case EggRarity.Rare:
                        return 3;

                    case EggRarity.Epic:
                        return 4;

                    case EggRarity.Legendary:
                        return 5;

                    case EggRarity.Mythic:
                        return 6;

                    case EggRarity.Divine:
                        return 7;

                    case EggRarity.Celestial:
                        return 8;

                    case EggRarity.Transcendent:
                        return 9;

                    case EggRarity.Omnipotent:
                        return 10;

                    default:
                        return 0;
                }
            }
        }

        public DateTime DiscoveredAt { get; set; }
        public string Location { get; set; } // Where in the OASIS it was found
        public Guid LocationId { get; set; } // Where in the OASIS it was found
        public EggDiscoveryMethod DiscoveryMethod { get; set; }
        public bool IsDisplayed { get; set; } // Whether it's shown in avatar's gallery
        
        public GalleryPosition GalleryPosition { get; set; } = GalleryPosition.Hidden; // Position in avatar's trophy gallery
        
        // Egg Categories
        public EggCategory EggCategory { get; set; }
        public bool IsHatchable { get; set; } // Can this egg be hatched?
        public bool IsHatched { get; set; } // Has this egg been hatched?
        public DateTime? HatchedAt { get; set; } // When was it hatched?
        public DateTime? HatchedDate { get; set; } // When was it hatched? (alternative property)
        public DateTime? DiscoveredDate { get; set; } // When was it discovered? (alternative property)
        public int Score { get; set; } // Score value for this egg
        
        // Rewards and Perks
        public List<string> UnlockedQuests { get; set; } = new List<string>(); // Quest IDs unlocked
        public List<string> UnlockedAreas { get; set; } = new List<string>(); // Area IDs unlocked
        public List<AvatarUpgrade> AvatarUpgrades { get; set; } = new List<AvatarUpgrade>(); // Perks/upgrades for avatar
        public List<Pet> HatchedPets { get; set; } = new List<Pet>(); // Pets that hatched from this egg
        
        // Stats and Abilities
        public Dictionary<string, object> Stats { get; set; } = new Dictionary<string, object>(); // Power, value, special abilities
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public List<string> Tags { get; set; } = new List<string>(); // For filtering and categorization
    }

    public class AvatarUpgrade
    {
        public string UpgradeType { get; set; } // Speed, Strength, Magic, Defense, etc.
        public int Value { get; set; }
        public string Description { get; set; }
        public bool IsPermanent { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class Pet
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Species { get; set; } // Dragon, Phoenix, Wolf, etc.
        public string Element { get; set; } // Fire, Ice, Lightning, etc.
        public int Level { get; set; }
        public int Experience { get; set; }
        public Dictionary<string, int> Stats { get; set; } = new Dictionary<string, int>(); // Attack, Defense, Speed, etc.
        public List<string> Abilities { get; set; } = new List<string>(); // Special abilities
        public bool IsActive { get; set; } // Is currently following avatar
        public DateTime HatchedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class EggQuest
    {
        public Guid Id { get; set; }
        public Guid AvatarId { get; set; }
        public string QuestName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public int RequiredSteps { get; set; }
        public int CompletedSteps { get; set; }
        public Dictionary<string, object> Rewards { get; set; } = new Dictionary<string, object>();
    }

    public class EggQuestLeaderboard
    {
        public Guid Id { get; set; }
        public Guid AvatarId { get; set; }
        public string AvatarName { get; set; }
        public int Score { get; set; }
        public int Rank { get; set; }
        public DateTime LastUpdated { get; set; }
        public Dictionary<string, object> Achievements { get; set; } = new Dictionary<string, object>();
    }
}
