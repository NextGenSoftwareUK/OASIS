using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// A single message in a chat-style completion request, normalised across every AI provider.
    /// Role values: "system" | "user" | "assistant" | "tool"
    /// </summary>
    public class ChatMessage
    {
        public string Role { get; set; }

        /// <summary>Text content. Null when Role="assistant" and the model issued tool calls instead of text.</summary>
        public string Content { get; set; }

        /// <summary>Tool calls the assistant wants to make. Populated on Role="assistant" when FinishReason="tool_calls".</summary>
        public List<ToolCall> ToolCalls { get; set; }

        /// <summary>The tool call id this message is a result for. Required when Role="tool".</summary>
        public string ToolCallId { get; set; }

        /// <summary>Tool name — required when Role="tool".</summary>
        public string Name { get; set; }
    }
}
