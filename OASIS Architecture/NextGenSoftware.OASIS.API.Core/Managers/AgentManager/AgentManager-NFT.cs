using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Agent NFT integration methods - Enables agents to be traded as NFTs
    /// </summary>
    public partial class AgentManager
    {
        /// <summary>
        /// Mint an NFT for an agent (makes agent tradable)
        /// </summary>
        public async Task<OASISResult<IWeb4NFT>> MintAgentNFTAsync(
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
                var agentCardResult = await GetAgentCardAsync(agentId);
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
                    ["AgentCard"] = JsonConvert.SerializeObject(agentCard),
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
                    MetaData = nftMetadata == null ? new Dictionary<string, string>() : ToStringMetaData(nftMetadata),
                    OnChainProvider = new EnumValue<ProviderType>(onChainProvider),
                    OffChainProvider = new EnumValue<ProviderType>(offChainProvider),
                    SendToAvatarAfterMintingId = currentOwnerId.Value,
                    NumberToMint = 1,
                    StoreNFTMetaDataOnChain = false, // Store off-chain for now
                    NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>(NFTOffChainMetaType.OASIS),
                    NFTStandardType = new EnumValue<NFTStandardType>(NFTStandardType.ERC1155),
                    WaitTillNFTMinted = true,
                    WaitForNFTToMintInSeconds = 60,
                    AttemptToMintEveryXSeconds = 1
                };

                // Mint NFT using NFTManager (requires ONODE.Core)
                // Since AgentManager is in API.Core, we can't directly use NFTManager
                // This method will be called from ONODE.WebAPI where NFTManager is available
                // For now, return a result indicating the request is ready
                OASISErrorHandling.HandleError(ref result, 
                    "MintAgentNFTAsync must be called from ONODE.WebAPI context where NFTManager is available. " +
                    "Use the API endpoint POST /api/a2a/agent/{agentId}/mint-nft instead.");
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting agent NFT: {ex.Message}", ex);
            }
            return result;
        }

        private static Dictionary<string, string> ToStringMetaData(Dictionary<string, object> meta)
        {
            if (meta == null) return new Dictionary<string, string>();
            var outDict = new Dictionary<string, string>();
            foreach (var kv in meta)
                outDict[kv.Key] = kv.Value == null ? "" : (kv.Value is string s ? s : JsonConvert.SerializeObject(kv.Value));
            return outDict;
        }

        /// <summary>
        /// Link an existing NFT to an agent
        /// </summary>
        public async Task<OASISResult<bool>> LinkNFTToAgentAsync(Guid agentId, Guid nftId, string nftMintAddress = null, string nftMintTransactionHash = null, ProviderType nftOnChainProvider = ProviderType.SolanaOASIS)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Verify agent exists
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

                var agent = agentResult.Result;

                // Set metadata
                if (agent.MetaData == null)
                    agent.MetaData = new Dictionary<string, object>();

                agent.MetaData["NFTId"] = nftId.ToString();
                if (!string.IsNullOrEmpty(nftMintAddress))
                    agent.MetaData["NFTMintAddress"] = nftMintAddress;
                if (!string.IsNullOrEmpty(nftMintTransactionHash))
                    agent.MetaData["NFTMintTransactionHash"] = nftMintTransactionHash;
                agent.MetaData["NFTOnChainProvider"] = nftOnChainProvider.ToString();
                agent.MetaData["NFTLinkedDate"] = DateTime.UtcNow.ToString("O");

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
                    OASISErrorHandling.HandleError(ref result, $"Failed to link NFT to agent: {saveResult.Message}");
                    return result;
                }

                result.Result = true;
                result.Message = $"NFT {nftId} successfully linked to agent {agentId}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error linking NFT to agent: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get NFT information for an agent
        /// </summary>
        public async Task<OASISResult<Guid?>> GetAgentNFTIdAsync(Guid agentId)
        {
            var result = new OASISResult<Guid?>();
            try
            {
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

                if (agentResult.Result.MetaData != null && agentResult.Result.MetaData.ContainsKey("NFTId"))
                {
                    var nftIdStr = agentResult.Result.MetaData["NFTId"]?.ToString();
                    if (Guid.TryParse(nftIdStr, out var nftId))
                    {
                        result.Result = nftId;
                        result.Message = $"Agent {agentId} has NFT {nftId}";
                    }
                    else
                    {
                        result.Result = null;
                        result.Message = $"Invalid NFT ID format for agent {agentId}";
                    }
                }
                else
                {
                    result.Result = null;
                    result.Message = $"Agent {agentId} has no NFT linked";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting agent NFT: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Sync agent ownership from NFT ownership (called after NFT transfer)
        /// </summary>
        public async Task<OASISResult<bool>> SyncAgentOwnershipFromNFTAsync(Guid agentId, Guid nftId)
        {
            var result = new OASISResult<bool>();
            try
            {
                // This method requires NFTManager which is in ONODE.Core
                // It will be implemented in ONODE.WebAPI context
                OASISErrorHandling.HandleError(ref result, 
                    "SyncAgentOwnershipFromNFTAsync must be called from ONODE.WebAPI context where NFTManager is available.");
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error syncing agent ownership from NFT: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get agent owner, checking NFT ownership if agent is NFT-backed
        /// </summary>
        public async Task<OASISResult<Guid?>> GetAgentOwnerWithNFTCheckAsync(Guid agentId)
        {
            var result = new OASISResult<Guid?>();
            try
            {
                // First check if agent has NFT
                var nftIdResult = await GetAgentNFTIdAsync(agentId);
                if (!nftIdResult.IsError && nftIdResult.Result.HasValue)
                {
                    // Agent is NFT-backed - ownership comes from NFT
                    // This requires NFTManager which is in ONODE.Core
                    // For now, fall back to metadata ownership
                    // Full implementation will be in ONODE.WebAPI
                }

                // Fall back to metadata-based ownership
                return await GetAgentOwnerAsync(agentId);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting agent owner with NFT check: {ex.Message}", ex);
            }
            return result;
        }
    }
}
