using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.API.Core.Objects.OpenServ;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// OpenSERV integration methods for A2A Protocol
    /// Provides integration with OpenSERV AI agents for workflow execution
    /// </summary>
    public partial class A2AManager
    {
        private static HttpClient _openServHttpClient;

        /// <summary>
        /// HTTP client for OpenSERV requests (can be set externally for dependency injection)
        /// </summary>
        public static HttpClient OpenServHttpClient
        {
            get
            {
                if (_openServHttpClient == null)
                    _openServHttpClient = new HttpClient();
                return _openServHttpClient;
            }
            set => _openServHttpClient = value;
        }

        /// <summary>
        /// Register an OpenSERV agent as an A2A agent and SERV service
        /// </summary>
        /// <param name="openServAgentId">OpenSERV agent ID</param>
        /// <param name="openServEndpoint">OpenSERV endpoint URL</param>
        /// <param name="capabilities">List of capabilities/services this agent provides</param>
        /// <param name="apiKey">OpenSERV API key (optional)</param>
        /// <param name="username">Username for the avatar (optional, defaults to openserv_{agentId})</param>
        /// <param name="email">Email for the avatar (optional, auto-generated if not provided)</param>
        /// <param name="password">Password for the avatar (optional, auto-generated if not provided)</param>
        /// <param name="localUrl">Local agent URL for dev fallback (e.g. http://localhost:7378)</param>
        /// <param name="authToken">Auth token for local agent (x-openserv-auth-token)</param>
        /// <returns>OASISResult with registration status</returns>
        public async Task<OASISResult<bool>> RegisterOpenServAgentAsync(
            string openServAgentId,
            string openServEndpoint,
            List<string> capabilities,
            string apiKey = null,
            string username = null,
            string email = null,
            string password = null,
            string localUrl = null,
            string authToken = null)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(openServAgentId))
                {
                    OASISErrorHandling.HandleError(ref result, "OpenServAgentId is required");
                    return result;
                }

                if (string.IsNullOrEmpty(openServEndpoint))
                {
                    OASISErrorHandling.HandleError(ref result, "OpenServEndpoint is required");
                    return result;
                }

                if (capabilities == null || capabilities.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "At least one capability is required");
                    return result;
                }

                // 1. Get or create A2A agent avatar
                var avatarUsername = username ?? $"openserv_{openServAgentId}";
                var avatarEmail = email ?? $"{avatarUsername}@openserv.agent";
                var avatarPassword = password ?? Guid.NewGuid().ToString("N")[..16]; // Generate secure password

                // Try to load existing avatar by email first (reuse pre-existing agent)
                var existingAvatarResult = await AvatarManager.Instance.LoadAvatarByEmailAsync(avatarEmail, false, true);
                IAvatar avatar;

                if (!existingAvatarResult.IsError && existingAvatarResult.Result != null)
                {
                    avatar = existingAvatarResult.Result;
                    LoggingManager.Log($"Using existing OpenSERV agent avatar: {avatar.Email} (Id: {avatar.Id})", Logging.LogType.Info);
                }
                else
                {
                    var avatarResult = await AvatarManager.Instance.RegisterAsync(
                        avatarTitle: "Agent",
                        firstName: "OpenSERV",
                        lastName: openServAgentId,
                        email: avatarEmail,
                        password: avatarPassword,
                        username: avatarUsername,
                        avatarType: AvatarType.Agent,
                        createdOASISType: OASISType.OASISAPIREST
                    );

                    if (avatarResult.IsError || avatarResult.Result == null)
                    {
                        OASISErrorHandling.HandleError(ref result,
                            $"Failed to create avatar for OpenSERV agent: {avatarResult.Message}");
                        return result;
                    }

                    avatar = avatarResult.Result;
                }

                // 2. Register A2A capabilities
                var a2aCapabilities = new AgentCapabilities
                {
                    Services = capabilities,
                    Skills = new List<string> { "AI", "OpenSERV", "Reasoning" },
                    Status = AgentStatus.Available,
                    Description = $"OpenSERV AI Agent: {openServAgentId}",
                    Metadata = new Dictionary<string, object>
                    {
                        ["openserv_agent_id"] = openServAgentId,
                        ["openserv_endpoint"] = openServEndpoint,
                        ["openserv_api_key"] = apiKey ?? string.Empty
                    }
                };
                if (!string.IsNullOrEmpty(localUrl))
                    a2aCapabilities.Metadata["openserv_local_url"] = localUrl;
                if (!string.IsNullOrEmpty(authToken))
                {
                    // OpenServ SDK expects client to send bcrypt hash; agent compares with plain token
                    var authTokenHash = BCrypt.Net.BCrypt.HashPassword(authToken);
                    a2aCapabilities.Metadata["openserv_auth_token"] = authTokenHash;
                }

                var capabilitiesResult = await AgentManager.Instance
                    .RegisterAgentCapabilitiesAsync(avatar.Id, a2aCapabilities);

                if (capabilitiesResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"Failed to register agent capabilities: {capabilitiesResult.Message}");
                    return result;
                }

                // 2b. Persist capabilities to avatar MetaData (MongoDB) so they survive API restarts
                if (avatar.MetaData == null)
                    avatar.MetaData = new Dictionary<string, object>();
                avatar.MetaData["A2A_Services"] = capabilities;
                avatar.MetaData["A2A_Description"] = a2aCapabilities.Description;
                avatar.MetaData["A2A_OpenservAgentId"] = openServAgentId;
                avatar.MetaData["A2A_OpenservEndpoint"] = openServEndpoint;
                avatar.MetaData["A2A_OpenservApiKey"] = apiKey ?? string.Empty;
                if (!string.IsNullOrEmpty(localUrl))
                    avatar.MetaData["A2A_OpenservLocalUrl"] = localUrl;
                if (!string.IsNullOrEmpty(authToken))
                {
                    var authTokenHash = BCrypt.Net.BCrypt.HashPassword(authToken);
                    avatar.MetaData["A2A_OpenservAuthToken"] = authTokenHash;
                }
                var saveResult = await AvatarManager.Instance.SaveAvatarAsync(avatar);
                if (saveResult.IsError)
                    LoggingManager.Log($"Warning: Failed to persist agent capabilities to avatar MetaData: {saveResult.Message}", Logging.LogType.Warning);

                // 3. Register with ONET Service Registry (UnifiedAgentServiceManager)
                try
                {
                    var unifiedServiceManagerType = Type.GetType("NextGenSoftware.OASIS.API.Core.Managers.UnifiedAgentServiceManager.UnifiedAgentServiceManager, NextGenSoftware.OASIS.API.Core");
                    if (unifiedServiceManagerType != null)
                    {
                        var instanceProperty = unifiedServiceManagerType.GetProperty("Instance");
                        if (instanceProperty != null)
                        {
                            var managerInstance = instanceProperty.GetValue(null);
                            var unifiedAgentServiceType = Type.GetType("NextGenSoftware.OASIS.API.Core.Managers.UnifiedAgentServiceManager.UnifiedAgentService, NextGenSoftware.OASIS.API.Core");
                            
                            if (unifiedAgentServiceType != null)
                            {
                                var unifiedService = Activator.CreateInstance(unifiedAgentServiceType);
                                unifiedAgentServiceType.GetProperty("ServiceId")?.SetValue(unifiedService, avatar.Id.ToString());
                                unifiedAgentServiceType.GetProperty("ServiceName")?.SetValue(unifiedService, $"OpenSERV Agent: {openServAgentId}");
                                unifiedAgentServiceType.GetProperty("ServiceType")?.SetValue(unifiedService, "OpenSERV_AI_Agent");
                                unifiedAgentServiceType.GetProperty("Endpoint")?.SetValue(unifiedService, openServEndpoint);
                                unifiedAgentServiceType.GetProperty("Protocol")?.SetValue(unifiedService, "OpenSERV_HTTP");
                                unifiedAgentServiceType.GetProperty("Description")?.SetValue(unifiedService, a2aCapabilities.Description);
                                unifiedAgentServiceType.GetProperty("AgentId")?.SetValue(unifiedService, avatar.Id);
                                unifiedAgentServiceType.GetProperty("RegisteredAt")?.SetValue(unifiedService, DateTime.UtcNow);
                                
                                var metadata = new Dictionary<string, object>
                                {
                                    ["openserv_agent_id"] = openServAgentId,
                                    ["a2a_agent_id"] = avatar.Id,
                                    ["capabilities"] = capabilities,
                                    ["openserv_endpoint"] = openServEndpoint,
                                    ["openserv_api_key"] = apiKey ?? string.Empty
                                };
                                unifiedAgentServiceType.GetProperty("Metadata")?.SetValue(unifiedService, metadata);
                                unifiedAgentServiceType.GetProperty("Capabilities")?.SetValue(unifiedService, capabilities);
                                
                                var statusType = Type.GetType("NextGenSoftware.OASIS.API.Core.Managers.UnifiedAgentServiceManager.UnifiedServiceStatus, NextGenSoftware.OASIS.API.Core");
                                if (statusType != null)
                                {
                                    var availableStatus = Enum.Parse(statusType, "Available");
                                    unifiedAgentServiceType.GetProperty("Status")?.SetValue(unifiedService, availableStatus);
                                }

                                var registerMethod = unifiedServiceManagerType.GetMethod("RegisterServiceAsync");
                                if (registerMethod != null)
                                {
                                    var registerTask = registerMethod.Invoke(managerInstance, new[] { unifiedService }) as Task<OASISResult<bool>>;
                                    if (registerTask != null)
                                    {
                                        var registerResult = await registerTask;
                                        if (registerResult.IsError)
                                        {
                                            LoggingManager.Log($"Warning: Failed to register OpenSERV agent with ONET Service Registry: {registerResult.Message}", Logging.LogType.Warning);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail if ONET Service Registry registration fails
                    LoggingManager.Log($"Warning: Could not register OpenSERV agent with ONET Service Registry: {ex.Message}", Logging.LogType.Warning);
                }

                // 4. Also register with ONET Unified Architecture for backward compatibility
                try
                {
                    var onetType = Type.GetType("NextGenSoftware.OASIS.API.ONODE.Core.Network.ONETUnifiedArchitecture, NextGenSoftware.OASIS.API.ONODE.Core");
                    if (onetType != null)
                    {
                        var instanceProperty = onetType.GetProperty("Instance");
                        if (instanceProperty != null)
                        {
                            var onetInstance = instanceProperty.GetValue(null);
                            var unifiedServiceType = Type.GetType("NextGenSoftware.OASIS.API.ONODE.Core.Network.UnifiedService, NextGenSoftware.OASIS.API.ONODE.Core");
                            
                            if (unifiedServiceType != null)
                            {
                                var unifiedService = Activator.CreateInstance(unifiedServiceType);
                                unifiedServiceType.GetProperty("ServiceId")?.SetValue(unifiedService, avatar.Id.ToString());
                                unifiedServiceType.GetProperty("ServiceName")?.SetValue(unifiedService, $"OpenSERV Agent: {openServAgentId}");
                                unifiedServiceType.GetProperty("ServiceType")?.SetValue(unifiedService, "OpenSERV_AI_Agent");
                                unifiedServiceType.GetProperty("Endpoint")?.SetValue(unifiedService, openServEndpoint);
                                unifiedServiceType.GetProperty("Protocol")?.SetValue(unifiedService, "OpenSERV_HTTP");
                                unifiedServiceType.GetProperty("Description")?.SetValue(unifiedService, a2aCapabilities.Description);
                                
                                var metadata = new Dictionary<string, object>
                                {
                                    ["openserv_agent_id"] = openServAgentId,
                                    ["a2a_agent_id"] = avatar.Id,
                                    ["capabilities"] = capabilities
                                };
                                unifiedServiceType.GetProperty("Metadata")?.SetValue(unifiedService, metadata);
                                unifiedServiceType.GetProperty("Capabilities")?.SetValue(unifiedService, capabilities);
                                unifiedServiceType.GetProperty("IsActive")?.SetValue(unifiedService, true);

                                var registerMethod = onetType.GetMethod("RegisterUnifiedServiceAsync");
                                if (registerMethod != null)
                                {
                                    var registerTask = registerMethod.Invoke(onetInstance, new[] { unifiedService }) as Task<OASISResult<bool>>;
                                    if (registerTask != null)
                                    {
                                        var registerResult = await registerTask;
                                        if (registerResult.IsError)
                                        {
                                            LoggingManager.Log($"Warning: Failed to register OpenSERV agent with ONET Unified Architecture: {registerResult.Message}", Logging.LogType.Warning);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail if ONET Unified Architecture registration fails
                    LoggingManager.Log($"Warning: Could not register OpenSERV agent with ONET Unified Architecture: {ex.Message}", Logging.LogType.Warning);
                }

                result.Result = true;
                result.Message = $"OpenSERV agent {openServAgentId} registered successfully as A2A agent {avatar.Id}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error registering OpenSERV agent: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Execute an AI workflow via OpenSERV, with A2A messaging
        /// </summary>
        /// <param name="fromAgentId">Source agent ID</param>
        /// <param name="toAgentId">Target OpenSERV agent ID</param>
        /// <param name="workflowRequest">Workflow request content</param>
        /// <param name="workflowParameters">Additional workflow parameters (optional)</param>
        /// <returns>OASISResult with workflow execution result</returns>
        public async Task<OASISResult<string>> ExecuteAIWorkflowAsync(
            Guid fromAgentId,
            Guid toAgentId,
            string workflowRequest,
            Dictionary<string, object> workflowParameters = null)
        {
            var result = new OASISResult<string>();
            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(workflowRequest))
                {
                    OASISErrorHandling.HandleError(ref result, "WorkflowRequest is required");
                    return result;
                }

                // 1. Send workflow request via A2A Protocol
                var a2aMessageResult = await SendServiceRequestAsync(
                    fromAgentId: fromAgentId,
                    toAgentId: toAgentId,
                    serviceName: "ai-workflow",
                    serviceParameters: new Dictionary<string, object>
                    {
                        ["workflow_request"] = workflowRequest,
                        ["parameters"] = workflowParameters ?? new Dictionary<string, object>()
                    }
                );

                if (a2aMessageResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"Failed to send A2A service request: {a2aMessageResult.Message}");
                    return result;
                }

                // 2. Get agent card to find OpenSERV endpoint
                var agentCardResult = await AgentManager.Instance.GetAgentCardAsync(toAgentId);
                
                if (agentCardResult.IsError || agentCardResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"Failed to retrieve agent card for {toAgentId}: {agentCardResult.Message}");
                    return result;
                }

                var agentCard = agentCardResult.Result;

                // 3. Extract OpenSERV endpoint and API key from metadata
                if (!agentCard.Metadata.ContainsKey("openserv_endpoint"))
                {
                    OASISErrorHandling.HandleError(ref result, 
                        "OpenSERV endpoint not found in agent metadata. Agent may not be an OpenSERV agent.");
                    return result;
                }

                var openServEndpoint = agentCard.Metadata["openserv_endpoint"]?.ToString();
                var openServApiKey = agentCard.Metadata.ContainsKey("openserv_api_key") 
                    ? agentCard.Metadata["openserv_api_key"]?.ToString() 
                    : null;

                if (string.IsNullOrEmpty(openServEndpoint))
                {
                    OASISErrorHandling.HandleError(ref result, "OpenSERV endpoint is empty");
                    return result;
                }

                // 4. Call OpenSERV agent via HTTP
                try
                {
                    var openServRequest = new OpenServWorkflowRequest
                    {
                        WorkflowRequest = workflowRequest,
                        AgentId = agentCard.Metadata.ContainsKey("openserv_agent_id") 
                            ? agentCard.Metadata["openserv_agent_id"]?.ToString() 
                            : toAgentId.ToString(),
                        Endpoint = openServEndpoint,
                        ApiKey = openServApiKey,
                        Parameters = workflowParameters ?? new Dictionary<string, object>()
                    };

                    // Use the bridge service pattern (create a simple HTTP call)
                    var httpClient = OpenServHttpClient;
                    var payload = new
                    {
                        workflow = workflowRequest,
                        parameters = workflowParameters ?? new Dictionary<string, object>()
                    };

                    var httpRequest = new HttpRequestMessage(HttpMethod.Post, openServEndpoint)
                    {
                        Content = JsonContent.Create(payload)
                    };

                    if (!string.IsNullOrEmpty(openServApiKey))
                    {
                        httpRequest.Headers.Add("Authorization", $"Bearer {openServApiKey}");
                    }

                    var httpResponse = await httpClient.SendAsync(httpRequest);
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            $"OpenSERV workflow execution failed with status {httpResponse.StatusCode}: {responseContent}");
                        return result;
                    }

                    // 5. Send result back via A2A Protocol
                    var responseMessage = new A2AMessage
                    {
                        MessageId = Guid.NewGuid(),
                        FromAgentId = toAgentId,
                        ToAgentId = fromAgentId,
                        MessageType = A2AMessageType.TaskCompletion,
                        Content = $"Workflow completed: {responseContent}",
                        ResponseToMessageId = a2aMessageResult.Result?.MessageId,
                        Timestamp = DateTime.UtcNow,
                        Priority = MessagePriority.Normal,
                        Payload = new Dictionary<string, object>
                        {
                            ["workflow_result"] = responseContent,
                            ["workflow_request"] = workflowRequest
                        }
                    };

                    await SendA2AMessageAsync(responseMessage);

                    result.Result = responseContent;
                    result.Message = "OpenSERV workflow executed successfully";
                }
                catch (HttpRequestException ex)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"HTTP error calling OpenSERV endpoint: {ex.Message}", ex);
                    return result;
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"Error executing OpenSERV workflow: {ex.Message}", ex);
                    return result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, 
                    $"Error in ExecuteAIWorkflowAsync: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Register an OASIS A2A agent with OpenSERV platform for bidirectional discovery
        /// </summary>
        /// <param name="agentId">OASIS A2A agent ID</param>
        /// <param name="openServApiKey">OpenSERV API key for registration</param>
        /// <param name="oasisAgentEndpoint">OASIS agent endpoint (e.g., https://api.oasisweb4.com/api/a2a/agent-card/{agentId})</param>
        /// <returns>OASISResult with OpenSERV registration status</returns>
        public async Task<OASISResult<string>> RegisterOasisAgentWithOpenServAsync(
            Guid agentId,
            string openServApiKey,
            string oasisAgentEndpoint = null)
        {
            var result = new OASISResult<string>();
            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(openServApiKey))
                {
                    OASISErrorHandling.HandleError(ref result, "OpenServApiKey is required");
                    return result;
                }

                // Get agent card for capabilities
                var agentCardResult = await AgentManager.Instance.GetAgentCardAsync(agentId);
                if (agentCardResult.IsError || agentCardResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"Failed to retrieve agent card for {agentId}: {agentCardResult.Message}");
                    return result;
                }

                var agentCard = agentCardResult.Result;

                // Build OASIS agent endpoint if not provided
                if (string.IsNullOrEmpty(oasisAgentEndpoint))
                {
                    // Default to OASIS API endpoint
                    var baseUrl = ProviderManager.Instance.OASISDNA?.OASIS?.OASISAPIURL ?? "https://api.oasisweb4.com";
                    oasisAgentEndpoint = $"{baseUrl}/api/a2a/agent-card/{agentId}";
                }

                // Get description from agent card metadata or use default
                var description = agentCard.Metadata?.ContainsKey("description") == true 
                    ? agentCard.Metadata["description"]?.ToString() 
                    : $"OASIS A2A Agent: {agentCard.Name}";

                // Prepare registration payload for OpenSERV
                var payload = new
                {
                    agent_id = $"oasis-{agentId}",
                    name = agentCard.Name ?? $"OASIS Agent {agentId}",
                    description = description,
                    endpoint = oasisAgentEndpoint,
                    capabilities = agentCard.Capabilities?.Services ?? new List<string>(),
                    metadata = new Dictionary<string, object>
                    {
                        ["oasis_agent_id"] = agentId.ToString(),
                        ["a2a_protocol"] = "jsonrpc2.0",
                        ["skills"] = agentCard.Capabilities?.Skills ?? new List<string>(),
                        ["version"] = agentCard.Version ?? "1.0"
                    }
                };

                // Register with OpenSERV platform
                var httpClient = OpenServHttpClient;
                var openServBaseUrl = "https://api.openserv.ai";
                var registerUrl = $"{openServBaseUrl}/api/v1/agents/register";

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, registerUrl)
                {
                    Content = JsonContent.Create(payload)
                };

                httpRequest.Headers.Add("Authorization", $"Bearer {openServApiKey}");
                httpRequest.Headers.Add("Content-Type", "application/json");

                var httpResponse = await httpClient.SendAsync(httpRequest);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"OpenSERV registration failed with status {httpResponse.StatusCode}: {responseContent}");
                    return result;
                }

                // Parse response to get OpenSERV agent ID
                try
                {
                    var response = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    var openServAgentId = response?.ContainsKey("agent_id") == true 
                        ? response["agent_id"]?.ToString() 
                        : $"oasis-{agentId}";

                    // Store OpenSERV registration info in agent card metadata
                    if (agentCard.Metadata == null)
                        agentCard.Metadata = new Dictionary<string, object>();

                    agentCard.Metadata["openserv_registered_agent_id"] = openServAgentId;
                    agentCard.Metadata["openserv_registration_endpoint"] = registerUrl;
                    agentCard.Metadata["openserv_registered_at"] = DateTime.UtcNow.ToString("O");

                    // Also store in IAgentCapabilities if available
                    var capabilitiesResult = await AgentManager.Instance.GetAgentCapabilitiesAsync(agentId);
                    if (!capabilitiesResult.IsError && capabilitiesResult.Result != null)
                    {
                        var capabilities = capabilitiesResult.Result;
                        if (capabilities.Metadata == null)
                            capabilities.Metadata = new Dictionary<string, object>();

                        capabilities.Metadata["openserv_registered_agent_id"] = openServAgentId;
                        capabilities.Metadata["openserv_registration_endpoint"] = registerUrl;
                        capabilities.Metadata["openserv_registered_at"] = DateTime.UtcNow.ToString("O");

                        await AgentManager.Instance.RegisterAgentCapabilitiesAsync(agentId, capabilities);
                    }

                    result.Result = openServAgentId;
                    result.Message = $"OASIS agent {agentId} registered with OpenSERV platform as {openServAgentId}";
                }
                catch (Exception ex)
                {
                    // Registration may have succeeded even if parsing fails
                    result.Result = $"oasis-{agentId}";
                    result.Message = $"OASIS agent registered with OpenSERV (response parsing failed: {ex.Message})";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, 
                    $"Error registering OASIS agent with OpenSERV: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Discover agents from OpenSERV platform registry (bidirectional discovery)
        /// </summary>
        /// <param name="openServApiKey">OpenSERV API key for authentication</param>
        /// <param name="capability">Optional capability filter</param>
        /// <returns>OASISResult with list of discovered agents from OpenSERV</returns>
        public async Task<OASISResult<List<IAgentCard>>> DiscoverAgentsFromOpenServAsync(
            string openServApiKey,
            string capability = null)
        {
            var result = new OASISResult<List<IAgentCard>>();
            try
            {
                if (string.IsNullOrEmpty(openServApiKey))
                {
                    OASISErrorHandling.HandleError(ref result, "OpenServApiKey is required");
                    return result;
                }

                // Query OpenSERV platform for agents
                var httpClient = OpenServHttpClient;
                var openServBaseUrl = "https://api.openserv.ai";
                var discoverUrl = string.IsNullOrEmpty(capability)
                    ? $"{openServBaseUrl}/api/v1/agents"
                    : $"{openServBaseUrl}/api/v1/agents?capability={capability}";

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, discoverUrl);
                httpRequest.Headers.Add("Authorization", $"Bearer {openServApiKey}");

                var httpResponse = await httpClient.SendAsync(httpRequest);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"OpenSERV discovery failed with status {httpResponse.StatusCode}: {responseContent}");
                    return result;
                }

                // Parse OpenSERV agents and convert to A2A Agent Cards
                var agentCards = new List<IAgentCard>();
                try
                {
                    var openServAgents = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(responseContent);
                    
                    if (openServAgents != null)
                    {
                        foreach (var openServAgent in openServAgents)
                        {
                            try
                            {
                                // Extract OpenSERV agent info
                                var agentId = openServAgent.ContainsKey("agent_id") 
                                    ? openServAgent["agent_id"]?.ToString() 
                                    : null;
                                var name = openServAgent.ContainsKey("name") 
                                    ? openServAgent["name"]?.ToString() 
                                    : "Unknown Agent";
                                var description = openServAgent.ContainsKey("description") 
                                    ? openServAgent["description"]?.ToString() 
                                    : "";
                                var endpoint = openServAgent.ContainsKey("endpoint") 
                                    ? openServAgent["endpoint"]?.ToString() 
                                    : "";
                                var capabilities = openServAgent.ContainsKey("capabilities") 
                                    ? openServAgent["capabilities"] as List<object> 
                                    : new List<object>();
                                var metadata = openServAgent.ContainsKey("metadata") 
                                    ? openServAgent["metadata"] as Dictionary<string, object> 
                                    : new Dictionary<string, object>();

                                // Check if this is an OASIS agent (has oasis_agent_id in metadata)
                                var oasisAgentId = metadata?.ContainsKey("oasis_agent_id") == true
                                    ? metadata["oasis_agent_id"]?.ToString()
                                    : null;

                                if (!string.IsNullOrEmpty(oasisAgentId) && Guid.TryParse(oasisAgentId, out var oasisGuid))
                                {
                                    // This is an OASIS agent registered with OpenSERV - get full agent card
                                    var agentCardResult = await AgentManager.Instance.GetAgentCardAsync(oasisGuid);
                                    if (!agentCardResult.IsError && agentCardResult.Result != null)
                                    {
                                        agentCards.Add(agentCardResult.Result);
                                    }
                                }
                                else
                                {
                                    // This is a native OpenSERV agent - create agent card representation
                                    var agentCardType = Type.GetType("NextGenSoftware.OASIS.API.Core.Interfaces.Agent.IAgentCard, NextGenSoftware.OASIS.API.Core");
                                    if (agentCardType != null)
                                    {
                                        // Create a simplified agent card for OpenSERV agents
                                        // Note: This would need proper implementation based on IAgentCard interface
                                        // For now, we'll log and continue
                                        LoggingManager.Log($"OpenSERV native agent found: {name} ({agentId}) - requires full agent card implementation", Logging.LogType.Info);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LoggingManager.Log($"Error processing OpenSERV agent: {ex.Message}", Logging.LogType.Warning);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"Error parsing OpenSERV discovery response: {ex.Message}", ex);
                    return result;
                }

                result.Result = agentCards;
                result.Message = string.IsNullOrEmpty(capability)
                    ? $"Found {agentCards.Count} agents from OpenSERV platform"
                    : $"Found {agentCards.Count} agents with capability '{capability}' from OpenSERV platform";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, 
                    $"Error discovering agents from OpenSERV: {ex.Message}", ex);
            }

            return result;
        }
    }
}

