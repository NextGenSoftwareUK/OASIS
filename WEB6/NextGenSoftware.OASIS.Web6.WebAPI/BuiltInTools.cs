using System.Collections.Generic;
using System.Text.Json.Nodes;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.WebAPI
{
    /// <summary>
    /// First-party OASIS tool definitions auto-injected into every completion request when an
    /// AvatarId is present and the caller has not supplied their own tools list.
    /// These definitions are intentionally provider-agnostic: they follow the JSON Schema subset
    /// accepted by OpenAI function-calling, Anthropic tool_use, and Gemini functionDeclarations.
    /// </summary>
    public static class BuiltInTools
    {
        public static readonly List<ToolDefinition> All = new List<ToolDefinition>
        {
            Make("oasis_avatar_get",
                "Returns the OASIS avatar profile for the currently authenticated user (name, karma, level, bio).",
                new { type = "object", properties = new { }, required = new string[0] }),

            Make("oasis_karma_get",
                "Returns the current karma score and recent karma history for a given avatar.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        avatar_id = new { type = "string", description = "UUID of the avatar. Omit to use the authenticated avatar." }
                    },
                    required = new string[0]
                }),

            Make("oasis_holon_search",
                "Searches the OASIS holonic graph for holons matching a keyword or phrase.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Keyword or phrase to search for." },
                        max_results = new { type = "integer", description = "Maximum results to return. Default 10." }
                    },
                    required = new[] { "query" }
                }),

            Make("oasis_quest_get",
                "Returns the active quest(s) for the authenticated avatar, including objectives and progress.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        quest_id = new { type = "string", description = "UUID of a specific quest to fetch. Omit for all active quests." }
                    },
                    required = new string[0]
                }),

            Make("oasis_memory_read",
                "Reads a named memory value stored in the avatar's holonic memory graph.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        key = new { type = "string", description = "The memory key to retrieve." }
                    },
                    required = new[] { "key" }
                }),

            Make("oasis_memory_write",
                "Stores a value in the avatar's holonic memory graph under the given key.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        key = new { type = "string", description = "The memory key." },
                        value = new { type = "string", description = "The value to store (serialised as string)." }
                    },
                    required = new[] { "key", "value" }
                }),

            Make("oasis_braid_graph_get",
                "Returns the Holonic BRAID shared reasoning graph for a given task type as a Mermaid diagram.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        task_type = new { type = "string", description = "Task type key (e.g. \"code\", \"legal\", \"general\")." }
                    },
                    required = new[] { "task_type" }
                })
        };

        private static ToolDefinition Make(string name, string description, object parametersAnon)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(parametersAnon);
            return new ToolDefinition
            {
                Function = new ToolFunction
                {
                    Name = name,
                    Description = description,
                    Parameters = System.Text.Json.Nodes.JsonNode.Parse(json) as JsonObject
                }
            };
        }
    }
}
