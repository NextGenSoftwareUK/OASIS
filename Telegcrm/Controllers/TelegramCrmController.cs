using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telegcrm.Models;
using Telegcrm.Services;

namespace Telegcrm.Controllers
{
    [ApiController]
    [Route("api/telegram-crm")]
    // [Authorize] // Uncomment when authentication is set up
    public class TelegramCrmController : ControllerBase
    {
        private readonly TelegramCrmService _crmService;

        public TelegramCrmController(TelegramCrmService crmService)
        {
            _crmService = crmService;
        }

        /// <summary>
        /// Get all conversations for a user
        /// </summary>
        [HttpGet("conversations")]
        public async Task<ActionResult<List<TelegramConversation>>> GetConversations(
            [FromQuery] Guid oasisAvatarId,
            [FromQuery] string status = null)
        {
            try
            {
                var conversations = await _crmService.GetConversationsAsync(oasisAvatarId, status);
                return Ok(conversations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific conversation by ID
        /// </summary>
        [HttpGet("conversations/{conversationId}")]
        public async Task<ActionResult<TelegramConversation>> GetConversation(string conversationId)
        {
            try
            {
                var conversations = await _crmService.GetConversationsAsync(Guid.Empty);
                var conversation = conversations.Find(c => c.Id == conversationId);
                
                if (conversation == null)
                {
                    return NotFound();
                }

                return Ok(conversation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get messages for a conversation
        /// </summary>
        [HttpGet("conversations/{conversationId}/messages")]
        public async Task<ActionResult<List<TelegramMessage>>> GetMessages(
            string conversationId,
            [FromQuery] int limit = 100)
        {
            try
            {
                var messages = await _crmService.GetMessagesAsync(conversationId, limit);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Mark conversation as read
        /// </summary>
        [HttpPost("conversations/{conversationId}/read")]
        public async Task<ActionResult> MarkAsRead(string conversationId)
        {
            try
            {
                await _crmService.MarkConversationAsReadAsync(conversationId);
                return Ok(new { message = "Conversation marked as read" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Set conversation priority
        /// </summary>
        [HttpPost("conversations/{conversationId}/priority")]
        public async Task<ActionResult> SetPriority(
            string conversationId,
            [FromBody] SetPriorityRequest request)
        {
            try
            {
                await _crmService.SetConversationPriorityAsync(conversationId, request.Priority);
                return Ok(new { message = "Priority updated" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Add tag to conversation
        /// </summary>
        [HttpPost("conversations/{conversationId}/tags")]
        public async Task<ActionResult> AddTag(
            string conversationId,
            [FromBody] AddTagRequest request)
        {
            try
            {
                await _crmService.AddTagToConversationAsync(conversationId, request.Tag);
                return Ok(new { message = "Tag added" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Search conversations
        /// </summary>
        [HttpGet("conversations/search")]
        public async Task<ActionResult<List<TelegramConversation>>> SearchConversations(
            [FromQuery] Guid oasisAvatarId,
            [FromQuery] string keyword)
        {
            try
            {
                var conversations = await _crmService.SearchConversationsAsync(oasisAvatarId, keyword);
                return Ok(conversations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all contacts
        /// </summary>
        [HttpGet("contacts")]
        public async Task<ActionResult<List<TelegramContact>>> GetContacts()
        {
            try
            {
                var contacts = await _crmService.GetContactsAsync();
                return Ok(contacts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Create a follow-up reminder
        /// </summary>
        [HttpPost("followups")]
        public async Task<ActionResult<FollowUp>> CreateFollowUp([FromBody] CreateFollowUpRequest request)
        {
            try
            {
                var followUp = await _crmService.CreateFollowUpAsync(
                    request.ConversationId,
                    request.OasisAvatarId,
                    request.Title,
                    request.Description,
                    request.DueDate,
                    request.Priority ?? "medium"
                );

                return Ok(followUp);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get pending follow-ups
        /// </summary>
        [HttpGet("followups/pending")]
        public async Task<ActionResult<List<FollowUp>>> GetPendingFollowUps(
            [FromQuery] Guid oasisAvatarId)
        {
            try
            {
                var followUps = await _crmService.GetPendingFollowUpsAsync(oasisAvatarId);
                return Ok(followUps);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    // Request DTOs
    public class SetPriorityRequest
    {
        public string Priority { get; set; }
    }

    public class AddTagRequest
    {
        public string Tag { get; set; }
    }

    public class CreateFollowUpRequest
    {
        public string ConversationId { get; set; }
        public Guid OasisAvatarId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public string Priority { get; set; }
    }
}

