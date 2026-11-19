using System;

namespace NextGenSoftware.OASIS.Edge.Messaging
{
    public sealed class EdgeConversation
    {
        public Guid ConversationId { get; set; }
        public Guid OtherAvatarId { get; set; }
        public EdgeMessage LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }
}
