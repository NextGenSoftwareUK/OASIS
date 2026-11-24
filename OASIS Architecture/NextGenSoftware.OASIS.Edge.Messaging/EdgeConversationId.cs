using System;
using System.Security.Cryptography;
using System.Text;

namespace NextGenSoftware.OASIS.Edge.Messaging
{
    /// <summary>
    /// Must produce exactly the same id as the server's <c>MessagingConversation.DirectId</c>, so a conversation
    /// started offline lines up with the server copy without any coordination.
    /// </summary>
    public static class EdgeConversationId
    {
        public static Guid Direct(Guid avatarA, Guid avatarB)
        {
            var (first, second) = avatarA.CompareTo(avatarB) <= 0 ? (avatarA, avatarB) : (avatarB, avatarA);
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes($"oasis-direct:{first:N}:{second:N}"));
                var bytes = new byte[16];
                Array.Copy(hash, bytes, 16);
                return new Guid(bytes);
            }
        }
    }
}
