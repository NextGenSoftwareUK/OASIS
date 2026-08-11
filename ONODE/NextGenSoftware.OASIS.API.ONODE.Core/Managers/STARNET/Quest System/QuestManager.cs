using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public partial class QuestManager : QuestManagerBase<Quest, DownloadedQuest, InstalledQuest, STARNETDNA>, IQuestManager
    {
        NFTManager _nftManager = null;

        private NFTManager NFTManager
        {
            get
            {
                if (_nftManager == null)
                    _nftManager = new NFTManager(AvatarId, OASISDNA);

                return _nftManager;
            }
        }

        public QuestManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(QuestType),
            HolonType.Quest,
            HolonType.InstalledQuest,
            "Quest",
            //"QuestId",
            "STARNETHolonId",
            "QuestName",
            "QuestType",
            "oquest",
            "oasis_quests",
            "QuestDNA.json",
            "QuestDNAJSON")
        { }

        public QuestManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(QuestType),
            HolonType.Quest,
            HolonType.InstalledQuest,
            "Quest",
            //"QuestId",
            "STARNETHolonId",
            "QuestName",
            "QuestType",
            "oquest",
            "oasis_quests",
            "QuestDNA.json",
            "QuestDNAJSON")
        { }

        //public override async Task<OASISResult<Quest>> CreateAsync(Guid avatarId, string name, string description, object holonSubType, string fullPathToSourceFolder, ISTARNETCreateOptions<Quest, STARNETDNA> createOptions = null, ProviderType providerType = ProviderType.Default)
        //{


        //    OASISResult<Quest> createResult = await base.CreateAsync(avatarId, name, description, holonSubType, fullPathToSourceFolder, createOptions, providerType);
        //    //{
        //        //CheckIfSourcePathExists = checkIfSourcePathExists,
        //        //STARNETHolon = new Quest
        //        //{
        //        //    QuestType = questType,
        //        //    ParentMissionId = parentMissionId,
        //        //    ParentQuestId = parentQuestId
        //        //}
        //    //}, providerType);



        //    //OASISResult<IQuest> result = new OASISResult<IQuest>((IQuest)createResult.Result);
        //    //OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(createResult, result);
        //    //return result;


        //    //return base.CreateAsync(avatarId, name, description, holonSubType, fullPathToSourceFolder, createOptions, providerType);
        //}

        //public async Task<OASISResult<IQuest>> CreateQuestForMissionAsync(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentMissionId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        //{
        //    return await CreateQuestInternalAsync(avatarId, name, description, questType, fullPathToQuest, parentMissionId, default, checkIfSourcePathExists, providerType);
        //}

        //public OASISResult<IQuest> CreateQuestForMission(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentMissionId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        //{
        //    return CreateQuestInternal(avatarId, name, description, questType, fullPathToQuest, parentMissionId, default, checkIfSourcePathExists, providerType);
        //}

        //public async Task<OASISResult<IQuest>> CreateSubQuestForQuestAsync(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentQuestId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        //{
        //    return await CreateQuestInternalAsync(avatarId, name, description, questType, fullPathToQuest, default, parentQuestId, checkIfSourcePathExists, providerType);
        //}

        //public OASISResult<IQuest> CreateSubQuestForQuest(Guid avatarId, string name, string description, QuestType questType, string fullPathToQuest, Guid parentQuestId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        //{
        //    return CreateQuestInternal(avatarId, name, description, questType, fullPathToQuest, default, parentQuestId, checkIfSourcePathExists, providerType);
        //}

    }


    /// <summary>
    /// Quest leaderboard entry
    /// </summary>
    public class QuestLeaderboard
    {
        public Guid AvatarId { get; set; }
        public string AvatarName { get; set; }
        public int Score { get; set; }
        public DateTime CompletedAt { get; set; }
        public int Rank { get; set; }
    }

    /// <summary>
    /// Quest reward
    /// </summary>
    public class QuestReward
    {
        public string Type { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Quest completion statistics
    /// </summary>
    public class QuestCompletionStats
    {
        public int TotalCompletions { get; set; }
        public double AverageCompletionTime { get; set; }
        public int UniqueCompleters { get; set; }
        public DateTime LastCompleted { get; set; }
    }

    /// <summary>
    /// Helper methods for quest rewards
    /// </summary>
    public partial class QuestManager
    {
        /// <summary>
        /// Get default rewards based on quest properties
        /// </summary>
        private List<QuestReward> GetDefaultQuestRewards(IQuest quest)
        {
            var rewards = new List<QuestReward>();
            
            // Base XP reward
            rewards.Add(new QuestReward
            {
                Type = "XP",
                Amount = CalculateBaseXPReward(quest),
                Description = "Experience points for completing the quest"
            });
            
            // Base karma reward
            rewards.Add(new QuestReward
            {
                Type = "Karma",
                Amount = CalculateBaseKarmaReward(quest),
                Description = "Karma points for completing the quest"
            });
            
            // Difficulty-based rewards
            var difficultyLevel = GetQuestDifficultyLevel(quest);
            if (difficultyLevel > 1)
            {
                rewards.Add(new QuestReward
                {
                    Type = "BonusXP",
                    Amount = difficultyLevel * 10,
                    Description = $"Difficulty bonus for {difficultyLevel}-star quest"
                });
            }
            
            return rewards;
        }
        
        /// <summary>
        /// Calculate bonus rewards based on completion statistics
        /// </summary>
        private List<QuestReward> CalculateBonusRewards(IQuest quest, QuestCompletionStats stats)
        {
            var bonusRewards = new List<QuestReward>();
            
            // First completion bonus
            if (stats.TotalCompletions == 1)
            {
                bonusRewards.Add(new QuestReward
                {
                    Type = "FirstCompletion",
                    Amount = 50,
                    Description = "Bonus for being the first to complete this quest"
                });
            }
            
            // Speed completion bonus
            var estimatedTime = GetQuestEstimatedCompletionTime(quest);
            if (stats.AverageCompletionTime > 0 && estimatedTime > 0)
            {
                var speedRatio = estimatedTime / stats.AverageCompletionTime;
                if (speedRatio > 1.5) // Completed 50% faster than average
                {
                    bonusRewards.Add(new QuestReward
                    {
                        Type = "SpeedBonus",
                        Amount = (int)(speedRatio * 25),
                        Description = "Bonus for completing the quest quickly"
                    });
                }
            }
            
            // Rare quest bonus
            if (stats.UniqueCompleters < 10)
            {
                bonusRewards.Add(new QuestReward
                {
                    Type = "RareQuest",
                    Amount = 100,
                    Description = "Bonus for completing a rare quest"
                });
            }
            
            return bonusRewards;
        }
        
        /// <summary>
        /// Calculate base XP reward
        /// </summary>
        private int CalculateBaseXPReward(IQuest quest)
        {
            var baseXP = 100; // Base XP
            var difficultyMultiplier = GetQuestDifficultyLevel(quest);
            var typeMultiplier = GetQuestTypeMultiplier(quest);
            
            return (int)(baseXP * difficultyMultiplier * typeMultiplier);
        }
        
        /// <summary>
        /// Calculate base karma reward
        /// </summary>
        private int CalculateBaseKarmaReward(IQuest quest)
        {
            var baseKarma = 50; // Base karma
            var difficultyMultiplier = GetQuestDifficultyLevel(quest);
            var typeMultiplier = GetQuestTypeMultiplier(quest);
            
            return (int)(baseKarma * difficultyMultiplier * typeMultiplier);
        }
        
        /// <summary>
        /// Get quest type multiplier for rewards
        /// </summary>
        private double GetQuestTypeMultiplier(IQuest quest)
        {
            var questType = GetQuestType(quest);
            return questType switch
            {
                "Main" => 2.0,
                "Side" => 1.0,
                "Daily" => 0.5,
                "Weekly" => 1.5,
                "Event" => 3.0,
                _ => 1.0
            };
        }
        
        /// <summary>
        /// Get completed quests for a specific quest
        /// </summary>
        private async Task<OASISResult<List<IQuest>>> GetCompletedQuestsForQuestAsync(Guid questId)
        {
            var result = new OASISResult<List<IQuest>>();
            
            try
            {
                // This would typically query the database for completed quests
                // For now, return empty list
                result.Result = new List<IQuest>();
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting completed quests: {ex.Message}", ex);
            }
            
            return result;
        }
        
        /// <summary>
        /// Get quest difficulty level safely
        /// </summary>
        private int GetQuestDifficultyLevel(IQuest quest)
        {
            try
            {
                // Try to get difficulty from metadata first
                if (quest.MetaData != null && quest.MetaData.ContainsKey("DifficultyLevel"))
                {
                    if (int.TryParse(quest.MetaData["DifficultyLevel"]?.ToString(), out var difficulty))
                        return difficulty;
                }
                
                // Default difficulty
                return 1;
            }
            catch
            {
                return 1;
            }
        }
        
        /// <summary>
        /// Get quest estimated completion time safely
        /// </summary>
        private double GetQuestEstimatedCompletionTime(IQuest quest)
        {
            try
            {
                // Try to get estimated time from metadata
                if (quest.MetaData != null && quest.MetaData.ContainsKey("EstimatedCompletionTime"))
                {
                    if (double.TryParse(quest.MetaData["EstimatedCompletionTime"]?.ToString(), out var time))
                        return time;
                }
                
                // Default estimated time (30 minutes)
                return 30.0;
            }
            catch
            {
                return 30.0;
            }
        }
        
        /// <summary>
        /// Get quest type safely
        /// </summary>
        private string GetQuestType(IQuest quest)
        {
            try
            {
                // Try to get type from metadata
                if (quest.MetaData != null && quest.MetaData.ContainsKey("QuestType"))
                {
                    return quest.MetaData["QuestType"]?.ToString() ?? "Side";
                }
                
                // Default type
                return "Side";
            }
            catch
            {
                return "Side";
            }
        }
        
        /// <summary>
        /// Get quest completion statistics
        /// </summary>
        private async Task<OASISResult<QuestCompletionStats>> GetQuestCompletionStatsAsync(Guid questId)
        {
            var result = new OASISResult<QuestCompletionStats>();
            
            try
            {
                var completedQuestsResult = await GetCompletedQuestsForQuestAsync(questId);
                if (completedQuestsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get completed quests: {completedQuestsResult.Message}");
                    return result;
                }
                
                var completedQuests = completedQuestsResult.Result;
                
                var stats = new QuestCompletionStats
                {
                    TotalCompletions = completedQuests.Count,
                    AverageCompletionTime = completedQuests.Any() ? 
                        completedQuests.Average(q => (q.CompletedOn - q.StartedOn).TotalMinutes) : 0,
                    UniqueCompleters = completedQuests.Select(q => q.CompletedBy).Distinct().Count(),
                    LastCompleted = completedQuests.Any() ? 
                        completedQuests.Max(q => q.CompletedOn) : DateTime.MinValue
                };
                
                result.Result = stats;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting quest completion stats: {ex.Message}", ex);
            }
            
            return result;
        }
    }

    /// <summary>In-game progress delta for ApplyQuestProgressAsync (kills, XP, pickups by type, level time). Matches Objective progress dictionaries: ArmorCollected, HealthCollected, WeaponsCollected, PowerupsCollected, AmmoCollected, ItemsCollected, KeysCollected.</summary>
    public class QuestProgressDelta
    {
        /// <summary>Optional profile active objective; when set, that incomplete objective is updated before others (then Order, Id). Omit when the caller does not specify one.</summary>
        public Guid? ActiveObjectiveId { get; set; }
        public int MonstersKilledDelta { get; set; }
        /// <summary>Classname of the specific monster killed (e.g. "cyberdemon", "cacodemon"). When set, matched against NeedToKillMonstersByType requirements. Leave empty when classname is unknown (falls back to NeedToKillMonsters any-type tracking).</summary>
        public string MonsterKilledClassname { get; set; }
        public int XpEarnedDelta { get; set; }
        public int KeysCollectedDelta { get; set; }
        public int ArmorCollectedDelta { get; set; }
        public int HealthCollectedDelta { get; set; }
        public int WeaponsCollectedDelta { get; set; }
        public int PowerupsCollectedDelta { get; set; }
        public int AmmoCollectedDelta { get; set; }
        public string ItemCollectedName { get; set; }
        public int GenericItemPickup { get; set; }
        public int? LevelTimeSeconds { get; set; }
    }

    /// <summary>Result of applying quest progress (percent complete, quest finished, events to dispatch, items to grant).</summary>
    public class QuestProgressApplyResult
    {
        public bool QuestCompleted { get; set; }
        public int ObjectivesCompleted { get; set; }
        public int PercentComplete { get; set; }
        public string Message { get; set; }
        /// <summary>
        /// CrossGameEvents the caller should dispatch immediately after receiving this result.
        /// Populated from CrossGameEventsOnComplete (objectives that completed this round) and
        /// CrossGameEventsOnActivate (the next objective that is now active).
        /// The API controller or game-side client is responsible for actually dispatching these
        /// (e.g. via OGEngineClient, or returning them in the response for the game to poll).
        /// </summary>
        public List<CrossGameEvent> CrossGameEventsToDispatch { get; set; } = new List<CrossGameEvent>();
        /// <summary>
        /// InventoryItem Holon IDs to grant to the avatar for objectives and quests completed in this update.
        /// The API controller should call InventoryItemManager.GrantAsync (or equivalent) for each ID.
        /// </summary>
        public List<Guid> InventoryItemsToGrant { get; set; } = new List<Guid>();
    }
}
