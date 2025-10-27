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
    /// Manages messaging between avatars, including direct messages, notifications, and communication
    /// </summary>
    public partial class MessagingManager : OASISManager
    {
        private static MessagingManager _instance;
        private readonly Dictionary<Guid, List<Message>> _messages;
        private readonly Dictionary<Guid, List<Notification>> _notifications;

        public static MessagingManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MessagingManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public MessagingManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            _messages = new Dictionary<Guid, List<Message>>();
            _notifications = new Dictionary<Guid, List<Notification>>();
        }

        /// <summary>
        /// Send a message to a specific avatar
        /// </summary>
        public async Task<OASISResult<bool>> SendMessageToAvatarAsync(Guid fromAvatarId, Guid toAvatarId, string content, MessagingType messageType = MessagingType.Direct)
        {
            var result = new OASISResult<bool>();
            try
            {
                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    FromAvatarId = fromAvatarId,
                    ToAvatarId = toAvatarId,
                    Content = content,
                    MessageType = messageType,
                    Timestamp = DateTime.UtcNow,
                    IsRead = false
                };

                // Store message for both sender and recipient
                if (!_messages.ContainsKey(fromAvatarId))
                    _messages[fromAvatarId] = new List<Message>();
                if (!_messages.ContainsKey(toAvatarId))
                    _messages[toAvatarId] = new List<Message>();

                _messages[fromAvatarId].Add(message);
                _messages[toAvatarId].Add(message);

                // Create notification for recipient
                await CreateNotificationAsync(toAvatarId, "New Message", $"You have a new message from {fromAvatarId}", NotificationType.Message);

                // Update messaging statistics in settings system whenever messages are sent
                try
                {
                    var messagingStats = new Dictionary<string, object>
                    {
                        ["totalMessagesSent"] = _messages[fromAvatarId].Count(m => m.FromAvatarId == fromAvatarId),
                        ["totalMessagesReceived"] = _messages[fromAvatarId].Count(m => m.ToAvatarId == fromAvatarId),
                        ["lastMessageSent"] = DateTime.UtcNow,
                        ["lastMessageType"] = messageType.ToString()
                    };
                    await HolonManager.Instance.SaveSettingsAsync(fromAvatarId, "messaging", messagingStats);
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the main operation
                    Console.WriteLine($"Warning: Failed to save messaging statistics: {ex.Message}");
                }

                result.Result = true;
                result.Message = "Message sent successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error sending message: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        /// <summary>
        /// Get messages for an avatar
        /// </summary>
        public async Task<OASISResult<List<Message>>> GetMessagesAsync(Guid avatarId, int limit = 50, int offset = 0)
        {
            var result = new OASISResult<List<Message>>();
            try
            {
                if (!_messages.ContainsKey(avatarId))
                {
                    result.Result = new List<Message>();
                    result.Message = "No messages found";
                    return result;
                }

                var messages = _messages[avatarId]
                    .OrderByDescending(m => m.Timestamp)
                    .Skip(offset)
                    .Take(limit)
                    .ToList();

                result.Result = messages;
                result.Message = "Messages retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving messages: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Get conversation between two avatars
        /// </summary>
        public async Task<OASISResult<List<Message>>> GetConversationAsync(Guid avatarId1, Guid avatarId2, int limit = 50, int offset = 0)
        {
            var result = new OASISResult<List<Message>>();
            try
            {
                var conversationMessages = new List<Message>();

                if (_messages.ContainsKey(avatarId1))
                {
                    conversationMessages.AddRange(_messages[avatarId1]
                        .Where(m => (m.FromAvatarId == avatarId1 && m.ToAvatarId == avatarId2) ||
                                   (m.FromAvatarId == avatarId2 && m.ToAvatarId == avatarId1)));
                }

                var orderedMessages = conversationMessages
                    .OrderBy(m => m.Timestamp)
                    .Skip(offset)
                    .Take(limit)
                    .ToList();

                result.Result = orderedMessages;
                result.Message = "Conversation retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving conversation: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Mark messages as read
        /// </summary>
        public async Task<OASISResult<bool>> MarkMessagesAsReadAsync(Guid avatarId, List<Guid> messageIds)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_messages.ContainsKey(avatarId))
                {
                    foreach (var messageId in messageIds)
                    {
                        var message = _messages[avatarId].FirstOrDefault(m => m.Id == messageId);
                        if (message != null)
                        {
                            message.IsRead = true;
                            message.ReadAt = DateTime.UtcNow;
                        }
                    }
                }

                result.Result = true;
                result.Message = "Messages marked as read";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error marking messages as read: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Get notifications for an avatar
        /// </summary>
        public async Task<OASISResult<List<Notification>>> GetNotificationsAsync(Guid avatarId, int limit = 20, int offset = 0)
        {
            var result = new OASISResult<List<Notification>>();
            try
            {
                if (!_notifications.ContainsKey(avatarId))
                {
                    result.Result = new List<Notification>();
                    result.Message = "No notifications found";
                    return result;
                }

                var notifications = _notifications[avatarId]
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip(offset)
                    .Take(limit)
                    .ToList();

                result.Result = notifications;
                result.Message = "Notifications retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving notifications: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Mark notifications as read
        /// </summary>
        public async Task<OASISResult<bool>> MarkNotificationsAsReadAsync(Guid avatarId, List<Guid> notificationIds)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_notifications.ContainsKey(avatarId))
                {
                    foreach (var notificationId in notificationIds)
                    {
                        var notification = _notifications[avatarId].FirstOrDefault(n => n.Id == notificationId);
                        if (notification != null)
                        {
                            notification.IsRead = true;
                            notification.ReadAt = DateTime.UtcNow;
                        }
                    }
                }

                result.Result = true;
                result.Message = "Notifications marked as read";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error marking notifications as read: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        private async Task CreateNotificationAsync(Guid avatarId, string title, string content, NotificationType type)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                AvatarId = avatarId,
                Title = title,
                Content = content,
                Type = type,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            if (!_notifications.ContainsKey(avatarId))
                _notifications[avatarId] = new List<Notification>();

            _notifications[avatarId].Add(notification);
            await Task.CompletedTask;
        }
    }

    public class Message
    {
        public Guid Id { get; set; }
        public Guid FromAvatarId { get; set; }
        public Guid ToAvatarId { get; set; }
        public string Content { get; set; }
        public MessagingType MessageType { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class Notification
    {
        public Guid Id { get; set; }
        public Guid AvatarId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public NotificationType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public enum MessagingType
    {
        Direct,
        Group,
        System,
        Notification
    }

    public enum NotificationType
    {
        Message,
        System,
        Achievement,
        Warning,
        Info
    }
}
