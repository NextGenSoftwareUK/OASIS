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
        public void LegacyMessage_DerivesConversationAndCursor()
        {
            var m = new Message { FromAvatarId = Guid.NewGuid(), ToAvatarId = Guid.NewGuid(), Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            Assert.Equal(MessagingConversation.DirectId(m.ToAvatarId, m.FromAvatarId), m.EffectiveConversationId);
            Assert.Equal(m.Timestamp, m.EffectiveReceivedAt);
            Assert.Equal(MessageDeliveryState.Sent, m.DeliveryState);
        }
    }
}
