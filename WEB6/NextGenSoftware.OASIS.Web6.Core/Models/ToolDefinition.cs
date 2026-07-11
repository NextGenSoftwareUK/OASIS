using System.Text.Json.Nodes;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// Describes a tool the model can call. Follows the OpenAI function-calling schema.
    /// </summary>
    public class ToolDefinition
    {
        /// <summary>"function" (only type supported today).</summary>
        public string Type { get; set; } = "function";

        public ToolFunction Function { get; set; }
    }

    public class ToolFunction
    {
        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>JSON Schema object describing the function parameters.</summary>
        public JsonObject Parameters { get; set; }
    }
}
