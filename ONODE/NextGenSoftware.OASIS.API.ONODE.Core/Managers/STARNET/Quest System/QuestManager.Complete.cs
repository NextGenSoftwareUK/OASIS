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
    public partial class QuestManager
    {
        public async Task<OASISResult<bool>> CompleteQuestAsync(Guid avatarId, Guid questId, string completionNotes = null)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occurred in QuestManager.CompleteQuestAsync. Reason:";

            try
            {
                // Load the quest
                var questResult = await LoadAsync(avatarId, questId);
                if (questResult.IsError || questResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Quest not found or could not be loaded. Reason: {questResult.Message}");
                    return result;
                }

                // Update quest status to completed
                questResult.Result.Status = QuestStatus.Completed;
                questResult.Result.CompletedOn = DateTime.UtcNow;
                questResult.Result.CompletedBy = avatarId;
                if (!string.IsNullOrEmpty(completionNotes))
                {
                    questResult.Result.CompletionNotes = completionNotes;
                }

                if (questResult.Result.MetaData == null) questResult.Result.MetaData = new Dictionary<string, object>();
                questResult.Result.MetaData["Status"] = questResult.Result.Status.ToString();

                // Save the updated quest
                var updateResult = await UpdateAsync(avatarId, questResult.Result);
                if (updateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to save completed quest. Reason: {updateResult.Message}");
                    return result;
                }

                result.Result = true;
                result.Message = "Quest completed successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occurred. Reason: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets quest leaderboard for a specific quest
        /// </summary>
        /// <param name="questId">The quest ID</param>
        /// <param name="limit">Number of entries to return</param>
        /// <returns>Quest leaderboard entries</returns>
        public async Task<OASISResult<List<QuestLeaderboard>>> GetQuestLeaderboardAsync(Guid questId, int limit = 50)
        {
            OASISResult<List<QuestLeaderboard>> result = new OASISResult<List<QuestLeaderboard>>();
            string errorMessage = "Error occurred in QuestManager.GetQuestLeaderboardAsync. Reason:";

            try
            {
                // Implement real leaderboard logic
                var leaderboard = new List<QuestLeaderboard>();
                
                // Get all completed quests for the specified quest
                var completedQuestsResult = await GetCompletedQuestsForQuestAsync(questId);
                if (completedQuestsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get completed quests: {completedQuestsResult.Message}");
                    return result;
                }
                
                var completedQuests = completedQuestsResult.Result;
                
                // Group by avatar and calculate scores
                var avatarScores = completedQuests
                    .GroupBy(q => q.CompletedBy)
                    .Select(g => new
                    {
                        AvatarId = g.Key,
                        TotalScore = g.Sum(q => q.RewardXP),
                        CompletionTime = g.Average(q => (q.CompletedOn - q.StartedOn).TotalMinutes),
                        QuestCount = g.Count(),
                        LastCompleted = g.Max(q => q.CompletedOn)
                    })
                    .OrderByDescending(x => x.TotalScore)
                    .ThenBy(x => x.CompletionTime)
                    .ToList();
                
                // Create leaderboard entries
                int rank = 1;
                foreach (var avatarScore in avatarScores)
                {
                    // Get avatar details
                    var avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarScore.AvatarId);
                    var avatarName = avatarResult.IsError ? "Unknown Avatar" : avatarResult.Result.Username;
                    
                    leaderboard.Add(new QuestLeaderboard
                    {
                        Rank = rank++,
                        AvatarId = avatarScore.AvatarId,
                        AvatarName = avatarName,
                        Score = (int)avatarScore.TotalScore,
                        CompletedAt = avatarScore.LastCompleted
                    });
                }
                
                result.Result = leaderboard;
                result.Message = "Quest leaderboard retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occurred. Reason: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets quest rewards for a specific quest
        /// </summary>
        /// <param name="questId">The quest ID</param>
        /// <returns>Quest rewards</returns>
        public async Task<OASISResult<List<QuestReward>>> GetQuestRewardsAsync(Guid questId)
        {
            OASISResult<List<QuestReward>> result = new OASISResult<List<QuestReward>>();
            string errorMessage = "Error occurred in QuestManager.GetQuestRewardsAsync. Reason:";

            try
            {
                // Load the quest to get its rewards
                var questResult = await LoadAsync(AvatarId, questId);
                if (questResult.IsError || questResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Quest not found. Reason: {questResult.Message}");
                    return result;
                }

                // Implement real rewards logic
                var rewards = new List<QuestReward>();
                var quest = questResult.Result;
                
                // Extract rewards from quest metadata
                if (quest.MetaData != null && quest.MetaData.ContainsKey("Rewards"))
                {
                    try
                    {
                        var rewardsJson = quest.MetaData["Rewards"]?.ToString();
                        if (!string.IsNullOrEmpty(rewardsJson))
                        {
                            var questRewards = System.Text.Json.JsonSerializer.Deserialize<List<QuestReward>>(rewardsJson);
                            if (questRewards != null)
                            {
                                rewards.AddRange(questRewards);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError($"Error parsing quest rewards: {ex.Message}");
                    }
                }
                
                // Add default rewards based on quest difficulty and type
                if (!rewards.Any())
                {
                    rewards.AddRange(GetDefaultQuestRewards(quest));
                }
                
                // Calculate dynamic rewards based on quest completion statistics
                var completionStats = await GetQuestCompletionStatsAsync(questId);
                if (!completionStats.IsError)
                {
                    var bonusRewards = CalculateBonusRewards(quest, completionStats.Result);
                    rewards.AddRange(bonusRewards);
                }
                
                result.Result = rewards;
                result.Message = "Quest rewards retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occurred. Reason: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets quest statistics for a specific avatar
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>Quest statistics</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetQuestStatsAsync(Guid avatarId)
        {
            OASISResult<Dictionary<string, object>> result = new OASISResult<Dictionary<string, object>>();
            string errorMessage = "Error occurred in QuestManager.GetQuestStatsAsync. Reason:";

            try
            {
                // Load all quests for the avatar
                var questsResult = await LoadAllForAvatarAsync(avatarId);
                if (questsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to load avatar quests. Reason: {questsResult.Message}");
                    return result;
                }

                if (questsResult != null && questsResult.Result != null && !questsResult.IsError)
                {
                    var stats = new Dictionary<string, object>
                    {
                        ["totalQuests"] = questsResult.Result.Count(),
                        ["completedQuests"] = questsResult.Result.Count(q => q.Status == QuestStatus.Completed),
                        ["activeQuests"] = questsResult.Result.Count(q => q.Status == QuestStatus.InProgress),
                        ["pendingQuests"] = questsResult.Result.Count(q => q.Status == QuestStatus.NotStarted),
                        ["totalKarmaEarnt"] = questsResult.Result.Where(q => q.Status == QuestStatus.Completed).Sum(q => q.RewardKarma),
                        ["totalXPEarnt"] = questsResult.Result.Where(q => q.Status == QuestStatus.Completed).Sum(q => q.RewardXP),
                        //["totalRewards"] = questsResult.Result.Where(q => q.Status == QuestStatus.Completed).Sum(q => q.Rewards?.Sum(r => r.Amount) ?? 0)
                    };


                    result.Result = stats;
                    result.Message = "Quest statistics retrieved successfully";
                }
                else      
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} No quests found for the avatar.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occurred. Reason: {ex.Message}");
            }

            return result;
        }
    }
}
