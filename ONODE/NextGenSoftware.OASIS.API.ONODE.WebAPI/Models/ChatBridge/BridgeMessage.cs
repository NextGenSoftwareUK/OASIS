using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.ChatBridge
{
    /// <summary>
    /// A single bridged message stored in the in-memory history feed.
    /// </summary>
    public class BridgeMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Source platform: "discord", "telegram", or "ide".</summary>
        public string Platform { get; set; }

        /// <summary>Resolved OASIS avatar name, or fallback platform username.</summary>
        public string SenderName { get; set; }

        public string Text { get; set; }
    }
}
