using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Hubs;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Messaging;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/messaging")]
    public class MessagingController : OASISControllerBase
    {
        //OASISSettings _settings;

        //public MessagingController(IOptions<OASISDNA> OASISSettings) : base(OASISSettings)
        //{
        //    _settings = OASISSettings.Value;
        //}

        public MessagingController() 
        {

        }

        /// <summary>
        /// Send's a message to the given avatar
        /// </summary>
        /// <param name="toAvatarId">ID of the avatar to send message to</param>
        /// <param name="content">Message content</param>
        /// <param name="messageType">Type of message</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("send-message-to-avatar/{toAvatarId}")]
        public async Task<OASISResult<bool>> SendMessageToAvatar(Guid toAvatarId, [FromBody] string content, [FromQuery] MessagingType messageType = MessagingType.Direct)
        {
            if (content == null)
                return new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide the message content." };
            // Use MessagingManager for business logic
            return await MessagingManager.Instance.SendMessageToAvatarAsync(Avatar.Id, toAvatarId, content, messageType);
        }

        /// <summary>Get total messages and unread received messages for the authenticated avatar.</summary>
        [Authorize]
        [HttpGet("current-avatar/summary")]
        public Task<OASISResult<MessageSummary>> GetCurrentAvatarMessageSummary() =>
            MessagingManager.Instance.GetMessageSummaryAsync(AvatarId);

        /// <summary>
        /// Get messages for the current avatar
        /// </summary>
        /// <param name="limit">Maximum number of messages to return</param>
        /// <param name="offset">Number of messages to skip</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("messages")]
        public Task<OASISResult<List<Message>>> GetMessages([FromQuery] int limit = 50, [FromQuery] int offset = 0) =>
            MessagingManager.Instance.GetMessagesAsync(Avatar.Id, limit, offset);

        /// <summary>
        /// Get conversation between current avatar and another avatar
        /// </summary>
        /// <param name="otherAvatarId">ID of the other avatar</param>
        /// <param name="limit">Maximum number of messages to return</param>
        /// <param name="offset">Number of messages to skip</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("conversation/{otherAvatarId}")]
        public Task<OASISResult<List<Message>>> GetConversation(Guid otherAvatarId, [FromQuery] int limit = 50, [FromQuery] int offset = 0) =>
            MessagingManager.Instance.GetConversationAsync(Avatar.Id, otherAvatarId, limit, offset);

        /// <summary>
        /// Mark messages as read
        /// </summary>
        /// <param name="messageIds">List of message IDs to mark as read</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("mark-messages-read")]
        public async Task<OASISResult<bool>> MarkMessagesAsRead([FromBody] List<Guid> messageIds)
        {
            if (messageIds == null)
                return new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid JSON array of message IDs." };
            // Use MessagingManager for business logic
            return await MessagingManager.Instance.MarkMessagesAsReadAsync(Avatar.Id, messageIds);
        }

        /// <summary>
        /// Get notifications for the current avatar
        /// </summary>
        /// <param name="limit">Maximum number of notifications to return</param>
        /// <param name="offset">Number of notifications to skip</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("notifications")]
        public Task<OASISResult<List<Notification>>> GetNotifications([FromQuery] int limit = 20, [FromQuery] int offset = 0) =>
            MessagingManager.Instance.GetNotificationsAsync(Avatar.Id, limit, offset);

        /// <summary>
        /// Mark notifications as read
        /// </summary>
        /// <param name="notificationIds">List of notification IDs to mark as read</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("mark-notifications-read")]
        public async Task<OASISResult<bool>> MarkNotificationsAsRead([FromBody] List<Guid> notificationIds)
        {
            if (notificationIds == null)
                return new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid JSON array of notification IDs." };
            // Use MessagingManager for business logic
            return await MessagingManager.Instance.MarkNotificationsAsReadAsync(Avatar.Id, notificationIds);
        }
        /// <summary>
        /// Idempotent send. The client supplies the message id, so an offline outbox can safely retry.
        /// </summary>
        [Authorize]
        [HttpPost("send")]
        public async Task<OASISResult<Message>> Send([FromBody] MessageSendRequest request)
        {
            if (request == null)
                return new OASISResult<Message> { IsError = true, Message = "The request body is required." };
            return await MessagingManager.Instance.SendMessageAsync(Avatar.Id, request);
        }

        /// <summary>
        /// Marks messages addressed to the current avatar as delivered.
        /// </summary>
        [Authorize]
        [HttpPost("mark-messages-delivered")]
        public async Task<OASISResult<bool>> MarkMessagesDelivered([FromBody] List<Guid> messageIds)
        {
            if (messageIds == null)
                return new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid JSON array of message IDs." };
            return await MessagingManager.Instance.MarkMessagesAsDeliveredAsync(Avatar.Id, messageIds);
        }

        /// <summary>
        /// Cursor sync: messages received after <paramref name="since"/> (UTC), oldest first.
        /// Pass the returned nextCursor back as since while hasMore is true.
        /// </summary>
        [Authorize]
        [HttpGet("sync")]
        public async Task<OASISResult<MessageSyncPage>> Sync([FromQuery] DateTime? since = null, [FromQuery] int limit = 100)
        {
            return await MessagingManager.Instance.GetMessagesSinceAsync(Avatar.Id, since, limit);
        }

        /// <summary>
        /// Conversation list for the current avatar, most recently active first.
        /// </summary>
        [Authorize]
        [HttpGet("conversations")]
        public async Task<OASISResult<List<ConversationSummary>>> Conversations()
        {
            return await MessagingManager.Instance.GetConversationsAsync(Avatar.Id);
        }
        /// <summary>
        /// Issues a single-use, 30-second ticket for connecting to <c>/ws/messaging?ticket=...</c>.
        /// </summary>
        [Authorize]
        [HttpPost("realtime-ticket")]
        public OASISResult<string> RealtimeTicket()
        {
            MessagingRealtimeHub.EnsureSubscribed(MessagingManager.Instance);
            return new OASISResult<string> { Result = MessagingRealtimeHub.IssueTicket(Avatar.Id), Message = "Connect to /ws/messaging?ticket=<value> within 30 seconds." };
        }

        /// <summary>
        /// Which of the given avatars currently have a live connection to this server instance.
        /// </summary>
        [Authorize]
        [HttpGet("presence")]
        public OASISResult<Dictionary<Guid, bool>> Presence([FromQuery] List<Guid> avatarIds)
        {
            if (avatarIds == null || avatarIds.Count == 0 || avatarIds.Count > 200)
                return new OASISResult<Dictionary<Guid, bool>> { IsError = true, Message = "Provide between 1 and 200 avatar IDs." };
            return new OASISResult<Dictionary<Guid, bool>> { Result = avatarIds.Distinct().ToDictionary(id => id, MessagingRealtimeHub.IsOnline) };
        }

        /// <summary>Creates a group conversation. The caller becomes its creator and a member.</summary>
        [Authorize]
        [HttpPost("groups")]
        public async Task<OASISResult<ConversationInfo>> CreateGroup([FromBody] CreateGroupRequest request)
        {
            if (request == null)
                return new OASISResult<ConversationInfo> { IsError = true, Message = "The request body is required." };
            return await MessagingManager.Instance.CreateGroupAsync(Avatar.Id, request.Title, request.ParticipantIds);
        }

        [Authorize]
        [HttpGet("groups")]
        public async Task<OASISResult<List<ConversationInfo>>> Groups() =>
            await MessagingManager.Instance.GetGroupsAsync(Avatar.Id);

        [Authorize]
        [HttpPost("groups/{conversationId}/members")]
        public async Task<OASISResult<ConversationInfo>> AddGroupMembers(Guid conversationId, [FromBody] List<Guid> avatarIds)
        {
            if (avatarIds == null)
                return new OASISResult<ConversationInfo> { IsError = true, Message = "The request body is required. Please provide a JSON array of avatar IDs." };
            return await MessagingManager.Instance.AddGroupMembersAsync(Avatar.Id, conversationId, avatarIds);
        }

        [Authorize]
        [HttpPost("groups/{conversationId}/leave")]
        public async Task<OASISResult<bool>> LeaveGroup(Guid conversationId) =>
            await MessagingManager.Instance.LeaveGroupAsync(Avatar.Id, conversationId);

        [Authorize]
        [HttpGet("groups/{conversationId}/messages")]
        public async Task<OASISResult<List<Message>>> GroupMessages(Guid conversationId, [FromQuery] int limit = 50, [FromQuery] int offset = 0) =>
            await MessagingManager.Instance.GetGroupMessagesAsync(Avatar.Id, conversationId, limit, offset);
    }
}