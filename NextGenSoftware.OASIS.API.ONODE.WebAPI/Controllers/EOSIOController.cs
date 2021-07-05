using Microsoft.AspNetCore.Mvc;
using System;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA.Manager;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/eosio")]
    public class EOSIOController : OASISControllerBase
    {
        EOSIOOASIS _EOSIOOASIS = null;

        EOSIOOASIS EOSIOOASIS
        {
            get
            {
                if (_EOSIOOASIS == null)
                    _EOSIOOASIS = (EOSIOOASIS)OASISDNAManager.GetAndActivateProvider(ProviderType.EOSIOOASIS);

                return _EOSIOOASIS;
            }
        }

        public EOSIOController()
        {

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
            return Ok(EOSIOOASIS.GetEOSIOAccountNameForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the EOSIO private key for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetEOSIOAccounPrivateKeyForAvatar")]
        public ActionResult<string> GetTelosAccounPrivateKeyForAvatar(Guid avatarId)
        {
            return Ok(EOSIOOASIS.GetEOSIOAccountPrivateKeyForAvatar(avatarId));
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
            return Ok(EOSIOOASIS.GetEOSIOAccount(eosioAccountName));
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
            return Ok(EOSIOOASIS.GetEOSIOAccountForAvatar(avatarId));
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
            return Ok(EOSIOOASIS.GetAvatarIdForEOSIOAccountName(eosioAccountName));
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
            return Ok(EOSIOOASIS.GetAvatarForEOSIOAccountName(eosioAccountName));
        }

        /// <summary>
        /// Get's the EOSIO balance for the given EOSIO account.
        /// </summary>
        /// <param name="eosioAccountName"></param>
        /// <param name="code"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetBalanceForEOSIOAccount")]
        public ActionResult<string> GetBalanceForEOSIOAccount(string eosioAccountName, string code, string symbol)
        {
            return Ok(EOSIOOASIS.GetBalanceForEOSIOAccount(eosioAccountName, code, symbol));
        }

        /// <summary>
        /// Get's the EOSIO balance for the given avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <param name="code"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetBalanceForAvatar")]
        public ActionResult<string> GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            return Ok(EOSIOOASIS.GetBalanceForAvatar(avatarId, code, symbol));
        }

        /// <summary>
        /// Link's a given eosioAccountName to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{eosioAccountName}")]
        public IActionResult LinkEOSIOAccountToAvatar(Guid avatarId, string eosioAccountName)
        {
            //return Ok(Program.AvatarManager.LinkEOSIOAccountToAvatar(avatarId, eosioAccountName));
            return Ok(Program.AvatarManager.LinkProviderKeyToAvatar(avatarId, ProviderType.EOSIOOASIS, eosioAccountName));
        }
    }
}
