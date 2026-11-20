using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Chat and messaging endpoints for real-time communication features.
    /// Provides chat session management, messaging, and communication capabilities within the OASIS ecosystem.
    /// </summary>
    [ApiController]
    [Route("api/chat")]
    public class ChatController : OASISControllerBase
    {
        /// <summary>
        /// Starts a persisted chat session for the authenticated participant.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("start-new-chat-session")]
        public async Task<OASISResult<string>> StartNewChatSession([FromBody] List<Guid> participantIds, [FromQuery] string sessionName = null)
        {
            if (participantIds == null || participantIds.Count == 0)
                return new OASISResult<string> { IsError = true, Message = "The request body is required. Please provide a valid JSON array of participant IDs (at least one)." };
            if (!participantIds.Contains(Avatar.Id))
                return new OASISResult<string> { IsError = true, ErrorCode = "CHAT_SESSION_FORBIDDEN", Message = "The authenticated avatar must be a chat participant." };
            // ChatManager attributes the session write to its first participant.
            participantIds.Remove(Avatar.Id);
            participantIds.Insert(0, Avatar.Id);
            return await ChatManager.Instance.StartNewChatSessionAsync(participantIds, sessionName);
        }

        /// <summary>
        /// Send a message in a chat session
        /// </summary>
        /// <param name="sessionId">Chat session ID</param>
        /// <param name="message">Message content</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("send-message/{sessionId}")]
        public async Task<OASISResult<string>> SendMessage(string sessionId, [FromBody] string message)
        {
            if (message == null)
                return new OASISResult<string> { IsError = true, Message = "The request body is required. Please provide the message content." };
            // Use ChatManager for business logic
            return await ChatManager.Instance.SendMessageAsync(sessionId, Avatar.Id, message);
        }

        /// <summary>
        /// Get chat session history
        /// </summary>
        /// <param name="sessionId">Chat session ID</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("history/{sessionId}")]
        public async Task<OASISResult<List<ChatMessage>>> GetChatHistory(string sessionId, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
        {
            return await ChatManager.Instance.GetChatHistoryAsync(sessionId, Avatar.Id, limit, offset);
        }

        /// <summary>Ends a persisted session for an authenticated participant.</summary>
        [Authorize]
        [HttpPost("end-chat-session/{sessionId}")]
        public Task<OASISResult<bool>> EndChatSession(string sessionId) =>
            ChatManager.Instance.EndChatSessionAsync(sessionId, Avatar.Id);
    }
}
