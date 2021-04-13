using Microsoft.AspNetCore.Mvc;
using System;
using NextGenSoftware.OASIS.API.Core.Enums;
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
        public ActionResult<string> GetHolochainAgentIdForAvatar(Guid avatarId)
        {
            return Ok(Program.AvatarManager.GetProviderKeyForAvatar(avatarId, ProviderType.HoloOASIS));
        }

        /// <summary>
        /// Get's the Holochain Agent's private key for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetHolochainAgentPrivateKeyForAvatar")]
        public ActionResult<string> GetHolochainAgentPrivateKeyForAvatar(Guid avatarId)
        {
            return Ok(Program.AvatarManager.GetProviderPrivateKeyForAvatar(avatarId, ProviderType.HoloOASIS));
        }

        ///// <summary>
        ///// Get's the EOSIO account.
        ///// </summary>
        ///// <param name="eosioAccountName"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("GetEOSIOAccount")]
        //public ActionResult<Account> GetEOSIOAccount(string eosioAccountName)
        //{
        //    return Ok(SEEDSOASIS.TelosOASIS.GetEOSIOAccount(eosioAccountName));
        //}

        ///// <summary>
        ///// Get's the EOSIO account for the given Avatar.
        ///// </summary>
        ///// <param name="avatarId"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("GetEOSIOAccountForAvatar")]
        //public ActionResult<Account> GetEOSIOAccountForAvatar(Guid avatarId)
        //{
        //    return Ok(SEEDSOASIS.TelosOASIS.GetEOSIOAccountForAvatar(avatarId));
        //}

        /// <summary>
        /// Get's the Avatar id for the the given EOS account name.
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarIdForHolochainAgentId")]
        public ActionResult<string> GetAvatarIdForHolochainAgentId(string agentId)
        {
            return Ok(Program.AvatarManager.GetAvatarIdForProviderKey(agentId, ProviderType.HoloOASIS));
        }

        /// <summary>
        /// Get's the Avatar for the the given Holochain agent id.
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarForHolochainAgentId")]
        public ActionResult<IAvatar> GetAvatarForHolochainAgentId(string agentId)
        {
            return Ok(Program.AvatarManager.GetAvatarForProviderKey(agentId, ProviderType.HoloOASIS));
        }

        /// <summary>
        /// Get's the HoloFuel balance for the given agent.
        /// </summary>
        /// <param name="agentID"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetHoloFuelBalanceForAgentId")]
        public ActionResult<string> GetHoloFuelBalanceForAgentId(string agentID)
        {
            return Ok();
        }

        /// <summary>
        /// Get's the EOSIO balance for the given avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetHoloFuelBalanceForAvatar")]
        public ActionResult<string> GetHoloFuelBalanceForAvatar(Guid avatarId)
        {
            return Ok();
        }

        /// <summary>
        /// Link's a given holochain AgentId to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="holochainAgentId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{holochainAgentId}")]
        public IActionResult LinkHolochainAgentIdToAvatar(Guid avatarId, string holochainAgentId)
        {
            return Ok(Program.AvatarManager.LinkProviderKeyToAvatar(avatarId, ProviderType.HoloOASIS, holochainAgentId));
        }
    }
}
