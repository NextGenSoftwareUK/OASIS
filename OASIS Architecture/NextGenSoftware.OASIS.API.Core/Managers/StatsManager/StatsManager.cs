using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Manages OASIS statistics and analytics
    /// </summary>
    public class StatsManager : OASISManager
    {
        private static StatsManager _instance;

        public static StatsManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new StatsManager(ProviderManager.Instance.CurrentStorageProvider, ProviderManager.Instance.OASISDNA);

                return _instance;
            }
        }

        private StatsManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
        }

        /// <summary>
        /// Get comprehensive stats for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Complete avatar statistics</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetAvatarStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                // Load avatar details
                var avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = "Avatar not found.";
                    return result;
                }

                var avatar = avatarResult.Result;

                // Get detailed statistics
                var karmaStats = await GetKarmaStatsAsync(avatarId);
                var karmaHistory = await GetKarmaHistoryAsync(avatarId);
                var giftStats = await GetGiftStatsAsync(avatarId);
                var chatStats = await GetChatStatsAsync(avatarId);
                var keyStats = await GetKeyStatsAsync(avatarId);
                var achievementStats = GetAchievementStats(avatar);
                var questStats = await GetQuestStatsAsync(avatarId);
                var leaderboardStats = await GetLeaderboardStatsAsync(avatarId);

                var stats = new Dictionary<string, object>
                {
                    // Basic Avatar Info
                    ["avatarId"] = avatar.Id,
                    ["username"] = avatar.Username,
                    ["fullName"] = avatar.FullName,
                    ["avatarType"] = avatar.AvatarType?.Value.ToString(),
                    ["level"] = avatar.Level,
                    
                    // Core Stats
                    ["karma"] = avatar.Karma,
                    ["xp"] = avatar.XP,
                    ["stats"] = avatar.Stats,
                    ["chakras"] = avatar.Chakras,
                    ["aura"] = avatar.Aura,
                    ["skills"] = avatar.Skills,
                    ["attributes"] = avatar.Attributes,
                    ["superPowers"] = avatar.SuperPowers,
                    
                    // Collections
                    ["gifts"] = avatar.Gifts?.Count ?? 0,
                    ["spells"] = avatar.Spells?.Count ?? 0,
                    ["achievements"] = avatar.Achievements?.Count ?? 0,
                    ["inventory"] = avatar.Inventory?.Count ?? 0,
                    ["geneKeys"] = avatar.GeneKeys?.Count ?? 0,
                    
                    // Detailed Statistics
                    ["karmaStats"] = karmaStats.Result,
                    ["karmaHistory"] = karmaHistory.Result,
                    ["giftStats"] = giftStats.Result,
                    ["chatStats"] = chatStats.Result,
                    ["keyStats"] = keyStats.Result,
                    ["achievementStats"] = achievementStats.Result,
                    ["questStats"] = questStats.Result,
                    ["leaderboardStats"] = leaderboardStats.Result,
                    
                    // System Info
                    ["createdDate"] = avatar.CreatedDate,
                    ["modifiedDate"] = avatar.ModifiedDate,
                    ["lastLoginDate"] = avatar.LastLoginDate,
                    ["version"] = avatar.Version
                };

                result.Result = stats;
                result.Message = "Avatar statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving avatar statistics: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get karma statistics for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Karma statistics</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetKarmaStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                // Use KarmaManager to get karma statistics
                var karmaStatsResult = await KarmaManager.Instance.GetKarmaStatsAsync(avatarId);
                if (karmaStatsResult.IsError)
                {
                    // Return default stats if KarmaManager fails
                    result.Result = new Dictionary<string, object>
                    {
                        ["totalKarma"] = 0,
                        ["karmaEarned"] = 0,
                        ["karmaLost"] = 0,
                        ["karmaTransactions"] = 0,
                        ["averageKarmaPerDay"] = 0,
                        ["karmaSources"] = new Dictionary<string, int>(),
                        ["recentActivity"] = new List<object>()
                    };
                }
                else
                {
                    result.Result = karmaStatsResult.Result;
                }
                
                result.Message = "Karma statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving karma statistics: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get karma history for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="limit">Number of records to return</param>
        /// <returns>Karma history</returns>
        public async Task<OASISResult<List<Dictionary<string, object>>>> GetKarmaHistoryAsync(Guid avatarId, int limit = 50)
        {
            var result = new OASISResult<List<Dictionary<string, object>>>();
            try
            {
                var karmaHistoryResult = await KarmaManager.Instance.GetKarmaHistoryAsync(avatarId, limit);
                if (karmaHistoryResult.IsError)
                {
                    result.Result = new List<Dictionary<string, object>>();
                }
                else
                {
                    // Convert KarmaTransaction objects to Dictionary objects
                    result.Result = karmaHistoryResult.Result.Select(t => new Dictionary<string, object>
                    {
                        ["id"] = t.Id,
                        ["avatarId"] = t.AvatarId,
                        ["amount"] = t.Amount,
                        ["sourceType"] = t.SourceType.ToString(),
                        ["description"] = t.Description,
                        ["relatedEntityId"] = t.RelatedEntityId,
                        ["timestamp"] = t.Timestamp
                    }).ToList();
                }
                
                result.Message = "Karma history retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving karma history: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get gift statistics for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Gift statistics</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetGiftStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var giftStatsResult = await GiftsManager.Instance.GetGiftStatsAsync(avatarId);
                if (giftStatsResult.IsError)
                {
                    result.Result = new Dictionary<string, object>
                    {
                        ["sentGifts"] = 0,
                        ["receivedGifts"] = 0,
                        ["openedGifts"] = 0,
                        ["unopenedGifts"] = 0,
                        ["giftTypeDistribution"] = new Dictionary<string, int>(),
                        ["totalScore"] = 0,
                        ["mostCommonGiftType"] = "None"
                    };
                }
                else
                {
                    result.Result = giftStatsResult.Result;
                }
                
                result.Message = "Gift statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving gift statistics: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get chat statistics for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Chat statistics</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetChatStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var chatStatsResult = await ChatManager.Instance.GetChatStatsAsync(avatarId);
                if (chatStatsResult.IsError)
                {
                    result.Result = new Dictionary<string, object>
                    {
                        ["totalMessages"] = 0,
                        ["totalSessions"] = 0,
                        ["totalCharacters"] = 0,
                        ["averageMessageLength"] = 0,
                        ["totalScore"] = 0,
                        ["mostActiveDay"] = "None",
                        ["longestMessage"] = 0
                    };
                }
                else
                {
                    result.Result = chatStatsResult.Result;
                }
                
                result.Message = "Chat statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving chat statistics: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get key statistics for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Key statistics</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetKeyStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var keyStatsResult = await KeyManager.Instance.GetKeyStatsAsync(avatarId);
                if (keyStatsResult.IsError)
                {
                    result.Result = new Dictionary<string, object>
                    {
                        ["totalKeys"] = 0,
                        ["activeKeys"] = 0,
                        ["inactiveKeys"] = 0,
                        ["totalUsage"] = 0,
                        ["keyTypeDistribution"] = new Dictionary<string, int>(),
                        ["averageUsagePerKey"] = 0,
                        ["mostUsedKeyType"] = "None",
                        ["totalScore"] = 0
                    };
                }
                else
                {
                    result.Result = keyStatsResult.Result;
                }
                
                result.Message = "Key statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving key statistics: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get achievement statistics for an avatar
        /// </summary>
        /// <param name="avatar">Avatar object</param>
        /// <returns>Achievement statistics</returns>
        public OASISResult<Dictionary<string, object>> GetAchievementStats(IAvatarDetail avatar)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var achievements = avatar.Achievements ?? new List<IAchievement>();
                var achievementTypes = achievements.GroupBy(a => a.Name).ToDictionary(g => g.Key, g => g.Count());
                
                var stats = new Dictionary<string, object>
                {
                    ["totalAchievements"] = achievements.Count,
                    ["achievementTypes"] = achievementTypes,
                    ["recentAchievements"] = achievements.OrderByDescending(a => a.CreatedDate).Take(5).Select(a => new
                    {
                        name = a.Name,
                        description = a.Description,
                        earnedDate = a.CreatedDate,
                        karmaReward = a.KarmaReward
                    }).ToList(),
                    ["totalKarmaFromAchievements"] = achievements.Sum(a => a.KarmaReward)
                };

                result.Result = stats;
                result.Message = "Achievement statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving achievement statistics: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get quest statistics for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Quest statistics</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetQuestStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                // TODO: Implement quest statistics when QuestManager is available
                // For now, return default quest stats
                result.Result = new Dictionary<string, object>
                {
                    ["totalQuests"] = 0,
                    ["completedQuests"] = 0,
                    ["activeQuests"] = 0,
                    ["questCompletionRate"] = 0,
                    ["averageQuestTime"] = 0,
                    ["questTypes"] = new Dictionary<string, int>(),
                    ["recentQuests"] = new List<object>()
                };
                
                result.Message = "Quest statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving quest statistics: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get leaderboard statistics for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Leaderboard statistics</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetLeaderboardStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                // TODO: Implement leaderboard statistics when CompetitionManager is available
                // For now, return default leaderboard stats
                result.Result = new Dictionary<string, object>
                {
                    ["currentRank"] = 0,
                    ["previousRank"] = 0,
                    ["rankChange"] = 0,
                    ["currentLeague"] = "Bronze",
                    ["previousLeague"] = "Bronze",
                    ["leaguePromoted"] = false,
                    ["leagueDemoted"] = false,
                    ["totalScore"] = 0,
                    ["seasonStart"] = DateTime.UtcNow.AddDays(-30),
                    ["seasonEnd"] = DateTime.UtcNow.AddDays(30),
                    ["badges"] = new List<string>(),
                    ["recentAchievements"] = new List<object>()
                };
                
                result.Message = "Leaderboard statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving leaderboard statistics: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get system-wide statistics
        /// </summary>
        /// <returns>System statistics</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetSystemStatsAsync()
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var systemStats = new Dictionary<string, object>
                {
                    ["totalAvatars"] = await GetTotalAvatarCountAsync(),
                    ["totalKarma"] = await GetTotalKarmaAsync(),
                    ["totalGifts"] = await GetTotalGiftsAsync(),
                    ["totalChatMessages"] = await GetTotalChatMessagesAsync(),
                    ["activeUsers"] = await GetActiveUsersCountAsync(),
                    ["systemUptime"] = GetSystemUptime(),
                    ["version"] = "1.0.0", // OASIS Runtime Version
                    ["timestamp"] = DateTime.UtcNow
                };

                result.Result = systemStats;
                result.Message = "System statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving system statistics: {ex.Message}", ex);
            }
            return result;
        }

        #region Private Helper Methods

        /// <summary>
        /// Get total avatar count
        /// </summary>
        /// <returns>Total avatar count</returns>
        private async Task<int> GetTotalAvatarCountAsync()
        {
            try
            {
                // Get avatar count from AvatarManager
                var avatarsResult = await AvatarManager.Instance.SearchAvatarsAsync("");
                if (avatarsResult.IsError)
                    return 0;
                
                return avatarsResult.Result?.Count() ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Get total karma across all avatars
        /// </summary>
        /// <returns>Total karma</returns>
        private async Task<long> GetTotalKarmaAsync()
        {
            try
            {
                // Get all avatars and sum their karma
                var avatarsResult = await AvatarManager.Instance.SearchAvatarsAsync("");
                if (avatarsResult.IsError || avatarsResult.Result == null)
                    return 0;
                
                long totalKarma = 0;
                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar is IAvatarDetail avatarDetail)
                    {
                        totalKarma += avatarDetail.Karma;
                    }
                }
                
                return totalKarma;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Get total gifts count
        /// </summary>
        /// <returns>Total gifts count</returns>
        private async Task<int> GetTotalGiftsAsync()
        {
            try
            {
                // Get gifts count from HolonManager
                var giftsHolon = await HolonManager.Instance.LoadHolonAsync("gifts_count");
                if (giftsHolon.IsError || giftsHolon.Result == null)
                    return 0;
                
                return Convert.ToInt32(giftsHolon.Result.MetaData.GetValueOrDefault("totalGifts", 0));
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Get total chat messages count
        /// </summary>
        /// <returns>Total chat messages count</returns>
        private async Task<int> GetTotalChatMessagesAsync()
        {
            try
            {
                // Get chat messages count from HolonManager
                var chatHolon = await HolonManager.Instance.LoadHolonAsync("chat_messages_count");
                if (chatHolon.IsError || chatHolon.Result == null)
                    return 0;
                
                return Convert.ToInt32(chatHolon.Result.MetaData.GetValueOrDefault("totalMessages", 0));
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Get active users count
        /// </summary>
        /// <returns>Active users count</returns>
        private async Task<int> GetActiveUsersCountAsync()
        {
            try
            {
                // Get active users count from HolonManager
                var activeUsersHolon = await HolonManager.Instance.LoadHolonAsync("active_users_count");
                if (activeUsersHolon.IsError || activeUsersHolon.Result == null)
                    return 0;
                
                return Convert.ToInt32(activeUsersHolon.Result.MetaData.GetValueOrDefault("activeUsers", 0));
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Get system uptime
        /// </summary>
        /// <returns>System uptime</returns>
        private TimeSpan GetSystemUptime()
        {
            try
            {
                return DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime;
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }

        #endregion
    }
}
