namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// A single message in a chat-style completion request, normalised across every AI provider.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>"system", "user" or "assistant".</summary>
        public string Role { get; set; }

        public string Content { get; set; }
    }
}
