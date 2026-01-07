using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers; // For extension methods (NFT, SERV)
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// A2A (Agent-to-Agent) Protocol API Controller
    /// Implements JSON-RPC 2.0 over HTTP(S) for agent communication
    /// Based on: https://github.com/a2aproject/A2A
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class A2AController : OASISControllerBase
    {
        /// <summary>
        /// JSON-RPC 2.0 endpoint - Main A2A Protocol endpoint
        /// </summary>
        /// <remarks>
        /// This is the primary endpoint for Agent-to-Agent communication using JSON-RPC 2.0 protocol.
        /// All A2A protocol methods (ping, service_request, capability_query, etc.) are sent through this endpoint.
        /// 
        /// **Authentication Required:** Yes (Bearer Token)
        /// 
        /// **Agent Type Required:** The authenticated avatar must be of type `Agent`
        /// 
        /// **Supported Methods:**
        /// - `ping` - Health check
        /// - `capability_query` - Query agent capabilities
        /// - `service_request` - Request a service from another agent
        /// - `task_delegation` - Delegate a task to another agent
        /// - `payment_request` - Request payment from another agent
        /// - And more...
        /// 
        /// **Example Request:**
        /// ```json
        /// {
        ///   "jsonrpc": "2.0",
        ///   "method": "ping",
        ///   "id": "request-123"
        /// }
        /// ```
        /// 
        /// **Example Response:**
        /// ```json
        /// {
        ///   "jsonrpc": "2.0",
        ///   "result": {
        ///     "status": "pong",
        ///     "timestamp": "2026-01-03T12:00:00Z"
        ///   },
        ///   "id": "request-123"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">JSON-RPC 2.0 request object</param>
        /// <returns>JSON-RPC 2.0 response</returns>
        /// <response code="200">Success - Returns JSON-RPC 2.0 response</response>
        /// <response code="400">Bad Request - Invalid request or avatar not an Agent type</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("jsonrpc")]
        [ProducesResponseType(typeof(JsonRpc2Response), 200)]
        [ProducesResponseType(typeof(JsonRpc2Response), 400)]
        [ProducesResponseType(typeof(JsonRpc2Response), 401)]
        [ProducesResponseType(typeof(JsonRpc2Response), 500)]
        public async Task<IActionResult> JsonRpc([FromBody] JsonRpc2Request request)
        {
            try
            {
                // Get authenticated avatar (agent)
                if (Avatar == null || Avatar.Id == Guid.Empty)
                {
                    return Unauthorized(new JsonRpc2Response
                    {
                        JsonRpc = "2.0",
                        Id = request?.Id ?? "unknown",
                        Error = new JsonRpc2Error
                        {
                            Code = JsonRpc2ErrorCodes.InvalidRequest,
                            Message = "Authentication required"
                        }
                    });
                }

                // Verify avatar is an Agent type
                if (Avatar.AvatarType.Value != AvatarType.Agent)
                {
                    return BadRequest(new JsonRpc2Response
                    {
                        JsonRpc = "2.0",
                        Id = request?.Id ?? "unknown",
                        Error = new JsonRpc2Error
                        {
                            Code = JsonRpc2ErrorCodes.InvalidRequest,
                            Message = "Avatar must be of type Agent"
                        }
                    });
                }

                // Process JSON-RPC 2.0 request
                var response = await A2AManager.Instance.ProcessJsonRpc2RequestAsync(request, Avatar.Id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new JsonRpc2Response
                {
                    JsonRpc = "2.0",
                    Id = request?.Id ?? "unknown",
                    Error = new JsonRpc2Error
                    {
                        Code = JsonRpc2ErrorCodes.InternalError,
                        Message = $"Internal error: {ex.Message}"
                    }
                });
            }
        }

        /// <summary>
        /// Get Agent Card for an agent (Official A2A Protocol)
        /// </summary>
        /// <remarks>
        /// Retrieves the Agent Card for a specific agent. Agent Cards contain:
        /// - Agent identification (ID, name, version)
        /// - Capabilities (services, skills)
        /// - Connection information (endpoint, protocol, auth)
        /// - Metadata (pricing, status, reputation)
        /// 
        /// **Authentication Required:** No (Public endpoint)
        /// 
        /// **Example Response:**
        /// ```json
        /// {
        ///   "agent_id": "123e4567-e89b-12d3-a456-426614174000",
        ///   "name": "data_analyst_agent",
        ///   "version": "1.0.0",
        ///   "capabilities": {
        ///     "services": ["data-analysis", "report-generation"],
        ///     "skills": ["Python", "Machine Learning"]
        ///   },
        ///   "connection": {
        ///     "endpoint": "https://api.oasisplatform.world/api/a2a/jsonrpc",
        ///     "protocol": "jsonrpc2.0",
        ///     "auth": {
        ///       "scheme": "bearer"
        ///     }
        ///   },
        ///   "metadata": {
        ///     "status": "Available",
        ///     "reputation_score": 4.8
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="agentId">The unique identifier (GUID) of the agent</param>
        /// <returns>Agent Card object</returns>
        /// <response code="200">Success - Returns Agent Card</response>
        /// <response code="404">Not Found - Agent not found</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("agent-card/{agentId}")]
        [ProducesResponseType(typeof(IAgentCard), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAgentCard(Guid agentId)
        {
            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                var result = await AgentManager.Instance.GetAgentCardAsync(agentId, baseUrl);

                if (result.IsError)
                {
                    return NotFound(new { error = result.Message });
                }

                return Ok(result.Result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get Agent Card for authenticated agent
        /// </summary>
        /// <remarks>
        /// Retrieves the Agent Card for the currently authenticated agent.
        /// This is a convenience endpoint that uses the authenticated avatar's ID.
        /// 
        /// **Authentication Required:** Yes (Bearer Token)
        /// 
        /// **Agent Type Required:** The authenticated avatar must be of type `Agent`
        /// </remarks>
        /// <returns>Agent Card object</returns>
        /// <response code="200">Success - Returns Agent Card</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="404">Not Found - Agent not found</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("agent-card")]
        [Authorize]
        [ProducesResponseType(typeof(IAgentCard), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetMyAgentCard()
        {
            try
            {
                if (Avatar == null || Avatar.Id == Guid.Empty)
                {
                    return Unauthorized(new { error = "Authentication required" });
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                var result = await AgentManager.Instance.GetAgentCardAsync(Avatar.Id, baseUrl);

                if (result.IsError)
                {
                    return NotFound(new { error = result.Message });
                }

                return Ok(result.Result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// List all available agents (Agent Cards)
        /// </summary>
        /// <remarks>
        /// Retrieves a list of all available agents and their Agent Cards.
        /// This endpoint is useful for agent discovery.
        /// 
        /// **Authentication Required:** No (Public endpoint)
        /// 
        /// **Returns:** Array of Agent Card objects
        /// </remarks>
        /// <returns>List of Agent Cards</returns>
        /// <response code="200">Success - Returns list of Agent Cards</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("agents")]
        [ProducesResponseType(typeof(List<IAgentCard>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAgents()
        {
            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                var result = await AgentManager.Instance.GetAllAgentCardsAsync(baseUrl);

                if (result.IsError)
                {
                    return StatusCode(500, new { error = result.Message });
                }

                return Ok(result.Result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Find agents by service
        /// </summary>
        /// <remarks>
        /// Finds all agents that provide a specific service.
        /// This is useful for service discovery in the A2A network.
        /// 
        /// **Authentication Required:** No (Public endpoint)
        /// 
        /// **Example:** `/api/a2a/agents/by-service/data-analysis` finds all agents that provide "data-analysis" service
        /// 
        /// **Returns:** Array of Agent Card objects that provide the specified service
        /// </remarks>
        /// <param name="serviceName">The name of the service to search for (e.g., "data-analysis", "payment-processing")</param>
        /// <returns>List of Agent Cards that provide the service</returns>
        /// <response code="200">Success - Returns list of Agent Cards</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("agents/by-service/{serviceName}")]
        [ProducesResponseType(typeof(List<IAgentCard>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAgentsByService(string serviceName)
        {
            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                var result = await AgentManager.Instance.GetAgentCardsByServiceAsync(serviceName, baseUrl);

                if (result.IsError)
                {
                    return StatusCode(500, new { error = result.Message });
                }

                return Ok(result.Result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Register agent capabilities
        /// </summary>
        /// <remarks>
        /// Registers or updates the capabilities of the authenticated agent.
        /// Capabilities include services offered, skills, pricing, and status.
        /// 
        /// **Authentication Required:** Yes (Bearer Token)
        /// 
        /// **Agent Type Required:** The authenticated avatar must be of type `Agent`
        /// 
        /// **Example Request:**
        /// ```json
        /// {
        ///   "services": ["data-analysis", "report-generation"],
        ///   "pricing": {
        ///     "data-analysis": 0.1,
        ///     "report-generation": 0.05
        ///   },
        ///   "skills": ["Python", "Machine Learning", "Data Science"],
        ///   "status": "Available",
        ///   "max_concurrent_tasks": 3,
        ///   "description": "Data analysis and reporting agent"
        /// }
        /// ```
        /// </remarks>
        /// <param name="capabilities">Agent capabilities object</param>
        /// <returns>Success response</returns>
        /// <response code="200">Success - Capabilities registered</response>
        /// <response code="400">Bad Request - Invalid capabilities or avatar not an Agent type</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("agent/capabilities")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> RegisterCapabilities([FromBody] AgentCapabilities capabilities)
        {
            try
            {
                if (Avatar == null || Avatar.Id == Guid.Empty)
                {
                    return Unauthorized(new { error = "Authentication required" });
                }

                if (Avatar.AvatarType.Value != AvatarType.Agent)
                {
                    return BadRequest(new { error = "Avatar must be of type Agent" });
                }

                var result = await AgentManager.Instance.RegisterAgentCapabilitiesAsync(Avatar.Id, capabilities);

                if (result.IsError)
                {
                    return BadRequest(new { error = result.Message });
                }

                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get pending A2A messages for authenticated agent
        /// </summary>
        /// <remarks>
        /// Retrieves all pending A2A messages for the authenticated agent.
        /// Messages are returned in chronological order (oldest first).
        /// 
        /// **Authentication Required:** Yes (Bearer Token)
        /// 
        /// **Returns:** Array of A2A message objects with status "Sent" or "Delivered"
        /// </remarks>
        /// <returns>List of pending A2A messages</returns>
        /// <response code="200">Success - Returns list of pending messages</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("messages")]
        [Authorize]
        [ProducesResponseType(typeof(List<IA2AMessage>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPendingMessages()
        {
            try
            {
                if (Avatar == null || Avatar.Id == Guid.Empty)
                {
                    return Unauthorized(new { error = "Authentication required" });
                }

                var result = await A2AManager.Instance.GetPendingMessagesAsync(Avatar.Id);

                if (result.IsError)
                {
                    return StatusCode(500, new { error = result.Message });
                }

                return Ok(result.Result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Mark message as processed
        /// </summary>
        /// <remarks>
        /// Marks a specific A2A message as processed.
        /// This should be called after an agent has successfully handled a message.
        /// 
        /// **Authentication Required:** Yes (Bearer Token)
        /// 
        /// **Note:** The message must belong to the authenticated agent's message queue.
        /// </remarks>
        /// <param name="messageId">The unique identifier (GUID) of the message to mark as processed</param>
        /// <returns>Success response</returns>
        /// <response code="200">Success - Message marked as processed</response>
        /// <response code="400">Bad Request - Message not found or invalid</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("messages/{messageId}/process")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> MarkMessageProcessed(Guid messageId)
        {
            try
            {
                if (Avatar == null || Avatar.Id == Guid.Empty)
                {
                    return Unauthorized(new { error = "Authentication required" });
                }

                var result = await A2AManager.Instance.MarkMessageProcessedAsync(Avatar.Id, messageId);

                if (result.IsError)
                {
                    return BadRequest(new { error = result.Message });
                }

                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Register A2A agent as SERV service
        /// </summary>
        /// <remarks>
        /// Registers the authenticated A2A agent as a UnifiedService in SERV infrastructure (ONET Unified Architecture).
        /// This makes the agent discoverable via SERV service discovery mechanisms.
        /// 
        /// **Authentication Required:** Yes (Bearer Token)
        /// 
        /// **Agent Type Required:** The authenticated avatar must be of type `Agent`
        /// 
        /// **Note:** The agent must have registered capabilities first using `/api/a2a/agent/capabilities`
        /// 
        /// **Example Response:**
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Agent registered as SERV service successfully"
        /// }
        /// ```
        /// </remarks>
        /// <returns>Success response</returns>
        /// <response code="200">Success - Agent registered as SERV service</response>
        /// <response code="400">Bad Request - Invalid request or agent capabilities not found</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("agent/register-service")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> RegisterAgentAsService()
        {
            try
            {
                if (Avatar == null || Avatar.Id == Guid.Empty)
                {
                    return Unauthorized(new { error = "Authentication required" });
                }

                if (Avatar.AvatarType.Value != AvatarType.Agent)
                {
                    return BadRequest(new { error = "Avatar must be of type Agent" });
                }

                // Get agent card to retrieve capabilities
                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                var agentCardResult = await AgentManager.Instance.GetAgentCardAsync(Avatar.Id, baseUrl);

                if (agentCardResult.IsError || agentCardResult.Result == null)
                {
                    return BadRequest(new { error = "Agent card not found. Please register agent capabilities first." });
                }

                var agentCard = agentCardResult.Result;

                // Convert agent card capabilities to IAgentCapabilities
                var capabilities = new AgentCapabilities
                {
                    Services = agentCard.Capabilities?.Services ?? new List<string>(),
                    Skills = agentCard.Capabilities?.Skills ?? new List<string>(),
                    Description = agentCard.Metadata.ContainsKey("description") 
                        ? agentCard.Metadata["description"].ToString() 
                        : agentCard.Name,
                    Status = agentCard.Metadata.ContainsKey("status") 
                        && Enum.TryParse<AgentStatus>(agentCard.Metadata["status"].ToString(), out var status)
                        ? status 
                        : AgentStatus.Available,
                    Metadata = agentCard.Metadata ?? new Dictionary<string, object>()
                };

                // Register agent as SERV service
                var result = await A2AManager.Instance.RegisterAgentAsServiceAsync(Avatar.Id, capabilities);

                if (result.IsError)
                {
                    return BadRequest(new { error = result.Message });
                }

                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Discover agents via SERV infrastructure
        /// </summary>
        /// <remarks>
        /// Discovers agents via SERV infrastructure (ONET Unified Architecture), enriched with A2A Agent Cards.
        /// This endpoint queries the SERV service registry for A2A agents and returns their Agent Cards.
        /// 
        /// **Authentication Required:** No (Public endpoint)
        /// 
        /// **Query Parameters:**
        /// - `service` (optional): Filter agents by service name (e.g., "data-analysis", "payment-processing")
        /// 
        /// **Example:** `/api/a2a/agents/discover-serv?service=data-analysis`
        /// 
        /// **Example Response:**
        /// ```json
        /// [
        ///   {
        ///     "agent_id": "123e4567-e89b-12d3-a456-426614174000",
        ///     "name": "data_analyst_agent",
        ///     "capabilities": {
        ///       "services": ["data-analysis", "report-generation"]
        ///     },
        ///     "connection": {
        ///       "endpoint": "https://api.oasisplatform.world/api/a2a/jsonrpc"
        ///     }
        ///   }
        /// ]
        /// ```
        /// </remarks>
        /// <param name="service">Optional service name to filter agents</param>
        /// <returns>List of Agent Cards discovered via SERV</returns>
        /// <response code="200">Success - Returns list of Agent Cards</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("agents/discover-serv")]
        [ProducesResponseType(typeof(List<IAgentCard>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DiscoverAgentsViaSERV([FromQuery] string service = null)
        {
            try
            {
                var result = await A2AManager.Instance.DiscoverAgentsViaSERVAsync(service);

                if (result.IsError)
                {
                    return StatusCode(500, new { error = result.Message });
                }

                return Ok(result.Result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Register OpenSERV agent
        /// </summary>
        /// <remarks>
        /// Registers an OpenSERV AI agent as an A2A agent and SERV service.
        /// This creates an avatar, registers A2A capabilities, and registers the agent with SERV infrastructure.
        /// 
        /// **Authentication Required:** No (Public endpoint, but requires valid OpenSERV credentials)
        /// 
        /// **Example Request:**
        /// ```json
        /// {
        ///   "openServAgentId": "agent-123",
        ///   "openServEndpoint": "https://api.openserv.ai/agents/agent-123",
        ///   "capabilities": ["data-analysis", "nlp", "image-generation"],
        ///   "apiKey": "sk-...",
        ///   "username": "openserv_agent_123",
        ///   "email": "agent123@openserv.ai",
        ///   "password": "secure-password"
        /// }
        /// ```
        /// 
        /// **Example Response:**
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "OpenSERV agent agent-123 registered successfully as A2A agent 123e4567-e89b-12d3-a456-426614174000",
        ///   "agentId": "123e4567-e89b-12d3-a456-426614174000"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">OpenSERV agent registration request</param>
        /// <returns>Success response with agent ID</returns>
        /// <response code="200">Success - OpenSERV agent registered</response>
        /// <response code="400">Bad Request - Invalid request parameters</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("openserv/register")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> RegisterOpenServAgent([FromBody] RegisterOpenServAgentRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Request body is required" });
                }

                if (string.IsNullOrEmpty(request.OpenServAgentId))
                {
                    return BadRequest(new { error = "OpenServAgentId is required" });
                }

                if (string.IsNullOrEmpty(request.OpenServEndpoint))
                {
                    return BadRequest(new { error = "OpenServEndpoint is required" });
                }

                if (request.Capabilities == null || request.Capabilities.Count == 0)
                {
                    return BadRequest(new { error = "At least one capability is required" });
                }

                var result = await A2AManager.Instance.RegisterOpenServAgentAsync(
                    request.OpenServAgentId,
                    request.OpenServEndpoint,
                    request.Capabilities,
                    request.ApiKey,
                    request.Username,
                    request.Email,
                    request.Password
                );

                if (result.IsError)
                {
                    return BadRequest(new { error = result.Message });
                }

                // Extract agent ID from message if possible (optional enhancement)
                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Execute AI workflow via OpenSERV
        /// </summary>
        /// <remarks>
        /// Executes an AI workflow via OpenSERV agent, with A2A messaging integration.
        /// This endpoint routes workflow requests through the A2A Protocol to OpenSERV agents.
        /// 
        /// **Authentication Required:** Yes (Bearer Token)
        /// 
        /// **Agent Type Required:** The authenticated avatar must be of type `Agent`
        /// 
        /// **Example Request:**
        /// ```json
        /// {
        ///   "toAgentId": "123e4567-e89b-12d3-a456-426614174000",
        ///   "workflowRequest": "Analyze the sales data and generate a report",
        ///   "workflowParameters": {
        ///     "data_source": "sales_data.csv",
        ///     "report_format": "pdf"
        ///   }
        /// }
        /// ```
        /// 
        /// **Example Response:**
        /// ```json
        /// {
        ///   "success": true,
        ///   "result": "Workflow completed: Analysis complete. Report generated.",
        ///   "message": "OpenSERV workflow executed successfully"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Workflow execution request</param>
        /// <returns>Workflow execution result</returns>
        /// <response code="200">Success - Workflow executed</response>
        /// <response code="400">Bad Request - Invalid request or agent not found</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("workflow/execute")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ExecuteAIWorkflow([FromBody] ExecuteWorkflowRequest request)
        {
            try
            {
                if (Avatar == null || Avatar.Id == Guid.Empty)
                {
                    return Unauthorized(new { error = "Authentication required" });
                }

                if (Avatar.AvatarType.Value != AvatarType.Agent)
                {
                    return BadRequest(new { error = "Avatar must be of type Agent" });
                }

                if (request == null)
                {
                    return BadRequest(new { error = "Request body is required" });
                }

                if (string.IsNullOrEmpty(request.WorkflowRequest))
                {
                    return BadRequest(new { error = "WorkflowRequest is required" });
                }

                if (request.ToAgentId == Guid.Empty)
                {
                    return BadRequest(new { error = "ToAgentId is required" });
                }

                var result = await A2AManager.Instance.ExecuteAIWorkflowAsync(
                    Avatar.Id,
                    request.ToAgentId,
                    request.WorkflowRequest,
                    request.WorkflowParameters
                );

                if (result.IsError)
                {
                    return BadRequest(new { error = result.Message });
                }

                return Ok(new { success = true, result = result.Result, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        // ============================================
        // NFT Integration Endpoints
        // ============================================

        /// <summary>
        /// Create a reputation NFT for the authenticated agent
        /// </summary>
        /// <remarks>
        /// Creates a blockchain-verified reputation NFT for the authenticated agent.
        /// The NFT contains metadata about the agent's reputation score and capabilities.
        /// 
        /// **Authentication Required:** Yes (Bearer Token)
        /// 
        /// **Agent Type Required:** The authenticated avatar must be of type `Agent`
        /// </remarks>
        /// <param name="reputationScore">The reputation score (default: retrieved from karma if not provided)</param>
        /// <param name="description">Optional description for the NFT</param>
        /// <param name="imageUrl">Optional image URL for the NFT</param>
        /// <returns>NFT transaction result</returns>
        /// <response code="200">Success - Reputation NFT created</response>
        /// <response code="400">Bad Request - Invalid request or avatar not an Agent type</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("nft/reputation")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateReputationNFT([FromQuery] decimal? reputationScore = null, [FromQuery] string description = null, [FromQuery] string imageUrl = null)
        {
            try
            {
                if (Avatar == null || Avatar.Id == Guid.Empty)
                {
                    return Unauthorized(new { error = "Authentication required" });
                }

                if (Avatar.AvatarType.Value != AvatarType.Agent)
                {
                    return BadRequest(new { error = "Avatar must be of type Agent" });
                }

                // If reputation score not provided, get from karma
                decimal score = reputationScore ?? (await A2AManager.Instance.GetAgentKarmaAsync(Avatar.Id));

                var result = await A2AManager.Instance.CreateAgentReputationNFTAsync(Avatar.Id, score, description, imageUrl);

                if (result.IsError)
                {
                    return BadRequest(new { error = result.Message });
                }

                return Ok(new { success = true, result = result.Result, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Create a service completion certificate NFT
        /// </summary>
        /// <remarks>
        /// Creates a blockchain-verified certificate NFT for completing a service.
        /// 
        /// **Authentication Required:** Yes (Bearer Token)
        /// 
        /// **Agent Type Required:** The authenticated avatar must be of type `Agent`
        /// </remarks>
        /// <param name="request">Service certificate request</param>
        /// <returns>NFT transaction result</returns>
        /// <response code="200">Success - Certificate NFT created</response>
        /// <response code="400">Bad Request - Invalid request</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("nft/service-certificate")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateServiceCertificate([FromBody] CreateServiceCertificateRequest request)
        {
            try
            {
                if (Avatar == null || Avatar.Id == Guid.Empty)
                {
                    return Unauthorized(new { error = "Authentication required" });
                }

                if (Avatar.AvatarType.Value != AvatarType.Agent)
                {
                    return BadRequest(new { error = "Avatar must be of type Agent" });
                }

                if (request == null || string.IsNullOrEmpty(request.ServiceName))
                {
                    return BadRequest(new { error = "ServiceName is required" });
                }

                var result = await A2AManager.Instance.CreateServiceCompletionCertificateAsync(
                    Avatar.Id,
                    request.ServiceName,
                    request.TaskId,
                    request.Description,
                    request.ImageUrl
                );

                if (result.IsError)
                {
                    return BadRequest(new { error = result.Message });
                }

                return Ok(new { success = true, result = result.Result, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        // ============================================
        // Karma Integration Endpoints
        // ============================================

        /// <summary>
        /// Get karma for the authenticated agent
        /// </summary>
        /// <remarks>
        /// Retrieves the current karma score for the authenticated agent.
        /// 
        /// **Authentication Required:** Yes (Bearer Token)
        /// 
        /// **Agent Type Required:** The authenticated avatar must be of type `Agent`
        /// </remarks>
        /// <returns>Karma score</returns>
        /// <response code="200">Success - Returns karma score</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("karma")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetKarma()
        {
            try
            {
                if (Avatar == null || Avatar.Id == Guid.Empty)
                {
                    return Unauthorized(new { error = "Authentication required" });
                }

                if (Avatar.AvatarType.Value != AvatarType.Agent)
                {
                    return BadRequest(new { error = "Avatar must be of type Agent" });
                }

                var karma = await A2AManager.Instance.GetAgentKarmaAsync(Avatar.Id);
                return Ok(new { karma = karma, agentId = Avatar.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Award karma for service completion
        /// </summary>
        /// <remarks>
        /// Awards karma points to an agent for successfully completing a service.
        /// 
        /// **Authentication Required:** Yes (Bearer Token)
        /// 
        /// **Agent Type Required:** The authenticated avatar must be of type `Agent`
        /// </remarks>
        /// <param name="request">Karma award request</param>
        /// <returns>Success response</returns>
        /// <response code="200">Success - Karma awarded</response>
        /// <response code="400">Bad Request - Invalid request</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("karma/award")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AwardKarma([FromBody] AwardKarmaRequest request)
        {
            try
            {
                if (Avatar == null || Avatar.Id == Guid.Empty)
                {
                    return Unauthorized(new { error = "Authentication required" });
                }

                if (Avatar.AvatarType.Value != AvatarType.Agent)
                {
                    return BadRequest(new { error = "Avatar must be of type Agent" });
                }

                if (request == null || string.IsNullOrEmpty(request.ServiceName))
                {
                    return BadRequest(new { error = "ServiceName is required" });
                }

                var result = await A2AManager.Instance.AwardKarmaForServiceCompletionAsync(
                    request.AgentId,
                    request.ServiceName,
                    request.TaskId,
                    request.KarmaAmount
                );

                if (result.IsError)
                {
                    return BadRequest(new { error = result.Message });
                }

                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        // ============================================
        // Task/Mission Integration Endpoints
        // ============================================

        /// <summary>
        /// Delegate a task to another agent
        /// </summary>
        /// <remarks>
        /// Delegates a task to another agent via A2A Protocol.
        /// Creates a task structure and sends it as an A2A message.
        /// 
        /// **Authentication Required:** Yes (Bearer Token)
        /// 
        /// **Agent Type Required:** The authenticated avatar must be of type `Agent`
        /// </remarks>
        /// <param name="request">Task delegation request</param>
        /// <returns>Task information</returns>
        /// <response code="200">Success - Task delegated</response>
        /// <response code="400">Bad Request - Invalid request</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("task/delegate")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DelegateTask([FromBody] DelegateTaskRequest request)
        {
            try
            {
                if (Avatar == null || Avatar.Id == Guid.Empty)
                {
                    return Unauthorized(new { error = "Authentication required" });
                }

                if (Avatar.AvatarType.Value != AvatarType.Agent)
                {
                    return BadRequest(new { error = "Avatar must be of type Agent" });
                }

                if (request == null || string.IsNullOrEmpty(request.TaskName) || request.ToAgentId == Guid.Empty)
                {
                    return BadRequest(new { error = "TaskName and ToAgentId are required" });
                }

                var result = await A2AManager.Instance.DelegateTaskToAgentAsync(
                    Avatar.Id,
                    request.ToAgentId,
                    request.TaskName,
                    request.TaskDescription ?? "",
                    request.TaskParameters,
                    request.RequiredServices
                );

                if (result.IsError)
                {
                    return BadRequest(new { error = result.Message });
                }

                return Ok(new { success = true, task = result.Result, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Complete a delegated task
        /// </summary>
        /// <remarks>
        /// Marks a task as completed and sends a completion notification.
        /// Automatically awards karma for task completion.
        /// 
        /// **Authentication Required:** Yes (Bearer Token)
        /// 
        /// **Agent Type Required:** The authenticated avatar must be of type `Agent`
        /// </remarks>
        /// <param name="request">Task completion request</param>
        /// <returns>Success response</returns>
        /// <response code="200">Success - Task completed</response>
        /// <response code="400">Bad Request - Task not found or invalid</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("task/complete")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CompleteTask([FromBody] CompleteTaskRequest request)
        {
            try
            {
                if (Avatar == null || Avatar.Id == Guid.Empty)
                {
                    return Unauthorized(new { error = "Authentication required" });
                }

                if (Avatar.AvatarType.Value != AvatarType.Agent)
                {
                    return BadRequest(new { error = "Avatar must be of type Agent" });
                }

                if (request == null || request.TaskId == Guid.Empty)
                {
                    return BadRequest(new { error = "TaskId is required" });
                }

                var result = await A2AManager.Instance.CompleteTaskAsync(
                    request.TaskId,
                    request.ResultData,
                    request.CompletionNotes
                );

                if (result.IsError)
                {
                    return BadRequest(new { error = result.Message });
                }

                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get tasks for the authenticated agent
        /// </summary>
        /// <remarks>
        /// Retrieves all tasks (pending, in-progress, completed) for the authenticated agent.
        /// 
        /// **Authentication Required:** Yes (Bearer Token)
        /// 
        /// **Agent Type Required:** The authenticated avatar must be of type `Agent`
        /// </remarks>
        /// <param name="status">Optional status filter (Pending, InProgress, Completed, Failed, Cancelled)</param>
        /// <returns>List of tasks</returns>
        /// <response code="200">Success - Returns list of tasks</response>
        /// <response code="401">Unauthorized - Authentication required</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("tasks")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTasks([FromQuery] string status = null)
        {
            try
            {
                if (Avatar == null || Avatar.Id == Guid.Empty)
                {
                    return Unauthorized(new { error = "Authentication required" });
                }

                if (Avatar.AvatarType.Value != AvatarType.Agent)
                {
                    return BadRequest(new { error = "Avatar must be of type Agent" });
                }

                A2ATaskStatus? taskStatus = null;
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<A2ATaskStatus>(status, true, out var parsedStatus))
                {
                    taskStatus = parsedStatus;
                }

                var result = await A2AManager.Instance.GetAgentTasksAsync(Avatar.Id, taskStatus);

                if (result.IsError)
                {
                    return BadRequest(new { error = result.Message });
                }

                return Ok(new { tasks = result.Result, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }
    }

    /// <summary>
    /// Request model for registering OpenSERV agent
    /// </summary>
    public class RegisterOpenServAgentRequest
    {
        /// <summary>
        /// OpenSERV agent ID
        /// </summary>
        public string OpenServAgentId { get; set; }

        /// <summary>
        /// OpenSERV endpoint URL
        /// </summary>
        public string OpenServEndpoint { get; set; }

        /// <summary>
        /// List of capabilities/services this agent provides
        /// </summary>
        public List<string> Capabilities { get; set; }

        /// <summary>
        /// OpenSERV API key (optional)
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Username for the avatar (optional, defaults to openserv_{agentId})
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Email for the avatar (optional, auto-generated if not provided)
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Password for the avatar (optional, auto-generated if not provided)
        /// </summary>
        public string Password { get; set; }
    }

    /// <summary>
    /// Request model for executing AI workflow
    /// </summary>
    public class ExecuteWorkflowRequest
    {
        /// <summary>
        /// Target OpenSERV agent ID
        /// </summary>
        public Guid ToAgentId { get; set; }

        /// <summary>
        /// Workflow request content/description
        /// </summary>
        public string WorkflowRequest { get; set; }

        /// <summary>
        /// Additional workflow parameters (optional)
        /// </summary>
        public Dictionary<string, object> WorkflowParameters { get; set; }
    }

    /// <summary>
    /// Request model for creating service completion certificate
    /// </summary>
    public class CreateServiceCertificateRequest
    {
        /// <summary>
        /// Service name that was completed
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Optional task/message ID
        /// </summary>
        public Guid? TaskId { get; set; }

        /// <summary>
        /// Optional description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Optional image URL for the certificate
        /// </summary>
        public string ImageUrl { get; set; }
    }

    /// <summary>
    /// Request model for awarding karma
    /// </summary>
    public class AwardKarmaRequest
    {
        /// <summary>
        /// Agent ID to award karma to
        /// </summary>
        public Guid AgentId { get; set; }

        /// <summary>
        /// Service name that was completed
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Optional task/message ID
        /// </summary>
        public Guid? TaskId { get; set; }

        /// <summary>
        /// Karma amount to award (default: 10)
        /// </summary>
        public long KarmaAmount { get; set; } = 10;
    }

    /// <summary>
    /// Request model for delegating a task
    /// </summary>
    public class DelegateTaskRequest
    {
        /// <summary>
        /// Target agent ID to delegate task to
        /// </summary>
        public Guid ToAgentId { get; set; }

        /// <summary>
        /// Task name
        /// </summary>
        public string TaskName { get; set; }

        /// <summary>
        /// Task description
        /// </summary>
        public string TaskDescription { get; set; }

        /// <summary>
        /// Optional task parameters
        /// </summary>
        public Dictionary<string, object> TaskParameters { get; set; }

        /// <summary>
        /// Optional list of required services
        /// </summary>
        public List<string> RequiredServices { get; set; }
    }

    /// <summary>
    /// Request model for completing a task
    /// </summary>
    public class CompleteTaskRequest
    {
        /// <summary>
        /// Task ID to complete
        /// </summary>
        public Guid TaskId { get; set; }

        /// <summary>
        /// Optional result data
        /// </summary>
        public Dictionary<string, object> ResultData { get; set; }

        /// <summary>
        /// Optional completion notes
        /// </summary>
        public string CompletionNotes { get; set; }
    }
}

