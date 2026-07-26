using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SystemWebSocket = System.Net.WebSockets.WebSocket;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// WebSocket bidirectional agent sessions.
    /// Priority 18 — WebSocket Bidirectional Agent Sessions.
    /// Connects at: GET /v1/ws/session (WebSocket upgrade).
    /// </summary>
    [ApiController]
    [Route("v1")]
    public class WebSocketSessionController : Web6ControllerBase
    {
        private static readonly ConcurrentDictionary<string, WsSession> _sessions = new ConcurrentDictionary<string, WsSession>();

        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Opens a bidirectional WebSocket session. On connect the server loads avatar context (if AvatarId
        /// is set via JWT) and maintains conversation history for the lifetime of the session.
        /// Client sends: { "type": "message"|"tool_result"|"interrupt"|"ping", ... }
        /// Server sends: { "type": "chunk"|"tool_call"|"done"|"error"|"pong", ... }
        /// </summary>
        [HttpGet("ws/session")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task Session()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
            string sessionId = Guid.NewGuid().ToString();

            var session = new WsSession
            {
                SessionId = sessionId,
                AvatarId = AvatarId,
                Messages = new List<ChatMessage>(),
                PendingToolCalls = new Dictionary<string, TaskCompletionSource<string>>()
            };

            _sessions[sessionId] = session;

            await Send(ws, new { type = "session_started", sessionId }, CancellationToken.None);

            try
            {
                await RunSessionLoop(ws, session);
            }
            finally
            {
                _sessions.TryRemove(sessionId, out _);
            }
        }

        private async Task RunSessionLoop(SystemWebSocket ws, WsSession session)
        {
            var buf = new byte[64 * 1024];
            using var cts = new CancellationTokenSource();

            while (ws.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await ws.ReceiveAsync(buf, cts.Token);
                }
                catch
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                string raw = Encoding.UTF8.GetString(buf, 0, result.Count);
                WsClientMessage msg;
                try { msg = JsonSerializer.Deserialize<WsClientMessage>(raw, _json); }
                catch { continue; }

                switch (msg?.Type)
                {
                    case "ping":
                        await Send(ws, new { type = "pong" }, cts.Token);
                        break;

                    case "interrupt":
                        cts.Cancel();
                        break;

                    case "tool_result":
                        if (!string.IsNullOrEmpty(msg.ToolCallId) && session.PendingToolCalls.TryGetValue(msg.ToolCallId, out var tcs))
                        {
                            tcs.TrySetResult(msg.Result ?? "");
                            session.PendingToolCalls.Remove(msg.ToolCallId);
                        }
                        break;

                    case "message":
                        await HandleUserMessage(ws, session, msg.Content ?? "", cts.Token);
                        break;
                }
            }

            if (ws.State == WebSocketState.Open)
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session ended", CancellationToken.None);
        }

        private async Task HandleUserMessage(SystemWebSocket ws, WsSession session, string userContent, CancellationToken ct)
        {
            session.Messages.Add(new ChatMessage { Role = "user", Content = userContent });

            var request = new CompletionRequest
            {
                AvatarId = session.AvatarId,
                Messages = new List<ChatMessage>(session.Messages),
                Stream = true
            };

            var manager = new AIProviderManager(session.AvatarId, OASISDNA);

            try
            {
                var sb = new StringBuilder();
                await foreach (var chunk in manager.CompleteStreamAsync(request).WithCancellation(ct))
                {
                    if (chunk.Done)
                    {
                        session.Messages.Add(new ChatMessage { Role = "assistant", Content = sb.ToString() });
                        await Send(ws, new { type = "done", totalTokens = chunk.CompletionTokens + chunk.PromptTokens, latencyMs = 0 }, ct);
                    }
                    else if (!string.IsNullOrEmpty(chunk.Delta))
                    {
                        sb.Append(chunk.Delta);
                        await Send(ws, new { type = "chunk", delta = chunk.Delta, provider = chunk.Provider, model = chunk.Model }, ct);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                await Send(ws, new { type = "error", message = "Interrupted" }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                await Send(ws, new { type = "error", message = ex.Message }, CancellationToken.None);
            }
        }

        private static async Task Send(SystemWebSocket ws, object payload, CancellationToken ct)
        {
            if (ws.State != WebSocketState.Open) return;
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload, _json);
            await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
        }

        private class WsSession
        {
            public string SessionId { get; set; }
            public Guid AvatarId { get; set; }
            public List<ChatMessage> Messages { get; set; }
            public Dictionary<string, TaskCompletionSource<string>> PendingToolCalls { get; set; }
        }

        private class WsClientMessage
        {
            [JsonPropertyName("type")] public string Type { get; set; }
            [JsonPropertyName("content")] public string Content { get; set; }
            [JsonPropertyName("toolCallId")] public string ToolCallId { get; set; }
            [JsonPropertyName("result")] public string Result { get; set; }
        }
    }
}
