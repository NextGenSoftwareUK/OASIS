using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TelegramController : ControllerBase
    {
        private readonly TelegramOASIS _telegramProvider;
        private readonly AvatarManager _avatarManager;
        private readonly AchievementManager _achievementManager;

        public TelegramController(
            TelegramOASIS telegramProvider,
            AvatarManager avatarManager,
            AchievementManager achievementManager)
        {
            _telegramProvider = telegramProvider;
            _avatarManager = avatarManager;
            _achievementManager = achievementManager;
        }

        #region Avatar Linking

        /// <summary>
        /// Link a Telegram account to an OASIS avatar
        /// </summary>
        [HttpPost("link-avatar")]
        public async Task<ActionResult<OASISResult<TelegramAvatar>>> LinkAvatar([FromBody] LinkAvatarRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required");

            // Check if avatar ID provided, otherwise create new avatar
            Guid avatarId = request.AvatarId ?? Guid.Empty;
            
            if (avatarId == Guid.Empty)
            {
                // Create new OASIS avatar
                var avatarResult = await _avatarManager.CreateAvatarAsync(
                    $"tg_{request.TelegramUsername}",
                    request.FirstName,
                    request.FirstName,
                    request.LastName ?? "",
                    $"{request.TelegramUsername}@telegram.oasis",
                    Guid.NewGuid().ToString() // Random password
                );

                if (avatarResult.IsError || avatarResult.Result == null)
                    return StatusCode(500, avatarResult);

                avatarId = avatarResult.Result.Id;
            }

            // Link Telegram to OASIS
            var result = await _telegramProvider.LinkTelegramToAvatarAsync(
                request.TelegramId,
                request.TelegramUsername,
                request.FirstName,
                request.LastName ?? "",
                avatarId
            );

            if (result.IsError)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// Get Telegram avatar by Telegram ID
        /// </summary>
        [HttpGet("avatar/telegram/{telegramId}")]
        public async Task<ActionResult<OASISResult<TelegramAvatar>>> GetAvatarByTelegramId(long telegramId)
        {
            var result = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(telegramId);

            if (result.IsError)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Get Telegram avatar by OASIS Avatar ID
        /// </summary>
        [HttpGet("avatar/oasis/{oasisAvatarId}")]
        public async Task<ActionResult<OASISResult<TelegramAvatar>>> GetAvatarByOASISId(Guid oasisAvatarId)
        {
            var result = await _telegramProvider.GetTelegramAvatarByOASISIdAsync(oasisAvatarId);

            if (result.IsError)
                return NotFound(result);

            return Ok(result);
        }

        #endregion

        #region Group Management

        /// <summary>
        /// Create a new accountability group
        /// </summary>
        [HttpPost("groups/create")]
        public async Task<ActionResult<OASISResult<TelegramGroup>>> CreateGroup([FromBody] CreateGroupRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required");

            var result = await _telegramProvider.CreateGroupAsync(
                request.Name,
                request.Description ?? "",
                request.CreatedBy,
                request.TelegramChatId
            );

            if (result.IsError)
                return StatusCode(500, result);

            // Add creator as first member
            if (request.CreatorTelegramId.HasValue)
            {
                await _telegramProvider.AddMemberToGroupAsync(result.Result.Id, request.CreatorTelegramId.Value);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get group by ID
        /// </summary>
        [HttpGet("groups/{groupId}")]
        public async Task<ActionResult<OASISResult<TelegramGroup>>> GetGroup(string groupId)
        {
            var result = await _telegramProvider.GetGroupAsync(groupId);

            if (result.IsError)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Join a group
        /// </summary>
        [HttpPost("groups/join")]
        public async Task<ActionResult<OASISResult<bool>>> JoinGroup([FromBody] JoinGroupRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required");

            var result = await _telegramProvider.AddMemberToGroupAsync(request.GroupId, request.TelegramUserId);

            if (result.IsError)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// Get user's groups
        /// </summary>
        [HttpGet("groups/user/{telegramUserId}")]
        public async Task<ActionResult<OASISResult<List<TelegramGroup>>>> GetUserGroups(long telegramUserId)
        {
            var result = await _telegramProvider.GetUserGroupsAsync(telegramUserId);

            if (result.IsError)
                return StatusCode(500, result);

            return Ok(result);
        }

        #endregion

        #region Achievement Management

        /// <summary>
        /// Create a new achievement
        /// </summary>
        [HttpPost("achievements/create")]
        public async Task<ActionResult<OASISResult<Achievement>>> CreateAchievement([FromBody] CreateAchievementRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required");

            var achievement = new Achievement
            {
                GroupId = request.GroupId,
                UserId = request.UserId,
                TelegramUserId = request.TelegramUserId,
                Description = request.Description,
                Type = request.Type,
                KarmaReward = request.KarmaReward,
                TokenReward = request.TokenReward,
                Deadline = request.Deadline
            };

            var result = await _telegramProvider.CreateAchievementAsync(achievement);

            if (result.IsError)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// Complete an achievement
        /// </summary>
        [HttpPost("achievements/complete")]
        public async Task<ActionResult<OASISResult<AchievementReward>>> CompleteAchievement([FromBody] CompleteAchievementRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required");

            var result = await _achievementManager.CompleteAchievementAsync(request.AchievementId, request.VerifiedBy);

            if (result.IsError)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// Get user achievements
        /// </summary>
        [HttpGet("achievements/user/{userId}")]
        public async Task<ActionResult<OASISResult<List<Achievement>>>> GetUserAchievements(Guid userId)
        {
            var result = await _telegramProvider.GetUserAchievementsAsync(userId);

            if (result.IsError)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// Get group achievements
        /// </summary>
        [HttpGet("achievements/group/{groupId}")]
        public async Task<ActionResult<OASISResult<List<Achievement>>>> GetGroupAchievements(string groupId)
        {
            var result = await _telegramProvider.GetGroupAchievementsAsync(groupId);

            if (result.IsError)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// Add a check-in
        /// </summary>
        [HttpPost("achievements/checkin")]
        public async Task<ActionResult<OASISResult<Achievement>>> AddCheckIn([FromBody] CheckInRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required");

            // Add check-in
            var result = await _telegramProvider.AddCheckInAsync(
                request.AchievementId,
                request.Message,
                request.KarmaAwarded
            );

            if (result.IsError)
                return StatusCode(500, result);

            // Award karma
            await _achievementManager.AwardKarmaAsync(request.UserId, request.KarmaAwarded);

            return Ok(result);
        }

        #endregion

        #region Webhook

        /// <summary>
        /// Webhook for Telegram bot updates
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] object update)
        {
            // Handle Telegram webhook updates here
            // This would be processed by TelegramBotService
            return Ok();
        }

        #endregion
    }

    #region Request Models

    public class LinkAvatarRequest
    {
        public long TelegramId { get; set; }
        public string TelegramUsername { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Guid? AvatarId { get; set; }
    }

    public class CreateGroupRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid CreatedBy { get; set; }
        public long TelegramChatId { get; set; }
        public long? CreatorTelegramId { get; set; }
    }

    public class JoinGroupRequest
    {
        public string GroupId { get; set; }
        public long TelegramUserId { get; set; }
    }

    public class CreateAchievementRequest
    {
        public string GroupId { get; set; }
        public Guid UserId { get; set; }
        public long TelegramUserId { get; set; }
        public string Description { get; set; }
        public AchievementType Type { get; set; }
        public int KarmaReward { get; set; }
        public decimal TokenReward { get; set; }
        public DateTime? Deadline { get; set; }
    }

    public class CompleteAchievementRequest
    {
        public string AchievementId { get; set; }
        public long? VerifiedBy { get; set; }
    }

    public class CheckInRequest
    {
        public string AchievementId { get; set; }
        public Guid UserId { get; set; }
        public string Message { get; set; }
        public int KarmaAwarded { get; set; }
    }

    #endregion
}





