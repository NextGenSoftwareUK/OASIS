namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// A tool call the model wants to make, returned in CompletionResponse.ToolCalls.
    /// </summary>
    public class ToolCall
    {
        /// <summary>Unique id for this call — pass back as ToolCallId when submitting the result.</summary>
        public string Id { get; set; }

        /// <summary>"function"</summary>
        public string Type { get; set; } = "function";

        public ToolCallFunction Function { get; set; }
    }

    public class ToolCallFunction
    {
        public string Name { get; set; }

        /// <summary>JSON-encoded arguments string (as returned by the model).</summary>
        public string Arguments { get; set; }
    }
}
