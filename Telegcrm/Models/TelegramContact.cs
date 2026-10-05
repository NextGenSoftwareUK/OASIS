using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Telegcrm.Models
{
    /// <summary>
    /// Represents a Telegram contact with business information
    /// </summary>
    public class TelegramContact
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }

        /// <summary>
        /// Telegram user ID
        /// </summary>
        [BsonElement("telegramId")]
        public long TelegramId { get; set; }

        /// <summary>
        /// First name
        /// </summary>
        [BsonElement("firstName")]
        public string FirstName { get; set; }

        /// <summary>
        /// Last name
        /// </summary>
        [BsonElement("lastName")]
        public string LastName { get; set; }

        /// <summary>
        /// Telegram username (without @)
        /// </summary>
        [BsonElement("username")]
        public string Username { get; set; }

        /// <summary>
        /// Phone number (if available)
        /// </summary>
        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Company name
        /// </summary>
        [BsonElement("company")]
        public string Company { get; set; }

        /// <summary>
        /// Job title
        /// </summary>
        [BsonElement("jobTitle")]
        public string JobTitle { get; set; }

        /// <summary>
        /// Email address
        /// </summary>
        [BsonElement("email")]
        public string Email { get; set; }

        /// <summary>
        /// Website
        /// </summary>
        [BsonElement("website")]
        public string Website { get; set; }

        /// <summary>
        /// Notes about the contact
        /// </summary>
        [BsonElement("notes")]
        public string Notes { get; set; }

        /// <summary>
        /// Category: "client", "prospect", "partner", "vendor", "other"
        /// </summary>
        [BsonElement("category")]
        public string Category { get; set; }

        /// <summary>
        /// Tags for organization
        /// </summary>
        [BsonElement("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Relationship status: "new", "active", "inactive"
        /// </summary>
        [BsonElement("relationshipStatus")]
        public string RelationshipStatus { get; set; } = "new";

        /// <summary>
        /// Last time contact was messaged
        /// </summary>
        [BsonElement("lastContactedAt")]
        public DateTime? LastContactedAt { get; set; }

        /// <summary>
        /// Total number of messages exchanged
        /// </summary>
        [BsonElement("totalMessages")]
        public int TotalMessages { get; set; } = 0;

        /// <summary>
        /// Linked OASIS Avatar ID (if contact has OASIS account)
        /// </summary>
        [BsonElement("linkedOasisAvatarId")]
        [BsonRepresentation(BsonType.String)]
        public Guid? LinkedOasisAvatarId { get; set; }

        /// <summary>
        /// When contact was created
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        public TelegramContact()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

