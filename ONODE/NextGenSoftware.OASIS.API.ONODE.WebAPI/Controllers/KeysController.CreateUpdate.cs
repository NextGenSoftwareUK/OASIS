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
        ///     Get's a given avatar's public keys for the given provider type using the avatar's email.
        /// </summary>
        /// <param name="providerKeyForAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-provider-public-keys-for-avatar-by-email")]
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
        [HttpGet("get-all-provider-public-keys-for-avatar-by-id/{id}")]
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
        [HttpGet("get-all-provider-public-keys-for-avatar-by-username/{username}")]
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
        [HttpGet("get-all-provider-public-keys-for-avatar-by-email/{email}")]
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
        [HttpGet("get-all-provider-private-keys-for-avatar-by-id/{id}")]
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
        [HttpGet("get-all-provider-private-keys-for-avatar-by-username/{username}")]
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
        //[HttpGet("get-all-provider-private-keys-for-avatar-by-email/{email}")]
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
        [HttpGet("get-all-provider-unique-storage-keys-for-avatar-by-id/{id}")]
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
        [HttpGet("get-all-provider-unique-storage-keys-for-avatar-by-username/{username}")]
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
        [HttpGet("get-all-provider-unique-storage-keys-for-avatar-by-email/{email}")]
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
        [HttpGet("get-avatar-id-for-provider-unique-storage-key/{providerKey}")]
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
        [HttpGet("get-avatar-username-for-provider-unique-storage-key/{providerKey}")]
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
        [HttpGet("get-avatar-email-for-provider-unique-storage-key/{providerKey}")]
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
        [HttpGet("get-avatar-for-provider-unique-storage-key/{providerKey}")]
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
        [HttpGet("get-avatar-id-for-provider-public-key/{providerKey}")]
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
        [HttpGet("get-avatar-username-for-provider-public-key/{providerKey}")]
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
        [HttpGet("get-avatar-email-for-provider-public-key/{providerKey}")]
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
        [HttpGet("get-avatar-for-provider-public-key/{providerKey}")]
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
        [HttpGet("get-avatar-id-for-provider-private-key/{providerKey}")]
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
        [HttpGet("get-avatar-username-for-provider-private-key/{providerKey}")]
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
        //[HttpGet("get-avatar-email-for-provider-private-key/{providerKey}")]
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
        [HttpGet("get-avatar-for-provider-private-key/{providerKey}")]
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
        [HttpPost("generate-keypair-for-provider/{providerType}")]
        public OASISResult<IKeyPairAndWallet> GenerateKeyPairForProvider(ProviderType providerType)
        {
            return KeyManager.GenerateKeyPairWithWalletAddress(providerType);
        }

        ///// <summary>
        /////     Generate's a new unique private/public keypair.
        ///// </summary>
        ///// <param name="keyPrefix"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("generate-keypair/{keyPrefix}")]
        //public OASISResult<IKeyPairAndWallet> GenerateKeyPair(string keyPrefix)
        //{
        //    return KeyManager.GenerateKeyPairWithWalletAddress(keyPrefix);
        //}

        /// <summary>
        ///     Get's the private WIF.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("get-private-wifi/{source}")]
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
        [HttpPost("get-public-wifi")]
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
        [HttpPost("decode-private-wif/{data}")]
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
        [HttpPost("base58-check-decode/{data}")]
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
        [HttpPost("encode-signature/{source}")]
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

    }
}
