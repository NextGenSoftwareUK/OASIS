using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Keys;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public partial class KeysController : OASISControllerBase
    {


        /// <summary>
        ///     Link's a given Avatar to a Providers Public Key (private/public key pairs or username, accountname, unique id, agentId, hash, etc).
        /// </summary>
        /// <param name="linkProviderKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("link-provider-public-key-to-avatar-by-id")]
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
        [HttpPost("link-provider-public-key-to-avatar-by-username")]
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
        [HttpPost("link-provider-public-key-to-avatar-by-email")]
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
        [HttpPost("link-provider-private-key-to-avatar-by-id")]
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
        [HttpPost("link-provider-private-key-to-avatar-by-username")]
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
        [HttpPost("link-provider-private-key-to-avatar-by-email")]
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
        [HttpPost("generate-keypair-and-link-provider-keys-to-avatar-by-id")]
        public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarId(LinkProviderKeyToAvatarParams generateKeyPairAndLinkProviderKeysToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(generateKeyPairAndLinkProviderKeysToAvatarParams);

            if (isValid)
                return KeyManager.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarById(avatarID, providerTypeToLinkTo, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPublicKey, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPrivateKey);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Generate's a new unique private/public keypair &amp; then links to the given avatar for the given provider type.
        /// </summary>
        /// <param name="generateKeyPairAndLinkProviderKeysToAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("generate-keypair-and-link-provider-keys-to-avatar-by-username")]
        public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarUsername(LinkProviderKeyToAvatarParams generateKeyPairAndLinkProviderKeysToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(generateKeyPairAndLinkProviderKeysToAvatarParams);

            if (isValid)
                return KeyManager.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByUsername(generateKeyPairAndLinkProviderKeysToAvatarParams.AvatarUsername, providerTypeToLinkTo, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPublicKey, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPrivateKey);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Generate's a new unique private/public keypair &amp; then links to the given avatar for the given provider type.
        /// </summary>
        /// <param name="generateKeyPairAndLinkProviderKeysToAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("generate-keypair-and-link-provider-keys-to-avatar-by-email")]
        public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarEmail(LinkProviderKeyToAvatarParams generateKeyPairAndLinkProviderKeysToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(generateKeyPairAndLinkProviderKeysToAvatarParams);

            if (isValid)
                return KeyManager.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByEmail(generateKeyPairAndLinkProviderKeysToAvatarParams.AvatarEmail, providerTypeToLinkTo, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPublicKey, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPrivateKey);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Get's a given avatar's unique storage key for the given provider type using the avatar's id.
        /// </summary>
        /// <param name="providerKeyForAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-provider-unique-storage-key-for-avatar-by-id")]
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
        [HttpGet("get-provider-unique-storage-key-for-avatar-by-username")]
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
        [HttpGet("get-provider-unique-storage-key-for-avatar-by-email")]
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
        [HttpGet("get-provider-private-key-for-avatar-by-id")]
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
        [HttpGet("get-provider-private-key-for-avatar-by-username")]
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
        //[HttpGet("get-provider-private-key-for-avatar-by-email")]
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
        [HttpGet("get-provider-public-keys-for-avatar-by-id")]
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
        [HttpGet("get-provider-public-keys-for-avatar-by-username")]
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
    }
}
