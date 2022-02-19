using Microsoft.AspNetCore.Mvc;
using System;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;

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
                {
                    OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateProvider(ProviderType.EOSIOOASIS);

                    //TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
                    if (result.IsError)
                        ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateProvider(ProviderType.EOSIOOASIS). Error details: ", result.Message), true, false, true);

                    _EOSIOOASIS = (EOSIOOASIS)result.Result;
                }

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
        public OASISResult<string> GetEOSIOAccountNameForAvatar(Guid avatarId)
        {
            return new(EOSIOOASIS.GetEOSIOAccountNameForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the EOSIO private key for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetEOSIOAccounPrivateKeyForAvatar")]
        public OASISResult<string> GetTelosAccounPrivateKeyForAvatar(Guid avatarId)
        {
            return new(EOSIOOASIS.GetEOSIOAccountPrivateKeyForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the EOSIO account.
        /// </summary>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetEOSIOAccount")]
        public OASISResult<Account> GetEOSIOAccount(string eosioAccountName)
        {
            return new(EOSIOOASIS.GetEOSIOAccount(eosioAccountName));
        }

        /// <summary>
        /// Get's the EOSIO account for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetEOSIOAccountForAvatar")]
        public OASISResult<Account> GetEOSIOAccountForAvatar(Guid avatarId)
        {
            return new(EOSIOOASIS.GetEOSIOAccountForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Avatar id for the the given EOS account name.
        /// </summary>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarIdForEOSIOAccountName")]
        public OASISResult<string> GetAvatarIdForEOSIOAccountName(string eosioAccountName)
        {
            return new(EOSIOOASIS.GetAvatarIdForEOSIOAccountName(eosioAccountName).ToString());
        }

        /// <summary>
        /// Get's the Avatar for the the given EOS account name.
        /// </summary>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAvatarForEOSIOAccountName")]
        public OASISResult<IAvatarDetail> GetAvatarForEOSIOAccountName(string eosioAccountName)
        {
            return new (EOSIOOASIS.GetAvatarForEOSIOAccountName(eosioAccountName));
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
        public OASISResult<string> GetBalanceForEOSIOAccount(string eosioAccountName, string code, string symbol)
        {
            return new(EOSIOOASIS.GetBalanceForEOSIOAccount(eosioAccountName, code, symbol));
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
        public OASISResult<string> GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            return new(EOSIOOASIS.GetBalanceForAvatar(avatarId, code, symbol));
        }

        /// <summary>
        /// Link's a given eosioAccountName to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{eosioAccountName}")]
        public OASISResult<bool> LinkEOSIOAccountToAvatar(Guid avatarId, string eosioAccountName)
        {
            return Program.AvatarManager.LinkPublicProviderKeyToAvatar(avatarId, ProviderType.EOSIOOASIS, eosioAccountName);
        }
    }
}
