using Microsoft.AspNetCore.Mvc;
using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;

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
            return KeyManager.GetProviderPublicKeyForAvatar(avatarId, ProviderType.HoloOASIS);
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
            return KeyManager.GetProviderPrivateKeyForAvatar(avatarId, ProviderType.HoloOASIS);
        }

        /// <summary>
        /// Get's the Avatar id for the the given EOS account name.
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarIdForHolochainAgentId")]
        public OASISResult<Guid> GetAvatarIdForHolochainAgentId(string agentId)
        {
            //TODO: Test that returning a GUID works?
            return KeyManager.GetAvatarIdForProviderPublicKey(agentId, ProviderType.HoloOASIS);
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
            return KeyManager.GetAvatarForProviderPublicKey(agentId, ProviderType.HoloOASIS);
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
        public OASISResult<bool> LinkHolochainAgentIdToAvatar(Guid avatarId, string holochainAgentId, ProviderType providerToLoadSaveAvatarTo = ProviderType.Default)
        {
            return KeyManager.LinkProviderPublicKeyToAvatar(avatarId, ProviderType.HoloOASIS, holochainAgentId, providerToLoadSaveAvatarTo);
            //return Program.AvatarManager.LinkPublicProviderKeyToAvatar(avatarId, ProviderType.HoloOASIS, holochainAgentId);
        }
    }
}
