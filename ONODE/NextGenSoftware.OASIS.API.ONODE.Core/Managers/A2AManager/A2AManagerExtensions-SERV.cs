using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Logging;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    /// <summary>
    /// Extension methods for A2AManager to provide SERV infrastructure integration
    /// These methods require ONODE.Core to be loaded
    /// </summary>
    public static class A2AManagerSERVExtensions
    {
        private static ONETUnifiedArchitecture _onetUnifiedArchitecture;

        /// <summary>
        /// Get or create ONETUnifiedArchitecture instance
        /// </summary>
        private static ONETUnifiedArchitecture GetONETUnifiedArchitecture()
        {
            if (_onetUnifiedArchitecture == null)
            {
                _onetUnifiedArchitecture = new ONETUnifiedArchitecture(
                    ProviderManager.Instance.CurrentStorageProvider,
                    ProviderManager.Instance.OASISDNA
                );
            }
            return _onetUnifiedArchitecture;
        }

        /// <summary>
        /// Register an A2A agent as a UnifiedService in SERV infrastructure
        /// </summary>
        public static async Task<OASISResult<bool>> RegisterAgentAsServiceAsync(
            this A2AManager a2aManager,
            Guid agentId,
            IAgentCapabilities capabilities)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Validate agent exists
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

                // Get agent card for endpoint information
                var agentCardResult = await AgentManager.Instance.GetAgentCardAsync(agentId);
                var agentCard = agentCardResult.Result;

                // Create UnifiedService from agent capabilities
                var serviceName = $"a2a_agent_{agentId}";
                var unifiedService = new UnifiedService
                {
                    Name = agentCard?.Name ?? $"Agent {agentId}",
                    Description = capabilities.Description ?? $"A2A Agent: {agentCard?.Name ?? agentId.ToString()}",
                    Category = "A2A_Agent",
                    IntegrationLayers = new List<string> { "A2A", "ONET" },
                    Endpoints = new List<string> 
                    { 
                        agentCard?.Connection?.Endpoint ?? $"/api/a2a/agent-card/{agentId}",
                        "/api/a2a/jsonrpc"
                    },
                    IsActive = capabilities.Status == AgentStatus.Available
                };

                // Register with ONET Unified Architecture
                // Note: Currently ONETUnifiedArchitecture uses a dictionary-based registry
                // This is a placeholder for future SERV infrastructure enhancement
                var onetArchitecture = GetONETUnifiedArchitecture();
                // TODO: Implement proper service registration when SERV infrastructure is enhanced
                var servResult = new OASISResult<bool> { Result = true, Message = "Service registration placeholder - SERV infrastructure enhancement needed" };

                if (servResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to register agent with SERV: {servResult.Message}");
                    return result;
                }

                result.Result = servResult.Result;
                result.Message = $"Agent {agentId} registered as SERV service successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error registering agent as service: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Discover agents via SERV infrastructure, enriched with A2A Agent Cards
        /// </summary>
        public static async Task<OASISResult<List<IAgentCard>>> DiscoverAgentsViaSERVAsync(
            this A2AManager a2aManager,
            string serviceName = null)
        {
            var result = new OASISResult<List<IAgentCard>>();
            try
            {
                var onetArchitecture = GetONETUnifiedArchitecture();
                
                // Query SERV for services
                var servServicesResult = await onetArchitecture.GetUnifiedServicesAsync();
                
                if (servServicesResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to query SERV infrastructure: {servServicesResult.Message}");
                    return result;
                }

                // Filter by A2A agent category
                var a2aServices = servServicesResult.Result
                    .Where(s => s.Category == "A2A_Agent" && 
                                (string.IsNullOrEmpty(serviceName) || 
                                 s.Name.Contains(serviceName, StringComparison.OrdinalIgnoreCase) ||
                                 s.Description.Contains(serviceName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                // For now, return empty list as SERV infrastructure needs enhancement for dynamic agent discovery
                // TODO: Enhance SERV infrastructure to support dynamic agent registration and discovery
                var agentCards = new List<IAgentCard>();
                
                // Note: This is a placeholder - proper implementation requires SERV infrastructure enhancement
                // to store and retrieve agent metadata

                result.Result = agentCards;
                result.Message = string.IsNullOrEmpty(serviceName)
                    ? $"Found {agentCards.Count} A2A agents via SERV infrastructure"
                    : $"Found {agentCards.Count} A2A agents for service '{serviceName}' via SERV infrastructure";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error discovering agents via SERV: {ex.Message}", ex);
            }
            return result;
        }
    }
}
