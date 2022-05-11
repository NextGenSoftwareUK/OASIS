using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;

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
                    OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

                    if (result.IsError)
                        ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider(). Error details: ", result.Message), true, false, true);

                    _keyManager = new KeyManager(result.Result, Program.AvatarManager);
                }

                return _keyManager;
            }
        }

        /// <summary>
        ///     Link's a given Avatar to a Providers Public Key (private/public key pairs or username, accountname, unique id, agentId, hash, etc).
        /// </summary>
        /// <param name="linkProviderKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("link_provider_public_key_to_avatar_by_id")]
        public OASISResult<bool> LinkProviderPublicKeyToAvatarByAvatarId(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(linkProviderKeyToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderPublicKeyToAvatar(avatarID, providerTypeToLinkTo, linkProviderKeyToAvatarParams.ProviderKey);
            else
                return new OASISResult<bool>(false) { IsError = true, Message = errorMessage };
        }


        /// <summary>
        ///     Link's a given Avatar to a Providers Public Key (private/public key pairs or username, accountname, unique id, agentId, hash, etc).
        /// </summary>
        /// <param name="linkProviderKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("link_provider_public_key_to_avatar_by_username")]
        public OASISResult<bool> LinkProviderPublicKeyToAvatarByUsername(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(linkProviderKeyToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderPublicKeyToAvatar(linkProviderKeyToAvatarParams.AvatarUsername, providerTypeToLinkTo, linkProviderKeyToAvatarParams.ProviderKey);
            else
                return new OASISResult<bool>(false) { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Link's a given Avatar to a Providers Private Key (password, crypto private key, etc).
        /// </summary>
        /// <param name="linkProviderPrivateKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("link_provider_private_key_to_avatar_by_id")]
        public OASISResult<bool> LinkProviderPrivateKeyToAvatarByAvatarId(LinkProviderKeyToAvatarParams linkProviderPrivateKeyToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(linkProviderPrivateKeyToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderPrivateKeyToAvatar(avatarID, providerTypeToLinkTo, linkProviderPrivateKeyToAvatarParams.ProviderKey);
            else
                return new OASISResult<bool>(false) { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Link's a given Avatar to a Providers Private Key (password, crypto private key, etc).
        /// </summary>
        /// <param name="linkProviderPrivateKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("link_provider_private_key_to_avatar_by_username")]
        public OASISResult<bool> LinkProviderPrivateKeyToAvatarByUsername(LinkProviderKeyToAvatarParams linkProviderPrivateKeyToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(linkProviderPrivateKeyToAvatarParams);

            if (isValid)
                return KeyManager.LinkProviderPrivateKeyToAvatar(linkProviderPrivateKeyToAvatarParams.AvatarUsername, providerTypeToLinkTo, linkProviderPrivateKeyToAvatarParams.ProviderKey);
            else
                return new OASISResult<bool>(false) { IsError = true, Message = errorMessage };
        }

        /*
        /// <summary>
        ///     Generate's a new unique private/public keypair &amp; then links to the given avatar for the given provider type.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("GenerateKeyPairAndLinkProviderKeysToAvatar")]
        public OASISResult<KeyPair> GenerateKeyPairAndLinkProviderKeysToAvatar(Guid avatarId, string providerTypeToLinkTo, string providerTypeToloadAvatarFrom)
        {
            object providerTypeToLinkToObject = null;
            object providerTypeToLoadAvatarFromObject = null;
            ProviderType providerTypeToLinkToEnumValue = ProviderType.Default;
            ProviderType providerTypeToloadAvatarFromEnumValue = ProviderType.Default;

            if (string.IsNullOrEmpty(providerTypeToLinkTo))
                return (new OASISResult<KeyPair> { IsError = true, Message = $"The providerTypeToLinkTo param cannot be null. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" });

            if (!string.IsNullOrEmpty(providerTypeToLinkTo) && !Enum.TryParse(typeof(ProviderType), providerTypeToLinkTo, out providerTypeToLinkToObject))
                return (new OASISResult<KeyPair> { IsError = true, Message = $"The given providerTypeToLinkTo {providerTypeToLinkTo} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" });

            if (!string.IsNullOrEmpty(providerTypeToloadAvatarFrom) && !Enum.TryParse(typeof(ProviderType), providerTypeToloadAvatarFrom, out providerTypeToLoadAvatarFromObject))
                return (new OASISResult<KeyPair> { IsError = true, Message = $"The given providerTypeToloadAvatarFrom {providerTypeToloadAvatarFrom} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" });

            if (providerTypeToLinkToObject != null)
                providerTypeToLinkToEnumValue = (ProviderType)providerTypeToLinkToObject;

            if (providerTypeToLoadAvatarFromObject != null)
                providerTypeToloadAvatarFromEnumValue = (ProviderType)providerTypeToLoadAvatarFromObject;

            return KeyManager.GenerateKeyPairAndLinkProviderKeysToAvatar(avatarId, providerTypeToLinkToEnumValue, providerTypeToloadAvatarFromEnumValue);
        }*/


        /// <summary>
        ///     Generate's a new unique private/public keypair &amp; then links to the given avatar for the given provider type.
        /// </summary>
        /// <param name="generateKeyPairAndLinkProviderKeysToAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("generate_keypair_and_link_provider_keys_to_avatar_by_id")]
        public OASISResult<KeyPair> GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarId(LinkProviderKeyToAvatarParams generateKeyPairAndLinkProviderKeysToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(generateKeyPairAndLinkProviderKeysToAvatarParams);

            if (isValid)
                return KeyManager.GenerateKeyPairAndLinkProviderKeysToAvatar(avatarID, providerTypeToLinkTo);
            else
                return new OASISResult<KeyPair>() { IsError = true, Message = errorMessage };
        }

        /// <summary>
        ///     Generate's a new unique private/public keypair &amp; then links to the given avatar for the given provider type.
        /// </summary>
        /// <param name="generateKeyPairAndLinkProviderKeysToAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("generate_keypair_and_link_provider_keys_to_avatar_by_username")]
        public OASISResult<KeyPair> GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarUsername(LinkProviderKeyToAvatarParams generateKeyPairAndLinkProviderKeysToAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerTypeToLinkTo;
            Guid avatarID;

            (isValid, providerTypeToLinkTo, avatarID, errorMessage) = ValidateParams(generateKeyPairAndLinkProviderKeysToAvatarParams);

            if (isValid)
                return KeyManager.GenerateKeyPairAndLinkProviderKeysToAvatar(generateKeyPairAndLinkProviderKeysToAvatarParams.AvatarUsername, providerTypeToLinkTo);
            else
                return new OASISResult<KeyPair>() { IsError = true, Message = errorMessage };
        }

        ///// <summary>
        /////     Get's a given avatar's unique storage key for the given provider type.
        ///// </summary>
        ///// <param name="avatarId">The Avatar's avatarId.</param>
        ///// <param name="providerType">The provider type to retreive the unique storage key for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("get_provider_unique_storage_key_for_avatar_by_id/{avatarId}/{providerType}")]
        //public OASISResult<string> GetProviderUniqueStorageKeyForAvatar(Guid avatarId, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderUniqueStorageKeyForAvatar(avatarId, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's unique storage key for the given provider type.
        ///// </summary>
        ///// <param name="username">The Avatar's username.</param>
        ///// <param name="providerType">The provider type to retreive the unique storage key for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("get_provider_unique_storage_key_for_avatar_by_username/{username}/{providerType}")]
        //public OASISResult<string> GetProviderUniqueStorageKeyForAvatar(string username, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderUniqueStorageKeyForAvatar(username, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's private key for the given provider type.
        ///// </summary>
        ///// <param name="avatarId">The Avatar's id.</param>
        ///// <param name="providerType">The provider type to retreive the private key for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("get_provider_private_key_for_avatar_by_id/{avatarId}/{providerType}")]
        //public OASISResult<string> GetProviderPrivateKeyForAvatar(Guid avatarId, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderPrivateKeyForAvatar(avatarId, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's private key for the given provider type.
        ///// </summary>
        ///// <param name="username">The Avatar's username.</param>
        ///// <param name="providerType">The provider type to retreive the private key for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("get_provider_private_key_for_avatar_by_username/{username}/{providerType}")]
        //public OASISResult<string> GetProviderPrivateKeyForAvatar(string username, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderPrivateKeyForAvatar(username, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's public keys for the given provider type.
        ///// </summary>
        ///// <param name="avatarId">The Avatar's id.</param>
        ///// <param name="providerType">The provider type to retreive the public keys for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("get_provider_public_keys_for_avatar_by_id/{avatarId}/{providerType}")]
        //public OASISResult<List<string>> GetProviderPublicKeysForAvatar(Guid avatarId, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderPublicKeysForAvatar(avatarId, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's public keys for the given provider type.
        ///// </summary>
        ///// <param name="username">The Avatar's username.</param>
        ///// <param name="providerType">The provider type to retreive the public keys for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("get_provider_public_keys_for_avatar_by_username/{username}/{providerType}")]
        //public OASISResult<List<string>> GetProviderPublicKeysForAvatar(string username, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderPublicKeysForAvatar(username, providerType);
        //}

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
                return KeyManager.GetProviderUniqueStorageKeyForAvatar(avatarID, providerType);
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
                return KeyManager.GetProviderUniqueStorageKeyForAvatar(providerKeyForAvatarParams.AvatarUsername, providerType);
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
        public OASISResult<string> GetProviderPrivateKeyForAvatarById(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            bool isValid;
            string errorMessage = "";
            ProviderType providerType;
            Guid avatarID;

            (isValid, providerType, avatarID, errorMessage) = ValidateParams(providerKeyForAvatarParams);

            if (isValid)
                return KeyManager.GetProviderUniqueStorageKeyForAvatar(providerKeyForAvatarParams.AvatarUsername, providerType);
            else
                return new OASISResult<string>() { IsError = true, Message = errorMessage };


            return KeyManager.GetProviderPrivateKeyForAvatar(providerKeyForAvatarParams.AvatarID, p);
        }

        /// <summary>
        ///     Get's a given avatar's private key for the given provider type using the avatar's username.
        /// </summary>
        /// <param name="providerKeyForAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_provider_private_key_for_avatar_by_username")]
        public OASISResult<string> GetProviderPrivateKeyForAvatarByUsername(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            return KeyManager.GetProviderPrivateKeyForAvatar(providerKeyForAvatarParams.AvatarUsername, providerKeyForAvatarParams.ProviderType);
        }

        /// <summary>
        ///     Get's a given avatar's public keys for the given provider type using the avatar's id.
        /// </summary>
        /// <param name="providerKeyForAvatarParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get_provider_public_keys_for_avatar_by_id")]
        public OASISResult<List<string>> GetProviderPublicKeysForAvatarById(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            return KeyManager.GetProviderPublicKeysForAvatar(providerKeyForAvatarParams.AvatarID, providerKeyForAvatarParams.ProviderType);
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
            return KeyManager.GetProviderPublicKeysForAvatar(providerKeyForAvatarParams.AvatarUsername, providerKeyForAvatarParams.ProviderType);
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
            return KeyManager.GetAllProviderPublicKeysForAvatar(id);
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
            return KeyManager.GetAllProviderPublicKeysForAvatar(username);
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
            return KeyManager.GetAllProviderPublicKeysForAvatar(email); //TODO: Implement email method.
        }

        /// <summary>
        ///     Generate's a new unique private/public keypair for a given provider type.
        /// </summary>
        /// <param name="providerType">TEST</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("generate_keypair_for_provider/{providerType}")]
        public OASISResult<KeyPair> GenerateKeyPairForProvider(ProviderType providerType)
        {
            return KeyManager.GenerateKeyPair(providerType);
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
            return KeyManager.GenerateKeyPair(keyPrefix);
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

        //private (bool, ProviderType, ProviderType, Guid, string) ValidateParams(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        private (bool, ProviderType, Guid, string) ValidateParams(ProviderKeyForAvatarParams linkProviderKeyToAvatarParams)
        {
            object providerTypeToLinkTo = null;
            //object providerTypeToAvatarFrom = null;
            Guid avatarID;

            if (string.IsNullOrEmpty(linkProviderKeyToAvatarParams.AvatarID) && string.IsNullOrEmpty(linkProviderKeyToAvatarParams.AvatarUsername))
                return (false, ProviderType.None, Guid.Empty, $"You need to either pass in a valid AvatarID or Avatar Username.");

            //if (string.IsNullOrEmpty(linkProviderKeyToAvatarParams.ProviderTypeToLinkTo))
            //    return (new OASISResult<KeyPair> { IsError = true, Message = $"The providerTypeToLinkTo param cannot be null. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" });

            if (!Enum.TryParse(typeof(ProviderType), linkProviderKeyToAvatarParams.ProviderType, out providerTypeToLinkTo))
                return (false, ProviderType.None, Guid.Empty, $"The given ProviderType param {linkProviderKeyToAvatarParams.ProviderType} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}");

            //Optional param.
            //if (!string.IsNullOrEmpty(linkProviderKeyToAvatarParams.ProviderTypeToLoadAvatarFrom) && !Enum.TryParse(typeof(ProviderType), linkProviderKeyToAvatarParams.ProviderTypeToLoadAvatarFrom, out providerTypeToAvatarFrom))
            //    return (false, ProviderType.None, ProviderType.None, Guid.Empty, $"The given ProviderTypeToLoadAvatarFrom param {linkProviderKeyToAvatarParams.ProviderTypeToLoadAvatarFrom} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}");

            if (!Guid.TryParse(linkProviderKeyToAvatarParams.AvatarID, out avatarID))
                return (false, ProviderType.None,  Guid.Empty, $"The given AvatarID {linkProviderKeyToAvatarParams.AvatarID} is not a valid Guid.");

            return (true, (ProviderType)providerTypeToLinkTo, avatarID, null);

            //if (string.IsNullOrEmpty(linkProviderKeyToAvatarParams.ProviderTypeToLoadAvatarFrom))
            //    return (true, (ProviderType)providerTypeToLinkTo, ProviderType.Default, avatarID, null);
            //else
            //    return (true, (ProviderType)providerTypeToLinkTo, (ProviderType)providerTypeToAvatarFrom, avatarID, null);
        }
    }
}