using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// Serves the AsyncAPI 2.6 specification describing all WEB6 event-streaming endpoints
    /// (SSE channels and the WebSocket session channel).
    /// GET /asyncapi.json  — JSON
    /// GET /asyncapi.yaml  — YAML
    /// </summary>
    [ApiController]
    [Route("")]
    public class AsyncApiController : ControllerBase
    {
        private static readonly string _spec = BuildSpec();

        [HttpGet("asyncapi.json")]
        [Produces("application/json")]
        public IActionResult GetJson() => Content(_spec, "application/json");

        [HttpGet("asyncapi.yaml")]
        [Produces("text/plain")]
        public IActionResult GetYaml() => Content(ConvertToYaml(), "text/yaml");

        // ── Spec construction ─────────────────────────────────────────────────────

        private static string BuildSpec()
        {
            var doc = new
            {
                asyncapi = "2.6.0",
                info = new
                {
                    title       = "OASIS WEB6 Event-Streaming API",
                    version     = "2.0.0",
                    description = "AsyncAPI specification for all SSE and WebSocket real-time channels exposed by the OASIS WEB6 AI API.",
                    contact = new
                    {
                        name  = "OASIS Support",
                        url   = "https://oasisomniverse.one",
                        email = "support@oasisomniverse.one"
                    },
                    license = new { name = "MIT", url = "https://opensource.org/licenses/MIT" }
                },
                servers = new
                {
                    production = new
                    {
                        url         = "api.web6.oasisomniverse.one",
                        protocol    = "https",
                        description = "Hosted WEB6 production API",
                        security    = new[] { new { BearerAuth = new string[0] } }
                    },
                    websocket = new
                    {
                        url         = "api.web6.oasisomniverse.one",
                        protocol    = "wss",
                        description = "WebSocket endpoint for bidirectional session chat",
                        security    = new[] { new { BearerAuth = new string[0] } }
                    }
                },
                defaultContentType = "application/json",
                channels = new
                {
                    // ── POST /v1/complete/stream ──────────────────────────────────────
                    completionStream = new
                    {
                        address     = "/v1/complete/stream",
                        description = "SSE stream of AI completion chunks. POST a CompletionRequest with Stream=true. Each event is a JSON CompletionChunk; the final event carries FinishReason.",
                        publish = new
                        {
                            operationId = "streamCompletion",
                            summary     = "Initiate a streaming AI completion",
                            message = new
                            {
                                @ref = "#/components/messages/CompletionRequest"
                            }
                        },
                        subscribe = new
                        {
                            operationId = "receiveCompletionChunk",
                            summary     = "Receive completion chunks as SSE events",
                            message = new
                            {
                                @ref = "#/components/messages/CompletionChunk"
                            }
                        },
                        bindings = new
                        {
                            http = new
                            {
                                method = "POST",
                                headers = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        Accept = new { type = "string", @enum = new[] { "text/event-stream" } },
                                        Authorization = new { type = "string", description = "Bearer <token>" }
                                    }
                                }
                            }
                        }
                    },
                    // ── GET /v1/telemetry/stream ─────────────────────────────────────
                    telemetryStream = new
                    {
                        address     = "/v1/telemetry/stream",
                        description = "SSE stream of real-time provider telemetry events (latency, token usage, error rates). Connect and receive continuous metric updates.",
                        subscribe = new
                        {
                            operationId = "receiveTelemetryEvent",
                            summary     = "Receive real-time telemetry events",
                            message = new
                            {
                                @ref = "#/components/messages/TelemetryEvent"
                            }
                        },
                        bindings = new
                        {
                            http = new
                            {
                                method = "GET",
                                headers = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        Accept = new { type = "string", @enum = new[] { "text/event-stream" } },
                                        Authorization = new { type = "string", description = "Bearer <token>" }
                                    }
                                }
                            }
                        }
                    },
                    // ── GET /a2a/tasks/{id}/events ────────────────────────────────────
                    a2aTaskEvents = new
                    {
                        address     = "/a2a/tasks/{taskId}/events",
                        description = "SSE stream of A2A task lifecycle events (submitted, working, completed, failed, cancelled). Subscribe after creating a task with POST /a2a/tasks/send.",
                        parameters = new
                        {
                            taskId = new { description = "The A2A task id returned by POST /a2a/tasks/send", schema = new { type = "string", format = "uuid" } }
                        },
                        subscribe = new
                        {
                            operationId = "receiveA2ATaskEvent",
                            summary     = "Receive A2A task lifecycle events",
                            message = new
                            {
                                @ref = "#/components/messages/A2ATaskEvent"
                            }
                        },
                        bindings = new
                        {
                            http = new
                            {
                                method = "GET",
                                headers = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        Accept = new { type = "string", @enum = new[] { "text/event-stream" } },
                                        Authorization = new { type = "string", description = "Bearer <token>" }
                                    }
                                }
                            }
                        }
                    },
                    // ── wss /v1/ws/session ────────────────────────────────────────────
                    websocketSession = new
                    {
                        address     = "/v1/ws/session",
                        description = "Bidirectional WebSocket channel for persistent session chat. Send ChatMessage frames; receive CompletionChunk frames. The connection is scoped to an AvatarId and maintains holonic memory context across turns.",
                        publish = new
                        {
                            operationId = "sendSessionMessage",
                            summary     = "Send a chat message in the session",
                            message = new
                            {
                                @ref = "#/components/messages/ChatMessage"
                            }
                        },
                        subscribe = new
                        {
                            operationId = "receiveSessionChunk",
                            summary     = "Receive streamed response chunks",
                            message = new
                            {
                                @ref = "#/components/messages/CompletionChunk"
                            }
                        },
                        bindings = new
                        {
                            ws = new
                            {
                                method  = "GET",
                                query = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        avatarId = new { type = "string", format = "uuid", description = "OASIS avatar id for session scoping and holonic memory" },
                                        token    = new { type = "string", description = "JWT bearer token (alternative to Authorization header for WS upgrade)" }
                                    }
                                }
                            }
                        }
                    }
                },
                components = new
                {
                    securitySchemes = new
                    {
                        BearerAuth = new
                        {
                            type        = "http",
                            scheme      = "bearer",
                            bearerFormat = "JWT",
                            description = "Obtain a token via POST /v1/auth/login"
                        }
                    },
                    messages = new
                    {
                        CompletionRequest = new
                        {
                            name        = "CompletionRequest",
                            contentType = "application/json",
                            payload = new
                            {
                                type = "object",
                                properties = new
                                {
                                    provider  = new { type = "string", @default = "auto", description = "AI provider: auto, openai, anthropic, gemini, groq, etc." },
                                    model     = new { type = "string", @default = "auto" },
                                    messages  = new { type = "array", items = new { @ref = "#/components/schemas/ChatMessage" } },
                                    stream    = new { type = "boolean", @default = true },
                                    avatarId  = new { type = "string", format = "uuid" },
                                    maxTokens = new { type = "integer" }
                                },
                                required = new[] { "messages" }
                            }
                        },
                        CompletionChunk = new
                        {
                            name        = "CompletionChunk",
                            contentType = "application/json",
                            payload = new
                            {
                                type = "object",
                                properties = new
                                {
                                    id           = new { type = "string" },
                                    delta        = new { type = "string", description = "Incremental content fragment" },
                                    finishReason = new { type = "string", @enum = new[] { "stop", "tool_calls", "length", "content_filter" }, nullable = true },
                                    provider     = new { type = "string" },
                                    model        = new { type = "string" },
                                    karmaTier    = new { type = "string", @enum = new[] { "Bronze", "Silver", "Gold", "Diamond" }, nullable = true }
                                }
                            }
                        },
                        TelemetryEvent = new
                        {
                            name        = "TelemetryEvent",
                            contentType = "application/json",
                            payload = new
                            {
                                type = "object",
                                properties = new
                                {
                                    provider      = new { type = "string" },
                                    model         = new { type = "string" },
                                    latencyMs     = new { type = "integer" },
                                    promptTokens  = new { type = "integer" },
                                    completionTokens = new { type = "integer" },
                                    isError       = new { type = "boolean" },
                                    timestampUtc  = new { type = "string", format = "date-time" }
                                }
                            }
                        },
                        A2ATaskEvent = new
                        {
                            name        = "A2ATaskEvent",
                            contentType = "application/json",
                            payload = new
                            {
                                type = "object",
                                properties = new
                                {
                                    taskId    = new { type = "string", format = "uuid" },
                                    status    = new { type = "string", @enum = new[] { "submitted", "working", "completed", "failed", "cancelled" } },
                                    message   = new { type = "string", nullable = true },
                                    artifacts = new { type = "array", items = new { type = "object" }, nullable = true }
                                }
                            }
                        },
                        ChatMessage = new
                        {
                            name        = "ChatMessage",
                            contentType = "application/json",
                            payload = new
                            {
                                type = "object",
                                properties = new
                                {
                                    role    = new { type = "string", @enum = new[] { "user", "assistant", "system" } },
                                    content = new { type = "string" }
                                },
                                required = new[] { "role", "content" }
                            }
                        }
                    },
                    schemas = new
                    {
                        ChatMessage = new
                        {
                            type = "object",
                            properties = new
                            {
                                role    = new { type = "string", @enum = new[] { "user", "assistant", "system" } },
                                content = new { type = "string" }
                            },
                            required = new[] { "role", "content" }
                        }
                    }
                }
            };

            return JsonSerializer.Serialize(doc, new JsonSerializerOptions
            {
                WriteIndented         = true,
                PropertyNamingPolicy  = JsonNamingPolicy.CamelCase
            });
        }

        /// <summary>Minimal JSON→YAML conversion sufficient for the AsyncAPI document (no anchors/aliases needed).</summary>
        private static string ConvertToYaml()
        {
            using JsonDocument doc = JsonDocument.Parse(_spec);
            var sb = new System.Text.StringBuilder();
            WriteYamlValue(sb, doc.RootElement, 0);
            return sb.ToString();
        }

        private static void WriteYamlValue(System.Text.StringBuilder sb, JsonElement el, int indent)
        {
            string pad  = new string(' ', indent);
            string pad2 = new string(' ', indent + 2);

            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in el.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            sb.AppendLine($"{pad}{prop.Name}:");
                            WriteYamlValue(sb, prop.Value, indent + 2);
                        }
                        else
                        {
                            sb.Append($"{pad}{prop.Name}: ");
                            WriteYamlValue(sb, prop.Value, 0);
                            sb.AppendLine();
                        }
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var item in el.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object || item.ValueKind == JsonValueKind.Array)
                        {
                            sb.AppendLine($"{pad}-");
                            WriteYamlValue(sb, item, indent + 2);
                        }
                        else
                        {
                            sb.Append($"{pad}- ");
                            WriteYamlValue(sb, item, 0);
                            sb.AppendLine();
                        }
                    }
                    break;

                case JsonValueKind.String:
                    string s = el.GetString() ?? "";
                    if (s.Contains('\n') || s.Contains(':') || s.Contains('#') || s.StartsWith("*") || s == "")
                        sb.Append($"\"{s.Replace("\"", "\\\"")}\"");
                    else
                        sb.Append(s);
                    break;

                case JsonValueKind.Number:
                    sb.Append(el.GetRawText());
                    break;

                case JsonValueKind.True:  sb.Append("true");  break;
                case JsonValueKind.False: sb.Append("false"); break;
                case JsonValueKind.Null:  sb.Append("null");  break;
            }
        }
    }
}
