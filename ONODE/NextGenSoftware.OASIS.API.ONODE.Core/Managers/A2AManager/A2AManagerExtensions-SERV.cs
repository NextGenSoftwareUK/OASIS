using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.UnifiedAgentServiceManager;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Logging;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    /// <summary>
    /// Extension methods for A2AManager to provide ONET Service Registry integration
    /// These methods require ONODE.Core to be loaded
    /// </summary>
    public static class A2AManagerONETServiceRegistryExtensions
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
        /// Register an A2A agent as a UnifiedService in ONET Service Registry
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
                if (agentCardResult.IsError || agentCardResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Could not retrieve agent card for agent {agentId}");
                    return result;
                }

                var agentCard = agentCardResult.Result;

                // Create UnifiedAgentService from A2A agent capabilities
                var unifiedService = new UnifiedAgentService
                {
                    ServiceId = agentId.ToString(),
                    ServiceName = agentCard.Name ?? $"Agent {agentId}",
                    ServiceType = "A2A_Agent",
                    Description = capabilities.Description ?? (agentCard.Metadata?.ContainsKey("description") == true
                        ? agentCard.Metadata["description"].ToString()
                        : $"A2A Agent: {agentCard.Name}"),
                    Capabilities = capabilities.Services ?? new List<string>(),
                    Endpoint = agentCard.Connection?.Endpoint ?? $"/api/a2a/agent-card/{agentId}",
                    Protocol = agentCard.Connection?.Protocol ?? "A2A_JSON-RPC_2.0",
                    Status = capabilities.Status == AgentStatus.Available 
                        ? UnifiedServiceStatus.Available 
                        : UnifiedServiceStatus.Offline,
                    AgentId = agentId,
                    RegisteredAt = DateTime.UtcNow,
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
                        ["description"] = capabilities.Description ?? "",
                        ["agent_card_name"] = agentCard.Name,
                        ["agent_card_version"] = agentCard.Version ?? "1.0"
                    },
                    ActiveTasks = capabilities.ActiveTasks,
                    MaxConcurrentTasks = capabilities.MaxConcurrentTasks
                };

                // Register with UnifiedAgentServiceManager (ONET Service Registry)
                var servResult = await UnifiedAgentServiceManager.Instance.RegisterServiceAsync(unifiedService);

                if (servResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to register agent with ONET Service Registry: {servResult.Message}");
                    return result;
                }

                // Also register with ONET Unified Architecture for backward compatibility
                var onetArchitecture = GetONETUnifiedArchitecture();
                var onetResult = await onetArchitecture.RegisterUnifiedServiceAsync(
                    new UnifiedService
                    {
                        ServiceId = agentId.ToString(),
                        Name = unifiedService.ServiceName,
                        Description = unifiedService.Description,
                        Category = "A2A_Agent",
                        IntegrationLayers = new List<string> { "A2A", "ONET" },
                        Endpoints = new List<string> { unifiedService.Endpoint, "/api/a2a/jsonrpc" },
                        IsActive = unifiedService.Status == UnifiedServiceStatus.Available,
                        Metadata = unifiedService.Metadata
                    }
                );

                result.Result = servResult.Result;
                result.Message = $"Agent {agentId} registered with ONET Service Registry successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error registering agent as service: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Discover agents via ONET Service Registry, enriched with A2A Agent Cards
        /// Optionally includes agents from OpenSERV platform for bidirectional discovery
        /// </summary>
        public static async Task<OASISResult<List<IAgentCard>>> DiscoverAgentsViaONETServiceRegistryAsync(
            this A2AManager a2aManager,
            string serviceName = null,
            bool includeOpenServAgents = false,
            string openServApiKey = null)
        {
            var result = new OASISResult<List<IAgentCard>>();
            try
            {
                // Query UnifiedAgentServiceManager for A2A agents (ONET Service Registry)
                List<IUnifiedAgentService> services;
                
                if (string.IsNullOrEmpty(serviceName))
                {
                    // Get all A2A agent services
                    var allServicesResult = await UnifiedAgentServiceManager.Instance.GetAllServicesAsync(healthyOnly: true);
                    if (allServicesResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to query ONET Service Registry: {allServicesResult.Message}");
                        return result;
                    }
                    
                    services = allServicesResult.Result
                        .Where(s => s.ServiceType == "A2A_Agent" || s.ServiceType == "OpenSERV_AI_Agent")
                        .ToList();
                }
                else
                {
                    // Discover services by capability/service name
                    var discoverResult = await UnifiedAgentServiceManager.Instance.DiscoverServicesAsync(serviceName, healthyOnly: true);
                    if (discoverResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to discover services: {discoverResult.Message}");
                        return result;
                    }
                    
                    services = discoverResult.Result
                        .Where(s => s.ServiceType == "A2A_Agent" || s.ServiceType == "OpenSERV_AI_Agent")
                        .ToList();
                }

                // Enrich with A2A Agent Cards
                var agentCards = new List<IAgentCard>();
                foreach (var service in services)
                {
                    try
                    {
                        // Extract agent ID from service
                        Guid agentId;
                        if (service.AgentId.HasValue)
                        {
                            agentId = service.AgentId.Value;
                        }
                        else if (service.Metadata != null && service.Metadata.ContainsKey("a2a_agent_id"))
                        {
                            if (!Guid.TryParse(service.Metadata["a2a_agent_id"].ToString(), out agentId))
                            {
                                continue; // Skip if we can't parse the agent ID
                            }
                        }
                        else if (Guid.TryParse(service.ServiceId, out agentId))
                        {
                            // Use ServiceId as fallback
                        }
                        else
                        {
                            continue; // Skip if we can't determine agent ID
                        }

                        // Get agent card
                        var agentCardResult = await AgentManager.Instance.GetAgentCardAsync(agentId);
                        if (!agentCardResult.IsError && agentCardResult.Result != null)
                        {
                            agentCards.Add(agentCardResult.Result);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing other agents
                        LoggingManager.Log($"Error retrieving agent card for service {service.ServiceId}: {ex.Message}", Logging.LogType.Warning);
                    }
                }

                // Optionally include agents from OpenSERV platform (bidirectional discovery)
                if (includeOpenServAgents && !string.IsNullOrEmpty(openServApiKey))
                {
                    try
                    {
                        var openServResult = await A2AManager.Instance.DiscoverAgentsFromOpenServAsync(openServApiKey, serviceName);
                        if (!openServResult.IsError && openServResult.Result != null)
                        {
                            // Merge OpenSERV agents, avoiding duplicates
                            var existingIds = new HashSet<Guid>(agentCards.Select(ac => Guid.Parse(ac.AgentId)));
                            foreach (var openServCard in openServResult.Result)
                            {
                                if (!existingIds.Contains(Guid.Parse(openServCard.AgentId)))
                                {
                                    agentCards.Add(openServCard);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail if OpenSERV discovery fails
                        LoggingManager.Log($"Warning: Failed to discover agents from OpenSERV platform: {ex.Message}", Logging.LogType.Warning);
                    }
                }

                result.Result = agentCards;
                result.Message = string.IsNullOrEmpty(serviceName)
                    ? $"Found {agentCards.Count} agents via ONET Service Registry" + (includeOpenServAgents ? " (including OpenSERV platform)" : "")
                    : $"Found {agentCards.Count} agents for service '{serviceName}' via ONET Service Registry" + (includeOpenServAgents ? " (including OpenSERV platform)" : "");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error discovering agents via ONET Service Registry: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Legacy method name for backward compatibility - redirects to DiscoverAgentsViaONETServiceRegistryAsync
        /// </summary>
        [Obsolete("Use DiscoverAgentsViaONETServiceRegistryAsync instead. SERV terminology has been replaced with ONET Service Registry.")]
        public static async Task<OASISResult<List<IAgentCard>>> DiscoverAgentsViaSERVAsync(
            this A2AManager a2aManager,
            string serviceName = null)
        {
            return await DiscoverAgentsViaONETServiceRegistryAsync(a2aManager, serviceName, false, null);
        }
    }
}
