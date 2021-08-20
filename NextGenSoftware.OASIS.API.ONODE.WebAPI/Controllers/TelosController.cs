using System;
using Microsoft.AspNetCore.Mvc;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.TelosOASIS;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/telos")]
    public class TelosController : OASISControllerBase
    {
        TelosOASIS _telosOASIS = null;

        TelosOASIS TelosOASIS
        {
            get
            {
                if (_telosOASIS == null)
                {
                    OASISResult<IOASISStorage> result = OASISBootLoader.OASISBootLoader.GetAndActivateProvider(ProviderType.TelosOASIS);

                    //TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
                    if (result.IsError)
                        ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateProvider(ProviderType.TelosOASIS). Error details: ", result.Message), true, false, true);

                    _telosOASIS = (TelosOASIS)result.Result;
                }

                return _telosOASIS;
            }
        }

        public TelosController()
        {

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
            return Ok(TelosOASIS.GetTelosAccountNameForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Telos private key for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetTelosAccounPrivateKeyForAvatar")]
        public ActionResult<string> GetTelosAccounPrivateKeyForAvatar(Guid avatarId)
        {
            return Ok(TelosOASIS.GetTelosAccountPrivateKeyForAvatar(avatarId));
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
            return Ok(TelosOASIS.GetTelosAccount(telosAccountName));
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
            return Ok(TelosOASIS.GetTelosAccountForAvatar(avatarId));
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
            return Ok(TelosOASIS.GetAvatarIdForTelosAccountName(telosAccountName));
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
            return Ok(TelosOASIS.GetAvatarForTelosAccountName(telosAccountName));
        }

        /// <summary>
        /// Get's the Telos balance for the given Telos account.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <param name="code"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetBalanceForTelosAccount")]
        public ActionResult<string> GetBalanceForTelosAccount(string telosAccountName, string code, string symbol)
        {
            return Ok(TelosOASIS.GetBalanceForTelosAccount(telosAccountName, code, symbol));
        }

        /// <summary>
        /// Get's the Telos balance for the given avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <param name="code"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetBalanceForAvatar")]
        public ActionResult<string> GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            return Ok(TelosOASIS.GetBalanceForAvatar(avatarId, code, symbol));
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
            return Ok(Program.AvatarManager.LinkProviderKeyToAvatar(avatarId, ProviderType.TelosOASIS, telosAccountName));
        }
    }
}
