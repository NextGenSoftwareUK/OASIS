using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Treasury management endpoints
    /// Supports automated fund allocation and budget management with MNEE
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TreasuryController : OASISControllerBase
    {
        private TreasuryManager _treasuryManager;

        public TreasuryManager TreasuryManager
        {
            get
            {
                if (_treasuryManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));
                    _treasuryManager = new TreasuryManager(result.Result);
                }
                return _treasuryManager;
            }
        }

        /// <summary>
        /// Create a new treasury
        /// </summary>
        [Authorize]
        [HttpPost("create")]
        public async Task<OASISResult<ITreasury>> CreateTreasury([FromBody] CreateTreasuryRequest request)
        {
            // Use authenticated avatar as owner if not specified
            if (request.OwnerAvatarId == Guid.Empty)
                request.OwnerAvatarId = AvatarId;

            return await TreasuryManager.CreateTreasuryAsync(request);
        }

        /// <summary>
        /// Get treasury by ID
        /// </summary>
        [Authorize]
        [HttpGet("{treasuryId}")]
        public async Task<OASISResult<ITreasury>> GetTreasury(Guid treasuryId)
        {
            return await TreasuryManager.GetTreasuryAsync(treasuryId);
        }

        /// <summary>
        /// Get all treasuries for an avatar
        /// </summary>
        [Authorize]
        [HttpGet("avatar/{avatarId?}")]
        public async Task<OASISResult<List<ITreasury>>> GetTreasuriesForAvatar(Guid? avatarId = null)
        {
            var targetAvatarId = avatarId ?? AvatarId;
            if (targetAvatarId == Guid.Empty)
            {
                var result = new OASISResult<List<ITreasury>>();
                OASISErrorHandling.HandleError(ref result, "Avatar ID is required");
                return result;
            }

            return await TreasuryManager.GetTreasuriesForAvatarAsync(targetAvatarId);
        }

        /// <summary>
        /// Execute automated fund allocation workflow
        /// </summary>
        [Authorize]
        [HttpPost("{treasuryId}/allocate")]
        public async Task<OASISResult<Dictionary<string, string>>> ExecuteFundAllocation(Guid treasuryId)
        {
            return await TreasuryManager.ExecuteFundAllocationAsync(treasuryId);
        }

        /// <summary>
        /// Get treasury balance summary
        /// </summary>
        [Authorize]
        [HttpGet("{treasuryId}/balance")]
        public async Task<OASISResult<TreasuryBalanceSummary>> GetTreasuryBalanceSummary(Guid treasuryId)
        {
            return await TreasuryManager.GetTreasuryBalanceSummaryAsync(treasuryId);
        }
    }
}
