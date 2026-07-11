using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    /// <summary>
    /// Abstracts and aggregates every agent protocol/orchestration framework (MCP, A2A, LangChain, AutoGen, CrewAI,
    /// Semantic Kernel, or any generic webhook-based orchestrator) behind one unified WEB6 interface. Register an
    /// external orchestrator endpoint once via RegisterAdapterAsync, then invoke it through InvokeAsync using the
    /// same normalised request/response shape regardless of which protocol it actually speaks underneath - the
    /// same "one API, every AI" abstraction the AIProviderManager provides for raw model providers, extended to
    /// cover entire orchestration frameworks and multi-agent protocols.
    /// </summary>
    public class OrchestratorManager : OASISManager
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public OrchestratorManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }

        public OrchestratorManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null)
            : base(OASISStorageProvider, avatarId, OASISDNA) { }

        /// <summary>Registers an external agent/orchestrator endpoint, persisted as an OrchestratorAdapter holon.</summary>
        public async Task<OASISResult<OrchestratorAdapterConfig>> RegisterAdapterAsync(OrchestratorAdapterConfig config)
        {
            OASISResult<OrchestratorAdapterConfig> result = new OASISResult<OrchestratorAdapterConfig>();

            Holon holon = new Holon(HolonType.OrchestratorAdapter)
            {
                Name = config.Name,
                Description = $"WEB6 orchestrator adapter for {config.Protocol} at {config.EndpointUrl}."
            };

            holon.MetaData["Protocol"] = config.Protocol.ToString();
            holon.MetaData["EndpointUrl"] = config.EndpointUrl;
            holon.MetaData["AuthToken"] = config.AuthToken;
            holon.MetaData["ExtraConfig"] = JsonSerializer.Serialize(config.ExtraConfig ?? new Dictionary<string, string>());

            OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(holon, AvatarId, false);

            if (saveResult.IsError || saveResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Error registering orchestrator adapter. Reason: {saveResult.Message}");
                return result;
            }

            config.Id = saveResult.Result.Id;
            result.Result = config;
            return result;
        }

        /// <summary>Lists every registered orchestrator adapter.</summary>
        public async Task<OASISResult<List<OrchestratorAdapterConfig>>> GetAdaptersAsync()
        {
            OASISResult<List<OrchestratorAdapterConfig>> result = new OASISResult<List<OrchestratorAdapterConfig>>();
            OASISResult<IEnumerable<IHolon>> loadResult = await Data.LoadAllHolonsAsync(HolonType.OrchestratorAdapter);

            if (loadResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading orchestrator adapters. Reason: {loadResult.Message}");
                return result;
            }

            result.Result = (loadResult.Result ?? Enumerable.Empty<IHolon>()).Select(MapToConfig).ToList();
            return result;
        }

        /// <summary>
        /// Invokes a registered orchestrator adapter with a normalised request, translating to/from its native
        /// protocol wire format under the hood, so every caller deals with the same WEB6 shape regardless of whether
        /// the adapter underneath is an MCP server, an A2A agent, a LangChain/AutoGen/CrewAI/Semantic Kernel
        /// deployment, or a plain webhook.
        /// </summary>
        public async Task<OASISResult<OrchestratorInvokeResponse>> InvokeAsync(OrchestratorInvokeRequest request)
        {
            OASISResult<OrchestratorInvokeResponse> result = new OASISResult<OrchestratorInvokeResponse>();
            OASISResult<IHolon> loadResult = await Data.LoadHolonAsync(request.AdapterId, false);

            if (loadResult.IsError || loadResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Orchestrator adapter {request.AdapterId} is not registered.");
                return result;
            }

            OrchestratorAdapterConfig config = MapToConfig(loadResult.Result);
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                string output = config.Protocol switch
                {
                    OrchestratorProtocolType.MCP => await InvokeMcpAsync(config, request),
                    OrchestratorProtocolType.A2A => await InvokeA2AAsync(config, request),
                    OrchestratorProtocolType.LangChain => await InvokeJsonWebhookAsync(config, request, "input"),
                    OrchestratorProtocolType.AutoGen => await InvokeJsonWebhookAsync(config, request, "message"),
                    OrchestratorProtocolType.CrewAI => await InvokeJsonWebhookAsync(config, request, "inputs"),
                    OrchestratorProtocolType.SemanticKernel => await InvokeJsonWebhookAsync(config, request, "input"),
                    OrchestratorProtocolType.ACP => await InvokeAcpAsync(config, request),
                    OrchestratorProtocolType.ANP => await InvokeAnpAsync(config, request),
                    OrchestratorProtocolType.GraphQL => await InvokeGraphQLAsync(config, request),
                    OrchestratorProtocolType.Kafka or OrchestratorProtocolType.AMQP or OrchestratorProtocolType.MQTT
                        => await InvokeEventStreamAsync(config, request),
                    OrchestratorProtocolType.GRPC => await InvokeJsonWebhookAsync(config, request, "input"), // gRPC stub — full impl requires .proto descriptor
                    _ => await InvokeJsonWebhookAsync(config, request, "input"),
                };

                sw.Stop();
                result.Result = new OrchestratorInvokeResponse
                {
                    AdapterName = config.Name,
                    Protocol = config.Protocol,
                    Output = output,
                    LatencyMs = sw.ElapsedMilliseconds
                };
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking orchestrator adapter '{config.Name}' ({config.Protocol}). Reason: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>Generic JSON-over-HTTP invocation for protocols whose wire format is a simple {fieldName, parameters} POST body (covers LangChain/AutoGen/CrewAI/Semantic Kernel/A2A REST bridges and plain webhooks).</summary>
        private async Task<string> InvokeJsonWebhookAsync(OrchestratorAdapterConfig config, OrchestratorInvokeRequest request, string inputFieldName)
        {
            Dictionary<string, object> payload = new Dictionary<string, object>(request.Parameters) { [inputFieldName] = request.Input };

            using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, config.EndpointUrl);

            if (!string.IsNullOrEmpty(config.AuthToken))
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AuthToken);

            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            string body = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
                throw new HttpRequestException($"Orchestrator '{config.Name}' returned {(int)httpResponse.StatusCode}: {body}");

            return body;
        }

        /// <summary>Invokes an MCP server's tools/call JSON-RPC method - the tool name comes from ExtraConfig["tool"].</summary>
        /// <summary>
        /// Performs the full MCP Streamable HTTP transport handshake - initialize, then the notifications/initialized
        /// notification, then the actual tools/call - carrying the server-issued Mcp-Session-Id across all three
        /// requests (per the MCP spec, a server may require this session to be established before accepting calls).
        /// </summary>
        private async Task<string> InvokeMcpAsync(OrchestratorAdapterConfig config, OrchestratorInvokeRequest request)
        {
            const string protocolVersion = "2025-06-18";
            string toolName = config.ExtraConfig != null && config.ExtraConfig.TryGetValue("tool", out string tool) ? tool : "default";

            var initializeResponse = await SendMcpRequestAsync(config, sessionId: null, new
            {
                jsonrpc = "2.0",
                id = "1",
                method = "initialize",
                @params = new
                {
                    protocolVersion,
                    capabilities = new { },
                    clientInfo = new { name = "OASIS-WEB6-FAHRN", version = "1.0.0" }
                }
            });

            string sessionId = initializeResponse.sessionId;

            // Notify the server initialization is complete - this is a JSON-RPC notification (no "id" field, no response expected).
            await SendMcpRequestAsync(config, sessionId, new { jsonrpc = "2.0", method = "notifications/initialized", @params = new { } }, expectResponse: false);

            var callResponse = await SendMcpRequestAsync(config, sessionId, new
            {
                jsonrpc = "2.0",
                id = "2",
                method = "tools/call",
                @params = new { name = toolName, arguments = new Dictionary<string, object>(request.Parameters) { ["input"] = request.Input } }
            });

            return callResponse.body;
        }

        private async Task<(string body, string sessionId)> SendMcpRequestAsync(OrchestratorAdapterConfig config, string sessionId, object jsonRpcPayload, bool expectResponse = true)
        {
            using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, config.EndpointUrl);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            if (!string.IsNullOrEmpty(config.AuthToken))
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AuthToken);

            if (!string.IsNullOrEmpty(sessionId))
                httpRequest.Headers.Add("Mcp-Session-Id", sessionId);

            httpRequest.Content = new StringContent(JsonSerializer.Serialize(jsonRpcPayload), Encoding.UTF8, "application/json");

            using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
            string body = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
                throw new HttpRequestException($"MCP server '{config.Name}' returned {(int)httpResponse.StatusCode}: {body}");

            if (!expectResponse)
                return (body, sessionId);

            string newSessionId = httpResponse.Headers.TryGetValues("Mcp-Session-Id", out var values) ? values.FirstOrDefault() : sessionId;

            // A Streamable HTTP server may respond with an SSE stream ("data: {...}" lines) instead of a single JSON body.
            if (httpResponse.Content.Headers.ContentType?.MediaType == "text/event-stream")
            {
                string lastDataLine = body.Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.StartsWith("data:"));
                body = lastDataLine != null ? lastDataLine["data:".Length..].Trim() : body;
            }

            return (body, newSessionId);
        }

        // ── Priority 4c — proper A2A wire format ────────────────────────────────────
        private async Task<string> InvokeA2AAsync(OrchestratorAdapterConfig config, OrchestratorInvokeRequest request)
        {
            // 1. POST {endpoint}/a2a/tasks/send → { "message": { "role": "user", "parts": [{"text": input}] } }
            string taskUrl = config.EndpointUrl.TrimEnd('/') + "/a2a/tasks/send";
            var body = new { message = new { role = "user", parts = new[] { new { text = request.Input } } } };
            using var req = new HttpRequestMessage(HttpMethod.Post, taskUrl);
            if (!string.IsNullOrEmpty(config.AuthToken))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AuthToken);
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var resp = await _httpClient.SendAsync(req);
            string raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"A2A '{config.Name}' returned {(int)resp.StatusCode}: {raw}");

            // 2. Poll task until completed
            using var doc = JsonDocument.Parse(raw);
            string taskId = doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
            if (string.IsNullOrEmpty(taskId)) return raw;

            string pollUrl = config.EndpointUrl.TrimEnd('/') + $"/a2a/tasks/{taskId}";
            for (int i = 0; i < 60; i++)
            {
                await Task.Delay(500);
                using var pollReq = new HttpRequestMessage(HttpMethod.Get, pollUrl);
                if (!string.IsNullOrEmpty(config.AuthToken))
                    pollReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AuthToken);
                using var pollResp = await _httpClient.SendAsync(pollReq);
                string pollRaw = await pollResp.Content.ReadAsStringAsync();
                using var pollDoc = JsonDocument.Parse(pollRaw);
                string state = pollDoc.RootElement.TryGetProperty("state", out var s) ? s.GetString() : "working";
                if (state == "completed" || state == "failed") return pollRaw;
            }
            return raw; // timeout — return last known state
        }

        // ── Priority 21a — ACP (Agent Communication Protocol) ───────────────────────
        private async Task<string> InvokeAcpAsync(OrchestratorAdapterConfig config, OrchestratorInvokeRequest request)
        {
            string agentId = config.ExtraConfig?.GetValueOrDefault("agentId") ?? "default";
            string runUrl = config.EndpointUrl.TrimEnd('/') + $"/agents/{agentId}/runs";
            var body = new { input = new[] { new { role = "user", content = new[] { new { type = "text", text = request.Input } } } } };
            using var req = new HttpRequestMessage(HttpMethod.Post, runUrl);
            if (!string.IsNullOrEmpty(config.AuthToken))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AuthToken);
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var resp = await _httpClient.SendAsync(req);
            string raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"ACP '{config.Name}' returned {(int)resp.StatusCode}: {raw}");

            using var doc = JsonDocument.Parse(raw);
            string runId = doc.RootElement.TryGetProperty("run_id", out var rid) ? rid.GetString()
                         : doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
            if (string.IsNullOrEmpty(runId)) return raw;

            // Poll until terminal state
            string statusUrl = config.EndpointUrl.TrimEnd('/') + $"/agents/{agentId}/runs/{runId}";
            for (int i = 0; i < 120; i++)
            {
                await Task.Delay(500);
                using var pollReq = new HttpRequestMessage(HttpMethod.Get, statusUrl);
                if (!string.IsNullOrEmpty(config.AuthToken))
                    pollReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AuthToken);
                using var pollResp = await _httpClient.SendAsync(pollReq);
                string pollRaw = await pollResp.Content.ReadAsStringAsync();
                using var pollDoc = JsonDocument.Parse(pollRaw);
                string status = pollDoc.RootElement.TryGetProperty("status", out var st) ? st.GetString() : "running";
                if (status == "completed" || status == "failed" || status == "error") return pollRaw;
            }
            return raw;
        }

        // ── Priority 21b — ANP (Agent Network Protocol) ─────────────────────────────
        private async Task<string> InvokeAnpAsync(OrchestratorAdapterConfig config, OrchestratorInvokeRequest request)
        {
            // Resolve DID document → extract ANP service endpoint
            string endpoint = config.EndpointUrl;
            if (endpoint.StartsWith("did:", StringComparison.OrdinalIgnoreCase))
            {
                string resolverUrl = (Environment.GetEnvironmentVariable("DID_RESOLVER_URL") ?? "https://resolver.identity.foundation").TrimEnd('/');
                using var resolveReq = new HttpRequestMessage(HttpMethod.Get, $"{resolverUrl}/1.0/identifiers/{Uri.EscapeDataString(endpoint)}");
                resolveReq.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                using var resolveResp = await _httpClient.SendAsync(resolveReq);
                if (resolveResp.IsSuccessStatusCode)
                {
                    string resolved = await resolveResp.Content.ReadAsStringAsync();
                    using var resolvedDoc = JsonDocument.Parse(resolved);
                    if (resolvedDoc.RootElement.TryGetProperty("didDocument", out var didDoc)
                        && didDoc.TryGetProperty("service", out var services))
                    {
                        foreach (var svc in services.EnumerateArray())
                        {
                            string svcType = svc.TryGetProperty("type", out var t) ? t.GetString() : "";
                            if (string.Equals(svcType, "ANPAgent", StringComparison.OrdinalIgnoreCase)
                                && svc.TryGetProperty("serviceEndpoint", out var ep))
                            {
                                endpoint = ep.GetString();
                                break;
                            }
                        }
                    }
                }
            }

            // POST ANP message envelope
            var anpBody = new { type = "message", content = request.Input, parameters = request.Parameters };
            using var postReq = new HttpRequestMessage(HttpMethod.Post, endpoint.TrimEnd('/') + "/messages");
            if (!string.IsNullOrEmpty(config.AuthToken))
                postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AuthToken);
            postReq.Content = new StringContent(JsonSerializer.Serialize(anpBody), Encoding.UTF8, "application/json");
            using var postResp = await _httpClient.SendAsync(postReq);
            string postRaw = await postResp.Content.ReadAsStringAsync();
            if (!postResp.IsSuccessStatusCode)
                throw new HttpRequestException($"ANP '{config.Name}' returned {(int)postResp.StatusCode}: {postRaw}");
            return postRaw;
        }

        // ── Priority 21e — GraphQL ────────────────────────────────────────────────────
        private async Task<string> InvokeGraphQLAsync(OrchestratorAdapterConfig config, OrchestratorInvokeRequest request)
        {
            string query = config.ExtraConfig?.GetValueOrDefault("query") ?? "mutation InvokeAgent($input: String!) { invoke(input: $input) { result } }";
            var variables = new Dictionary<string, object>(request.Parameters) { ["input"] = request.Input };
            var gqlBody = new { query, variables };
            using var req = new HttpRequestMessage(HttpMethod.Post, config.EndpointUrl);
            if (!string.IsNullOrEmpty(config.AuthToken))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AuthToken);
            req.Content = new StringContent(JsonSerializer.Serialize(gqlBody), Encoding.UTF8, "application/json");
            using var resp = await _httpClient.SendAsync(req);
            string raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"GraphQL '{config.Name}' returned {(int)resp.StatusCode}: {raw}");
            return raw;
        }

        // ── Priority 21d — Event streaming (Kafka/AMQP/MQTT stub) ───────────────────
        private async Task<string> InvokeEventStreamAsync(OrchestratorAdapterConfig config, OrchestratorInvokeRequest request)
        {
            // Fire-and-forget via generic HTTP POST to a webhook bridge
            // Full Kafka/AMQP/MQTT clients require additional NuGet packages (Confluent.Kafka, RabbitMQ.Client, MQTTnet)
            // For now, delegate to a configurable HTTP bridge: ExtraConfig["bridgeUrl"]
            string bridgeUrl = config.ExtraConfig?.GetValueOrDefault("bridgeUrl") ?? config.EndpointUrl;
            var payload = new { topic = config.ExtraConfig?.GetValueOrDefault("topic") ?? "oasis.web6", protocol = config.Protocol.ToString(), message = request.Input, parameters = request.Parameters };
            using var req = new HttpRequestMessage(HttpMethod.Post, bridgeUrl);
            if (!string.IsNullOrEmpty(config.AuthToken))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AuthToken);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var resp = await _httpClient.SendAsync(req);
            return await resp.Content.ReadAsStringAsync();
        }

        private static OrchestratorAdapterConfig MapToConfig(IHolon holon)
        {
            OrchestratorAdapterConfig config = new OrchestratorAdapterConfig
            {
                Id = holon.Id,
                Name = holon.Name,
                EndpointUrl = holon.MetaData.TryGetValue("EndpointUrl", out object e) ? e?.ToString() : null,
                AuthToken = holon.MetaData.TryGetValue("AuthToken", out object a) ? a?.ToString() : null
            };

            if (holon.MetaData.TryGetValue("Protocol", out object p) && Enum.TryParse(p?.ToString(), true, out OrchestratorProtocolType protocol))
                config.Protocol = protocol;

            if (holon.MetaData.TryGetValue("ExtraConfig", out object ec) && ec != null)
                config.ExtraConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(ec.ToString()) ?? new Dictionary<string, string>();

            return config;
        }
    }
}
