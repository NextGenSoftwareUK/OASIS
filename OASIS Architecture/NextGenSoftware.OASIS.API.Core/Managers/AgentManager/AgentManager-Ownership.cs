using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Agent ownership and linking methods
    /// </summary>
    public partial class AgentManager
    {
        /// <summary>
        /// Link an agent to a user avatar (owner)
        /// </summary>
        public async Task<OASISResult<bool>> LinkAgentToUserAsync(Guid agentId, Guid ownerAvatarId)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Verify agent exists and is Agent type
                var agentResult = await AvatarManager.Instance.LoadAvatarAsync(agentId, false, true);
                if (agentResult.IsError || agentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Agent {agentId} not found");
                    return result;
                }

                if (agentResult.Result.AvatarType.Value != AvatarType.Agent)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar {agentId} is not an Agent type");
                    return result;
                }

                // Verify owner exists and is User type
                var ownerResult = await AvatarManager.Instance.LoadAvatarAsync(ownerAvatarId, false, true);
                if (ownerResult.IsError || ownerResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Owner avatar {ownerAvatarId} not found");
                    return result;
                }

                if (ownerResult.Result.AvatarType.Value != AvatarType.User)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar {ownerAvatarId} is not a User type. Only User avatars can own agents.");
                    return result;
                }

                // Check agent limit per user (default: 10 agents per user)
                var maxAgentsPerUser = 10; // TODO: Make this configurable via OASIS_DNA
                var userAgentsResult = await GetAgentsByOwnerAsync(ownerAvatarId);
                if (!userAgentsResult.IsError && userAgentsResult.Result != null && userAgentsResult.Result.Count >= maxAgentsPerUser)
                {
                    OASISErrorHandling.HandleError(ref result, $"User {ownerAvatarId} has reached the maximum limit of {maxAgentsPerUser} agents");
                    return result;
                }

                // Store owner relationship in agent's metadata
                // Load avatar WITHOUT hideAuthDetails to preserve metadata
                var agent = agentResult.Result;
                
                // Set metadata FIRST before any password operations
                if (agent.MetaData == null)
                    agent.MetaData = new Dictionary<string, object>();

                agent.MetaData["OwnerAvatarId"] = ownerAvatarId.ToString();
                agent.MetaData["OwnerLinkedDate"] = DateTime.UtcNow.ToString("O");

                // Ensure password is set to avoid reload in SaveAvatarAsync
                // IMPORTANT: Only copy the password, don't replace the entire avatar object
                if (string.IsNullOrEmpty(agent.Password))
                {
                    var passwordReload = await AvatarManager.Instance.LoadAvatarAsync(agentId, false, false);
                    if (!passwordReload.IsError && passwordReload.Result != null)
                    {
                        // Only copy the password, keep our metadata-modified avatar object
                        agent.Password = passwordReload.Result.Password;
                        // Metadata is already set above, no need to re-apply
                    }
                }

                // Verify metadata is still set before saving
                if (agent.MetaData == null || !agent.MetaData.ContainsKey("OwnerAvatarId"))
                {
                    OASISErrorHandling.HandleError(ref result, "Metadata was lost before save operation");
                    return result;
                }

                // Save directly with SaveAvatarAsync - password is set so it won't reload
                var saveResult = await AvatarManager.Instance.SaveAvatarAsync(agent);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save agent ownership: {saveResult.Message}");
                    return result;
                }
                
                // Verify the save worked by immediately reloading and checking metadata
                var verifyResult = await AvatarManager.Instance.LoadAvatarAsync(agentId, false, false);
                if (!verifyResult.IsError && verifyResult.Result != null)
                {
                    if (verifyResult.Result.MetaData == null || !verifyResult.Result.MetaData.ContainsKey("OwnerAvatarId"))
                    {
                        OASISErrorHandling.HandleError(ref result, "Metadata was not persisted after save operation");
                        return result;
                    }
                }

                result.Result = true;
                result.Message = $"Agent {agentId} successfully linked to user {ownerAvatarId}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error linking agent to user: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Unlink an agent from its owner
        /// </summary>
        public async Task<OASISResult<bool>> UnlinkAgentFromUserAsync(Guid agentId)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Verify agent exists
                var agentResult = await AvatarManager.Instance.LoadAvatarAsync(agentId, false, true);
                if (agentResult.IsError || agentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Agent {agentId} not found");
                    return result;
                }

                var agent = agentResult.Result;
                if (agent.MetaData == null || !agent.MetaData.ContainsKey("OwnerAvatarId"))
                {
                    OASISErrorHandling.HandleError(ref result, $"Agent {agentId} is not linked to any user");
                    return result;
                }

                // Remove owner relationship
                agent.MetaData.Remove("OwnerAvatarId");
                agent.MetaData.Remove("OwnerLinkedDate");

                // Save agent with updated metadata
                var saveResult = await AvatarManager.Instance.SaveAvatarAsync(agent);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlink agent: {saveResult.Message}");
                    return result;
                }

                result.Result = true;
                result.Message = $"Agent {agentId} successfully unlinked from owner";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlinking agent from user: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get all agents owned by a user
        /// </summary>
        public async Task<OASISResult<List<Guid>>> GetAgentsByOwnerAsync(Guid ownerAvatarId)
        {
            var result = new OASISResult<List<Guid>>();
            try
            {
                // Load all avatars WITHOUT hideAuthDetails to preserve metadata
                var allAvatarsResult = await AvatarManager.Instance.LoadAllAvatarsAsync(false, false);
                if (allAvatarsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatars: {allAvatarsResult.Message}");
                    return result;
                }

                var ownedAgents = new List<Guid>();
                if (allAvatarsResult.Result != null)
                {
                    foreach (var avatar in allAvatarsResult.Result)
                    {
                        if (avatar.AvatarType.Value == AvatarType.Agent &&
                            avatar.MetaData != null &&
                            avatar.MetaData.ContainsKey("OwnerAvatarId"))
                        {
                            var ownerId = avatar.MetaData["OwnerAvatarId"]?.ToString();
                            if (ownerId == ownerAvatarId.ToString())
                            {
                                ownedAgents.Add(avatar.Id);
                            }
                        }
                    }
                }

                result.Result = ownedAgents;
                result.Message = $"Found {ownedAgents.Count} agents owned by user {ownerAvatarId}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting agents by owner: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get the owner of an agent
        /// </summary>
        public async Task<OASISResult<Guid?>> GetAgentOwnerAsync(Guid agentId)
        {
            var result = new OASISResult<Guid?>();
            try
            {
                // Load avatar WITHOUT hideAuthDetails to preserve metadata
                var agentResult = await AvatarManager.Instance.LoadAvatarAsync(agentId, false, false);
                if (agentResult.IsError || agentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Agent {agentId} not found");
                    return result;
                }

                if (agentResult.Result.AvatarType.Value != AvatarType.Agent)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar {agentId} is not an Agent type");
                    return result;
                }

                if (agentResult.Result.MetaData != null && agentResult.Result.MetaData.ContainsKey("OwnerAvatarId"))
                {
                    var ownerIdStr = agentResult.Result.MetaData["OwnerAvatarId"]?.ToString();
                    if (Guid.TryParse(ownerIdStr, out var ownerId))
                    {
                        result.Result = ownerId;
                        result.Message = $"Agent {agentId} is owned by user {ownerId}";
                    }
                    else
                    {
                        result.Result = null;
                        result.Message = $"Invalid owner ID format for agent {agentId}";
                    }
                }
                else
                {
                    result.Result = null;
                    result.Message = $"Agent {agentId} has no owner";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting agent owner: {ex.Message}", ex);
            }
            return result;
        }
    }
}
