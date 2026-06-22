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
                    OrchestratorProtocolType.A2A => await InvokeJsonWebhookAsync(config, request, "task"),
                    OrchestratorProtocolType.LangChain => await InvokeJsonWebhookAsync(config, request, "input"),
                    OrchestratorProtocolType.AutoGen => await InvokeJsonWebhookAsync(config, request, "message"),
                    OrchestratorProtocolType.CrewAI => await InvokeJsonWebhookAsync(config, request, "inputs"),
                    OrchestratorProtocolType.SemanticKernel => await InvokeJsonWebhookAsync(config, request, "input"),
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
