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
    [Route("api/telos")]
    public class TelosController : OASISControllerBase
    {
        //TODO: Finish moving these to TelosOASIS and making the cache avatar lookups generic for all providers.
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

        public TelosController()
        {
            //_settings = OASISSettings.Value;
        }


        /// <summary>
        /// Get's the Telos account name for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetTelosAccountNameForAvatar")]
        public ActionResult<string> GetTelosAccountNameForAvatar(Guid avatarId)
        {
            return Ok(SEEDSOASIS.EOSIOOASIS.GetEOSAccountNameForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Telos account.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetTelosAccount")]
        public ActionResult<Account> GetTelosAccount(string telosAccountName)
        {
            return Ok(SEEDSOASIS.EOSIOOASIS.GetEOSAccount(telosAccountName));
        }

        /// <summary>
        /// Get's the Telos account for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetTelosAccountForAvatar")]
        public ActionResult<Account> GetTelosAccountForAvatar(Guid avatarId)
        {
            return Ok(SEEDSOASIS.EOSIOOASIS.GetEOSAccountForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Avatar id for the the given Telos account name.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarIdForTelosAccountName")]
        public ActionResult<string> GetAvatarIdForTelosAccountName(string telosAccountName)
        {
            return Ok(SEEDSOASIS.EOSIOOASIS.GetAvatarIdForEOSAccountName(telosAccountName));
        }

        /// <summary>
        /// Get's the Avatar for the the given Telos account name.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarForTelosAccountName")]
        public ActionResult<string> GetAvatarForTelosAccountName(string telosAccountName)
        {
            return Ok(SEEDSOASIS.EOSIOOASIS.GetAvatarForEOSAccountName(telosAccountName));
        }

        /// <summary>
        /// Get's the SEEDS balance for the given Telos account.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetBalanceForTelosAccount")]
        public ActionResult<string> GetBalanceForTelosAccount(string telosAccountName)
        {
            return Ok(SEEDSOASIS.GetBalanceForTelosAccount(telosAccountName));
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
        /// Link's a given telosAccount to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{telosAccountName}")]
        public IActionResult LinkTelosAccountToAvatar(Guid avatarId, string telosAccountName)
        {
            return Ok(Program.AvatarManager.LinkTelosAccountToAvatar(avatarId, telosAccountName));
        }
    }
}
