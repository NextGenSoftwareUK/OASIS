using Microsoft.AspNetCore.Mvc;
using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/holochain")]
    public class HolochainController : OASISControllerBase
    {
        public HolochainController()
        {

        }

        /// <summary>
        /// Get's the Holochain Agent ID for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetHolochainAgentIdForAvatar")]
        public OASISResult<string> GetHolochainAgentIdForAvatar(Guid avatarId)
        {
            return new(Program.AvatarManager.GetProviderKeyForAvatar(avatarId, ProviderType.HoloOASIS));
        }

        /// <summary>
        /// Get's the Holochain Agent's private key for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetHolochainAgentPrivateKeyForAvatar")]
        public OASISResult<string> GetHolochainAgentPrivateKeyForAvatar(Guid avatarId)
        {
            return new(Program.AvatarManager.GetPrivateProviderKeyForAvatar(avatarId, ProviderType.HoloOASIS));
        }

        /// <summary>
        /// Get's the Avatar id for the the given EOS account name.
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarIdForHolochainAgentId")]
        public OASISResult<string> GetAvatarIdForHolochainAgentId(string agentId)
        {
            return new(Program.AvatarManager.GetAvatarIdForProviderKey(agentId, ProviderType.HoloOASIS).ToString());
        }

        /// <summary>
        /// Get's the Avatar for the the given Holochain agent id.
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarForHolochainAgentId")]
        public OASISResult<IAvatar> GetAvatarForHolochainAgentId(string agentId)
        {
            return new(Program.AvatarManager.GetAvatarForProviderKey(agentId, ProviderType.HoloOASIS));
        }

        /// <summary>
        /// Get's the HoloFuel balance for the given agent.
        /// </summary>
        /// <param name="agentID"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetHoloFuelBalanceForAgentId")]
        public OASISResult<string> GetHoloFuelBalanceForAgentId(string agentID)
        {
            return new();
        }

        /// <summary>
        /// Get's the EOSIO balance for the given avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetHoloFuelBalanceForAvatar")]
        public OASISResult<string> GetHoloFuelBalanceForAvatar(Guid avatarId)
        {
            return new();
        }

        /// <summary>
        /// Link's a given holochain AgentId to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="holochainAgentId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{holochainAgentId}")]
        public OASISResult<IAvatarDetail> LinkHolochainAgentIdToAvatar(Guid avatarId, string holochainAgentId)
        {
            return new(Program.AvatarManager.LinkProviderKeyToAvatar(avatarId, ProviderType.HoloOASIS, holochainAgentId));
        }
    }
}
