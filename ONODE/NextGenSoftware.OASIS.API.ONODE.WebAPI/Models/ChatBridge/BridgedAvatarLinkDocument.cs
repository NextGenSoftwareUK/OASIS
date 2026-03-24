using System;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.ChatBridge
{
    /// <summary>
    /// MongoDB document that records the link between a platform user and an OASIS avatar.
    /// One document per (platform, platformUserId) pair.
    /// </summary>
    public class BridgedAvatarLinkDocument
    {
        /// <summary>Composite key: "discord:&lt;userId&gt;" or "telegram:&lt;userId&gt;".</summary>
        [BsonId]
        public string Id { get; set; }

        /// <summary>"discord" or "telegram".</summary>
        public string Platform { get; set; }

        /// <summary>Platform-native user ID as a string (Discord snowflake or Telegram numeric ID).</summary>
        public string PlatformUserId { get; set; }

        /// <summary>Username on the platform at time of linking (display only; may go stale).</summary>
        public string PlatformUsername { get; set; }

        /// <summary>Linked OASIS avatar ID.</summary>
        public Guid OasisAvatarId { get; set; }

        /// <summary>OASIS avatar username at time of linking.</summary>
        public string OasisUsername { get; set; }

        public DateTime LinkedAtUtc { get; set; }
    }
}
