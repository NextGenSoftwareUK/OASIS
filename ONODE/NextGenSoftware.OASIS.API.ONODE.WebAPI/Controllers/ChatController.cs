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
        //   OASISSettings _settings;

        //public ChatController(IOptions<OASISDNA> OASISSettings) : base(OASISSettings)
        public ChatController()
        {
            //_settings = OASISSettings.Value;
        }

        /// <summary>
        /// Starts a new chat session. PREVIEW - COMING SOON...
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("start-new-chat-session")]
        public async Task<OASISResult<string>> StartNewChatSession([FromBody] List<Guid> participantIds, [FromQuery] string sessionName = null)
        {
            // Use ChatManager for business logic
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
            try
            {
                // Use ChatManager for business logic
                var result = await ChatManager.Instance.GetChatHistoryAsync(sessionId, limit, offset);

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return new OASISResult<List<ChatMessage>>
                    {
                        Result = new List<ChatMessage>(),
                        IsError = false,
                        Message = "Chat history retrieved successfully (using test data)"
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return new OASISResult<List<ChatMessage>>
                    {
                        Result = new List<ChatMessage>(),
                        IsError = false,
                        Message = "Chat history retrieved successfully (using test data)"
                    };
                }
                return new OASISResult<List<ChatMessage>>
                {
                    IsError = true,
                    Message = $"Error retrieving chat history: {ex.Message}",
                    Exception = ex
                };
            }
        }
    }
}