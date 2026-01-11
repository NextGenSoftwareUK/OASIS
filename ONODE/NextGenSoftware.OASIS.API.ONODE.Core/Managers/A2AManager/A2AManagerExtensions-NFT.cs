using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    /// <summary>
    /// Extension methods for A2AManager to provide NFT functionality
    /// These methods require ONODE.Core to be loaded
    /// </summary>
    public static class A2AManagerNFTExtensions
    {
        /// <summary>
        /// Create a reputation NFT for an agent
        /// </summary>
        public static async Task<OASISResult<object>> CreateAgentReputationNFTAsync(
            this A2AManager a2aManager,
            Guid agentId,
            decimal reputationScore,
            string description = null,
            string imageUrl = null)
        {
            var result = new OASISResult<object>();
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

                // Get agent capabilities for metadata
                var agentCardResult = await AgentManager.Instance.GetAgentCardAsync(agentId);
                var agentCard = agentCardResult.Result;

                // Create NFT metadata
                var metadata = new Dictionary<string, object>
                {
                    ["agent_id"] = agentId.ToString(),
                    ["reputation_score"] = reputationScore,
                    ["nft_type"] = "agent_reputation",
                    ["timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["agent_name"] = agentCard?.Name ?? "Unknown Agent"
                };

                if (agentCard?.Capabilities != null)
                {
                    metadata["services"] = agentCard.Capabilities.Services ?? new List<string>();
                    metadata["skills"] = agentCard.Capabilities.Skills ?? new List<string>();
                }

                // Create NFT request
                var nftRequest = new MintWeb4NFTRequest
                {
                    MintedByAvatarId = agentId,
                    Title = $"Agent Reputation - {agentCard?.Name ?? agentId.ToString()}",
                    Description = description ?? $"Reputation NFT for agent with score: {reputationScore}",
                    ImageUrl = imageUrl ?? "https://oasisplatform.io/images/agent-reputation-nft.png",
                    MetaData = metadata,
                    NumberToMint = 1,
                    StoreNFTMetaDataOnChain = false,
                    OnChainProvider = new EnumValue<ProviderType>(ProviderType.SolanaOASIS),
                    OffChainProvider = new EnumValue<ProviderType>(ProviderType.IPFSOASIS),
                    NFTStandardType = new EnumValue<NFTStandardType>(NFTStandardType.ERC721),
                    WaitTillNFTMinted = true,
                    WaitForNFTToMintInSeconds = 60,
                    SendToAvatarAfterMintingId = agentId
                };
                
                // Mint NFT using NFTManager from ONODE.Core
                var nftManager = new NFTManager(agentId, ProviderManager.Instance.OASISDNA);
                var nftResult = await nftManager.MintNftAsync(nftRequest);

                if (nftResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint reputation NFT: {nftResult.Message}");
                    return result;
                }

                result.Result = nftResult.Result;
                result.Message = "Reputation NFT created successfully";
                LoggingManager.Log($"Reputation NFT created for agent {agentId} with score {reputationScore}", LogType.Info);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating reputation NFT: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Create a service completion certificate NFT for an agent
        /// </summary>
        public static async Task<OASISResult<object>> CreateServiceCompletionCertificateAsync(
            this A2AManager a2aManager,
            Guid agentId,
            string serviceName,
            Guid? taskId = null,
            string description = null,
            string imageUrl = null)
        {
            var result = new OASISResult<object>();
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

                // Create certificate metadata
                var metadata = new Dictionary<string, object>
                {
                    ["agent_id"] = agentId.ToString(),
                    ["service_name"] = serviceName,
                    ["nft_type"] = "service_completion_certificate",
                    ["timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["certificate_type"] = "service_completion"
                };

                if (taskId.HasValue)
                {
                    metadata["task_id"] = taskId.Value.ToString();
                }

                // Create NFT request
                var nftRequest = new MintWeb4NFTRequest
                {
                    MintedByAvatarId = agentId,
                    Title = $"Service Completion Certificate - {serviceName}",
                    Description = description ?? $"Certificate for completing service: {serviceName}",
                    ImageUrl = imageUrl ?? "https://oasisplatform.io/images/service-certificate-nft.png",
                    MetaData = metadata,
                    NumberToMint = 1,
                    StoreNFTMetaDataOnChain = false,
                    OnChainProvider = new EnumValue<ProviderType>(ProviderType.SolanaOASIS),
                    OffChainProvider = new EnumValue<ProviderType>(ProviderType.IPFSOASIS),
                    NFTStandardType = new EnumValue<NFTStandardType>(NFTStandardType.ERC721),
                    WaitTillNFTMinted = true,
                    WaitForNFTToMintInSeconds = 60,
                    SendToAvatarAfterMintingId = agentId
                };
                
                // Mint NFT using NFTManager from ONODE.Core
                var nftManager = new NFTManager(agentId, ProviderManager.Instance.OASISDNA);
                var nftResult = await nftManager.MintNftAsync(nftRequest);

                if (nftResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint certificate NFT: {nftResult.Message}");
                    return result;
                }

                result.Result = nftResult.Result;
                result.Message = "Service completion certificate created successfully";
                LoggingManager.Log($"Service completion certificate created for agent {agentId} for service {serviceName}", LogType.Info);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating service certificate: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Create an achievement badge NFT for an agent
        /// </summary>
        public static async Task<OASISResult<object>> CreateAchievementBadgeAsync(
            this A2AManager a2aManager,
            Guid agentId,
            string achievementName,
            string achievementDescription,
            string imageUrl = null)
        {
            var result = new OASISResult<object>();
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

                // Create badge metadata
                var metadata = new Dictionary<string, object>
                {
                    ["agent_id"] = agentId.ToString(),
                    ["achievement_name"] = achievementName,
                    ["achievement_description"] = achievementDescription,
                    ["nft_type"] = "achievement_badge",
                    ["timestamp"] = DateTime.UtcNow.ToString("O")
                };

                // Create NFT request
                var nftRequest = new MintWeb4NFTRequest
                {
                    MintedByAvatarId = agentId,
                    Title = $"Achievement Badge - {achievementName}",
                    Description = achievementDescription,
                    ImageUrl = imageUrl ?? "https://oasisplatform.io/images/achievement-badge-nft.png",
                    MetaData = metadata,
                    NumberToMint = 1,
                    StoreNFTMetaDataOnChain = false,
                    OnChainProvider = new EnumValue<ProviderType>(ProviderType.SolanaOASIS),
                    OffChainProvider = new EnumValue<ProviderType>(ProviderType.IPFSOASIS),
                    NFTStandardType = new EnumValue<NFTStandardType>(NFTStandardType.ERC721),
                    WaitTillNFTMinted = true,
                    WaitForNFTToMintInSeconds = 60,
                    SendToAvatarAfterMintingId = agentId
                };
                
                // Mint NFT using NFTManager from ONODE.Core
                var nftManager = new NFTManager(agentId, ProviderManager.Instance.OASISDNA);
                var nftResult = await nftManager.MintNftAsync(nftRequest);

                if (nftResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint achievement badge: {nftResult.Message}");
                    return result;
                }

                result.Result = nftResult.Result;
                result.Message = "Achievement badge created successfully";
                LoggingManager.Log($"Achievement badge created for agent {agentId}: {achievementName}", LogType.Info);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating achievement badge: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Mint an NFT representing ownership of an agent (makes agent tradable)
        /// </summary>
        public static async Task<OASISResult<IWeb4NFT>> MintAgentOwnershipNFTAsync(
            this A2AManager a2aManager,
            Guid agentId,
            Guid mintedByAvatarId,
            ProviderType onChainProvider = ProviderType.SolanaOASIS,
            ProviderType offChainProvider = ProviderType.MongoDBOASIS,
            string title = null,
            string description = null,
            string imageUrl = null,
            decimal price = 0,
            string symbol = null,
            Dictionary<string, object> additionalMetadata = null)
        {
            var result = new OASISResult<IWeb4NFT>();
            try
            {
                // Verify agent exists and is Agent type
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

                // Check if agent already has an NFT
                if (agentResult.Result.MetaData != null && agentResult.Result.MetaData.ContainsKey("NFTId"))
                {
                    OASISErrorHandling.HandleError(ref result, $"Agent {agentId} already has an NFT linked. Use GetAgentNFTAsync to retrieve it.");
                    return result;
                }

                // Get current owner (for sending NFT to)
                Guid? currentOwnerId = null;
                if (agentResult.Result.MetaData != null && agentResult.Result.MetaData.ContainsKey("OwnerAvatarId"))
                {
                    var ownerIdStr = agentResult.Result.MetaData["OwnerAvatarId"]?.ToString();
                    if (Guid.TryParse(ownerIdStr, out var ownerId))
                    {
                        currentOwnerId = ownerId;
                    }
                }

                if (!currentOwnerId.HasValue)
                {
                    OASISErrorHandling.HandleError(ref result, $"Agent {agentId} has no owner. Link agent to a user first before minting NFT.");
                    return result;
                }

                // Get Agent Card for NFT metadata
                var agentCardResult = await AgentManager.Instance.GetAgentCardAsync(agentId);
                if (agentCardResult.IsError || agentCardResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Could not retrieve Agent Card for agent {agentId}: {agentCardResult.Message}");
                    return result;
                }

                var agentCard = agentCardResult.Result;

                // Build NFT metadata with agent information
                var nftMetadata = new Dictionary<string, object>
                {
                    ["AgentId"] = agentId.ToString(),
                    ["AgentType"] = "Agent",
                    ["AgentCard"] = Newtonsoft.Json.JsonConvert.SerializeObject(agentCard),
                    ["AgentVersion"] = agentCard.Version ?? "1.0.0",
                    ["AgentStatus"] = "Available"
                };

                // Add any additional metadata
                if (additionalMetadata != null)
                {
                    foreach (var kvp in additionalMetadata)
                    {
                        nftMetadata[kvp.Key] = kvp.Value;
                    }
                }

                // Build NFT mint request
                var mintRequest = new MintWeb4NFTRequest
                {
                    MintedByAvatarId = mintedByAvatarId,
                    Title = title ?? $"{agentCard.Name ?? "Agent"} NFT",
                    Description = description ?? $"NFT representing ownership of agent {agentCard.Name ?? agentId.ToString()}",
                    ImageUrl = imageUrl,
                    Price = price,
                    Symbol = symbol ?? "AGENTNFT",
                    MetaData = nftMetadata,
                    OnChainProvider = new EnumValue<ProviderType>(onChainProvider),
                    OffChainProvider = new EnumValue<ProviderType>(offChainProvider),
                    SendToAvatarAfterMintingId = currentOwnerId.Value,
                    NumberToMint = 1,
                    StoreNFTMetaDataOnChain = false,
                    NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>(NFTOffChainMetaType.OASIS),
                    NFTStandardType = new EnumValue<NFTStandardType>(NFTStandardType.ERC1155),
                    WaitTillNFTMinted = true,
                    WaitForNFTToMintInSeconds = 60,
                    AttemptToMintEveryXSeconds = 1
                };

                // Mint NFT using NFTManager
                var nftManager = new NFTManager(mintedByAvatarId, ProviderManager.Instance.OASISDNA);
                var nftResult = await nftManager.MintNftAsync(mintRequest, false, Core.Enums.ResponseFormatType.SimpleText);

                if (nftResult.IsError || nftResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint agent NFT: {nftResult.Message}");
                    return result;
                }

                var mintedNFT = nftResult.Result;

                // Link NFT to agent
                var linkResult = await AgentManager.Instance.LinkNFTToAgentAsync(
                    agentId,
                    mintedNFT.Id,
                    mintedNFT.Web3NFTs?.Count > 0 ? mintedNFT.Web3NFTs[0].NFTMintedUsingWalletAddress : null,
                    mintedNFT.Web3NFTs?.Count > 0 ? mintedNFT.Web3NFTs[0].MintTransactionHash : null,
                    onChainProvider);

                if (linkResult.IsError)
                {
                    OASISErrorHandling.HandleWarning(ref result, $"NFT minted but failed to link to agent: {linkResult.Message}");
                }

                // Update NFT's CurrentOwnerAvatarId to match agent's owner
                if (mintedNFT.CurrentOwnerAvatarId == Guid.Empty && currentOwnerId.HasValue)
                {
                    mintedNFT.CurrentOwnerAvatarId = currentOwnerId.Value;
                    // Save NFT with updated owner
                    // Note: This would require NFTManager.SaveWeb4NFTAsync which may not exist
                    // For now, the ownership is set via SendToAvatarAfterMintingId during mint
                }

                result.Result = mintedNFT;
                result.Message = $"Agent ownership NFT minted and linked successfully. NFT ID: {mintedNFT.Id}";
                LoggingManager.Log($"Agent ownership NFT minted for agent {agentId}, NFT ID: {mintedNFT.Id}", LogType.Info);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting agent ownership NFT: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Sync agent ownership from NFT ownership (called after NFT transfer)
        /// </summary>
        public static async Task<OASISResult<bool>> SyncAgentOwnershipFromNFTAsync(
            this A2AManager a2aManager,
            Guid agentId,
            Guid nftId)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Load NFT to get current owner
                var nftManager = new NFTManager(agentId, ProviderManager.Instance.OASISDNA);
                var nftResult = await nftManager.LoadWeb4NftAsync(nftId);

                if (nftResult.IsError || nftResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"NFT {nftId} not found: {nftResult.Message}");
                    return result;
                }

                var nft = nftResult.Result;
                var newOwnerId = nft.CurrentOwnerAvatarId;
                var previousOwnerId = nft.PreviousOwnerAvatarId;

                if (newOwnerId == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"NFT {nftId} has no current owner");
                    return result;
                }

                // Load agent
                var agentResult = await AvatarManager.Instance.LoadAvatarAsync(agentId, false, false);
                if (agentResult.IsError || agentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Agent {agentId} not found");
                    return result;
                }

                var agent = agentResult.Result;

                // Update agent metadata with new owner
                if (agent.MetaData == null)
                    agent.MetaData = new Dictionary<string, object>();

                agent.MetaData["OwnerAvatarId"] = newOwnerId.ToString();
                if (previousOwnerId != Guid.Empty)
                    agent.MetaData["PreviousOwnerAvatarId"] = previousOwnerId.ToString();
                agent.MetaData["LastNFTTransferDate"] = DateTime.UtcNow.ToString("O");

                // Ensure password is set
                if (string.IsNullOrEmpty(agent.Password))
                {
                    var passwordReload = await AvatarManager.Instance.LoadAvatarAsync(agentId, false, false);
                    if (!passwordReload.IsError && passwordReload.Result != null)
                    {
                        agent.Password = passwordReload.Result.Password;
                    }
                }

                // Save agent
                var saveResult = await AvatarManager.Instance.SaveAvatarAsync(agent);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to sync agent ownership: {saveResult.Message}");
                    return result;
                }

                result.Result = true;
                result.Result = true;
                result.Message = $"Agent {agentId} ownership synced from NFT {nftId}. New owner: {newOwnerId}";
                LoggingManager.Log($"Agent ownership synced from NFT: Agent {agentId} -> Owner {newOwnerId}", LogType.Info);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error syncing agent ownership from NFT: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Check if an NFT represents an agent and sync ownership if needed
        /// Call this after NFT transfers to automatically update agent ownership
        /// </summary>
        public static async Task<OASISResult<bool>> CheckAndSyncAgentOwnershipFromNFTAsync(
            this A2AManager a2aManager,
            Guid nftId)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Load NFT
                var nftManager = new NFTManager(Guid.Empty, ProviderManager.Instance.OASISDNA);
                var nftResult = await nftManager.LoadWeb4NftAsync(nftId);

                if (nftResult.IsError || nftResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"NFT {nftId} not found: {nftResult.Message}");
                    return result;
                }

                var nft = nftResult.Result;

                // Check if NFT represents an agent
                if (nft.MetaData == null || !nft.MetaData.ContainsKey("AgentId"))
                {
                    result.Result = false;
                    result.Message = $"NFT {nftId} does not represent an agent";
                    return result;
                }

                var agentIdStr = nft.MetaData["AgentId"]?.ToString();
                if (string.IsNullOrEmpty(agentIdStr) || !Guid.TryParse(agentIdStr, out var agentId))
                {
                    OASISErrorHandling.HandleError(ref result, $"Invalid AgentId in NFT {nftId} metadata");
                    return result;
                }

                // Sync agent ownership from NFT
                var syncResult = await a2aManager.SyncAgentOwnershipFromNFTAsync(agentId, nftId);
                if (syncResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to sync agent ownership: {syncResult.Message}");
                    return result;
                }

                result.Result = true;
                result.Message = $"Agent {agentId} ownership synced from NFT {nftId}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error checking and syncing agent ownership: {ex.Message}", ex);
            }
            return result;
        }
    }
}





























