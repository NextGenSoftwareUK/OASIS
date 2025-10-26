//using Microsoft.AspNetCore.Mvc;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Managers;
//using NextGenSoftware.OASIS.Common;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
//{
//    [ApiController]
//    [Route("api/messaging")]
//    public class MessagingController : STARControllerBase
//    {
//        public MessagingController() 
//        {
//        }

//        /// <summary>
//        /// Send's a message to the given avatar
//        /// </summary>
//        /// <param name="toAvatarId">ID of the avatar to send message to</param>
//        /// <param name="content">Message content</param>
//        /// <param name="messageType">Type of message</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("send-message-to-avatar/{toAvatarId}")]
//        public async Task<OASISResult<bool>> SendMessageToAvatar(Guid toAvatarId, [FromBody] string content, [FromQuery] MessagingType messageType = MessagingType.Direct)
//        {
//            // Use MessagingManager for business logic
//            return await MessagingManager.Instance.SendMessageToAvatarAsync(Avatar.Id, toAvatarId, content, messageType);
//        }

//        /// <summary>
//        /// Get messages for the current avatar
//        /// </summary>
//        /// <param name="limit">Maximum number of messages to return</param>
//        /// <param name="offset">Number of messages to skip</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpGet("messages")]
//        public async Task<OASISResult<List<Message>>> GetMessages([FromQuery] int limit = 50, [FromQuery] int offset = 0)
//        {
//            // Use MessagingManager for business logic
//            return await MessagingManager.Instance.GetMessagesAsync(Avatar.Id, limit, offset);
//        }

//        /// <summary>
//        /// Get conversation between current avatar and another avatar
//        /// </summary>
//        /// <param name="otherAvatarId">ID of the other avatar</param>
//        /// <param name="limit">Maximum number of messages to return</param>
//        /// <param name="offset">Number of messages to skip</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpGet("conversation/{otherAvatarId}")]
//        public async Task<OASISResult<List<Message>>> GetConversation(Guid otherAvatarId, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
//        {
//            // Use MessagingManager for business logic
//            return await MessagingManager.Instance.GetConversationAsync(Avatar.Id, otherAvatarId, limit, offset);
//        }

//        /// <summary>
//        /// Mark messages as read
//        /// </summary>
//        /// <param name="messageIds">List of message IDs to mark as read</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("mark-messages-read")]
//        public async Task<OASISResult<bool>> MarkMessagesAsRead([FromBody] List<Guid> messageIds)
//        {
//            // Use MessagingManager for business logic
//            return await MessagingManager.Instance.MarkMessagesAsReadAsync(Avatar.Id, messageIds);
//        }

//        /// <summary>
//        /// Get notifications for the current avatar
//        /// </summary>
//        /// <param name="limit">Maximum number of notifications to return</param>
//        /// <param name="offset">Number of notifications to skip</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpGet("notifications")]
//        public async Task<OASISResult<List<Notification>>> GetNotifications([FromQuery] int limit = 20, [FromQuery] int offset = 0)
//        {
//            // Use MessagingManager for business logic
//            return await MessagingManager.Instance.GetNotificationsAsync(Avatar.Id, limit, offset);
//        }

//        /// <summary>
//        /// Mark notifications as read
//        /// </summary>
//        /// <param name="notificationIds">List of notification IDs to mark as read</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("mark-notifications-read")]
//        public async Task<OASISResult<bool>> MarkNotificationsAsRead([FromBody] List<Guid> notificationIds)
//        {
//            // Use MessagingManager for business logic
//            return await MessagingManager.Instance.MarkNotificationsAsReadAsync(Avatar.Id, notificationIds);
//        }
//    }
//}
