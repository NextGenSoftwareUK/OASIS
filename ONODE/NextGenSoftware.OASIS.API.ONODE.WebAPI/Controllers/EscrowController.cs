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
    /// Escrow management endpoints
    /// Supports multi-party escrow with MNEE stablecoin
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class EscrowController : OASISControllerBase
    {
        private EscrowManager _escrowManager;

        public EscrowManager EscrowManager
        {
            get
            {
                if (_escrowManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));
                    _escrowManager = new EscrowManager(result.Result);
                }
                return _escrowManager;
            }
        }

        /// <summary>
        /// Create a new escrow contract
        /// </summary>
        [Authorize]
        [HttpPost("create")]
        public async Task<OASISResult<IEscrow>> CreateEscrow([FromBody] CreateEscrowRequest request)
        {
            // Use authenticated avatar as payer if not specified
            if (request.PayerAvatarId == Guid.Empty)
                request.PayerAvatarId = AvatarId;

            return await EscrowManager.CreateEscrowAsync(request);
        }

        /// <summary>
        /// Get escrow by ID
        /// </summary>
        [Authorize]
        [HttpGet("{escrowId}")]
        public async Task<OASISResult<IEscrow>> GetEscrow(Guid escrowId)
        {
            return await EscrowManager.GetEscrowAsync(escrowId);
        }

        /// <summary>
        /// Get all escrows for an avatar
        /// </summary>
        [Authorize]
        [HttpGet("avatar/{avatarId?}")]
        public async Task<OASISResult<List<IEscrow>>> GetEscrowsForAvatar(Guid? avatarId, [FromQuery] EscrowStatus? status = null)
        {
            var targetAvatarId = avatarId ?? AvatarId;
            if (targetAvatarId == Guid.Empty)
            {
                var result = new OASISResult<List<IEscrow>>();
                OASISErrorHandling.HandleError(ref result, "Avatar ID is required");
                return result;
            }

            return await EscrowManager.GetEscrowsForAvatarAsync(targetAvatarId, status);
        }

        /// <summary>
        /// Fund escrow by transferring MNEE from payer
        /// </summary>
        [Authorize]
        [HttpPost("{escrowId}/fund")]
        public async Task<OASISResult<string>> FundEscrow(Guid escrowId)
        {
            return await EscrowManager.FundEscrowAsync(escrowId, AvatarId);
        }

        /// <summary>
        /// Release escrow funds to payee
        /// </summary>
        [Authorize]
        [HttpPost("{escrowId}/release")]
        public async Task<OASISResult<string>> ReleaseEscrow(Guid escrowId)
        {
            return await EscrowManager.ReleaseEscrowAsync(escrowId, AvatarId);
        }
    }
}
