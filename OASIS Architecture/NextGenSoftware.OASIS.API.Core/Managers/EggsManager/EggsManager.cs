using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;

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
                if (_avatarEggs.TryGetValue(avatarId, out var eggs))
                {
                    result.Result = eggs.ToList();
                    result.Message = "Hidden eggs discovered by this avatar retrieved successfully.";
                }
                else
                {
                    result.Result = new List<Egg>();
                    result.Message = "No hidden eggs discovered yet. Keep exploring the OASIS!";
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

        public async Task<OASISResult<Egg>> DiscoverEggAsync(Guid avatarId, string eggType, string name, string location, string discoveryMethod, string rarity = "Common", int rarityLevel = 1)
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
                    Location = location,
                    DiscoveryMethod = discoveryMethod,
                    DiscoveredAt = DateTime.UtcNow,
                    IsDisplayed = true,
                    GalleryPosition = "auto", // Will be positioned automatically in gallery
                    Tags = new List<string> { eggType.ToLower(), rarity.ToLower(), discoveryMethod.ToLower() }
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

        public async Task<OASISResult<Egg>> DiscoverEggFromQuestAsync(Guid avatarId, Guid questId, string eggType, string name, string rarity = "Rare", int rarityLevel = 3)
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
                    Location = "Quest Reward",
                    DiscoveryMethod = "Quest Completion",
                    DiscoveredAt = DateTime.UtcNow,
                    IsDisplayed = true,
                    GalleryPosition = "auto",
                    Tags = new List<string> { eggType.ToLower(), rarity.ToLower(), "quest", "reward" }
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

        public async Task<OASISResult<Egg>> DiscoverEggFromPuzzleAsync(Guid avatarId, string puzzleName, string eggType, string name, string rarity = "Epic", int rarityLevel = 5)
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
                    DiscoveryMethod = "Puzzle Solved",
                    DiscoveredAt = DateTime.UtcNow,
                    IsDisplayed = true,
                    GalleryPosition = "auto",
                    Tags = new List<string> { eggType.ToLower(), rarity.ToLower(), "puzzle", "solved" }
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

        public async Task<OASISResult<Egg>> DiscoverHiddenEggAsync(Guid avatarId, string secretLocation, string eggType, string name, string rarity = "Legendary", int rarityLevel = 8)
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
                    DiscoveryMethod = "Secret Location",
                    DiscoveredAt = DateTime.UtcNow,
                    IsDisplayed = true,
                    GalleryPosition = "auto",
                    Tags = new List<string> { eggType.ToLower(), rarity.ToLower(), "hidden", "secret" }
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
        }

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
                _ => 10
            };

            // Bonus for unique types
            var typeBonus = egg.EggType switch
            {
                EggType.Dragon => 50,
                EggType.Diamond => 100,
                EggType.Platinum => 75,
                EggType.Gold => 50,
                EggType.Silver => 25,
                EggType.Bronze => 10,
                _ => 0
            };

            // Discovery method bonus
            var discoveryBonus = egg.DiscoveryMethod switch
            {
                "Secret Location" => 100,
                "Puzzle Solved" => 75,
                "Quest Completion" => 50,
                "Exploration" => 25,
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
                        ["discoveryMethods"] = eggs.GroupBy(e => e.DiscoveryMethod).ToDictionary(g => g.Key, g => g.Count()),
                        ["totalScore"] = eggs.Sum(e => CalculateEggScore(e)),
                        ["averageRarity"] = eggs.Average(e => (int)e.Rarity),
                        ["mostCommonType"] = eggs.GroupBy(e => e.EggType).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key.ToString(),
                        ["rarestEgg"] = eggs.OrderByDescending(e => (int)e.Rarity).FirstOrDefault()?.Name
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
    }

    public class Egg
    {
        public Guid Id { get; set; }
        public Guid AvatarId { get; set; }
        public string EggType { get; set; } // bronze, silver, gold, platinum, diamond, dragon, fire, ice, lightning, storm, wind, earth, etc.
        public string Name { get; set; }
        public string Description { get; set; }
        public string Rarity { get; set; } // Common, Rare, Epic, Legendary, Mythic
        public int RarityLevel { get; set; } // 1-10 scale
        public DateTime DiscoveredAt { get; set; }
        public string Location { get; set; } // Where in the OASIS it was found
        public string DiscoveryMethod { get; set; } // How it was discovered (quest, exploration, puzzle, etc.)
        public bool IsDisplayed { get; set; } // Whether it's shown in avatar's gallery
        public string GalleryPosition { get; set; } // Position in avatar's trophy gallery
        
        // Egg Categories
        public string EggCategory { get; set; } // Trophy, Reward, Pet, Upgrade, Quest, Area
        public bool IsHatchable { get; set; } // Can this egg be hatched?
        public bool IsHatched { get; set; } // Has this egg been hatched?
        public DateTime? HatchedAt { get; set; } // When was it hatched?
        
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
