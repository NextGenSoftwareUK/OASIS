using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Agent Card methods for A2A Protocol compliance
    /// </summary>
    public partial class AgentManager
    {
        /// <summary>
        /// Get Agent Card for an agent (Official A2A Protocol format)
        /// </summary>
        public async Task<OASISResult<IAgentCard>> GetAgentCardAsync(Guid agentId, string baseUrl = null)
        {
            var result = new OASISResult<IAgentCard>();
            try
            {
                // Load avatar
                var avatarResult = await AvatarManager.Instance.LoadAvatarAsync(agentId, false, true);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Agent {agentId} not found");
                    return result;
                }

                if (avatarResult.Result.AvatarType.Value != AvatarType.Agent)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar {agentId} is not an Agent type");
                    return result;
                }

                // Get capabilities
                var capabilitiesResult = await GetAgentCapabilitiesAsync(agentId);
                if (capabilitiesResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Could not retrieve capabilities for agent {agentId}");
                    return result;
                }

                var capabilities = capabilitiesResult.Result;

                // Build Agent Card
                var agentCard = new AgentCard
                {
                    AgentId = agentId.ToString(),
                    Name = avatarResult.Result.Username ?? $"Agent {agentId}",
                    Version = "1.0.0",
                    Capabilities = new AgentCapabilitiesInfo
                    {
                        Services = capabilities.Services ?? new List<string>(),
                        Skills = capabilities.Skills ?? new List<string>()
                    },
                    Connection = new AgentConnectionInfo
                    {
                        Endpoint = baseUrl ?? "https://api.oasisplatform.world/api/a2a/jsonrpc",
                        Protocol = "jsonrpc2.0",
                        Auth = new AgentAuthInfo
                        {
                            Scheme = "bearer",
                            Credentials = null // Will be set via JWT token in Authorization header
                        }
                    },
                    Metadata = new Dictionary<string, object>
                    {
                        ["description"] = capabilities.Description ?? "",
                        ["status"] = capabilities.Status.ToString(),
                        ["reputation_score"] = capabilities.ReputationScore,
                        ["max_concurrent_tasks"] = capabilities.MaxConcurrentTasks,
                        ["active_tasks"] = capabilities.ActiveTasks,
                        ["pricing"] = capabilities.Pricing ?? new Dictionary<string, decimal>()
                    }
                };

                // Add avatar metadata if available
                if (avatarResult.Result.Email != null)
                    agentCard.Metadata["email"] = avatarResult.Result.Email;

                result.Result = agentCard;
                result.Message = "Agent Card retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving Agent Card: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get all Agent Cards (for discovery)
        /// </summary>
        public async Task<OASISResult<List<IAgentCard>>> GetAllAgentCardsAsync(string baseUrl = null)
        {
            var result = new OASISResult<List<IAgentCard>>();
            try
            {
                var availableAgentsResult = await GetAvailableAgentsAsync();
                if (availableAgentsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, availableAgentsResult.Message);
                    return result;
                }

                var agentCards = new List<IAgentCard>();
                foreach (var agentId in availableAgentsResult.Result)
                {
                    var cardResult = await GetAgentCardAsync(agentId, baseUrl);
                    if (!cardResult.IsError && cardResult.Result != null)
                    {
                        agentCards.Add(cardResult.Result);
                    }
                }

                result.Result = agentCards;
                result.Message = $"Retrieved {agentCards.Count} Agent Cards";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving Agent Cards: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get Agent Cards by service
        /// </summary>
        public async Task<OASISResult<List<IAgentCard>>> GetAgentCardsByServiceAsync(string serviceName, string baseUrl = null)
        {
            var result = new OASISResult<List<IAgentCard>>();
            try
            {
                var agentsResult = await FindAgentsByServiceAsync(serviceName);
                if (agentsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, agentsResult.Message);
                    return result;
                }

                var agentCards = new List<IAgentCard>();
                foreach (var agentId in agentsResult.Result)
                {
                    var cardResult = await GetAgentCardAsync(agentId, baseUrl);
                    if (!cardResult.IsError && cardResult.Result != null)
                    {
                        agentCards.Add(cardResult.Result);
                    }
                }

                result.Result = agentCards;
                result.Message = $"Found {agentCards.Count} agents for service '{serviceName}'";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving Agent Cards by service: {ex.Message}", ex);
            }
            return result;
        }
    }

    /// <summary>
    /// Agent Card implementation
    /// </summary>
    public class AgentCard : IAgentCard
    {
        public string AgentId { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public AgentCapabilitiesInfo Capabilities { get; set; }
        public AgentConnectionInfo Connection { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}































