using Microsoft.AspNetCore.Mvc;
using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
using EOSNewYork.EOSCore.Response.API;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/eos")]
    public class EOSIOController : OASISControllerBase
    {
        //TODO: Finish moving these to EOSOASIS and making the cache avatar lookups generic for all providers.
        SEEDSOASIS _SEEDSOASIS = null;

        SEEDSOASIS SEEDSOASIS
        {
            get
            {
                if (_SEEDSOASIS == null)
                    _SEEDSOASIS = new SEEDSOASIS((EOSIOOASIS)OASISDNAManager.GetAndActivateProvider(ProviderType.EOSIOOASIS));

                return _SEEDSOASIS;
            }
        }

        public EOSIOController()
        {
            //_settings = OASISSettings.Value;
        }


        /// <summary>
        /// Get's the EOSIO account name for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetEOSIOAccountNameForAvatar")]
        public ActionResult<string> GetEOSIOAccountNameForAvatar(Guid avatarId)
        {
            return Ok(SEEDSOASIS.EOSIOOASIS.GetEOSIOAccountNameForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the EOSIO account.
        /// </summary>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetEOSIOAccount")]
        public ActionResult<Account> GetEOSIOAccount(string eosioAccountName)
        {
            return Ok(SEEDSOASIS.EOSIOOASIS.GetEOSIOAccount(eosioAccountName));
        }

        /// <summary>
        /// Get's the EOSIO account for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetEOSIOAccountForAvatar")]
        public ActionResult<Account> GetEOSIOAccountForAvatar(Guid avatarId)
        {
            return Ok(SEEDSOASIS.EOSIOOASIS.GetEOSIOAccountForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Avatar id for the the given EOS account name.
        /// </summary>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarIdForEOSIOAccountName")]
        public ActionResult<string> GetAvatarIdForEOSIOAccountName(string eosioAccountName)
        {
            return Ok(SEEDSOASIS.EOSIOOASIS.GetAvatarIdForEOSIOAccountName(eosioAccountName));
        }

        /// <summary>
        /// Get's the Avatar for the the given EOS account name.
        /// </summary>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarForEOSIOAccountName")]
        public ActionResult<string> GetAvatarForEOSIOAccountName(string eosioAccountName)
        {
            return Ok(SEEDSOASIS.EOSIOOASIS.GetAvatarForEOSIOAccountName(eosioAccountName));
        }

        /// <summary>
        /// Get's the EOSIO balance for the given EOSIO account.
        /// </summary>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetBalanceForEOSIOAccount")]
        public ActionResult<string> GetBalanceForEOSIOAccount(string eosioAccountName)
        {
            return Ok();
           // return Ok(SEEDSOASIS.GetBalanceForEOSAccount(eosAccountName));
        }

        /// <summary>
        /// Get's the EOSIO balance for the given avatar.
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
        /// Link's a given eosioAccountName to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{eosioAccountName}")]
        public IActionResult LinkEOSAccountToAvatar(Guid avatarId, string eosioAccountName)
        {
            return Ok(Program.AvatarManager.LinkEOSIOAccountToAvatar(avatarId, eosioAccountName));
        }
    }
}
