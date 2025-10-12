using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors]
    public class TelegramController : OASISControllerBase
    {
        private readonly TelegramOASIS _telegramProvider;
        private readonly TelegramBotService _botService;
        private readonly ILogger<TelegramController> _logger;

        public TelegramController(
            TelegramOASIS telegramProvider,
            TelegramBotService botService,
            ILogger<TelegramController> logger)
        {
            _telegramProvider = telegramProvider;
            _botService = botService;
            _logger = logger;
        }

        #region Webhook

        /// <summary>
        /// Telegram webhook endpoint - receives updates from Telegram servers
        /// </summary>
        /// <param name="update">Telegram update object</param>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook([FromBody] Update update)
        {
            try
            {
                if (update == null)
                {
                    return BadRequest("Invalid update");
                }

                // Process the update asynchronously
                // The TelegramBotService will handle the actual processing
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessWebhookUpdate(update);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing webhook update");
                    }
                });

                // Return 200 OK immediately to Telegram
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling webhook");
                return Ok(); // Still return 200 to Telegram to avoid retries
            }
        }

        private async Task ProcessWebhookUpdate(Update update)
        {
            // Forward to bot service for processing
            // In production, you might want to use a message queue here
            _logger.LogInformation($"Processing webhook update: {update.Type}");
        }

        #endregion

        #region Avatar Linking

        /// <summary>
        /// Link Telegram account to OASIS avatar
        /// </summary>
        /// <param name="request">Link request containing Telegram and avatar info</param>
        [HttpPost("link-avatar")]
        [AllowAnonymous]
        public async Task<OASISResult<TelegramAvatar>> LinkAvatar([FromBody] LinkAvatarRequest request)
        {
            try
            {
                if (request == null || request.TelegramUserId == 0)
                {
                    return new OASISResult<TelegramAvatar>
                    {
                        IsError = true,
                        Message = "Invalid request"
                    };
                }

                // Create or link avatar
                var oasisAvatarId = request.OasisAvatarId ?? Guid.NewGuid();

                var result = await _telegramProvider.LinkTelegramToAvatarAsync(
                    request.TelegramUserId,
                    request.TelegramUsername,
                    request.FirstName,
                    request.LastName,
                    oasisAvatarId
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking avatar");
                return new OASISResult<TelegramAvatar>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Get Telegram avatar by Telegram user ID
        /// </summary>
        [HttpGet("avatar/telegram/{telegramUserId}")]
        public async Task<OASISResult<TelegramAvatar>> GetAvatarByTelegramId(long telegramUserId)
        {
            try
            {
                return await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(telegramUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting avatar for Telegram user {telegramUserId}");
                return new OASISResult<TelegramAvatar>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Get Telegram avatar by OASIS avatar ID
        /// </summary>
        [HttpGet("avatar/oasis/{oasisAvatarId}")]
        public async Task<OASISResult<TelegramAvatar>> GetAvatarByOasisId(Guid oasisAvatarId)
        {
            try
            {
                return await _telegramProvider.GetTelegramAvatarByOASISIdAsync(oasisAvatarId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting avatar for OASIS ID {oasisAvatarId}");
                return new OASISResult<TelegramAvatar>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        #endregion

        #region Group Management

        /// <summary>
        /// Create accountability group
        /// </summary>
        [HttpPost("groups/create")]
        public async Task<OASISResult<TelegramGroup>> CreateGroup([FromBody] CreateGroupRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Name))
                {
                    return new OASISResult<TelegramGroup>
                    {
                        IsError = true,
                        Message = "Group name is required"
                    };
                }

                var result = await _telegramProvider.CreateGroupAsync(
                    request.Name,
                    request.Description,
                    request.CreatedBy,
                    request.TelegramChatId
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group");
                return new OASISResult<TelegramGroup>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Get group by ID
        /// </summary>
        [HttpGet("groups/{groupId}")]
        public async Task<OASISResult<TelegramGroup>> GetGroup(string groupId)
        {
            try
            {
                return await _telegramProvider.GetGroupAsync(groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting group {groupId}");
                return new OASISResult<TelegramGroup>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Get all groups for a user
        /// </summary>
        [HttpGet("groups/user/{telegramUserId}")]
        public async Task<OASISResult<List<TelegramGroup>>> GetUserGroups(long telegramUserId)
        {
            try
            {
                return await _telegramProvider.GetUserGroupsAsync(telegramUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting groups for user {telegramUserId}");
                return new OASISResult<List<TelegramGroup>>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Add member to group
        /// </summary>
        [HttpPost("groups/{groupId}/join")]
        public async Task<OASISResult<bool>> JoinGroup(string groupId, [FromBody] JoinGroupRequest request)
        {
            try
            {
                return await _telegramProvider.AddMemberToGroupAsync(groupId, request.TelegramUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error joining group {groupId}");
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        #endregion

        #region Achievement Management

        /// <summary>
        /// Create achievement/goal
        /// </summary>
        [HttpPost("achievements/create")]
        public async Task<OASISResult<Achievement>> CreateAchievement([FromBody] Achievement achievement)
        {
            try
            {
                if (achievement == null)
                {
                    return new OASISResult<Achievement>
                    {
                        IsError = true,
                        Message = "Achievement data is required"
                    };
                }

                return await _telegramProvider.CreateAchievementAsync(achievement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating achievement");
                return new OASISResult<Achievement>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Get user achievements
        /// </summary>
        [HttpGet("achievements/user/{userId}")]
        public async Task<OASISResult<List<Achievement>>> GetUserAchievements(Guid userId)
        {
            try
            {
                return await _telegramProvider.GetUserAchievementsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting achievements for user {userId}");
                return new OASISResult<List<Achievement>>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Get group achievements
        /// </summary>
        [HttpGet("achievements/group/{groupId}")]
        public async Task<OASISResult<List<Achievement>>> GetGroupAchievements(string groupId)
        {
            try
            {
                return await _telegramProvider.GetGroupAchievementsAsync(groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting achievements for group {groupId}");
                return new OASISResult<List<Achievement>>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Update achievement status
        /// </summary>
        [HttpPut("achievements/{achievementId}/status")]
        public async Task<OASISResult<Achievement>> UpdateAchievementStatus(
            string achievementId,
            [FromBody] UpdateAchievementStatusRequest request)
        {
            try
            {
                return await _telegramProvider.UpdateAchievementStatusAsync(
                    achievementId,
                    request.Status,
                    request.VerifiedBy
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating achievement {achievementId}");
                return new OASISResult<Achievement>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Add check-in to achievement
        /// </summary>
        [HttpPost("achievements/{achievementId}/checkin")]
        public async Task<OASISResult<Achievement>> AddCheckIn(
            string achievementId,
            [FromBody] CheckInRequest request)
        {
            try
            {
                return await _telegramProvider.AddCheckInAsync(
                    achievementId,
                    request.Message,
                    request.KarmaAwarded
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding check-in to achievement {achievementId}");
                return new OASISResult<Achievement>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        #endregion

        #region Leaderboard & Stats

        /// <summary>
        /// Get group leaderboard
        /// </summary>
        [HttpGet("leaderboard/{groupId}")]
        public async Task<OASISResult<List<LeaderboardEntry>>> GetLeaderboard(string groupId)
        {
            try
            {
                // Get all achievements for the group
                var achievementsResult = await _telegramProvider.GetGroupAchievementsAsync(groupId);
                if (achievementsResult.IsError || achievementsResult.Result == null)
                {
                    return new OASISResult<List<LeaderboardEntry>>
                    {
                        IsError = true,
                        Message = "Failed to load group achievements"
                    };
                }

                // Aggregate karma by user
                var leaderboard = achievementsResult.Result
                    .GroupBy(a => a.UserId)
                    .Select(g => new LeaderboardEntry
                    {
                        UserId = g.Key,
                        TotalKarma = g.Sum(a => a.KarmaReward),
                        TotalTokens = g.Sum(a => a.TokenReward),
                        CompletedAchievements = g.Count(a => a.Status == AchievementStatus.Completed),
                        CheckinCount = g.Sum(a => a.Checkins?.Count ?? 0)
                    })
                    .OrderByDescending(e => e.TotalKarma)
                    .ThenByDescending(e => e.TotalTokens)
                    .ToList();

                return new OASISResult<List<LeaderboardEntry>>
                {
                    Result = leaderboard
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting leaderboard for group {groupId}");
                return new OASISResult<List<LeaderboardEntry>>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Get user stats
        /// </summary>
        [HttpGet("stats/user/{userId}")]
        public async Task<OASISResult<UserStats>> GetUserStats(Guid userId)
        {
            try
            {
                var achievementsResult = await _telegramProvider.GetUserAchievementsAsync(userId);
                if (achievementsResult.IsError)
                {
                    return new OASISResult<UserStats>
                    {
                        IsError = true,
                        Message = achievementsResult.Message
                    };
                }

                var achievements = achievementsResult.Result ?? new List<Achievement>();

                var stats = new UserStats
                {
                    UserId = userId,
                    TotalKarma = achievements.Sum(a => a.KarmaReward),
                    TotalTokens = achievements.Sum(a => a.TokenReward),
                    CompletedAchievements = achievements.Count(a => a.Status == AchievementStatus.Completed),
                    ActiveGoals = achievements.Count(a => a.Status == AchievementStatus.Active),
                    TotalCheckins = achievements.Sum(a => a.Checkins?.Count ?? 0),
                    StreakDays = CalculateStreak(achievements)
                };

                return new OASISResult<UserStats>
                {
                    Result = stats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting stats for user {userId}");
                return new OASISResult<UserStats>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        private int CalculateStreak(List<Achievement> achievements)
        {
            // Calculate consecutive days with check-ins
            var checkInDates = achievements
                .SelectMany(a => a.Checkins ?? new List<CheckIn>())
                .Select(c => c.Timestamp.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            if (!checkInDates.Any())
                return 0;

            int streak = 1;
            for (int i = 0; i < checkInDates.Count - 1; i++)
            {
                if ((checkInDates[i] - checkInDates[i + 1]).Days == 1)
                    streak++;
                else
                    break;
            }

            return streak;
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Send message to user via Telegram
        /// </summary>
        [HttpPost("message/send")]
        [Authorize]
        public async Task<OASISResult<bool>> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                return await _botService.SendMessageAsync(request.ChatId, request.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        #endregion
    }

    #region Request/Response Models

    public class LinkAvatarRequest
    {
        public long TelegramUserId { get; set; }
        public string TelegramUsername { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Guid? OasisAvatarId { get; set; }
    }

    public class CreateGroupRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid CreatedBy { get; set; }
        public long TelegramChatId { get; set; }
    }

    public class JoinGroupRequest
    {
        public long TelegramUserId { get; set; }
    }

    public class UpdateAchievementStatusRequest
    {
        public AchievementStatus Status { get; set; }
        public long? VerifiedBy { get; set; }
    }

    public class CheckInRequest
    {
        public string Message { get; set; }
        public int KarmaAwarded { get; set; } = 10;
    }

    public class SendMessageRequest
    {
        public long ChatId { get; set; }
        public string Message { get; set; }
    }

    public class LeaderboardEntry
    {
        public Guid UserId { get; set; }
        public int TotalKarma { get; set; }
        public decimal TotalTokens { get; set; }
        public int CompletedAchievements { get; set; }
        public int CheckinCount { get; set; }
        public int Rank { get; set; }
    }

    public class UserStats
    {
        public Guid UserId { get; set; }
        public int TotalKarma { get; set; }
        public decimal TotalTokens { get; set; }
        public int CompletedAchievements { get; set; }
        public int ActiveGoals { get; set; }
        public int TotalCheckins { get; set; }
        public int StreakDays { get; set; }
    }

    #endregion
}

