using Microsoft.AspNetCore.Mvc;
using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Config;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
using EOSNewYork.EOSCore.Response.API;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/eos")]
    public class EOSController : OASISControllerBase
    {
        //TODO: Finish moving these to EOSOASIS and making the cache avatar lookups generic for all providers.
        SEEDSOASIS _SEEDSOASIS = null;

        SEEDSOASIS SEEDSOASIS
        {
            get
            {
                if (_SEEDSOASIS == null)
                    _SEEDSOASIS = new SEEDSOASIS((EOSIOOASIS)OASISConfigManager.GetAndActivateProvider(ProviderType.EOSOASIS));

                return _SEEDSOASIS;
            }
        }

        public EOSController()
        {
            //_settings = OASISSettings.Value;
        }


        /// <summary>
        /// Get's the EOS account name for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetEOSAccountNameForAvatar")]
        public ActionResult<string> GetEOSAccountNameForAvatar(Guid avatarId)
        {
            return Ok(SEEDSOASIS.EOSIOOASIS.GetEOSAccountNameForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the EOS account.
        /// </summary>
        /// <param name="eosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetEOSAccount")]
        public ActionResult<Account> GetEOSAccount(string eosAccountName)
        {
            return Ok(SEEDSOASIS.EOSIOOASIS.GetEOSAccount(eosAccountName));
        }

        /// <summary>
        /// Get's the EOS account for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetEOSAccountForAvatar")]
        public ActionResult<Account> GetEOSAccountForAvatar(Guid avatarId)
        {
            return Ok(SEEDSOASIS.EOSIOOASIS.GetEOSAccountForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Avatar id for the the given EOS account name.
        /// </summary>
        /// <param name="eosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarIdForEOSAccountName")]
        public ActionResult<string> GetAvatarIdForEOSAccountName(string eosAccountName)
        {
            return Ok(SEEDSOASIS.EOSIOOASIS.GetAvatarIdForEOSAccountName(eosAccountName));
        }

        /// <summary>
        /// Get's the Avatar for the the given EOS account name.
        /// </summary>
        /// <param name="eosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarForEOSAccountName")]
        public ActionResult<string> GetAvatarForEOSAccountName(string eosAccountName)
        {
            return Ok(SEEDSOASIS.EOSIOOASIS.GetAvatarForEOSAccountName(eosAccountName));
        }

        /// <summary>
        /// Get's the SEEDS balance for the given EOS account.
        /// </summary>
        /// <param name="eosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetBalanceForEOSAccount")]
        public ActionResult<string> GetBalanceForEOSAccount(string eosAccountName)
        {
            return Ok();
           // return Ok(SEEDSOASIS.GetBalanceForEOSAccount(eosAccountName));
        }

        /// <summary>
        /// Get's the SEEDS balance for the given avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetBalanceForAvatar")]
        public ActionResult<string> GetBalanceForAvatar(Guid avatarId)
        {
            return Ok(SEEDSOASIS.GetBalanceForAvatar(avatarId));
        }

        /// <summary>
        /// Link's a given eosAccountName to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="eosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{eosAccountName}")]
        public IActionResult LinkEOSAccountToAvatar(Guid avatarId, string eosAccountName)
        {
            return Ok(Program.AvatarManager.LinkEOSAccountToAvatar(avatarId, eosAccountName));
        }
    }
}
