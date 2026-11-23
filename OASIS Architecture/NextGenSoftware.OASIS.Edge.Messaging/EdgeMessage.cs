using System;

namespace NextGenSoftware.OASIS.Edge.Messaging
{
    public sealed class EdgeMessage
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid FromAvatarId { get; set; }
        public Guid ToAvatarId { get; set; }
        public string Content { get; set; }
        public int MessageType { get; set; }
        public DateTime ClientTimestamp { get; set; }
        public DateTime? ServerReceivedAt { get; set; }
        public EdgeMessageState State { get; set; }

        /// <summary>A delivery receipt command for this incoming message is already in the outbox.</summary>
        public bool ReceiptQueued { get; set; }
    }
}
