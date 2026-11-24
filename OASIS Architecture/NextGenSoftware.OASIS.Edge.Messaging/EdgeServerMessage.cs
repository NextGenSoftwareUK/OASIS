using System;

namespace NextGenSoftware.OASIS.Edge.Messaging
{
    /// <summary>A message as the hosted API returns it from <c>api/messaging/sync</c>.</summary>
    public sealed class EdgeServerMessage
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid FromAvatarId { get; set; }
        public Guid ToAvatarId { get; set; }
        public string Content { get; set; }
        public int MessageType { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime? ClientTimestamp { get; set; }
        public DateTime ReceivedAt { get; set; }
        /// <summary>0 Sent, 1 Delivered, 2 Read.</summary>
        public int DeliveryState { get; set; }
        public bool IsRead { get; set; }
    }
}
