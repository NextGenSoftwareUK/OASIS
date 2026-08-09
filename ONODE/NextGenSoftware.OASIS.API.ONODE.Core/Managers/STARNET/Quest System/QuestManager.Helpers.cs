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
{    public partial class QuestManager
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
}