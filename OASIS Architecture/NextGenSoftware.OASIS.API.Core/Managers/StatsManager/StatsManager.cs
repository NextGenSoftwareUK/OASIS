using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
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
        private readonly object _cacheLock = new object();
        private readonly Dictionary<string, (DateTime fetchedAt, Dictionary<string, object> data)> _statsCache
            = new Dictionary<string, (DateTime, Dictionary<string, object>)>();

        // Caching controls (can be toggled at runtime)
        public static bool EnableCaching { get; set; } = false;
        public static TimeSpan CacheTtl { get; set; } = TimeSpan.FromSeconds(45);

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
            try
            {
                if (OASISDNA != null && OASISDNA.OASIS != null)
                {
                    EnableCaching = OASISDNA.OASIS.StatsCacheEnabled;
                    if (OASISDNA.OASIS.StatsCacheTtlSeconds > 0)
                        CacheTtl = TimeSpan.FromSeconds(OASISDNA.OASIS.StatsCacheTtlSeconds);
                }
            }
            catch
            {
                // ignore config issues; defaults remain
            }
        }

        private string BuildCacheKey(Guid avatarId, string category)
        {
            return $"{avatarId}:{category}";
        }

        private bool TryGetFromCache(Guid avatarId, string category, out Dictionary<string, object> cached)
        {
            cached = null;
            if (!EnableCaching)
                return false;

            var key = BuildCacheKey(avatarId, category);
            lock (_cacheLock)
            {
                if (_statsCache.TryGetValue(key, out var entry))
                {
                    if (DateTime.UtcNow - entry.fetchedAt <= CacheTtl && entry.data != null)
                    {
                        cached = entry.data;
                        return true;
                    }
                    _statsCache.Remove(key);
                }
            }
            return false;
        }

        private void SetCache(Guid avatarId, string category, Dictionary<string, object> data)
        {
            if (!EnableCaching || data == null)
                return;

            var key = BuildCacheKey(avatarId, category);
            lock (_cacheLock)
            {
                _statsCache[key] = (DateTime.UtcNow, data);
            }
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
                    ["level"] = 1, // Default level since IAvatar doesn't have Level property
                    
                    // Core Stats (default values since IAvatar doesn't have these properties)
                    ["karma"] = 0,
                    ["xp"] = 0,
                    ["stats"] = new Dictionary<string, object>(),
                    ["chakras"] = new Dictionary<string, object>(),
                    ["aura"] = new Dictionary<string, object>(),
                    ["skills"] = new Dictionary<string, object>(),
                    ["attributes"] = new Dictionary<string, object>(),
                    ["superPowers"] = new List<object>(),
                    
                    // Collections (default values since IAvatar doesn't have these properties)
                    ["gifts"] = 0,
                    ["spells"] = 0,
                    ["achievements"] = 0,
                    ["inventory"] = 0,
                    ["geneKeys"] = 0,
                    
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
                    ["lastLoginDate"] = DateTime.UtcNow, // Default value since IAvatar doesn't have LastLoginDate property
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
                // cache check
                if (TryGetFromCache(avatarId, "karma", out var cachedKarma))
                {
                    result.Result = cachedKarma;
                    result.Message = "Karma statistics retrieved from cache.";
                    return result;
                }
                // Try to load karma statistics from the settings system first
                var karmaStatsResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "karma");
                if (!karmaStatsResult.IsError && karmaStatsResult.Result != null && karmaStatsResult.Result.Count > 0)
                {
                    result.Result = karmaStatsResult.Result;
                    result.Message = "Karma statistics retrieved from settings system.";
                    SetCache(avatarId, "karma", result.Result);
                }
                else
                {
                    // Fallback to KarmaManager if settings system fails or has no data
                    var karmaManagerResult = await KarmaManager.Instance.GetKarmaStatsAsync(avatarId);
                    if (karmaManagerResult.IsError)
                    {
                        // Return default stats if both fail
                        result.Result = new Dictionary<string, object>
                        {
                            ["totalKarma"] = 0,
                            ["karmaTransactions"] = 0,
                            ["lastKarmaChange"] = DateTime.UtcNow,
                            ["lastKarmaAmount"] = 0,
                            ["lastKarmaSource"] = "None"
                        };
                    }
                    else
                    {
                        result.Result = karmaManagerResult.Result;
                        SetCache(avatarId, "karma", result.Result);
                    }
                    result.Message = "Karma statistics retrieved from KarmaManager (fallback).";
                }
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
                // cache check
                if (TryGetFromCache(avatarId, "gifts", out var cachedGifts))
                {
                    result.Result = cachedGifts;
                    result.Message = "Gift statistics retrieved from cache.";
                    return result;
                }

                // Try storage first
                var giftsSettings = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "gifts");
                if (!giftsSettings.IsError && giftsSettings.Result != null && giftsSettings.Result.Count > 0)
                {
                    result.Result = giftsSettings.Result;
                    result.Message = "Gift statistics retrieved from settings system.";
                    SetCache(avatarId, "gifts", result.Result);
                }
                else
                {
                    // Fallback to manager
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
                        SetCache(avatarId, "gifts", result.Result);
                    }
                    result.Message = "Gift statistics retrieved from GiftsManager (fallback).";
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
                // cache check
                if (TryGetFromCache(avatarId, "chat", out var cachedChat))
                {
                    result.Result = cachedChat;
                    result.Message = "Chat statistics retrieved from cache.";
                    return result;
                }
                // Try to load chat statistics from the settings system first
                var chatStatsResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "chat");
                if (!chatStatsResult.IsError && chatStatsResult.Result != null && chatStatsResult.Result.Count > 0)
                {
                    result.Result = chatStatsResult.Result;
                    result.Message = "Chat statistics retrieved from settings system.";
                    SetCache(avatarId, "chat", result.Result);
                }
                else
                {
                    // Fallback to ChatManager if settings system fails or has no data
                    var chatManagerResult = await ChatManager.Instance.GetChatStatsAsync(avatarId);
                    if (chatManagerResult.IsError)
                    {
                        // Return default stats if both fail
                        result.Result = new Dictionary<string, object>
                        {
                            ["totalMessagesSent"] = 0,
                            ["totalSessions"] = 0,
                            ["lastMessageSent"] = DateTime.UtcNow,
                            ["lastMessageType"] = "None"
                        };
                    }
                    else
                    {
                        result.Result = chatManagerResult.Result;
                        SetCache(avatarId, "chat", result.Result);
                    }
                    result.Message = "Chat statistics retrieved from ChatManager (fallback).";
                }
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
                // cache check
                if (TryGetFromCache(avatarId, "keys", out var cachedKeys))
                {
                    result.Result = cachedKeys;
                    result.Message = "Key statistics retrieved from cache.";
                    return result;
                }

                // Try storage first
                var keySettings = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "keys");
                if (!keySettings.IsError && keySettings.Result != null && keySettings.Result.Count > 0)
                {
                    result.Result = keySettings.Result;
                    result.Message = "Key statistics retrieved from settings system.";
                    SetCache(avatarId, "keys", result.Result);
                }
                else
                {
                    // Fallback to manager
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
                        SetCache(avatarId, "keys", result.Result);
                    }
                    result.Message = "Key statistics retrieved from KeyManager (fallback).";
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
        public OASISResult<Dictionary<string, object>> GetAchievementStats(IAvatar avatar)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                // Since IAvatar doesn't have Achievements property, return default values
                var achievements = new List<IAchievement>();
                var achievementTypes = new Dictionary<string, int>();
                
                var stats = new Dictionary<string, object>
                {
                    ["totalAchievements"] = 0,
                    ["achievementTypes"] = achievementTypes,
                    ["recentAchievements"] = new List<object>(),
                    ["totalKarmaFromAchievements"] = 0
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
                // cache check
                if (TryGetFromCache(avatarId, "quests", out var cachedQuests))
                {
                    result.Result = cachedQuests;
                    result.Message = "Quest statistics retrieved from cache.";
                    return result;
                }

                // Load quest statistics using the new settings system (storage-first)
                var questStatsResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "quests");
                if (!questStatsResult.IsError && questStatsResult.Result != null && questStatsResult.Result.Count > 0)
                {
                    result.Result = questStatsResult.Result;
                    result.Message = "Quest statistics retrieved from settings system.";
                    SetCache(avatarId, "quests", result.Result);
                }
                else
                {
                    // Fallback not available here to avoid cross-project dependency on QuestManager.
                    // Return safe defaults.
                    result.Result = new Dictionary<string, object>
                    {
                        ["totalQuests"] = 0,
                        ["completedQuests"] = 0,
                        ["activeQuests"] = 0,
                        ["questCompletionRate"] = 0.0,
                        ["averageQuestTime"] = 0,
                        ["questTypes"] = new Dictionary<string, int>(),
                        ["recentQuests"] = new List<object>(),
                        ["questStreak"] = 0,
                        ["longestQuestStreak"] = 0,
                        ["totalQuestRewards"] = 0
                    };
                }
                
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
                // cache check (we key as leaderboard)
                if (TryGetFromCache(avatarId, "leaderboard", out var cachedLeaderboard))
                {
                    result.Result = cachedLeaderboard;
                    result.Message = "Leaderboard statistics retrieved from cache.";
                    return result;
                }

                // Try storage first for latest saved stats from CompetitionManager
                var lbSettings = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "leaderboard");
                if (!lbSettings.IsError && lbSettings.Result != null && lbSettings.Result.Count > 0)
                {
                    result.Result = lbSettings.Result;
                    result.Message = "Leaderboard statistics retrieved from settings system.";
                    SetCache(avatarId, "leaderboard", result.Result);
                    return result;
                }

                // Get leaderboard statistics using CompetitionManager directly
                var competitionManager = CompetitionManager.Instance;
                
                var currentRank = 0;
                var totalScore = 0;
                var currentLeague = "Bronze";
                
                try
                {
                    // Get rank for karma competition
                    var rankResult = await competitionManager.GetAvatarRankAsync(avatarId, CompetitionType.Karma, SeasonType.Monthly);
                    if (!rankResult.IsError && rankResult.Result != null)
                    {
                        currentRank = (int)rankResult.Result.Rank;
                        totalScore = (int)rankResult.Result.Score;
                        
                        // Determine league based on rank
                        if (currentRank <= 10) currentLeague = "Diamond";
                        else if (currentRank <= 50) currentLeague = "Platinum";
                        else if (currentRank <= 100) currentLeague = "Gold";
                        else if (currentRank <= 500) currentLeague = "Silver";
                        else currentLeague = "Bronze";
                    }
                }
                catch
                {
                    // Use defaults if CompetitionManager fails
                }
                
                result.Result = new Dictionary<string, object>
                {
                    ["currentRank"] = currentRank,
                    ["previousRank"] = currentRank, // Would need historical data for this
                    ["rankChange"] = 0, // Would need historical data for this
                    ["currentLeague"] = currentLeague,
                    ["previousLeague"] = currentLeague, // Would need historical data for this
                    ["leaguePromoted"] = false, // Would need historical data for this
                    ["leagueDemoted"] = false, // Would need historical data for this
                    ["totalScore"] = totalScore,
                    ["seasonStart"] = DateTime.UtcNow.AddDays(-30),
                    ["seasonEnd"] = DateTime.UtcNow.AddDays(30),
                    ["badges"] = new List<string>(),
                    ["recentAchievements"] = new List<object>()
                };
                SetCache(avatarId, "leaderboard", result.Result);
                
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
                // Load gifts count using the new settings system
                var giftsResult = await HolonManager.Instance.LoadSettingAsync<int>(Guid.Empty, "system", "totalGifts", 0);
                return giftsResult.Result;
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
                // Load chat messages count using the new settings system
                var messagesResult = await HolonManager.Instance.LoadSettingAsync<int>(Guid.Empty, "system", "totalMessages", 0);
                return messagesResult.Result;
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
                // Load active users count using the new settings system
                var usersResult = await HolonManager.Instance.LoadSettingAsync<int>(Guid.Empty, "system", "activeUsers", 0);
                return usersResult.Result;
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
