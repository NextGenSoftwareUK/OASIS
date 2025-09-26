using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/eosio")]
    public class EOSIOController : OASISControllerBase
    {
        private EOSIOOASIS _EOSIOOASIS = null;
        private KeyManager _keyManager = null;

        EOSIOOASIS EOSIOOASIS
        {
            get
            {
                if (_EOSIOOASIS == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(async () => await OASISBootLoader.OASISBootLoader.GetAndActivateStorageProviderAsync(ProviderType.EOSIOOASIS)).Result;

                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateProvider(ProviderType.EOSIOOASIS). Error details: ", result.Message));

                    _EOSIOOASIS = (EOSIOOASIS)result.Result;
                }

                return _EOSIOOASIS;
            }
        }

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

        public EOSIOController()
        {

        }

        /// <summary>
        /// Get's the EOSIO account name for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-eosio-account-name-for-avatar")]
        public OASISResult<List<string>> GetEOSIOAccountNamesForAvatar(Guid avatarId)
        {
            return new(EOSIOOASIS.GetEOSIOAccountNamesForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the EOSIO private key for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-eosio-account-private-key-for-avatar")]
        public OASISResult<string> GetTelosAccountPrivateKeyForAvatar(Guid avatarId)
        {
            return new(EOSIOOASIS.GetEOSIOAccountPrivateKeyForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the EOSIO account.
        /// </summary>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-eosio-account")]
        public OASISResult<GetAccountResponseDto> GetEOSIOAccount(string eosioAccountName)
        {
            return new(EOSIOOASIS.GetEOSIOAccount(eosioAccountName));
        }

        /// <summary>
        /// Get's the EOSIO account for the given Avatar.
        /// </summary>
        /// <param name="avatarId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-eosio-account-for-avatar")]
        public OASISResult<GetAccountResponseDto> GetEOSIOAccountForAvatar(Guid avatarId)
        {
            return new(EOSIOOASIS.GetEOSIOAccountForAvatar(avatarId));
        }

        /// <summary>
        /// Get's the Avatar id for the the given EOS account name.
        /// </summary>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-id-for-eosio-account-name")]
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
        [HttpGet("get-avatar-for-eosio-account-name")]
        public OASISResult<IAvatar> GetAvatarForEOSIOAccountName(string eosioAccountName)
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
        [HttpGet("get-balance-for-eosio-account")]
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
        [HttpGet("get-balance-for-avatar")]
        public OASISResult<string> GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            return new(EOSIOOASIS.GetBalanceForAvatar(avatarId, code, symbol));
        }

        /// <summary>
        /// Link's a given eosioAccountName to the given avatar.
        /// </summary>
        /// <param name="walletId">The id of the wallet (if any).</param>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{eosioAccountName}")]
        public OASISResult<Guid> LinkEOSIOAccountToAvatar(Guid walletId, Guid avatarId, string eosioAccountName)
        {
            return KeyManager.LinkProviderPublicKeyToAvatarById(walletId, avatarId, ProviderType.EOSIOOASIS, eosioAccountName);
        }
    }
}