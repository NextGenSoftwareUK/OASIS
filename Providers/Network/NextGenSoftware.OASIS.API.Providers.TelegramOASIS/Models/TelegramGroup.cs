using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models
{
    /// <summary>
    /// Represents an accountability group on Telegram
    /// </summary>
    public class TelegramGroup
    {
        [BsonId]
        [BsonElement("_id")]
        public string Id { get; set; }

        /// <summary>
        /// Human-readable group name
        /// </summary>
        [BsonElement("name")]
        public string Name { get; set; }

        /// <summary>
        /// Group description
        /// </summary>
        [BsonElement("description")]
        public string Description { get; set; }

        /// <summary>
        /// Group type (public, private, etc.)
        /// </summary>
        [BsonElement("type")]
        public string Type { get; set; }

        /// <summary>
        /// OASIS Avatar ID of the group creator
        /// </summary>
        [BsonElement("createdBy")]
        [BsonRepresentation(BsonType.String)]
        public Guid CreatedBy { get; set; }

        /// <summary>
        /// Telegram chat ID for this group
        /// </summary>
        [BsonElement("telegramChatId")]
        public long TelegramChatId { get; set; }

        /// <summary>
        /// List of admin Telegram user IDs
        /// </summary>
        [BsonElement("adminIds")]
        public List<long> AdminIds { get; set; }

        /// <summary>
        /// List of member Telegram user IDs
        /// </summary>
        [BsonElement("memberIds")]
        public List<long> MemberIds { get; set; }

        /// <summary>
        /// Group rules and reward configuration
        /// </summary>
        [BsonElement("rules")]
        public GroupRules Rules { get; set; }

        /// <summary>
        /// When the group was created
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Whether the group is active
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; }

        public TelegramGroup()
        {
            Id = Guid.NewGuid().ToString();
            Type = "public";
            AdminIds = new List<long>();
            MemberIds = new List<long>();
            Rules = new GroupRules();
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }
    }

    /// <summary>
    /// Rules and rewards for a Telegram group
    /// </summary>
    public class GroupRules
    {
        /// <summary>
        /// Karma points awarded per check-in
        /// </summary>
        [BsonElement("karmaPerCheckin")]
        public int KarmaPerCheckin { get; set; }

        /// <summary>
        /// Solana tokens awarded per milestone completion
        /// </summary>
        [BsonElement("tokenPerMilestone")]
        public decimal TokenPerMilestone { get; set; }

        /// <summary>
        /// Required check-ins per week to stay in good standing
        /// </summary>
        [BsonElement("requiredCheckinsPerWeek")]
        public int RequiredCheckinsPerWeek { get; set; }

        public GroupRules()
        {
            KarmaPerCheckin = 10;
            TokenPerMilestone = 1.0m;
            RequiredCheckinsPerWeek = 3;
        }
    }
}





