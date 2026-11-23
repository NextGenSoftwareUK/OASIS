using System;
using NextGenSoftware.OASIS.API.Core.Managers;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.Messaging
{
    public class MessagingConversationTests
    {
        [Fact]
        public void DirectId_IsSymmetricAndStable()
        {
            var a = Guid.NewGuid();
            var b = Guid.NewGuid();
            Assert.Equal(MessagingConversation.DirectId(a, b), MessagingConversation.DirectId(b, a));
            Assert.Equal(MessagingConversation.DirectId(a, b), MessagingConversation.DirectId(a, b));
            Assert.NotEqual(MessagingConversation.DirectId(a, b), MessagingConversation.DirectId(a, Guid.NewGuid()));
        }

        [Fact]
        public void DirectId_MatchesTheEdgeClientVector()
        {
            // Pinned literal shared with EdgeMessagingTests.ConversationId_MatchesTheServerAlgorithm.
            var a = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var b = Guid.Parse("00000000-0000-0000-0000-000000000002");
            Assert.Equal(Guid.Parse("3ed2b6b5-cd81-0191-5888-aa97c075dbe4"), MessagingConversation.DirectId(a, b));
        }

        [Fact]
        public void LegacyMessage_DerivesConversationAndCursor()
        {
            var m = new Message { FromAvatarId = Guid.NewGuid(), ToAvatarId = Guid.NewGuid(), Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            Assert.Equal(MessagingConversation.DirectId(m.ToAvatarId, m.FromAvatarId), m.EffectiveConversationId);
            Assert.Equal(m.Timestamp, m.EffectiveReceivedAt);
            Assert.Equal(MessageDeliveryState.Sent, m.DeliveryState);
        }
    }
}
