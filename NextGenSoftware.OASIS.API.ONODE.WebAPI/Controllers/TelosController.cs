using System;
using Microsoft.AspNetCore.Mvc;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.TelosOASIS;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/telos")]
    public class TelosController : OASISControllerBase
    {
        private TelosOASIS _telosOASIS = null;
        private KeyManager _keyManager = null;

        public KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

                    if (result.IsError)
                        ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider(). Error details: ", result.Message), true, false, true);

                    _keyManager = new KeyManager(result.Result, Program.AvatarManager);
                }

                return _keyManager;
            }
        }

        TelosOASIS TelosOASIS
        {
            get
            {
                if (_telosOASIS == null)
                {
                    OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateProvider(ProviderType.TelosOASIS);

                    //TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
                    if (result.IsError)
                        ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateProvider(ProviderType.TelosOASIS). Error details: ", result.Message), true, false, true);

                    _telosOASIS = (TelosOASIS)result.Result;
                }

                return _telosOASIS;
            }
        }

        /// <summary>
        /// Get's the Telos account name for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetTelosAccountNamesForAvatar")]
        public OASISResult<List<string>> GetTelosAccountNameForAvatar(Guid avatarId)
        {
            return new(TelosOASIS.GetTelosAccountNamesForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Telos private key for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetTelosAccounPrivateKeyForAvatar")]
        public OASISResult<string> GetTelosAccounPrivateKeyForAvatar(Guid avatarId)
        {
            return new(TelosOASIS.GetTelosAccountPrivateKeyForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Telos account.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetTelosAccount")]
        public OASISResult<Account> GetTelosAccount(string telosAccountName)
        {
            return new(TelosOASIS.GetTelosAccount(telosAccountName));
        }

        /// <summary>
        /// Get's the Telos account for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetTelosAccountForAvatar")]
        public OASISResult<Account> GetTelosAccountForAvatar(Guid avatarId)
        {
            return new(TelosOASIS.GetTelosAccountForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Avatar id for the the given Telos account name.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarIdForTelosAccountName")]
        public OASISResult<string> GetAvatarIdForTelosAccountName(string telosAccountName)
        {
            return new(TelosOASIS.GetAvatarIdForTelosAccountName(telosAccountName).ToString());
        }

        /// <summary>
        /// Get's the Avatar for the the given Telos account name.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarForTelosAccountName")]
        public OASISResult<IAvatar> GetAvatarForTelosAccountName(string telosAccountName)
        {
            return new(TelosOASIS.GetAvatarForTelosAccountName(telosAccountName));
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
        public OASISResult<string> GetBalanceForTelosAccount(string telosAccountName, string code, string symbol)
        {
            return new(TelosOASIS.GetBalanceForTelosAccount(telosAccountName, code, symbol));
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
        public OASISResult<string> GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            return new(TelosOASIS.GetBalanceForAvatar(avatarId, code, symbol));
        }

        /// <summary>
        /// Link's a given telosAccount to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{telosAccountName}")]
        public OASISResult<bool> LinkTelosAccountToAvatar(Guid avatarId, string telosAccountName)
        {
            return KeyManager.LinkProviderPublicKeyToAvatarById(avatarId, ProviderType.TelosOASIS, telosAccountName);
        }
    }
}
