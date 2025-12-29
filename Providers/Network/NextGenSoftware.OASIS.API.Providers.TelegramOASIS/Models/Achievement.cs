using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models
{
    /// <summary>
    /// Represents an achievement or goal within a Telegram accountability group
    /// </summary>
    public class Achievement
    {
        [BsonId]
        [BsonElement("_id")]
        public string Id { get; set; }

        /// <summary>
        /// Group this achievement belongs to
        /// </summary>
        [BsonElement("groupId")]
        public string GroupId { get; set; }

        /// <summary>
        /// OASIS Avatar ID of the user
        /// </summary>
        [BsonElement("userId")]
        [BsonRepresentation(BsonType.String)]
        public Guid UserId { get; set; }

        /// <summary>
        /// Telegram user ID
        /// </summary>
        [BsonElement("telegramUserId")]
        public long TelegramUserId { get; set; }

        /// <summary>
        /// Type of achievement tracking
        /// </summary>
        [BsonElement("type")]
        public AchievementType Type { get; set; }

        /// <summary>
        /// Achievement description
        /// </summary>
        [BsonElement("description")]
        public string Description { get; set; }

        /// <summary>
        /// Achievement deadline
        /// </summary>
        [BsonElement("deadline")]
        public DateTime? Deadline { get; set; }

        /// <summary>
        /// Current status
        /// </summary>
        [BsonElement("status")]
        public AchievementStatus Status { get; set; }

        /// <summary>
        /// Karma reward for completion
        /// </summary>
        [BsonElement("karmaReward")]
        public int KarmaReward { get; set; }

        /// <summary>
        /// Solana token reward for completion
        /// </summary>
        [BsonElement("tokenReward")]
        public decimal TokenReward { get; set; }

        /// <summary>
        /// When the achievement was created
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the achievement was completed
        /// </summary>
        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Telegram ID of admin who verified completion (for manual verification)
        /// </summary>
        [BsonElement("verifiedBy")]
        public long? VerifiedBy { get; set; }

        /// <summary>
        /// Check-in history
        /// </summary>
        [BsonElement("checkins")]
        public List<CheckIn> Checkins { get; set; }

        public Achievement()
        {
            Id = Guid.NewGuid().ToString();
            Type = AchievementType.Manual;
            Status = AchievementStatus.Active;
            CreatedAt = DateTime.UtcNow;
            Checkins = new List<CheckIn>();
            KarmaReward = 50;
            TokenReward = 1.0m;
        }
    }

    /// <summary>
    /// Types of achievement tracking
    /// </summary>
    public enum AchievementType
    {
        Manual,
        Automated,
        External
    }

    /// <summary>
    /// Achievement status
    /// </summary>
    public enum AchievementStatus
    {
        Active,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Represents a check-in/progress update
    /// </summary>
    public class CheckIn
    {
        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }

        [BsonElement("message")]
        public string Message { get; set; }

        [BsonElement("karmaAwarded")]
        public int KarmaAwarded { get; set; }

        public CheckIn()
        {
            Timestamp = DateTime.UtcNow;
        }
    }
}

