using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models
{
    /// <summary>
    /// Represents the mapping between a Telegram user and an OASIS Avatar
    /// </summary>
    public class TelegramAvatar
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }

        /// <summary>
        /// Telegram user ID (unique identifier from Telegram)
        /// </summary>
        [BsonElement("telegramId")]
        public long TelegramId { get; set; }

        /// <summary>
        /// Telegram username (without @)
        /// </summary>
        [BsonElement("telegramUsername")]
        public string TelegramUsername { get; set; }

        /// <summary>
        /// Telegram first name
        /// </summary>
        [BsonElement("firstName")]
        public string FirstName { get; set; }

        /// <summary>
        /// Telegram last name
        /// </summary>
        [BsonElement("lastName")]
        public string LastName { get; set; }

        /// <summary>
        /// OASIS Avatar ID this Telegram account is linked to
        /// </summary>
        [BsonElement("oasisAvatarId")]
        [BsonRepresentation(BsonType.String)]
        public Guid OasisAvatarId { get; set; }

        /// <summary>
        /// Date when the Telegram account was linked to OASIS
        /// </summary>
        [BsonElement("linkedAt")]
        public DateTime LinkedAt { get; set; }

        /// <summary>
        /// Whether this account is active
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Last time the user interacted with the bot
        /// </summary>
        [BsonElement("lastInteractionAt")]
        public DateTime LastInteractionAt { get; set; }

        /// <summary>
        /// Groups this user is a member of
        /// </summary>
        [BsonElement("groupIds")]
        public List<string> GroupIds { get; set; }

        public TelegramAvatar()
        {
            Id = Guid.NewGuid();
            LinkedAt = DateTime.UtcNow;
            IsActive = true;
            LastInteractionAt = DateTime.UtcNow;
            GroupIds = new List<string>();
        }
    }
}

