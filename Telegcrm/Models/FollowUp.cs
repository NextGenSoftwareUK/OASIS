using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Telegcrm.Models
{
    /// <summary>
    /// Represents a follow-up reminder for a conversation
    /// </summary>
    public class FollowUp
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }

        /// <summary>
        /// Conversation this follow-up is for
        /// </summary>
        [BsonElement("conversationId")]
        public string ConversationId { get; set; }

        /// <summary>
        /// OASIS Avatar ID of the user who created this follow-up
        /// </summary>
        [BsonElement("createdByOasisAvatarId")]
        [BsonRepresentation(BsonType.String)]
        public Guid CreatedByOasisAvatarId { get; set; }

        /// <summary>
        /// Title of the follow-up
        /// </summary>
        [BsonElement("title")]
        public string Title { get; set; }

        /// <summary>
        /// Description/details
        /// </summary>
        [BsonElement("description")]
        public string Description { get; set; }

        /// <summary>
        /// When the follow-up is due
        /// </summary>
        [BsonElement("dueDate")]
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Priority: "low", "medium", "high", "urgent"
        /// </summary>
        [BsonElement("priority")]
        public string Priority { get; set; } = "medium";

        /// <summary>
        /// Whether follow-up has been completed
        /// </summary>
        [BsonElement("isCompleted")]
        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// When follow-up was completed
        /// </summary>
        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// When follow-up was created
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        public FollowUp()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
        }
    }
}

