using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Telegcrm.Models
{
    /// <summary>
    /// Represents a Telegram conversation (chat) with CRM metadata
    /// </summary>
    public class TelegramConversation
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }

        /// <summary>
        /// Telegram chat ID (can be user ID for private chats or group ID)
        /// </summary>
        [BsonElement("telegramChatId")]
        public long TelegramChatId { get; set; }

        /// <summary>
        /// Type of chat: "private", "group", "supergroup", "channel"
        /// </summary>
        [BsonElement("chatType")]
        public string ChatType { get; set; }

        /// <summary>
        /// OASIS Avatar ID of the CRM user (your friend)
        /// </summary>
        [BsonElement("oasisAvatarId")]
        [BsonRepresentation(BsonType.String)]
        public Guid OasisAvatarId { get; set; }

        /// <summary>
        /// Telegram ID of the contact (for private chats)
        /// </summary>
        [BsonElement("contactTelegramId")]
        public long? ContactTelegramId { get; set; }

        /// <summary>
        /// Display name of the contact
        /// </summary>
        [BsonElement("contactName")]
        public string ContactName { get; set; }

        /// <summary>
        /// Telegram username of the contact
        /// </summary>
        [BsonElement("contactUsername")]
        public string ContactUsername { get; set; }

        /// <summary>
        /// Priority level: "low", "medium", "high", "urgent"
        /// </summary>
        [BsonElement("priority")]
        public string Priority { get; set; } = "medium";

        /// <summary>
        /// Tags for categorization
        /// </summary>
        [BsonElement("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Status: "active", "archived", "waiting"
        /// </summary>
        [BsonElement("status")]
        public string Status { get; set; } = "active";

        /// <summary>
        /// Timestamp of last message in conversation
        /// </summary>
        [BsonElement("lastMessageAt")]
        public DateTime? LastMessageAt { get; set; }

        /// <summary>
        /// Timestamp when conversation was last read
        /// </summary>
        [BsonElement("lastReadAt")]
        public DateTime? LastReadAt { get; set; }

        /// <summary>
        /// Number of unread messages
        /// </summary>
        [BsonElement("unreadCount")]
        public int UnreadCount { get; set; } = 0;

        /// <summary>
        /// AI-generated or manual summary of conversation
        /// </summary>
        [BsonElement("summary")]
        public string Summary { get; set; }

        /// <summary>
        /// Custom fields for additional metadata
        /// </summary>
        [BsonElement("customFields")]
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// When conversation was first created
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        public TelegramConversation()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

