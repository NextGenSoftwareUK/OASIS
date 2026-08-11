using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public partial class COSMICManager
    {

        /// <summary>
        /// Gets The Grand Simulation system multiverse
        /// </summary>
        public async Task<OASISResult<IMultiverse>> GetGrandSimulationAsync()
        {
            var result = new OASISResult<IMultiverse>();

            try
            {
                var searchResult = await SearchHolonsForParentAsync<Holon>(
                    "The Grand Simulation",
                    default(Guid),
                    default(Guid),
                    null,
                    MetaKeyValuePairMatchMode.All,
                    false,
                    HolonType.Multiverse,
                    ProviderType.Default
                );

                if (!searchResult.IsError && searchResult.Result != null)
                {
                    var grandSim = searchResult.Result.FirstOrDefault(m => 
                        m.Name.Equals("The Grand Simulation", StringComparison.OrdinalIgnoreCase));
                    if (grandSim != null)
                    {
                        result.Result = grandSim as IMultiverse;
                        return result;
                    }
                }

                OASISErrorHandling.HandleError(ref result, "The Grand Simulation not found.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting The Grand Simulation: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Factory method to create a Multiverse instance
        /// </summary>
        public async Task<OASISResult<IMultiverse>> CreateMultiverseFactoryAsync(
            IOmiverse parentOmniverse,
            string name,
            string description = null)
        {
            var result = new OASISResult<IMultiverse>();

            try
            {
                if (parentOmniverse == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Parent Omniverse cannot be null.");
                    return result;
                }

                // Create a new Holon with Multiverse type and cast to IMultiverse
                var multiverseHolon = new NextGenSoftware.OASIS.API.Core.Holons.Holon(HolonType.Multiverse)
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description ?? $"Multiverse: {name}",
                    HolonType = HolonType.Multiverse,
                    CreatedByAvatarId = AvatarId,
                    ModifiedByAvatarId = AvatarId,
                    IsNewHolon = true,
                    ParentOmniverseId = parentOmniverse.Id,
                    ParentOmniverse = parentOmniverse,
                    ParentHolonId = parentOmniverse.Id,
                    ParentHolon = parentOmniverse,
                    ParentCelestialSpaceId = parentOmniverse.Id,
                    ParentCelestialSpace = parentOmniverse
                };

                // Save the multiverse
                var saveResult = await SaveHolonAsync<Holon>(multiverseHolon, AvatarId);
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                if (!saveResult.IsError && saveResult.Result != null)
                {
                    result.Result = saveResult.Result as IMultiverse;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating multiverse: {ex.Message}", ex);
            }

            return result;
        }



        /// <summary>
        /// Creates a proposal for The Grand Simulation
        /// </summary>
        public async Task<OASISResult<ISimulationProposal>> CreateSimulationProposalAsync(
            IHolon proposedHolon, Guid parentUniverseId)
        {
            var result = new OASISResult<ISimulationProposal>();

            try
            {
                if (proposedHolon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Proposed holon cannot be null.");
                    return result;
                }

                // Validate that parent is a Universe (top level for proposals)
                var parentLoad = await LoadTypedHolonAsync<IUniverse>(parentUniverseId, HolonType.Universe);
                if (parentLoad.IsError || parentLoad.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(parentLoad, result);
                    OASISErrorHandling.HandleError(ref result, "Parent must be a Universe for simulation proposals.");
                    return result;
                }

                // Get avatar name for proposal
                string avatarName = AvatarId.ToString();
                try
                {
                    var avatarLoad = await AvatarManager.Instance.LoadAvatarAsync(AvatarId);
                    if (!avatarLoad.IsError && avatarLoad.Result != null)
                    {
                        avatarName = avatarLoad.Result.Username ?? avatarLoad.Result.Name ?? AvatarId.ToString();
                    }
                }
                catch
                {
                    // Use ID if name lookup fails
                }

                // Create SimulationProposal holon
                var proposal = new SimulationProposal
                {
                    Id = Guid.NewGuid(),
                    Name = $"Proposal: {proposedHolon.Name}",
                    Description = $"Proposal for {proposedHolon.HolonType}: {proposedHolon.Description ?? ""}",
                    HolonType = HolonType.Proposal,
                    CreatedByAvatarId = AvatarId,
                    CreatedByAvatarName = avatarName,
                    CreatedDate = DateTime.UtcNow,
                    ProposedHolon = proposedHolon,
                    ProposedHolonId = proposedHolon.Id,
                    ProposedHolonType = proposedHolon.HolonType,
                    ProposedHolonName = proposedHolon.Name,
                    ProposedHolonDescription = proposedHolon.Description ?? "",
                    ParentUniverseId = parentUniverseId,
                    ProposalCategory = $"Universe.{parentUniverseId}",
                    ParentHolonId = parentUniverseId,
                    IsNewHolon = true
                };

                // Save the proposal holon using generic overload
                var saveResult = await SaveHolonAsync<ISimulationProposal>(proposal);
                if (saveResult.IsError || saveResult.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                    return result;
                }

                result.Result = saveResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating simulation proposal: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Lists all simulation proposals
        /// </summary>
        public async Task<OASISResult<IEnumerable<ISimulationProposal>>> ListSimulationProposalsAsync(bool onlyMine = false)
        {
            var result = new OASISResult<IEnumerable<ISimulationProposal>>();

            try
            {
                // Load all Proposal holons with ProposalType = "Simulation"
                var searchResult = await SearchHolonsForParentAsync<Holon>(
                    "Simulation",
                    onlyMine ? AvatarId : default(Guid),
                    default(Guid),
                    null,
                    MetaKeyValuePairMatchMode.All,
                    onlyMine,
                    HolonType.Proposal,
                    ProviderType.Default
                );

                if (searchResult.IsError)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(searchResult, result);
                    return result;
                }

                // Filter to only Simulation proposals
                var proposals = new List<ISimulationProposal>();
                if (searchResult.Result != null)
                {
                    foreach (var proposal in searchResult.Result)
                    {
                        if (proposal is ISimulationProposal simProposal && 
                            simProposal.ProposalType == "Simulation")
                        {
                            proposals.Add(simProposal);
                        }
                    }
                }

                result.Result = proposals;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error listing simulation proposals: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Votes on a simulation proposal (accept or reject)
        /// </summary>
        public async Task<OASISResult<bool>> VoteOnSimulationProposalAsync(
            Guid proposalId, bool accept)
        {
            var result = new OASISResult<bool>();

            try
            {
                // Load the proposal holon using generic overload
                var loadResult = await Data.LoadHolonAsync<Holon>(proposalId, childHolonType: HolonType.Proposal);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                    OASISErrorHandling.HandleError(ref result, "Proposal not found.");
                    return result;
                }

                var proposalHolon = loadResult.Result;
                var proposal = proposalHolon as ISimulationProposal;
                
                if (proposal == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Proposal is not a valid ISimulationProposal.");
                    return result;
                }

                // Check if user already voted
                if (proposal.HasUserVoted(AvatarId))
                {
                    OASISErrorHandling.HandleError(ref result, "You have already voted on this proposal. Only one vote per user is allowed.");
                    return result;
                }

                // Add vote
                if (!proposal.AddVote(AvatarId, accept))
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to add vote.");
                    return result;
                }

                // Save updated proposal
                var saveResult = await SaveHolonAsync<Holon>(proposalHolon, AvatarId);
                if (saveResult.IsError)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                    return result;
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error voting on proposal: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Gets user's vote on a proposal (if they voted)
        /// </summary>
        public async Task<OASISResult<bool?>> GetUserVoteOnProposalAsync(Guid proposalId)
        {
            var result = new OASISResult<bool?>();

            try
            {
                // Load the proposal holon using generic overload
                var loadResult = await Data.LoadHolonAsync<Holon>(proposalId, childHolonType: HolonType.Proposal);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                    return result;
                }

                var proposalHolon = loadResult.Result;
                var proposal = proposalHolon as ISimulationProposal;
                
                if (proposal == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Proposal is not a valid ISimulationProposal.");
                    return result;
                }

                // Get user's vote
                result.Result = proposal.GetUserVote(AvatarId);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting user vote: {ex.Message}", ex);
            }

            return result;
        }

    }
}
