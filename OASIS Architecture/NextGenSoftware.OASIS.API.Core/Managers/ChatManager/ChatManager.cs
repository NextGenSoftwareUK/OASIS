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

        #region Competition Tracking

        private async Task UpdateChatCompetitionScoresAsync(Guid avatarId, int messageLength)
        {
            try
            {
                var competitionManager = CompetitionManager.Instance;
                
                // Calculate score based on message length and activity
                var score = CalculateChatScore(messageLength);
                
                // Update chat session competition scores
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.ChatSessions, SeasonType.Daily, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.ChatSessions, SeasonType.Weekly, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.ChatSessions, SeasonType.Monthly, score);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating chat competition scores: {ex.Message}");
            }
        }

        private long CalculateChatScore(int messageLength)
        {
            // Base score for sending a message
            var baseScore = 1;
            
            // Bonus for longer messages (encouraging meaningful communication)
            var lengthBonus = Math.Min(messageLength / 10, 10); // Max 10 bonus points
            
            return baseScore + lengthBonus;
        }

        public async Task<OASISResult<Dictionary<string, object>>> GetChatStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var totalMessages = 0;
                var totalSessions = 0;
                var totalCharacters = 0;
                var averageMessageLength = 0.0;

                // Calculate stats from chat history
                foreach (var session in _chatHistory.Values)
                {
                    var userMessages = session.Where(m => m.SenderId == avatarId).ToList();
                    totalMessages += userMessages.Count;
                    totalCharacters += userMessages.Sum(m => m.Content?.Length ?? 0);
                }

                totalSessions = _activeSessions.Values.Count(s => s.ParticipantIds.Contains(avatarId));
                averageMessageLength = totalMessages > 0 ? (double)totalCharacters / totalMessages : 0;

                var stats = new Dictionary<string, object>
                {
                    ["totalMessages"] = totalMessages,
                    ["totalSessions"] = totalSessions,
                    ["totalCharacters"] = totalCharacters,
                    ["averageMessageLength"] = Math.Round(averageMessageLength, 2),
                    ["totalScore"] = totalMessages + (totalCharacters / 10), // Simple scoring
                    ["mostActiveDay"] = GetMostActiveDay(avatarId),
                    ["longestMessage"] = GetLongestMessage(avatarId)
                };

                result.Result = stats;
                result.Message = "Chat statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving chat statistics: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        private string GetMostActiveDay(Guid avatarId)
        {
            var dayCounts = new Dictionary<string, int>();
            
            foreach (var session in _chatHistory.Values)
            {
                var userMessages = session.Where(m => m.SenderId == avatarId);
                foreach (var message in userMessages)
                {
                    var day = message.Timestamp.ToString("dddd");
                    dayCounts[day] = dayCounts.GetValueOrDefault(day, 0) + 1;
                }
            }

            return dayCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "Unknown";
        }

        private string GetLongestMessage(Guid avatarId)
        {
            var longestMessage = "";
            var maxLength = 0;

            foreach (var session in _chatHistory.Values)
            {
                var userMessages = session.Where(m => m.SenderId == avatarId);
                foreach (var message in userMessages)
                {
                    if (message.Content?.Length > maxLength)
                    {
                        maxLength = message.Content.Length;
                        longestMessage = message.Content;
                    }
                }
            }

            return longestMessage.Length > 100 ? longestMessage.Substring(0, 100) + "..." : longestMessage;
        }

        #endregion
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
