using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;
using NextGenSoftware.Logging;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Manages autonomous agents, their capabilities, and A2A (Agent-to-Agent) communication
    /// </summary>
    public partial class AgentManager : OASISManager
    {
        private static AgentManager _instance;
        private readonly Dictionary<Guid, IAgentCapabilities> _agentCapabilities = new Dictionary<Guid, IAgentCapabilities>();
        private readonly Dictionary<string, List<Guid>> _serviceRegistry = new Dictionary<string, List<Guid>>(); // Service name -> List of agent IDs

        public static AgentManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AgentManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public AgentManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
        }

        /// <summary>
        /// Register an agent's capabilities and services
        /// </summary>
        public async Task<OASISResult<bool>> RegisterAgentCapabilitiesAsync(Guid agentId, IAgentCapabilities capabilities)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Verify avatar is an Agent type
                var avatarResult = await AvatarManager.Instance.LoadAvatarAsync(agentId, false, true);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar {agentId} not found");
                    return result;
                }

                if (avatarResult.Result.AvatarType.Value != AvatarType.Agent)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar {agentId} is not an Agent type. Current type: {avatarResult.Result.AvatarType.Value}");
                    return result;
                }

                // Store capabilities
                _agentCapabilities[agentId] = capabilities;

                // Update service registry
                if (capabilities.Services != null)
                {
                    foreach (var service in capabilities.Services)
                    {
                        if (!_serviceRegistry.ContainsKey(service))
                            _serviceRegistry[service] = new List<Guid>();
                        
                        if (!_serviceRegistry[service].Contains(agentId))
                            _serviceRegistry[service].Add(agentId);
                    }
                }

                // Auto-register with SERV infrastructure
                // NOTE: SERV registration requires ONODE.Core extension methods
                // This functionality is available via extension methods in ONODE.Core (A2AManagerExtensions-SERV.cs)
                // Since AgentManager is in API.Core, it cannot reference ONODE.Core
                // SERV registration should be done from ONODE.WebAPI context where extension methods are available
                // Commented out to avoid circular dependency - register manually via API endpoint when needed
                // To register an agent with SERV, use: POST /api/a2a/agent/register-service

                result.Result = true;
                result.Message = "Agent capabilities registered successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error registering agent capabilities: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get agent capabilities by agent ID
        /// </summary>
        public async Task<OASISResult<IAgentCapabilities>> GetAgentCapabilitiesAsync(Guid agentId)
        {
            var result = new OASISResult<IAgentCapabilities>();
            try
            {
                if (_agentCapabilities.ContainsKey(agentId))
                {
                    result.Result = _agentCapabilities[agentId];
                    result.Message = "Agent capabilities retrieved successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"No capabilities found for agent {agentId}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving agent capabilities: {ex.Message}", ex);
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Find agents by service capability
        /// </summary>
        public async Task<OASISResult<List<Guid>>> FindAgentsByServiceAsync(string serviceName)
        {
            var result = new OASISResult<List<Guid>>();
            try
            {
                if (_serviceRegistry.ContainsKey(serviceName))
                {
                    // Filter to only available agents
                    var availableAgents = _serviceRegistry[serviceName]
                        .Where(agentId => _agentCapabilities.ContainsKey(agentId) &&
                                        _agentCapabilities[agentId].Status == AgentStatus.Available &&
                                        _agentCapabilities[agentId].ActiveTasks < _agentCapabilities[agentId].MaxConcurrentTasks)
                        .ToList();

                    result.Result = availableAgents;
                    result.Message = $"Found {availableAgents.Count} available agents for service '{serviceName}'";
                }
                else
                {
                    result.Result = new List<Guid>();
                    result.Message = $"No agents found for service '{serviceName}'";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error finding agents by service: {ex.Message}", ex);
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Find agents by skill
        /// </summary>
        public async Task<OASISResult<List<Guid>>> FindAgentsBySkillAsync(string skill)
        {
            var result = new OASISResult<List<Guid>>();
            try
            {
                var agentsWithSkill = _agentCapabilities
                    .Where(kvp => kvp.Value.Skills != null && 
                                kvp.Value.Skills.Contains(skill, StringComparer.OrdinalIgnoreCase) &&
                                kvp.Value.Status == AgentStatus.Available)
                    .Select(kvp => kvp.Key)
                    .ToList();

                result.Result = agentsWithSkill;
                result.Message = $"Found {agentsWithSkill.Count} agents with skill '{skill}'";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error finding agents by skill: {ex.Message}", ex);
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Get all available agents
        /// </summary>
        public async Task<OASISResult<List<Guid>>> GetAvailableAgentsAsync()
        {
            var result = new OASISResult<List<Guid>>();
            try
            {
                var availableAgents = _agentCapabilities
                    .Where(kvp => kvp.Value.Status == AgentStatus.Available &&
                                kvp.Value.ActiveTasks < kvp.Value.MaxConcurrentTasks)
                    .Select(kvp => kvp.Key)
                    .ToList();

                result.Result = availableAgents;
                result.Message = $"Found {availableAgents.Count} available agents";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting available agents: {ex.Message}", ex);
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Update agent status
        /// </summary>
        public async Task<OASISResult<bool>> UpdateAgentStatusAsync(Guid agentId, AgentStatus status)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_agentCapabilities.ContainsKey(agentId))
                {
                    _agentCapabilities[agentId].Status = status;
                    result.Result = true;
                    result.Message = $"Agent {agentId} status updated to {status}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"No capabilities found for agent {agentId}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating agent status: {ex.Message}", ex);
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Update agent active task count
        /// </summary>
        public async Task<OASISResult<bool>> UpdateAgentTaskCountAsync(Guid agentId, int taskCount)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_agentCapabilities.ContainsKey(agentId))
                {
                    _agentCapabilities[agentId].ActiveTasks = taskCount;
                    result.Result = true;
                    result.Message = $"Agent {agentId} task count updated to {taskCount}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"No capabilities found for agent {agentId}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating agent task count: {ex.Message}", ex);
            }
            return await Task.FromResult(result);
        }
    }
}



