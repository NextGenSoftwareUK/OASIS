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
        /// <returns>OASISResult with registration status</returns>
        public async Task<OASISResult<bool>> RegisterOpenServAgentAsync(
            string openServAgentId,
            string openServEndpoint,
            List<string> capabilities,
            string apiKey = null,
            string username = null,
            string email = null,
            string password = null)
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

                // 1. Create A2A agent avatar
                var avatarUsername = username ?? $"openserv_{openServAgentId}";
                var avatarEmail = email ?? $"{avatarUsername}@openserv.agent";
                var avatarPassword = password ?? Guid.NewGuid().ToString("N")[..16]; // Generate secure password

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

                var avatar = avatarResult.Result;

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

                var capabilitiesResult = await AgentManager.Instance
                    .RegisterAgentCapabilitiesAsync(avatar.Id, a2aCapabilities);

                if (capabilitiesResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"Failed to register agent capabilities: {capabilitiesResult.Message}");
                    return result;
                }

                // 3. Register as UnifiedService in SERV infrastructure
                // Note: This requires ONETUnifiedArchitecture from ONODE.Core assembly
                // If not available, this step will be skipped
                try
                {
                    // Use reflection to access ONETUnifiedArchitecture if available
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
                                            LoggingManager.Log($"Warning: Failed to register OpenSERV agent with SERV infrastructure: {registerResult.Message}", Logging.LogType.Warning);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail if SERV registration fails (optional integration)
                    LoggingManager.Log($"Warning: Could not register OpenSERV agent with SERV infrastructure: {ex.Message}", Logging.LogType.Warning);
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
    }
}

