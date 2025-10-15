using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Manages chat sessions, messages, and real-time communication
    /// </summary>
    public class ChatManager : OASISManager
    {
        private static ChatManager _instance = null;
        private readonly Dictionary<string, ChatSession> _activeSessions = new Dictionary<string, ChatSession>();
        private readonly Dictionary<string, List<ChatMessage>> _chatHistory = new Dictionary<string, List<ChatMessage>>();

        public static ChatManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ChatManager(ProviderManager.Instance.CurrentStorageProvider);

                return _instance;
            }
        }

        public ChatManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
        }

        /// <summary>
        /// Starts a new chat session
        /// </summary>
        /// <param name="participantIds">List of participant avatar IDs</param>
        /// <param name="sessionName">Optional session name</param>
        /// <returns>Session ID and details</returns>
        public async Task<OASISResult<string>> StartNewChatSessionAsync(List<Guid> participantIds, string sessionName = null)
        {
            var result = new OASISResult<string>();
            
            try
            {
                // Validate participants
                if (participantIds == null || participantIds.Count < 2)
                {
                    result.IsError = true;
                    result.Message = "At least 2 participants are required for a chat session";
                    return result;
                }

                // Generate unique session ID
                var sessionId = Guid.NewGuid().ToString();
                
                // Create chat session
                var session = new ChatSession
                {
                    Id = sessionId,
                    Name = sessionName ?? $"Chat Session {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                    ParticipantIds = participantIds,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                // Store session
                _activeSessions[sessionId] = session;
                _chatHistory[sessionId] = new List<ChatMessage>();

                // Initialize session in storage
                var holon = new Holon
                {
                    Id = Guid.NewGuid(),
                    Name = session.Name,
                    Description = $"Chat session with {participantIds.Count} participants",
                    CreatedDate = DateTime.UtcNow,
                    MetaData = new Dictionary<string, object>
                    {
                        { "SessionId", sessionId },
                        { "ParticipantIds", string.Join(",", participantIds) },
                        { "IsActive", true }
                    }
                };

                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to save chat session: {saveResult.Message}";
                    return result;
                }

                result.Result = sessionId;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error starting chat session: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Sends a message in a chat session
        /// </summary>
        /// <param name="sessionId">Chat session ID</param>
        /// <param name="senderId">Avatar ID of the sender</param>
        /// <param name="message">Message content</param>
        /// <param name="messageType">Type of message (text, image, file, etc.)</param>
        /// <returns>Message ID and delivery status</returns>
        public async Task<OASISResult<string>> SendMessageAsync(string sessionId, Guid senderId, string message, MessageType messageType = MessageType.Text)
        {
            var result = new OASISResult<string>();
            
            try
            {
                // Validate session exists
                if (!_activeSessions.ContainsKey(sessionId))
                {
                    result.IsError = true;
                    result.Message = "Chat session not found";
                    return result;
                }

                var session = _activeSessions[sessionId];
                
                // Validate sender is participant
                if (!session.ParticipantIds.Contains(senderId))
                {
                    result.IsError = true;
                    result.Message = "Sender is not a participant in this session";
                    return result;
                }

                // Create message
                var messageId = Guid.NewGuid().ToString();
                var chatMessage = new ChatMessage
                {
                    Id = messageId,
                    SessionId = sessionId,
                    SenderId = senderId,
                    Content = message,
                    MessageType = messageType,
                    Timestamp = DateTime.UtcNow,
                    IsDelivered = true
                };

                // Store message
                _chatHistory[sessionId].Add(chatMessage);

                // Save message to storage
                var holon = new Holon
                {
                    Id = Guid.NewGuid(),
                    Name = $"Message {messageId}",
                    Description = message,
                    CreatedDate = DateTime.UtcNow,
                    MetaData = new Dictionary<string, object>
                    {
                        { "MessageId", messageId },
                        { "SessionId", sessionId },
                        { "SenderId", senderId.ToString() },
                        { "MessageType", messageType.ToString() },
                        { "IsDelivered", true }
                    }
                };

                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to save message: {saveResult.Message}";
                    return result;
                }

                result.Result = messageId;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error sending message: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Retrieves chat history for a session
        /// </summary>
        /// <param name="sessionId">Chat session ID</param>
        /// <param name="limit">Maximum number of messages to retrieve</param>
        /// <param name="offset">Number of messages to skip</param>
        /// <returns>List of chat messages</returns>
        public async Task<OASISResult<List<ChatMessage>>> GetChatHistoryAsync(string sessionId, int limit = 50, int offset = 0)
        {
            var result = new OASISResult<List<ChatMessage>>();
            
            try
            {
                // Validate session exists
                if (!_chatHistory.ContainsKey(sessionId))
                {
                    result.IsError = true;
                    result.Message = "Chat session not found";
                    return result;
                }

                var messages = _chatHistory[sessionId]
                    .OrderByDescending(m => m.Timestamp)
                    .Skip(offset)
                    .Take(limit)
                    .OrderBy(m => m.Timestamp)
                    .ToList();

                result.Result = messages;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving chat history: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Gets active chat sessions for a user
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>List of active chat sessions</returns>
        public async Task<OASISResult<List<ChatSession>>> GetActiveSessionsAsync(Guid avatarId)
        {
            var result = new OASISResult<List<ChatSession>>();
            
            try
            {
                var userSessions = _activeSessions.Values
                    .Where(s => s.ParticipantIds.Contains(avatarId) && s.IsActive)
                    .ToList();

                result.Result = userSessions;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving active sessions: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Ends a chat session
        /// </summary>
        /// <param name="sessionId">Chat session ID</param>
        /// <param name="endedById">Avatar ID of the user ending the session</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> EndChatSessionAsync(string sessionId, Guid endedById)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                if (!_activeSessions.ContainsKey(sessionId))
                {
                    result.IsError = true;
                    result.Message = "Chat session not found";
                    return result;
                }

                var session = _activeSessions[sessionId];
                
                // Validate user is participant
                if (!session.ParticipantIds.Contains(endedById))
                {
                    result.IsError = true;
                    result.Message = "User is not a participant in this session";
                    return result;
                }

                // Mark session as inactive
                session.IsActive = false;
                session.EndedDate = DateTime.UtcNow;
                session.EndedById = endedById;

                result.Result = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error ending chat session: {ex.Message}";
            }

            return result;
        }
    }

    /// <summary>
    /// Represents a chat session
    /// </summary>
    public class ChatSession
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Guid> ParticipantIds { get; set; } = new List<Guid>();
        public DateTime CreatedDate { get; set; }
        public DateTime? EndedDate { get; set; }
        public Guid? EndedById { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Represents a chat message
    /// </summary>
    public class ChatMessage
    {
        public string Id { get; set; }
        public string SessionId { get; set; }
        public Guid SenderId { get; set; }
        public string Content { get; set; }
        public MessageType MessageType { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsDelivered { get; set; }
        public bool IsRead { get; set; }
    }

    /// <summary>
    /// Types of chat messages
    /// </summary>
    public enum MessageType
    {
        Text,
        Image,
        File,
        Audio,
        Video,
        System
    }
}
