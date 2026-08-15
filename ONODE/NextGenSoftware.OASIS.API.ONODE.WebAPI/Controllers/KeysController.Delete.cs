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
                var holonsResult = await HolonManager.Instance.LoadHolonsForParentAsync(AvatarId);
                if (holonsResult.IsError)
                    return new OASISResult<List<KeyInfo>> { IsError = true, Message = holonsResult.Message };

                var keys = (holonsResult.Result ?? Enumerable.Empty<IHolon>())
                    .Where(h => h.MetaData != null && h.MetaData.ContainsKey("oasisKeyRecord"))
                    .Select(h => new KeyInfo
                    {
                        Id = h.Id,
                        Name = h.Name,
                        Type = h.MetaData.ContainsKey("keyType") ? h.MetaData["keyType"]?.ToString() : null,
                        CreatedAt = h.CreatedDate,
                        UpdatedAt = h.ModifiedDate == DateTime.MinValue ? (DateTime?)null : h.ModifiedDate,
                        IsActive = h.IsActive
                    }).ToList();

                return new OASISResult<List<KeyInfo>> { Result = keys, IsError = false, Message = "Keys retrieved successfully" };
            }
            catch (Exception ex)
            {
                return new OASISResult<List<KeyInfo>> { IsError = true, Message = $"Error retrieving keys: {ex.Message}", Exception = ex };
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
            if (keyRequest == null)
                return new OASISResult<KeyInfo> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with Name and Type." };
            try
            {
                var holon = new Holon
                {
                    Name = keyRequest.Name,
                    ParentHolonId = AvatarId,
                    IsActive = true,
                    MetaData = new Dictionary<string, object>
                    {
                        ["oasisKeyRecord"] = true,
                        ["keyType"] = keyRequest.Type
                    }
                };
                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon, AvatarId);
                if (saveResult.IsError)
                    return new OASISResult<KeyInfo> { IsError = true, Message = saveResult.Message };

                var keyInfo = new KeyInfo
                {
                    Id = saveResult.Result.Id,
                    Name = saveResult.Result.Name,
                    Type = keyRequest.Type,
                    CreatedAt = saveResult.Result.CreatedDate,
                    IsActive = true
                };
                return new OASISResult<KeyInfo> { Result = keyInfo, IsError = false, Message = "Key created successfully" };
            }
            catch (Exception ex)
            {
                return new OASISResult<KeyInfo> { IsError = true, Message = $"Error creating key: {ex.Message}", Exception = ex };
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
            if (keyRequest == null)
                return new OASISResult<KeyInfo> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with Name and Type." };
            try
            {
                var loadResult = await HolonManager.Instance.LoadHolonAsync(keyId);
                if (loadResult.IsError || loadResult.Result == null)
                    return new OASISResult<KeyInfo> { IsError = true, Message = loadResult.IsError ? loadResult.Message : "Key not found." };

                var holon = loadResult.Result;
                holon.Name = keyRequest.Name;
                if (holon.MetaData == null) holon.MetaData = new Dictionary<string, object>();
                holon.MetaData["keyType"] = keyRequest.Type;
                holon.MetaData["oasisKeyRecord"] = true;

                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon, AvatarId);
                if (saveResult.IsError)
                    return new OASISResult<KeyInfo> { IsError = true, Message = saveResult.Message };

                var keyInfo = new KeyInfo
                {
                    Id = saveResult.Result.Id,
                    Name = saveResult.Result.Name,
                    Type = keyRequest.Type,
                    CreatedAt = saveResult.Result.CreatedDate,
                    UpdatedAt = saveResult.Result.ModifiedDate == DateTime.MinValue ? (DateTime?)null : saveResult.Result.ModifiedDate,
                    IsActive = saveResult.Result.IsActive
                };
                return new OASISResult<KeyInfo> { Result = keyInfo, IsError = false, Message = "Key updated successfully" };
            }
            catch (Exception ex)
            {
                return new OASISResult<KeyInfo> { IsError = true, Message = $"Error updating key: {ex.Message}", Exception = ex };
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
                var deleteResult = await HolonManager.Instance.DeleteHolonAsync(keyId, AvatarId);
                if (deleteResult.IsError)
                    return new OASISResult<bool> { IsError = true, Message = deleteResult.Message };

                return new OASISResult<bool> { Result = true, IsError = false, Message = "Key deleted successfully" };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool> { IsError = true, Message = $"Error deleting key: {ex.Message}", Exception = ex };
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
                var holonsResult = await HolonManager.Instance.LoadHolonsForParentAsync(AvatarId);
                if (holonsResult.IsError)
                    return new OASISResult<Dictionary<string, object>> { IsError = true, Message = holonsResult.Message };

                var keyHolons = (holonsResult.Result ?? Enumerable.Empty<IHolon>())
                    .Where(h => h.MetaData != null && h.MetaData.ContainsKey("oasisKeyRecord")).ToList();

                var keyTypeGroups = keyHolons
                    .GroupBy(h => h.MetaData.ContainsKey("keyType") ? h.MetaData["keyType"]?.ToString() ?? "Unknown" : "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count());

                var stats = new Dictionary<string, object>
                {
                    ["totalKeys"] = keyHolons.Count,
                    ["activeKeys"] = keyHolons.Count(h => h.IsActive),
                    ["inactiveKeys"] = keyHolons.Count(h => !h.IsActive),
                    ["keyTypes"] = keyTypeGroups
                };

                return new OASISResult<Dictionary<string, object>> { Result = stats, IsError = false, Message = "Key statistics retrieved successfully" };
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
        [HttpPost("link-provider-wallet-address-to-avatar-by-id")]
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
        [HttpPost("link-provider-wallet-address-to-avatar-by-username")]
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
        [HttpPost("link-provider-wallet-address-to-avatar-by-email")]
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
        [HttpPost("generate-keypair-with-wallet-address-and-link-provider-keys-to-avatar-by-id")]
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
        [HttpPost("generate-keypair-with-wallet-address-and-link-provider-keys-to-avatar-by-username")]
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
        [HttpPost("generate-keypair-with-wallet-address-and-link-provider-keys-to-avatar-by-email")]
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
        [HttpPost("generate-keypair-with-wallet-address-for-provider/{providerType}")]
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
}
