using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Logging;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// A2A Protocol integration with KarmaManager
    /// Handles agent reputation scoring, karma rewards, and agent ranking
    /// </summary>
    public partial class A2AManager
    {
        /// <summary>
        /// Award karma to an agent for successful service completion
        /// </summary>
        /// <param name="agentId">The agent ID</param>
        /// <param name="serviceName">The service that was completed</param>
        /// <param name="taskId">Optional task/message ID</param>
        /// <param name="karmaAmount">Optional karma amount (default: 10)</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> AwardKarmaForServiceCompletionAsync(
            Guid agentId,
            string serviceName,
            Guid? taskId = null,
            long karmaAmount = 10)
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

                // Award karma
                var description = $"Completed service: {serviceName}";
                if (taskId.HasValue)
                {
                    description += $" (Task: {taskId.Value})";
                }

                var karmaResult = await KarmaManager.Instance.AddKarmaAsync(
                    agentId,
                    karmaAmount,
                    KarmaSourceType.Service,
                    description,
                    taskId
                );

                if (karmaResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to award karma: {karmaResult.Message}");
                    return result;
                }

                result.Result = true;
                result.Message = $"Karma awarded successfully. New total: {await GetAgentKarmaAsync(agentId)}";
                LoggingManager.Log($"Awarded {karmaAmount} karma to agent {agentId} for service {serviceName}", LogType.Info);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error awarding karma: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get karma for an agent
        /// </summary>
        /// <param name="agentId">The agent ID</param>
        /// <returns>Karma amount</returns>
        public async Task<long> GetAgentKarmaAsync(Guid agentId)
        {
            try
            {
                var karmaResult = await KarmaManager.Instance.GetKarmaAsync(agentId);
                if (karmaResult.IsError)
                {
                    return 0;
                }
                return karmaResult.Result;
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"Error getting karma for agent {agentId}: {ex.Message}", LogType.Error);
                return 0;
            }
        }

        /// <summary>
        /// Get top agents by karma
        /// </summary>
        /// <param name="limit">Number of agents to return (default: 10)</param>
        /// <returns>List of agent IDs with karma scores</returns>
        public async Task<OASISResult<List<AgentKarmaRanking>>> GetTopAgentsByKarmaAsync(int limit = 10)
        {
            var result = new OASISResult<List<AgentKarmaRanking>>();
            try
            {
                // Get all agents (this would need to be enhanced with actual agent list)
                // For now, we'll return agents that have been registered
                var rankings = new List<AgentKarmaRanking>();

                // Note: This is a simplified implementation
                // A full implementation would query all agents and sort by karma
                // For now, we return an empty list as a placeholder

                result.Result = rankings;
                result.Message = "Top agents by karma retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting top agents by karma: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Award karma for high-quality service response
        /// </summary>
        /// <param name="agentId">The agent ID</param>
        /// <param name="qualityScore">Quality score (1-10)</param>
        /// <param name="serviceName">The service name</param>
        /// <param name="taskId">Optional task/message ID</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> AwardKarmaForQualityServiceAsync(
            Guid agentId,
            int qualityScore,
            string serviceName,
            Guid? taskId = null)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Validate quality score
                if (qualityScore < 1 || qualityScore > 10)
                {
                    OASISErrorHandling.HandleError(ref result, "Quality score must be between 1 and 10");
                    return result;
                }

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

                // Calculate karma based on quality score (higher score = more karma)
                // Base karma: 5, bonus: qualityScore * 2
                long karmaAmount = 5 + (qualityScore * 2);

                var description = $"High-quality service: {serviceName} (Quality Score: {qualityScore}/10)";
                if (taskId.HasValue)
                {
                    description += $" (Task: {taskId.Value})";
                }

                var karmaResult = await KarmaManager.Instance.AddKarmaAsync(
                    agentId,
                    karmaAmount,
                    KarmaSourceType.Service,
                    description,
                    taskId
                );

                if (karmaResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to award karma: {karmaResult.Message}");
                    return result;
                }

                result.Result = true;
                result.Message = $"Karma awarded for quality service. New total: {await GetAgentKarmaAsync(agentId)}";
                LoggingManager.Log($"Awarded {karmaAmount} karma to agent {agentId} for quality service (Score: {qualityScore})", LogType.Info);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error awarding karma for quality: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Deduct karma for service failure or poor quality
        /// </summary>
        /// <param name="agentId">The agent ID</param>
        /// <param name="reason">Reason for karma deduction</param>
        /// <param name="taskId">Optional task/message ID</param>
        /// <param name="karmaAmount">Optional karma amount to deduct (default: 5)</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> DeductKarmaForServiceFailureAsync(
            Guid agentId,
            string reason,
            Guid? taskId = null,
            long karmaAmount = 5)
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

                var description = $"Service failure: {reason}";
                if (taskId.HasValue)
                {
                    description += $" (Task: {taskId.Value})";
                }

                var karmaResult = await KarmaManager.Instance.DeductKarmaAsync(
                    agentId,
                    karmaAmount,
                    KarmaSourceType.Service,
                    description,
                    taskId
                );

                if (karmaResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to deduct karma: {karmaResult.Message}");
                    return result;
                }

                result.Result = true;
                result.Message = $"Karma deducted. New total: {await GetAgentKarmaAsync(agentId)}";
                LoggingManager.Log($"Deducted {karmaAmount} karma from agent {agentId} for service failure: {reason}", LogType.Warning);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deducting karma: {ex.Message}", ex);
            }
            return result;
        }
    }

    /// <summary>
    /// Agent karma ranking
    /// </summary>
    public class AgentKarmaRanking
    {
        public Guid AgentId { get; set; }
        public string AgentName { get; set; }
        public long Karma { get; set; }
        public int Rank { get; set; }
    }
}

