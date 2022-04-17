//using System;
//using System.Collections.Generic;
//using Microsoft.AspNetCore.Mvc;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Managers;
//using NextGenSoftware.OASIS.API.Core.Objects;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class KeysController : OASISControllerBase
//    {
//        private KeyManager _keyManager = null;

//        public KeyManager KeyManager
//        {
//            get
//            {
//                if (_keyManager == null)
//                {
//                    OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

//                    if (result.IsError)
//                        ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider(). Error details: ", result.Message), true, false, true);

//                    _keyManager = new KeyManager(result.Result, Program.AvatarManager);
//                }

//                return _keyManager;
//            }
//        }

//        /// <summary>
//        ///     Link's a given Avatar to a Providers Public Key (private/public key pairs or username, accountname, unique id, agentId, hash, etc).
//        /// </summary>
//        /// <param name="linkProviderKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("LinkProviderPublicKeyToAvatarByAvatarId")]
//        public OASISResult<bool> LinkProviderPublicKeyToAvatarByAvatarId(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
//        {
//            bool isValid;
//            string errorMessage = "";
//            ProviderType providerTypeToLinkTo;
//            ProviderType providerTypeToLoadAvatarFrom;
//            Guid avatarID;

//            (isValid, providerTypeToLinkTo, providerTypeToLoadAvatarFrom, avatarID, errorMessage) = ValidateLinkProviderKeyToAvatarParams(linkProviderKeyToAvatarParams);

//            if (isValid)
//                return KeyManager.LinkProviderPublicKeyToAvatar(avatarID, providerTypeToLinkTo, linkProviderKeyToAvatarParams.ProviderKey, providerTypeToLoadAvatarFrom);
//            else
//                return new OASISResult<bool>(false) { IsError = true, Message = errorMessage };
//        }


//        /// <summary>
//        ///     Link's a given Avatar to a Providers Public Key (private/public key pairs or username, accountname, unique id, agentId, hash, etc).
//        /// </summary>
//        /// <param name="linkProviderKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("LinkProviderPublicKeyToAvatarByUsername")]
//        public OASISResult<bool> LinkProviderPublicKeyToAvatarByUsername(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
//        {
//            bool isValid;
//            string errorMessage = "";
//            ProviderType providerTypeToLinkTo;
//            ProviderType providerTypeToLoadAvatarFrom;
//            Guid avatarID;

//            (isValid, providerTypeToLinkTo, providerTypeToLoadAvatarFrom, avatarID, errorMessage) = ValidateLinkProviderKeyToAvatarParams(linkProviderKeyToAvatarParams);

//            if (isValid)
//                return KeyManager.LinkProviderPublicKeyToAvatar(linkProviderKeyToAvatarParams.AvatarUsername, providerTypeToLinkTo, linkProviderKeyToAvatarParams.ProviderKey, providerTypeToLoadAvatarFrom);
//            else
//                return new OASISResult<bool>(false) { IsError = true, Message = errorMessage };
//        }

//        /// <summary>
//        ///     Link's a given Avatar to a Providers Private Key (password, crypto private key, etc).
//        /// </summary>
//        /// <param name="linkProviderPrivateKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("LinkProviderPrivateKeyToAvatarByAvatarId")]
//        public OASISResult<bool> LinkProviderPrivateKeyToAvatarByAvatarId(LinkProviderKeyToAvatarParams linkProviderPrivateKeyToAvatarParams)
//        {
//            bool isValid;
//            string errorMessage = "";
//            ProviderType providerTypeToLinkTo;
//            ProviderType providerTypeToLoadAvatarFrom;
//            Guid avatarID;

//            (isValid, providerTypeToLinkTo, providerTypeToLoadAvatarFrom, avatarID, errorMessage) = ValidateLinkProviderKeyToAvatarParams(linkProviderPrivateKeyToAvatarParams);

//            if (isValid)
//                return KeyManager.LinkProviderPrivateKeyToAvatar(avatarID, providerTypeToLinkTo, linkProviderPrivateKeyToAvatarParams.ProviderKey, providerTypeToLoadAvatarFrom);
//            else
//                return new OASISResult<bool>(false) { IsError = true, Message = errorMessage };
//        }

//        /// <summary>
//        ///     Link's a given Avatar to a Providers Private Key (password, crypto private key, etc).
//        /// </summary>
//        /// <param name="linkProviderPrivateKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("LinkProviderPrivateKeyToAvatarByUsername")]
//        public OASISResult<bool> LinkProviderPrivateKeyToAvatarByUsername(LinkProviderKeyToAvatarParams linkProviderPrivateKeyToAvatarParams)
//        {
//            bool isValid;
//            string errorMessage = "";
//            ProviderType providerTypeToLinkTo;
//            ProviderType providerTypeToLoadAvatarFrom;
//            Guid avatarID;

//            (isValid, providerTypeToLinkTo, providerTypeToLoadAvatarFrom, avatarID, errorMessage) = ValidateLinkProviderKeyToAvatarParams(linkProviderPrivateKeyToAvatarParams);

//            if (isValid)
//                return KeyManager.LinkProviderPrivateKeyToAvatar(linkProviderPrivateKeyToAvatarParams.AvatarUsername, providerTypeToLinkTo, linkProviderPrivateKeyToAvatarParams.ProviderKey, providerTypeToLoadAvatarFrom);
//            else
//                return new OASISResult<bool>(false) { IsError = true, Message = errorMessage };
//        }

//        /*
//        /// <summary>
//        ///     Generate's a new unique private/public keypair &amp; then links to the given avatar for the given provider type.
//        /// </summary>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("GenerateKeyPairAndLinkProviderKeysToAvatar")]
//        public OASISResult<KeyPair> GenerateKeyPairAndLinkProviderKeysToAvatar(Guid avatarId, string providerTypeToLinkTo, string providerTypeToloadAvatarFrom)
//        {
//            object providerTypeToLinkToObject = null;
//            object providerTypeToLoadAvatarFromObject = null;
//            ProviderType providerTypeToLinkToEnumValue = ProviderType.Default;
//            ProviderType providerTypeToloadAvatarFromEnumValue = ProviderType.Default;

//            if (string.IsNullOrEmpty(providerTypeToLinkTo))
//                return (new OASISResult<KeyPair> { IsError = true, Message = $"The providerTypeToLinkTo param cannot be null. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" });

//            if (!string.IsNullOrEmpty(providerTypeToLinkTo) && !Enum.TryParse(typeof(ProviderType), providerTypeToLinkTo, out providerTypeToLinkToObject))
//                return (new OASISResult<KeyPair> { IsError = true, Message = $"The given providerTypeToLinkTo {providerTypeToLinkTo} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" });

//            if (!string.IsNullOrEmpty(providerTypeToloadAvatarFrom) && !Enum.TryParse(typeof(ProviderType), providerTypeToloadAvatarFrom, out providerTypeToLoadAvatarFromObject))
//                return (new OASISResult<KeyPair> { IsError = true, Message = $"The given providerTypeToloadAvatarFrom {providerTypeToloadAvatarFrom} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" });

//            if (providerTypeToLinkToObject != null)
//                providerTypeToLinkToEnumValue = (ProviderType)providerTypeToLinkToObject;

//            if (providerTypeToLoadAvatarFromObject != null)
//                providerTypeToloadAvatarFromEnumValue = (ProviderType)providerTypeToLoadAvatarFromObject;

//            return KeyManager.GenerateKeyPairAndLinkProviderKeysToAvatar(avatarId, providerTypeToLinkToEnumValue, providerTypeToloadAvatarFromEnumValue);
//        }*/


//        /// <summary>
//        ///     Generate's a new unique private/public keypair &amp; then links to the given avatar for the given provider type.
//        /// </summary>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarId")]
//        public OASISResult<KeyPair> GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarId(LinkProviderKeyToAvatarParams generateKeyPairAndLinkProviderKeysToAvatarParams)
//        {
//            bool isValid;
//            string errorMessage = "";
//            ProviderType providerTypeToLinkTo;
//            ProviderType providerTypeToLoadAvatarFrom;
//            Guid avatarID;

//            (isValid, providerTypeToLinkTo, providerTypeToLoadAvatarFrom, avatarID, errorMessage) = ValidateLinkProviderKeyToAvatarParams(generateKeyPairAndLinkProviderKeysToAvatarParams);

//            if (isValid)
//                return KeyManager.GenerateKeyPairAndLinkProviderKeysToAvatar(avatarID, providerTypeToLinkTo, providerTypeToLoadAvatarFrom);
//            else
//                return new OASISResult<KeyPair>() { IsError = true, Message = errorMessage };
//        }

//        /// <summary>
//        ///     Get's a given avatar's unique storage key for the given provider type.
//        /// </summary>
//        /// <param name="avatarId">The Avatar's avatarId.</param>
//        /// <param name="providerType">The provider type to retreive the unique storage key for.</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("GetProviderUniqueStorageKeyForAvatar")]
//        public OASISResult<string> GetProviderUniqueStorageKeyForAvatar(Guid avatarId, ProviderType providerType)
//        {
//            return KeyManager.GetProviderUniqueStorageKeyForAvatar(avatarId, providerType);
//        }

//        /// <summary>
//        ///     Get's a given avatar's unique storage key for the given provider type.
//        /// </summary>
//        /// <param name="username">The Avatar's username.</param>
//        /// <param name="providerType">The provider type to retreive the unique storage key for.</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("GetProviderUniqueStorageKeyForAvatar")]
//        public OASISResult<string> GetProviderUniqueStorageKeyForAvatar(string username, ProviderType providerType)
//        {
//            return KeyManager.GetProviderUniqueStorageKeyForAvatar(username, providerType);
//        }

//        /// <summary>
//        ///     Get's a given avatar's private key for the given provider type.
//        /// </summary>
//        /// <param name="avatarId">The Avatar's id.</param>
//        /// <param name="providerType">The provider type to retreive the private key for.</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("GetProviderPrivateKeyForAvatar")]
//        public OASISResult<string> GetProviderPrivateKeyForAvatar(Guid avatarId, ProviderType providerType)
//        {
//            return KeyManager.GetProviderPrivateKeyForAvatar(avatarId, providerType);
//        }

//        /// <summary>
//        ///     Get's a given avatar's private key for the given provider type.
//        /// </summary>
//        /// <param name="username">The Avatar's username.</param>
//        /// <param name="providerType">The provider type to retreive the private key for.</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("GetProviderPrivateKeyForAvatar")]
//        public OASISResult<string> GetProviderPrivateKeyForAvatar(string username, ProviderType providerType)
//        {
//            return KeyManager.GetProviderPrivateKeyForAvatar(username, providerType);
//        }

//        /// <summary>
//        ///     Get's a given avatar's public keys for the given provider type.
//        /// </summary>
//        /// <param name="avatarId">The Avatar's id.</param>
//        /// <param name="providerType">The provider type to retreive the public keys for.</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("GetProviderPublicKeysForAvatar")]
//        public OASISResult<List<string>> GetProviderPublicKeysForAvatar(Guid avatarId, ProviderType providerType)
//        {
//            return KeyManager.GetProviderPublicKeysForAvatar(avatarId, providerType);
//        }

//        /// <summary>
//        ///     Get's a given avatar's public keys for the given provider type.
//        /// </summary>
//        /// <param name="username">The Avatar's username.</param>
//        /// <param name="providerType">The provider type to retreive the public keys for.</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("GetProviderPublicKeysForAvatar")]
//        public OASISResult<List<string>> GetProviderPublicKeysForAvatar(string username, ProviderType providerType)
//        {
//            return KeyManager.GetProviderPublicKeysForAvatar(username, providerType);
//        }

//        /// <summary>
//        ///     Get's a given avatar's public keys for the given provider type.
//        /// </summary>
//        /// <param name="username">The Avatar's username.</param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("GetAllProviderPublicKeysForAvatar")]
//        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatar(string username)
//        {
//            return KeyManager.GetAllProviderPublicKeysForAvatar(username);
//        }

//        /// <summary>
//        ///     Generate's a new unique private/public keypair for a given provider type.
//        /// </summary>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("GenerateKeyPairForProvider")]
//        public OASISResult<KeyPair> GenerateKeyPairForProvider(ProviderType providerType)
//        {
//            return KeyManager.GenerateKeyPair(providerType);
//        }

//        /// <summary>
//        ///     Generate's a new unique private/public keypair.
//        /// </summary>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("GenerateKeyPair")]
//        public OASISResult<KeyPair> GenerateKeyPair(string keyPrefix)
//        {
//            return KeyManager.GenerateKeyPair(keyPrefix);
//        }








//        /*
//        /// <summary>
//        ///     Link's a given telosAccount to the given avatar.
//        /// </summary>
//        /// <param name="avatarId">The id of the avatar.</param>
//        /// <param name="telosAccountName"></param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("{id:Guid}/{telosAccountName}")]
//        public async Task<OASISResult<IAvatarDetail>> LinkTelosAccountToAvatar(Guid id, string telosAccountName)
//        {
//            return await _avatarService.LinkProviderKeyToAvatar(id, ProviderType.TelosOASIS, telosAccountName);
//        }

//        /// <summary>
//        ///     Link's a given telosAccount to the given avatar.
//        /// </summary>
//        /// <param name="avatarId">The id of the avatar.</param>
//        /// <param name="telosAccountName"></param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost]
//        public async Task<OASISResult<IAvatarDetail>> LinkTelosAccountToAvatar2(
//            LinkProviderKeyToAvatar linkProviderKeyToAvatar)
//        {
//            return await _avatarService.LinkProviderKeyToAvatar(linkProviderKeyToAvatar.AvatarID,
//                ProviderType.TelosOASIS, linkProviderKeyToAvatar.ProviderUniqueStorageKey);
//        }


//        /// <summary>
//        ///     Link's a given eosioAccountName to the given avatar.
//        /// </summary>
//        /// <param name="avatarId">The id of the avatar.</param>
//        /// <param name="eosioAccountName"></param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("{avatarId}/{eosioAccountName}")]
//        public async Task<OASISResult<IAvatarDetail>> LinkEOSIOAccountToAvatar(Guid avatarId, string eosioAccountName)
//        {
//            return await _avatarService.LinkProviderKeyToAvatar(avatarId, ProviderType.EOSIOOASIS, eosioAccountName);
//        }

//        /// <summary>
//        ///     Link's a given holochain AgentID to the given avatar.
//        /// </summary>
//        /// <param name="avatarId">The id of the avatar.</param>
//        /// <param name="holochainAgentID"></param>
//        /// <returns></returns>
//        [Authorize]
//        [HttpPost("{avatarId}/{holochainAgentID}")]
//        public async Task<OASISResult<IAvatarDetail>> LinkHolochainAgentIDToAvatar(Guid avatarId,
//            string holochainAgentID)
//        {
//            return await _avatarService.LinkProviderKeyToAvatar(avatarId, ProviderType.HoloOASIS, holochainAgentID);
//        }*/

//        ///// <summary>
//        /////     Get's the provider key for the given avatar and provider type.
//        ///// </summary>
//        ///// <param name="avatarUsername">The avatar username.</param>
//        ///// <param name="providerType">The provider type.</param>
//        ///// <returns></returns>
//        //[Authorize]
//        //[HttpPost("{avatarUsername}/{providerType}")]
//        //public async Task<OASISResult<string>> GetProviderKeyForAvatar(string avatarUsername, ProviderType providerType)
//        //{
//        //    //return await _avatarService.GetProviderKeyForAvatar(avatarUsername, providerType);
//        //    return await Program.AvatarManager.GetProviderKeyForAvatar(avatarUsername, providerType);
//        //}

//        private (bool, ProviderType, ProviderType, Guid, string) ValidateLinkProviderKeyToAvatarParams(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
//        {
//            object providerTypeToLinkTo = null;
//            object providerTypeToAvatarFrom = null;
//            Guid avatarID;

//            if (string.IsNullOrEmpty(linkProviderKeyToAvatarParams.AvatarID) && string.IsNullOrEmpty(linkProviderKeyToAvatarParams.AvatarUsername))
//                return (false, ProviderType.None, ProviderType.None, Guid.Empty, $"You need to either pass in a valid AvatarID or Avatar Username.");

//            //if (string.IsNullOrEmpty(linkProviderKeyToAvatarParams.ProviderTypeToLinkTo))
//            //    return (new OASISResult<KeyPair> { IsError = true, Message = $"The providerTypeToLinkTo param cannot be null. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" });

//            if (!Enum.TryParse(typeof(ProviderType), linkProviderKeyToAvatarParams.ProviderTypeToLinkTo, out providerTypeToLinkTo))
//                return (false, ProviderType.None, ProviderType.None, Guid.Empty, $"The given ProviderTypeToLinkTo param {linkProviderKeyToAvatarParams.ProviderTypeToLinkTo} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}");

//            //Optional param.
//            if (!string.IsNullOrEmpty(linkProviderKeyToAvatarParams.ProviderTypeToLoadAvatarFrom) && !Enum.TryParse(typeof(ProviderType), linkProviderKeyToAvatarParams.ProviderTypeToLoadAvatarFrom, out providerTypeToAvatarFrom))
//                return (false, ProviderType.None, ProviderType.None, Guid.Empty, $"The given ProviderTypeToLoadAvatarFrom param {linkProviderKeyToAvatarParams.ProviderTypeToLoadAvatarFrom} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}");

//            if (!Guid.TryParse(linkProviderKeyToAvatarParams.AvatarID, out avatarID))
//                return (false, ProviderType.None, ProviderType.None, Guid.Empty, $"The given AvatarID {linkProviderKeyToAvatarParams.AvatarID} is not a valid Guid.");

//            if (string.IsNullOrEmpty(linkProviderKeyToAvatarParams.ProviderTypeToLoadAvatarFrom))
//                return (true, (ProviderType)providerTypeToLinkTo, ProviderType.Default, avatarID, null);
//            else
//                return (true, (ProviderType)providerTypeToLinkTo, (ProviderType)providerTypeToAvatarFrom, avatarID, null);
//        }
//    }
//}