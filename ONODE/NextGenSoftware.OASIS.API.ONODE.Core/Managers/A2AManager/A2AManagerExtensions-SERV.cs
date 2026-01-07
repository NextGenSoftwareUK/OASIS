using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    /// <summary>
    /// Extension methods for A2AManager to provide SERV functionality
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
                // Get agent card for additional information
                var agentCardResult = await AgentManager.Instance.GetAgentCardAsync(agentId);
                if (agentCardResult.IsError || agentCardResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Agent {agentId} not found or could not retrieve agent card");
                    return result;
                }

                var agentCard = agentCardResult.Result;

                // Create UnifiedService from A2A agent capabilities
                var unifiedService = new UnifiedService
                {
                    ServiceId = agentId.ToString(),
                    ServiceName = agentCard.Name ?? $"Agent {agentId}",
                    ServiceType = "A2A_Agent",
                    Name = agentCard.Name ?? $"Agent {agentId}",
                    Description = capabilities.Description ?? (agentCard.Metadata.ContainsKey("description") 
                        ? agentCard.Metadata["description"].ToString() 
                        : $"A2A Agent: {agentCard.Name}"),
                    Category = "Agent",
                    Capabilities = capabilities.Services ?? new List<string>(),
                    Endpoint = agentCard.Connection?.Endpoint ?? $"/api/a2a/agent-card/{agentId}",
                    Protocol = agentCard.Connection?.Protocol ?? "A2A_JSON-RPC_2.0",
                    IntegrationLayers = new List<string> { "A2A", "ONET" },
                    Endpoints = new List<string> 
                    { 
                        $"/api/a2a/agent-card/{agentId}",
                        $"/api/a2a/jsonrpc"
                    },
                    IsActive = capabilities.Status == AgentStatus.Available,
                    Metadata = new Dictionary<string, object>
                    {
                        ["a2a_agent_id"] = agentId,
                        ["services"] = capabilities.Services ?? new List<string>(),
                        ["skills"] = capabilities.Skills ?? new List<string>(),
                        ["pricing"] = capabilities.Pricing ?? new Dictionary<string, decimal>(),
                        ["status"] = capabilities.Status.ToString(),
                        ["reputation_score"] = capabilities.ReputationScore,
                        ["max_concurrent_tasks"] = capabilities.MaxConcurrentTasks,
                        ["active_tasks"] = capabilities.ActiveTasks,
                        ["description"] = capabilities.Description ?? ""
                    }
                };

                // Register with ONET Unified Architecture
                var onetArchitecture = GetONETUnifiedArchitecture();
                var servResult = await onetArchitecture.RegisterUnifiedServiceAsync(unifiedService);

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
                // Query SERV infrastructure for services
                var onetArchitecture = GetONETUnifiedArchitecture();
                var servServicesResult = await onetArchitecture.GetUnifiedServicesAsync();

                if (servServicesResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to query SERV infrastructure: {servServicesResult.Message}");
                    return result;
                }

                // Filter by service name and A2A agent type if provided
                // Check ServiceType if available, otherwise check Category or Metadata
                var a2aServices = servServicesResult.Result
                    .Where(s => (s.ServiceType == "A2A_Agent" || 
                                s.Category == "Agent" ||
                                (s.Metadata != null && s.Metadata.ContainsKey("a2a_agent_id"))) && 
                                (string.IsNullOrEmpty(serviceName) || 
                                 (s.Capabilities != null && s.Capabilities.Contains(serviceName, StringComparer.OrdinalIgnoreCase)) ||
                                 (s.Metadata != null && s.Metadata.ContainsKey("services") && 
                                  s.Metadata["services"] is List<string> services && 
                                  services.Contains(serviceName, StringComparer.OrdinalIgnoreCase))))
                    .ToList();

                // Enrich with A2A Agent Cards
                var agentCards = new List<IAgentCard>();
                foreach (var service in a2aServices)
                {
                    try
                    {
                        // Extract agent ID from metadata or ServiceId
                        Guid agentId = Guid.Empty;
                        
                        if (service.Metadata != null && service.Metadata.ContainsKey("a2a_agent_id"))
                        {
                            Guid.TryParse(service.Metadata["a2a_agent_id"].ToString(), out agentId);
                        }
                        else if (!string.IsNullOrEmpty(service.ServiceId))
                        {
                            Guid.TryParse(service.ServiceId, out agentId);
                        }
                        
                        if (agentId != Guid.Empty)
                        {
                            var agentCardResult = await AgentManager.Instance.GetAgentCardAsync(agentId);
                            if (!agentCardResult.IsError && agentCardResult.Result != null)
                            {
                                agentCards.Add(agentCardResult.Result);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing other agents
                        var serviceId = !string.IsNullOrEmpty(service.ServiceId) ? service.ServiceId : service.Name;
                        LoggingManager.Log($"Error retrieving agent card for service {serviceId}: {ex.Message}", LogType.Warning);
                    }
                }

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














