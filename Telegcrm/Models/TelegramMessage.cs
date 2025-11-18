using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Telegcrm.Models
{
    /// <summary>
    /// Represents a single Telegram message stored in CRM
    /// </summary>
    public class TelegramMessage
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }

        /// <summary>
        /// Reference to the conversation this message belongs to
        /// </summary>
        [BsonElement("conversationId")]
        public string ConversationId { get; set; }

        /// <summary>
        /// Original Telegram message ID
        /// </summary>
        [BsonElement("telegramMessageId")]
        public int TelegramMessageId { get; set; }

        /// <summary>
        /// Telegram ID of the sender
        /// </summary>
        [BsonElement("fromTelegramId")]
        public long FromTelegramId { get; set; }

        /// <summary>
        /// Display name of the sender
        /// </summary>
        [BsonElement("fromName")]
        public string FromName { get; set; }

        /// <summary>
        /// Message content (text)
        /// </summary>
        [BsonElement("content")]
        public string Content { get; set; }

        /// <summary>
        /// Type of message: "text", "photo", "document", "video", "audio", "voice", etc.
        /// </summary>
        [BsonElement("messageType")]
        public string MessageType { get; set; } = "text";

        /// <summary>
        /// URLs to media files (photos, documents, etc.)
        /// </summary>
        [BsonElement("mediaUrls")]
        public List<string> MediaUrls { get; set; } = new List<string>();

        /// <summary>
        /// When message was sent
        /// </summary>
        [BsonElement("sentAt")]
        public DateTime SentAt { get; set; }

        /// <summary>
        /// Whether message has been read
        /// </summary>
        [BsonElement("isRead")]
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// Whether message was sent by the CRM user (your friend)
        /// </summary>
        [BsonElement("isFromMe")]
        public bool IsFromMe { get; set; } = false;

        /// <summary>
        /// Sentiment analysis result: "positive", "neutral", "negative" (optional AI feature)
        /// </summary>
        [BsonElement("sentiment")]
        public string Sentiment { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        [BsonElement("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public TelegramMessage()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}

