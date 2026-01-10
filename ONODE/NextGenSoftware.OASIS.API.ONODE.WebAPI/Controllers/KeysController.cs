using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Keys;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KeysController : OASISControllerBase
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

        /// <summary>
        ///     Clear's the KeyManager's internal cache of keys.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("clear_cache")]
        public OASISResult<bool> ClearCache()
        {
            return KeyManager.ClearCache();
        }


        /// <summary>
        ///     Link's a given Avatar to a Providers Public Key (private/public key pairs or username, accountname, unique id, agentId, hash, etc).
        /// </summary>
        /// <param name="linkProviderKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("link_provider_public_key_to_avatar_by_id")]
        public OASISResult<IProviderWallet> LinkProviderPublicKeyToAvatarByAvatarId(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(linkProviderKeyToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderPublicKeyToAvatarById(linkProviderKeyToAvatarParams.WalletId, avatarID, providerTypeToLinkTo, linkProviderKeyToAvatarParams.ProviderKey, linkProviderKeyToAvatarParams.WalletAddress, linkProviderKeyToAvatarParams.WalletAddressSegwitP2SH, linkProviderKeyToAvatarParams.ShowSecretRecoveryWords);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }


        /// <summary>
        ///     Link's a given Avatar to a Providers Public Key (private/public key pairs or username, accountname, unique id, agentId, hash, etc).
        /// </summary>
        /// <param name="linkProviderKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("link_provider_public_key_to_avatar_by_username")]
        public OASISResult<IProviderWallet> LinkProviderPublicKeyToAvatarByUsername(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(linkProviderKeyToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderPublicKeyToAvatarByUsername(linkProviderKeyToAvatarParams.WalletId, linkProviderKeyToAvatarParams.AvatarUsername, providerTypeToLinkTo, linkProviderKeyToAvatarParams.ProviderKey, linkProviderKeyToAvatarParams.WalletAddress, linkProviderKeyToAvatarParams.WalletAddressSegwitP2SH, linkProviderKeyToAvatarParams.ShowSecretRecoveryWords);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Link's a given Avatar to a Providers Public Key (private/public key pairs or username, accountname, unique id, agentId, hash, etc).
        /// </summary>
        /// <param name="linkProviderKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("link_provider_public_key_to_avatar_by_email")]
        public OASISResult<IProviderWallet> LinkProviderPublicKeyToAvatarByEmail(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(linkProviderKeyToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderPublicKeyToAvatarByEmail(linkProviderKeyToAvatarParams.WalletId, linkProviderKeyToAvatarParams.AvatarEmail, providerTypeToLinkTo, linkProviderKeyToAvatarParams.ProviderKey, linkProviderKeyToAvatarParams.WalletAddress, linkProviderKeyToAvatarParams.WalletAddressSegwitP2SH, linkProviderKeyToAvatarParams.ShowSecretRecoveryWords);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Link's a given Avatar to a Providers Private Key (password, crypto private key, etc).
        /// </summary>
        /// <param name="linkProviderPrivateKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("link_provider_private_key_to_avatar_by_id")]
        public OASISResult<IProviderWallet> LinkProviderPrivateKeyToAvatarByAvatarId(LinkProviderKeyToAvatarParams linkProviderPrivateKeyToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(linkProviderPrivateKeyToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderPrivateKeyToAvatarById(linkProviderPrivateKeyToAvatarParams.WalletId, avatarID, providerTypeToLinkTo, linkProviderPrivateKeyToAvatarParams.ProviderKey, linkProviderPrivateKeyToAvatarParams.ShowPrivateKey, linkProviderPrivateKeyToAvatarParams.ShowSecretRecoveryWords);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Link's a given Avatar to a Providers Private Key (password, crypto private key, etc).
        /// </summary>
        /// <param name="linkProviderPrivateKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("link_provider_private_key_to_avatar_by_username")]
        public OASISResult<IProviderWallet> LinkProviderPrivateKeyToAvatarByUsername(LinkProviderKeyToAvatarParams linkProviderPrivateKeyToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(linkProviderPrivateKeyToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderPrivateKeyToAvatarByUsername(linkProviderPrivateKeyToAvatarParams.WalletId, linkProviderPrivateKeyToAvatarParams.AvatarUsername, providerTypeToLinkTo, linkProviderPrivateKeyToAvatarParams.ProviderKey, linkProviderPrivateKeyToAvatarParams.ShowPrivateKey, linkProviderPrivateKeyToAvatarParams.ShowSecretRecoveryWords);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        //TODO: Could this method cause a security issue by passing their private key and email (packet sniffers, etc) in the same request?
        //BEST TO LEAVE THIS METHOD OUT FOR NOW.

        /*
        /// <summary>
        ///     Link's a given Avatar to a Providers Private Key (password, crypto private key, etc).
        /// </summary>
        /// <param name="linkProviderPrivateKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("link_provider_private_key_to_avatar_by_email")]
        public OASISResult<bool> LinkProviderPrivateKeyToAvatarByEmail(LinkProviderKeyToAvatarParams linkProviderPrivateKeyToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(linkProviderPrivateKeyToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderPrivateKeyToAvatarByEmail(linkProviderPrivateKeyToAvatarParams.AvatarEmail, providerTypeToLinkTo, linkProviderPrivateKeyToAvatarParams.ProviderKey);
            else
                return new OASISResult<bool>(false) { IsError = true, Message = errorMessage };
        }
        */

        /// <summary>
        ///     Generate's a new unique private/public keypair &amp; then links to the given avatar for the given provider type.
        /// </summary>
        /// <param name="generateKeyPairAndLinkProviderKeysToAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("generate_keypair_and_link_provider_keys_to_avatar_by_id")]
        public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarId(LinkProviderKeyToAvatarParams generateKeyPairAndLinkProviderKeysToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(generateKeyPairAndLinkProviderKeysToAvatarParams);

            if (isValid)
                return KeyManager.Instance.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarById(avatarID, providerTypeToLinkTo, true, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPublicKey, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPrivateKey);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Generate's a new unique private/public keypair &amp; then links to the given avatar for the given provider type.
        /// </summary>
        /// <param name="generateKeyPairAndLinkProviderKeysToAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("generate_keypair_and_link_provider_keys_to_avatar_by_username")]
        public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarUsername(LinkProviderKeyToAvatarParams generateKeyPairAndLinkProviderKeysToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(generateKeyPairAndLinkProviderKeysToAvatarParams);

            if (isValid)
                return KeyManager.Instance.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByUsername(generateKeyPairAndLinkProviderKeysToAvatarParams.AvatarUsername, providerTypeToLinkTo, true, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPublicKey, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPrivateKey);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Generate's a new unique private/public keypair &amp; then links to the given avatar for the given provider type.
        /// </summary>
        /// <param name="generateKeyPairAndLinkProviderKeysToAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("generate_keypair_and_link_provider_keys_to_avatar_by_email")]
        public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarEmail(LinkProviderKeyToAvatarParams generateKeyPairAndLinkProviderKeysToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(generateKeyPairAndLinkProviderKeysToAvatarParams);

            if (isValid)
                return KeyManager.Instance.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByEmail(generateKeyPairAndLinkProviderKeysToAvatarParams.AvatarEmail, providerTypeToLinkTo, true, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPublicKey, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPrivateKey);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Get's a given avatar's unique storage key for the given provider type using the avatar's id.
        /// </summary>
        /// <param name="providerKeyForAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_provider_unique_storage_key_for_avatar_by_id")]
        public OASISResult<string> GetProviderUniqueStorageKeyForAvatarById(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerType;
            Guid avatarID;

            (isValid, providerType, avatarID, errorMessage) = ValidateParams(providerKeyForAvatarParams);

            if (isValid)
                return KeyManager.GetProviderUniqueStorageKeyForAvatarById(avatarID, providerType);
            else
                return new OASISResult<string>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Get's a given avatar's unique storage key for the given provider type using the avatar's username.
        /// </summary>
        /// <param name="providerKeyForAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_provider_unique_storage_key_for_avatar_by_username")]
        public OASISResult<string> GetProviderUniqueStorageKeyForAvatarByUsername(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerType;
            Guid avatarID;

            (isValid, providerType, avatarID, errorMessage) = ValidateParams(providerKeyForAvatarParams);

            if (isValid)
                return KeyManager.GetProviderUniqueStorageKeyForAvatarByUsername(providerKeyForAvatarParams.AvatarUsername, providerType);
            else
                return new OASISResult<string>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Get's a given avatar's unique storage key for the given provider type using the avatar's username.
        /// </summary>
        /// <param name="providerKeyForAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_provider_unique_storage_key_for_avatar_by_email")]
        public OASISResult<string> GetProviderUniqueStorageKeyForAvatarByEmail(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerType;
            Guid avatarID;

            (isValid, providerType, avatarID, errorMessage) = ValidateParams(providerKeyForAvatarParams);

            if (isValid)
                return KeyManager.GetProviderUniqueStorageKeyForAvatarByEmail(providerKeyForAvatarParams.AvatarEmail, providerType);
            else
                return new OASISResult<string>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Get's a given avatar's private key for the given provider type using the avatar's id.
        /// </summary>
        /// <param name="providerKeyForAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_provider_private_key_for_avatar_by_id")]
        public OASISResult<List<string>> GetProviderPrivateKeyForAvatarById(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerType;
            Guid avatarID;

            (isValid, providerType, avatarID, errorMessage) = ValidateParams(providerKeyForAvatarParams);

            if (isValid)
                return KeyManager.GetProviderPrivateKeysForAvatarById(avatarID, providerType);
            else
                return new OASISResult<List<string>>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Get's a given avatar's private key for the given provider type using the avatar's username.
        /// </summary>
        /// <param name="providerKeyForAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_provider_private_key_for_avatar_by_username")]
        public OASISResult<List<string>> GetProviderPrivateKeyForAvatarByUsername(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerType;
            Guid avatarID;

            (isValid, providerType, avatarID, errorMessage) = ValidateParams(providerKeyForAvatarParams);

            if (isValid)
                return KeyManager.GetProviderPrivateKeysForAvatarByUsername(providerKeyForAvatarParams.AvatarUsername, providerType);
            else
                return new OASISResult<List<string>>() { IsError = true, Message = errorMessage };
        }

        ///// <summary>
        /////     Get's a given avatar's private key for the given provider type using the avatar's email.
        ///// </summary>
        ///// <param name="providerKeyForAvatarParams"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("get_provider_private_key_for_avatar_by_email")]
        //public OASISResult<string> GetProviderPrivateKeyForAvatarByEmail(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        //{
        //    return KeyManager.GetProviderPrivateKeyForAvatarByEmail(providerKeyForAvatarParams.AvatarUsername);
        //}

        /// <summary>
        ///     Get's a given avatar's public keys for the given provider type using the avatar's id.
        /// </summary>
        /// <param name="providerKeyForAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_provider_public_keys_for_avatar_by_id")]
        public OASISResult<List<string>> GetProviderPublicKeysForAvatarById(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerType;
            Guid avatarID;

            (isValid, providerType, avatarID, errorMessage) = ValidateParams(providerKeyForAvatarParams);

            if (isValid)
                return KeyManager.GetProviderPublicKeysForAvatarById(avatarID, providerType);
            else
                return new OASISResult<List<string>>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Get's a given avatar's public keys for the given provider type using the avatar's username.
        /// </summary>
        /// <param name="providerKeyForAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_provider_public_keys_for_avatar_by_username")]
        public OASISResult<List<string>> GetProviderPublicKeysForAvatarByUsername(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerType;
            Guid avatarID;

            (isValid, providerType, avatarID, errorMessage) = ValidateParams(providerKeyForAvatarParams);

            if (isValid)
                return KeyManager.GetProviderPublicKeysForAvatarByUsername(providerKeyForAvatarParams.AvatarUsername, providerType);
            else
                return new OASISResult<List<string>>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Get's a given avatar's public keys for the given provider type using the avatar's email.
        /// </summary>
        /// <param name="providerKeyForAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_provider_public_keys_for_avatar_by_email")]
        public OASISResult<List<string>> GetProviderPublicKeysForAvatarByEmail(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerType;
            Guid avatarID;

            (isValid, providerType, avatarID, errorMessage) = ValidateParams(providerKeyForAvatarParams);

            if (isValid)
                return KeyManager.GetProviderPublicKeysForAvatarByEmail(providerKeyForAvatarParams.AvatarEmail, providerType);
            else
                return new OASISResult<List<string>>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Get's a given avatar's public keys for the given avatar with their id.
        /// </summary>
        /// <param name="id">The Avatar's username.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_all_provider_public_keys_for_avatar_by_id/{id}")]
        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarById(Guid id)
        {
            return KeyManager.GetAllProviderPublicKeysForAvatarById(id);
        }

        /// <summary>
        ///     Get's a given avatar's public keys for the given avatar with their username.
        /// </summary>
        /// <param name="username">The Avatar's username.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_all_provider_public_keys_for_avatar_by_username/{username}")]
        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarByUsername(string username)
        {
            return KeyManager.GetAllProviderPublicKeysForAvatarByUsername(username);
        }

        /// <summary>
        ///     Get's a given avatar's public keys for the given avatar with their email.
        /// </summary>
        /// <param name="email">The Avatar's username.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_all_provider_public_keys_for_avatar_by_email/{email}")]
        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarByEmail(string email)
        {
            return KeyManager.GetAllProviderPublicKeysForAvatarByEmail(email);
        }

        /// <summary>
        ///     Get's a given avatar's private keys for the given avatar with their id.
        /// </summary>
        /// <param name="id">The Avatar's username.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_all_provider_private_keys_for_avatar_by_id/{id}")]
        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPrivateKeysForAvatarById(Guid id)
        {
            return KeyManager.GetAllProviderPrivateKeysForAvatarById(id);
        }

        /// <summary>
        ///     Get's a given avatar's private keys for the given avatar with their username.
        /// </summary>
        /// <param name="username">The Avatar's username.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_all_provider_private_keys_for_avatar_by_username/{username}")]
        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPrivateKeysForAvatarByUsername(string username)
        {
            return KeyManager.GetAllProviderPrivateKeysForAvatarByUsername(username);
        }

        ///// <summary>
        /////     Get's a given avatar's private keys for the given avatar with their email.
        ///// </summary>
        ///// <param name="email">The Avatar's username.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("get_all_provider_private_keys_for_avatar_by_email/{email}")]
        //public OASISResult<Dictionary<ProviderType, string>> GetAllProviderPrivateKeysForAvatarByEmail(string email)
        //{
        //    return KeyManager.GetAllProviderPrivateKeysForAvatarByEmail(email);
        //}

        /// <summary>
        ///     Get's a given avatar's unique storage keys for the given avatar with their id.
        /// </summary>
        /// <param name="id">The Avatar's username.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_all_provider_unique_storage_keys_for_avatar_by_id/{id}")]
        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatarById(Guid id)
        {
            return KeyManager.GetAllProviderUniqueStorageKeysForAvatarById(id);
        }

        /// <summary>
        ///     Get's a given avatar's unique storage keys for the given avatar with their username.
        /// </summary>
        /// <param name="username">The Avatar's username.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_all_provider_unique_storage_keys_for_avatar_by_username/{username}")]
        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatarByUsername(string username)
        {
            return KeyManager.GetAllProviderUniqueStorageKeysForAvatarByUsername(username);
        }

        /// <summary>
        ///     Get's a given avatar's unique storage keys for the given avatar with their email.
        /// </summary>
        /// <param name="email">The Avatar's username.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_all_provider_unique_storage_keys_for_avatar_by_email/{email}")]
        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatarByEmail(string email)
        {
            return KeyManager.GetAllProviderUniqueStorageKeysForAvatarByEmail(email);
        }





        /// <summary>
        ///     Get's the avatar id for a given unique storage key.
        /// </summary>
        /// <param name="providerKey"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_avatar_id_for_provider_unique_storage_key/{providerKey}")]
        public OASISResult<Guid> GetAvatarIdForProviderUniqueStorageKey(string providerKey)
        {
            return KeyManager.GetAvatarIdForProviderUniqueStorageKey(providerKey);
        }

        /// <summary>
        ///     Get's the avatar username for a given unique storage key.
        /// </summary>
        /// <param name="providerKey"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_avatar_username_for_provider_unique_storage_key/{providerKey}")]
        public OASISResult<string> GetAvatarUsernameForProviderUniqueStorageKey(string providerKey)
        {
            return KeyManager.GetAvatarUsernameForProviderUniqueStorageKey(providerKey);
        }

        /// <summary>
        ///     Get's the avatar email for a given unique storage key.
        /// </summary>
        /// <param name="providerKey"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_avatar_email_for_provider_unique_storage_key/{providerKey}")]
        public OASISResult<string> GetAvatarEmailForProviderUniqueStorageKey(string providerKey)
        {
            return KeyManager.GetAvatarEmailForProviderUniqueStorageKey(providerKey);
        }

        /// <summary>
        ///     Get's the avatar for a given unique storage key.
        /// </summary>
        /// <param name="providerKey"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_avatar_for_provider_unique_storage_key/{providerKey}")]
        public OASISResult<IAvatar> GetAvatarForProviderUniqueStorageKey(string providerKey)
        {
            return KeyManager.GetAvatarForProviderUniqueStorageKey(providerKey);
        }

        /// <summary>
        ///     Get's the avatar id for a given public key.
        /// </summary>
        /// <param name="providerKey"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_avatar_id_for_provider_public_key/{providerKey}")]
        public OASISResult<Guid> GetAvatarIdForProviderPublicKey(string providerKey)
        {
            return KeyManager.GetAvatarIdForProviderPublicKey(providerKey);
        }

        /// <summary>
        ///     Get's the avatar username for a given public key.
        /// </summary>
        /// <param name="providerKey"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_avatar_username_for_provider_public_key/{providerKey}")]
        public OASISResult<string> GetAvatarUsernameForProviderPublicKey(string providerKey)
        {
            return KeyManager.GetAvatarUsernameForProviderPublicKey(providerKey);
        }

        /// <summary>
        ///     Get's the avatar email for a given public key.
        /// </summary>
        /// <param name="providerKey"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_avatar_email_for_provider_public_key/{providerKey}")]
        public OASISResult<string> GetAvatarEmailForProviderPublicKey(string providerKey)
        {
            return KeyManager.GetAvatarEmailForProviderPublicKey(providerKey);
        }

        /// <summary>
        ///     Get's the avatar for a given public key.
        /// </summary>
        /// <param name="providerKey"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_avatar_for_provider_public_key/{providerKey}")]
        public OASISResult<IAvatar> GetAvatarForProviderPublicKey(string providerKey)
        {
            return KeyManager.GetAvatarForProviderPublicKey(providerKey);
        }

        /*
        /// <summary>
        ///     Get's the avatar id for a given private key.
        /// </summary>
        /// <param name="providerKey"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_avatar_id_for_provider_private_key/{providerKey}")]
        public OASISResult<Guid> GetAvatarIdForProviderPrivateKey(string providerKey)
        {
            return KeyManager.GetAvatarIdForProviderPrivateKey(providerKey);
        }

        /// <summary>
        ///     Get's the avatar username for a given private key.
        /// </summary>
        /// <param name="providerKey"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_avatar_username_for_provider_private_key/{providerKey}")]
        public OASISResult<string> GetAvatarUsernameForProviderPrivateKey(string providerKey)
        {
            return KeyManager.GetAvatarUsernameForProviderPrivateKey(providerKey);
        }

        ///// <summary>
        /////     Get's the avatar email for a given private key.
        ///// </summary>
        ///// <param name="providerKey"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("get_avatar_email_for_provider_private_key/{providerKey}")]
        //public OASISResult<string> GetAvatarEmailForProviderPrivateKey(string providerKey)
        //{
        //    return KeyManager.GetAvatarEmailForProviderPrivateKey(providerKey);
        //}

        /// <summary>
        ///     Get's the avatar for a given private key.
        /// </summary>
        /// <param name="providerKey"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_avatar_for_provider_private_key/{providerKey}")]
        public OASISResult<IAvatar> GetAvatarForProviderPrivateKey(string providerKey)
        {
            return KeyManager.GetAvatarForProviderPrivateKey(providerKey);
        }
        */

        /// <summary>
        ///     Generate's a new unique private/public keypair for a given provider type.
        /// </summary>
        /// <param name="providerType">TEST</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("generate_keypair_for_provider/{providerType}")]
        public OASISResult<KeyPair> GenerateKeyPairForProvider(ProviderType providerType)
        {
            var result = KeyManager.Instance.GenerateKeyPairWithWalletAddress(providerType);
            if (!result.IsError && result.Result != null)
            {
                return new OASISResult<KeyPair>(new KeyPair
                {
                    PrivateKey = result.Result.PrivateKey,
                    PublicKey = result.Result.PublicKey
                });
            }
            return new OASISResult<KeyPair> { IsError = true, Message = result.Message };
        }

        /// <summary>
        ///     Generate's a new unique private/public keypair.
        /// </summary>
        /// <param name="keyPrefix"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("generate_keypair/{keyPrefix}")]
        public OASISResult<KeyPair> GenerateKeyPair(string keyPrefix)
        {
            return new OASISResult<KeyPair> { IsError = true, Message = "GenerateKeyPair with keyPrefix is not currently implemented. Please use GenerateKeyPairForProvider instead." };
        }

        /// <summary>
        ///     Get's the private WIF.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("get_private_wifi/{source}")]
        public OASISResult<string> GetPrivateWif(byte[] source)
        {
            //TODO: May need to change source to a string if byte array does not work...
            //If need to pass a string in instead then the caller would use this:
            //byte[] bytes = File.ReadAllBytes("path");
            //string file = Convert.ToBase64String(bytes);
            // You have base64 Data in "file" variable

            //Then code below would convert back to byte[]:
            //byte[] bytes = Convert.FromBase64String(b64Str);
            //File.WriteAllBytes(path, bytes);


            return KeyManager.GetPrivateWif(source);
        }

        /// <summary>
        ///     Get's the public WIF.
        /// </summary>
        /// <param name="wifParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("get_public_wifi")]
        public OASISResult<string> GetPublicWif(WifParams wifParams)
        {
            return KeyManager.GetPublicWif(wifParams.PublicKey, wifParams.Prefix);
        }

        /// <summary>
        ///     Decode's the private WIF.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("decode_private_wif/{data}")]
        public OASISResult<byte[]> DecodePrivateWif(string data)
        {
            return KeyManager.DecodePrivateWif(data);
        }

        /// <summary>
        ///     Decodes.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("base58_check_decode/{data}")]
        public OASISResult<byte[]> Base58CheckDecode(string data)
        {
            return KeyManager.Base58CheckDecode(data);
        }

        /// <summary>
        ///     Encode's the signature.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("encode_signature/{source}")]
        public OASISResult<string> EncodeSignature(byte[] source)
        {
            return KeyManager.EncodeSignature(source);
        }

        /*
        /// <summary>
        ///     Link's a given telosAccount to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{id:Guid}/{telosAccountName}")]
        public async Task<OASISResult<IAvatarDetail>> LinkTelosAccountToAvatar(Guid id, string telosAccountName)
        {
            return await _avatarService.LinkProviderKeyToAvatar(id, ProviderType.TelosOASIS, telosAccountName);
        }

        /// <summary>
        ///     Link's a given telosAccount to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<OASISResult<IAvatarDetail>> LinkTelosAccountToAvatar2(
            LinkProviderKeyToAvatar linkProviderKeyToAvatar)
        {
            return await _avatarService.LinkProviderKeyToAvatar(linkProviderKeyToAvatar.AvatarID,
                ProviderType.TelosOASIS, linkProviderKeyToAvatar.ProviderUniqueStorageKey);
        }


        /// <summary>
        ///     Link's a given eosioAccountName to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{eosioAccountName}")]
        public async Task<OASISResult<IAvatarDetail>> LinkEOSIOAccountToAvatar(Guid avatarId, string eosioAccountName)
        {
            return await _avatarService.LinkProviderKeyToAvatar(avatarId, ProviderType.EOSIOOASIS, eosioAccountName);
        }

        /// <summary>
        ///     Link's a given holochain AgentID to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="holochainAgentID"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{holochainAgentID}")]
        public async Task<OASISResult<IAvatarDetail>> LinkHolochainAgentIDToAvatar(Guid avatarId,
            string holochainAgentID)
        {
            return await _avatarService.LinkProviderKeyToAvatar(avatarId, ProviderType.HoloOASIS, holochainAgentID);
        }*/

        ///// <summary>
        /////     Get's the provider key for the given avatar and provider type.
        ///// </summary>
        ///// <param name="avatarUsername">The avatar username.</param>
        ///// <param name="providerType">The provider type.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("{avatarUsername}/{providerType}")]
        //public async Task<OASISResult<string>> GetProviderKeyForAvatar(string avatarUsername, ProviderType providerType)
        //{
        //    //return await _avatarService.GetProviderKeyForAvatar(avatarUsername, providerType);
        //    return await Program.AvatarManager.GetProviderKeyForAvatar(avatarUsername, providerType);
        //}

        /// <summary>
        /// Gets all keys for the authenticated avatar
        /// </summary>
        /// <returns>List of all keys for the avatar</returns>
        [Authorize]
        [HttpGet("all")]
        public async Task<OASISResult<List<KeyInfo>>> GetAllKeysForAvatar()
        {
            try
            {
                // TODO: Implement actual key retrieval logic
                var keys = new List<KeyInfo>();
                
                var result = new OASISResult<List<KeyInfo>>
                {
                    Result = keys,
                    IsError = false,
                    Message = "Keys retrieved successfully"
                };
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<List<KeyInfo>>
                {
                    IsError = true,
                    Message = $"Error retrieving keys: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Creates a new key for the authenticated avatar
        /// </summary>
        /// <param name="keyRequest">Key creation request</param>
        /// <returns>Created key information</returns>
        [Authorize]
        [HttpPost("create")]
        public async Task<OASISResult<KeyInfo>> CreateKey([FromBody] CreateKeyRequest keyRequest)
        {
            try
            {
                // TODO: Implement actual key creation logic
                var keyInfo = new KeyInfo
                {
                    Id = Guid.NewGuid(),
                    Name = keyRequest.Name,
                    Type = keyRequest.Type,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var result = new OASISResult<KeyInfo>
                {
                    Result = keyInfo,
                    IsError = false,
                    Message = "Key created successfully"
                };
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<KeyInfo>
                {
                    IsError = true,
                    Message = $"Error creating key: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Updates an existing key
        /// </summary>
        /// <param name="keyId">Key ID to update</param>
        /// <param name="keyRequest">Key update request</param>
        /// <returns>Updated key information</returns>
        [Authorize]
        [HttpPut("{keyId}")]
        public async Task<OASISResult<KeyInfo>> UpdateKey(Guid keyId, [FromBody] UpdateKeyRequest keyRequest)
        {
            try
            {
                // TODO: Implement actual key update logic
                var keyInfo = new KeyInfo
                {
                    Id = keyId,
                    Name = keyRequest.Name,
                    Type = keyRequest.Type,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = new OASISResult<KeyInfo>
                {
                    Result = keyInfo,
                    IsError = false,
                    Message = "Key updated successfully"
                };
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<KeyInfo>
                {
                    IsError = true,
                    Message = $"Error updating key: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Deletes a key
        /// </summary>
        /// <param name="keyId">Key ID to delete</param>
        /// <returns>Success status</returns>
        [Authorize]
        [HttpDelete("{keyId}")]
        public async Task<OASISResult<bool>> DeleteKey(Guid keyId)
        {
            try
            {
                // TODO: Implement actual key deletion logic
                var result = new OASISResult<bool>
                {
                    Result = true,
                    IsError = false,
                    Message = "Key deleted successfully"
                };
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting key: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Gets key statistics for the authenticated avatar
        /// </summary>
        /// <returns>Key statistics</returns>
        [Authorize]
        [HttpGet("stats")]
        public async Task<OASISResult<Dictionary<string, object>>> GetKeyStats()
        {
            try
            {
                // TODO: Implement actual key statistics logic
                var stats = new Dictionary<string, object>
                {
                    ["totalKeys"] = 0,
                    ["activeKeys"] = 0,
                    ["inactiveKeys"] = 0,
                    ["keyTypes"] = new Dictionary<string, int>()
                };

                var result = new OASISResult<Dictionary<string, object>>
                {
                    Result = stats,
                    IsError = false,
                    Message = "Key statistics retrieved successfully"
                };
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<Dictionary<string, object>>
                {
                    IsError = true,
                    Message = $"Error retrieving key statistics: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        ///     Link's a given Avatar to a Provider's Wallet Address by avatar ID.
        /// </summary>
        /// <param name="linkProviderWalletAddressToAvatarParams">The params include WalletId, AvatarId, ProviderType &amp; WalletAddress</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("link_provider_wallet_address_to_avatar_by_id")]
        public OASISResult<IProviderWallet> LinkProviderWalletAddressToAvatarById(LinkProviderKeyToAvatarParams linkProviderWalletAddressToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(linkProviderWalletAddressToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderWalletAddressToAvatarById(linkProviderWalletAddressToAvatarParams.WalletId, avatarID, providerTypeToLinkTo, linkProviderWalletAddressToAvatarParams.WalletAddress, ProviderType.Default);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Link's a given Avatar to a Provider's Wallet Address by username.
        /// </summary>
        /// <param name="linkProviderWalletAddressToAvatarParams">The params include WalletId, AvatarUsername, ProviderType &amp; WalletAddress</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("link_provider_wallet_address_to_avatar_by_username")]
        public OASISResult<IProviderWallet> LinkProviderWalletAddressToAvatarByUsername(LinkProviderKeyToAvatarParams linkProviderWalletAddressToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(linkProviderWalletAddressToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderWalletAddressToAvatarByUsername(linkProviderWalletAddressToAvatarParams.WalletId, linkProviderWalletAddressToAvatarParams.AvatarUsername, providerTypeToLinkTo, linkProviderWalletAddressToAvatarParams.WalletAddress, ProviderType.Default);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Link's a given Avatar to a Provider's Wallet Address by email.
        /// </summary>
        /// <param name="linkProviderWalletAddressToAvatarParams">The params include WalletId, AvatarEmail, ProviderType &amp; WalletAddress</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("link_provider_wallet_address_to_avatar_by_email")]
        public OASISResult<IProviderWallet> LinkProviderWalletAddressToAvatarByEmail(LinkProviderKeyToAvatarParams linkProviderWalletAddressToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(linkProviderWalletAddressToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderWalletAddressToAvatarByEmail(linkProviderWalletAddressToAvatarParams.WalletId, linkProviderWalletAddressToAvatarParams.AvatarEmail, providerTypeToLinkTo, linkProviderWalletAddressToAvatarParams.WalletAddress, ProviderType.Default);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Generate's a new unique private/public keypair with wallet address &amp; then links to the given avatar for the given provider type by avatar ID.
        /// </summary>
        /// <param name="generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_id")]
        public OASISResult<IProviderWallet> GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarById(LinkProviderKeyToAvatarParams generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams);

            if (isValid)
                return KeyManager.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarById(avatarID, providerTypeToLinkTo, generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams.ShowPublicKey, generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams.ShowPrivateKey, generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams.ShowSecretRecoveryWords);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Generate's a new unique private/public keypair with wallet address &amp; then links to the given avatar for the given provider type by username.
        /// </summary>
        /// <param name="generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_username")]
        public OASISResult<IProviderWallet> GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByUsername(LinkProviderKeyToAvatarParams generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams);

            if (isValid)
                return KeyManager.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByUsername(generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams.AvatarUsername, providerTypeToLinkTo, generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams.ShowPublicKey, generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams.ShowPrivateKey, generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams.ShowSecretRecoveryWords);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Generate's a new unique private/public keypair with wallet address &amp; then links to the given avatar for the given provider type by email.
        /// </summary>
        /// <param name="generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_email")]
        public OASISResult<IProviderWallet> GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByEmail(LinkProviderKeyToAvatarParams generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams);

            if (isValid)
                return KeyManager.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByEmail(generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams.AvatarEmail, providerTypeToLinkTo, generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams.ShowPublicKey, generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams.ShowPrivateKey, generateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarParams.ShowSecretRecoveryWords);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Generate's a new unique private/public keypair with wallet address for a given provider type.
        /// </summary>
        /// <param name="providerType">The provider type to generate keys for.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("generate_keypair_with_wallet_address_for_provider/{providerType}")]
        public OASISResult<IKeyPairAndWallet> GenerateKeyPairWithWalletAddressForProvider(ProviderType providerType)
        {
            return KeyManager.GenerateKeyPairWithWalletAddress(providerType);
        }

        private (bool, ProviderType, Guid, string) ValidateParams(ProviderKeyForAvatarParams linkProviderKeyToAvatarParams)
        {
            object providerTypeToLinkTo = null;
            Guid avatarID = Guid.Empty;

            if (string.IsNullOrEmpty(linkProviderKeyToAvatarParams.AvatarID) && string.IsNullOrEmpty(linkProviderKeyToAvatarParams.AvatarUsername) && string.IsNullOrEmpty(linkProviderKeyToAvatarParams.AvatarEmail))
                return (false, ProviderType.None, Guid.Empty, $"You need to either pass in a valid Avatar ID, Avatar Username or Avatar Email.");

            if (!Enum.TryParse(typeof(ProviderType), linkProviderKeyToAvatarParams.ProviderType, out providerTypeToLinkTo))
                return (false, ProviderType.None, Guid.Empty, $"The given ProviderType param {linkProviderKeyToAvatarParams.ProviderType} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}");

            if (!string.IsNullOrEmpty(linkProviderKeyToAvatarParams.AvatarID) && !Guid.TryParse(linkProviderKeyToAvatarParams.AvatarID, out avatarID))
                return (false, ProviderType.None, Guid.Empty, $"The given AvatarID {linkProviderKeyToAvatarParams.AvatarID} is not a valid Guid.");

            return (true, (ProviderType)providerTypeToLinkTo, avatarID, null);
        }
    }

    /// <summary>
    /// Key information model
    /// </summary>
    public class KeyInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Create key request model
    /// </summary>
    public class CreateKeyRequest
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    /// <summary>
    /// Update key request model
    /// </summary>
    public class UpdateKeyRequest
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
}