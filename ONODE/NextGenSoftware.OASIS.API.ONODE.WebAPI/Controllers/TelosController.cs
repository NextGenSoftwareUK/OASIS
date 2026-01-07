#if false
// TelosController temporarily disabled - missing TelosOASIS provider
using System;
using Microsoft.AspNetCore.Mvc;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core.Enums;
// using NextGenSoftware.OASIS.API.Providers.TelosOASIS; // Missing provider
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.Common;
using System.Threading.Tasks;

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
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;

                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));

                    _keyManager = new KeyManager(result.Result);
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
                    OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateStorageProvider(ProviderType.TelosOASIS);

                    //TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateProvider(ProviderType.TelosOASIS). Error details: ", result.Message));

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
        [HttpGet("get-telos-account-names-for-avatar")]
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
        [HttpGet("get-telos-accoun-private-key-for-avatar")]
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
        [HttpGet("get-telos-account")]
        public OASISResult<GetAccountResponseDto> GetTelosAccount(string telosAccountName)
        {
            return new(TelosOASIS.GetTelosAccount(telosAccountName));
        }

        /// <summary>
        /// Get's the Telos account for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-telos-account-for-avatar")]
        public OASISResult<GetAccountResponseDto> GetTelosAccountForAvatar(Guid avatarId)
        {
            return new(TelosOASIS.GetTelosAccountForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Avatar id for the the given Telos account name.
        /// </summary>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-id-for-telos-account-name")]
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
        [HttpGet("get-avatar-for-telos-account-name")]
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
        [HttpGet("get-balance-for-telos-account")]
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
        [HttpGet("get-balance-for-avatar")]
        public OASISResult<string> GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            return new(TelosOASIS.GetBalanceForAvatar(avatarId, code, symbol));
        }

        /// <summary>
        /// Link's a given telosAccount to the given avatar.
        /// </summary>
        /// <param name="walletId">The id of the avatar.</param>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{telosAccountName}")]
        public OASISResult<Guid> LinkTelosAccountToAvatar(Guid walletId, Guid avatarId, string telosAccountName)
        {
            return KeyManager.LinkProviderPublicKeyToAvatarById(walletId, avatarId, ProviderType.TelosOASIS, telosAccountName);
        }
    }
}
#endif
