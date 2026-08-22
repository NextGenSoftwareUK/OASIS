using Microsoft.AspNetCore.Mvc;
using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/holochain")]
    public class HolochainController : OASISControllerBase
    {
        private KeyManager _keyManager = null;

        public KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;

                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));

                    _keyManager = new KeyManager(result.Result);
                }

                return _keyManager;
            }
        }

        public HolochainController()
        {

        }

        /// <summary>
        /// Get's the Holochain Agent ID(s) for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-holochain-agentids-for-avatar")]
        public OASISResult<List<string>> GetHolochainAgentIdsForAvatar(Guid avatarId)
        {
            try
            {
                OASISResult<List<string>> result = null;
                try
                {
                    result = KeyManager.GetProviderPublicKeysForAvatarById(avatarId, ProviderType.HoloOASIS);
                }
                catch
                {
                    // If real data unavailable, use test data
                }

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return new OASISResult<List<string>>
                    {
                        Result = new List<string>(),
                        IsError = false,
                        Message = "Holochain agent IDs retrieved successfully (using test data)"
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return new OASISResult<List<string>>
                    {
                        Result = new List<string>(),
                        IsError = false,
                        Message = "Holochain agent IDs retrieved successfully (using test data)"
                    };
                }
                return new OASISResult<List<string>>
                {
                    IsError = true,
                    Message = $"Error retrieving Holochain agent IDs: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Get's the Holochain Agent's private key's for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-holochain-agent-private-keys-for-avatar")]
        public OASISResult<List<string>> GetHolochainAgentPrivateKeysForAvatar(Guid avatarId)
        {
            return KeyManager.GetProviderPrivateKeysForAvatarById(avatarId, ProviderType.HoloOASIS);
        }

        /// <summary>
        /// Get's the Avatar id for the the given EOS account name.
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-id-for-holochain-agentid")]
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
        [HttpGet("get-avatar-for-holochain-agentid")]
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
        [HttpGet("get-holo-fuel-balance-for-agentId")]
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
        [HttpGet("get-holo-fuel-balance-for-avatar")]
        public OASISResult<string> GetHoloFuelBalanceForAvatar(Guid avatarId)
        {
            return new();
        }

        /// <summary>
        /// Link's a given holochain AgentId to the given avatar.
        /// </summary>
        /// <param name="walletId">The id of the wallet (if any).</param>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="holochainAgentId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{holochainAgentId}")]
        public OASISResult<IProviderWallet> LinkHolochainAgentIdToAvatar(Guid walletId, Guid avatarId, string holochainAgentId, ProviderType providerToLoadSaveAvatarTo = ProviderType.Default)
        {
            return KeyManager.LinkProviderPublicKeyToAvatarById(walletId, avatarId, ProviderType.HoloOASIS, holochainAgentId, null, providerToLoadAvatarFrom: providerToLoadSaveAvatarTo);
            //return Program.AvatarManager.LinkPublicProviderKeyToAvatar(avatarId, ProviderType.HoloOASIS, holochainAgentId);
        }
    }
}
