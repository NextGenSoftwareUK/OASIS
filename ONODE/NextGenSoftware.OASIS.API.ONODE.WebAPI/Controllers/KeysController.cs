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
        [Authorize]
        [HttpPost("clear-cache")]
        public OASISResult<bool> ClearCache()
        {
            return KeyManager.ClearCache();
        }

        // ── LINK ──────────────────────────────────────────────────────────────

        [Authorize]
        [HttpPost("link-provider-public-key-to-avatar-by-id")]
        public OASISResult<IProviderWallet> LinkProviderPublicKeyToAvatarByAvatarId(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        {
            (bool isValid, ProviderType providerTypeToLinkTo, Guid avatarID, string errorMessage) = ValidateParams(linkProviderKeyToAvatarParams);
            if (isValid)
                return KeyManager.LinkProviderPublicKeyToAvatarById(linkProviderKeyToAvatarParams.WalletId, avatarID, providerTypeToLinkTo, linkProviderKeyToAvatarParams.ProviderKey, linkProviderKeyToAvatarParams.WalletAddress, linkProviderKeyToAvatarParams.WalletAddressSegwitP2SH, linkProviderKeyToAvatarParams.ShowSecretRecoveryWords);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpPost("link-provider-public-key-to-avatar-by-username")]
        public OASISResult<IProviderWallet> LinkProviderPublicKeyToAvatarByUsername(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        {
            (bool isValid, ProviderType providerTypeToLinkTo, Guid avatarID, string errorMessage) = ValidateParams(linkProviderKeyToAvatarParams);
            if (isValid)
                return KeyManager.LinkProviderPublicKeyToAvatarByUsername(linkProviderKeyToAvatarParams.WalletId, linkProviderKeyToAvatarParams.AvatarUsername, providerTypeToLinkTo, linkProviderKeyToAvatarParams.ProviderKey, linkProviderKeyToAvatarParams.WalletAddress, linkProviderKeyToAvatarParams.WalletAddressSegwitP2SH, linkProviderKeyToAvatarParams.ShowSecretRecoveryWords);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpPost("link-provider-public-key-to-avatar-by-email")]
        public OASISResult<IProviderWallet> LinkProviderPublicKeyToAvatarByEmail(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        {
            (bool isValid, ProviderType providerTypeToLinkTo, Guid avatarID, string errorMessage) = ValidateParams(linkProviderKeyToAvatarParams);
            if (isValid)
                return KeyManager.LinkProviderPublicKeyToAvatarByEmail(linkProviderKeyToAvatarParams.WalletId, linkProviderKeyToAvatarParams.AvatarEmail, providerTypeToLinkTo, linkProviderKeyToAvatarParams.ProviderKey, linkProviderKeyToAvatarParams.WalletAddress, linkProviderKeyToAvatarParams.WalletAddressSegwitP2SH, linkProviderKeyToAvatarParams.ShowSecretRecoveryWords);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpPost("link-provider-private-key-to-avatar-by-id")]
        public OASISResult<IProviderWallet> LinkProviderPrivateKeyToAvatarByAvatarId(LinkProviderKeyToAvatarParams linkProviderPrivateKeyToAvatarParams)
        {
            (bool isValid, ProviderType providerTypeToLinkTo, Guid avatarID, string errorMessage) = ValidateParams(linkProviderPrivateKeyToAvatarParams);
            if (isValid)
                return KeyManager.LinkProviderPrivateKeyToAvatarById(linkProviderPrivateKeyToAvatarParams.WalletId, avatarID, providerTypeToLinkTo, linkProviderPrivateKeyToAvatarParams.ProviderKey, linkProviderPrivateKeyToAvatarParams.ShowPrivateKey, linkProviderPrivateKeyToAvatarParams.ShowSecretRecoveryWords);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpPost("link-provider-private-key-to-avatar-by-username")]
        public OASISResult<IProviderWallet> LinkProviderPrivateKeyToAvatarByUsername(LinkProviderKeyToAvatarParams linkProviderPrivateKeyToAvatarParams)
        {
            (bool isValid, ProviderType providerTypeToLinkTo, Guid avatarID, string errorMessage) = ValidateParams(linkProviderPrivateKeyToAvatarParams);
            if (isValid)
                return KeyManager.LinkProviderPrivateKeyToAvatarByUsername(linkProviderPrivateKeyToAvatarParams.WalletId, linkProviderPrivateKeyToAvatarParams.AvatarUsername, providerTypeToLinkTo, linkProviderPrivateKeyToAvatarParams.ProviderKey, linkProviderPrivateKeyToAvatarParams.ShowPrivateKey, linkProviderPrivateKeyToAvatarParams.ShowSecretRecoveryWords);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpPost("generate-keypair-and-link-provider-keys-to-avatar-by-id")]
        public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarId(LinkProviderKeyToAvatarParams generateKeyPairAndLinkProviderKeysToAvatarParams)
        {
            (bool isValid, ProviderType providerTypeToLinkTo, Guid avatarID, string errorMessage) = ValidateParams(generateKeyPairAndLinkProviderKeysToAvatarParams);
            if (isValid)
                return KeyManager.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarById(avatarID, providerTypeToLinkTo, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPublicKey, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPrivateKey);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpPost("generate-keypair-and-link-provider-keys-to-avatar-by-username")]
        public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarUsername(LinkProviderKeyToAvatarParams generateKeyPairAndLinkProviderKeysToAvatarParams)
        {
            (bool isValid, ProviderType providerTypeToLinkTo, Guid avatarID, string errorMessage) = ValidateParams(generateKeyPairAndLinkProviderKeysToAvatarParams);
            if (isValid)
                return KeyManager.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByUsername(generateKeyPairAndLinkProviderKeysToAvatarParams.AvatarUsername, providerTypeToLinkTo, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPublicKey, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPrivateKey);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpPost("generate-keypair-and-link-provider-keys-to-avatar-by-email")]
        public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarEmail(LinkProviderKeyToAvatarParams generateKeyPairAndLinkProviderKeysToAvatarParams)
        {
            (bool isValid, ProviderType providerTypeToLinkTo, Guid avatarID, string errorMessage) = ValidateParams(generateKeyPairAndLinkProviderKeysToAvatarParams);
            if (isValid)
                return KeyManager.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByEmail(generateKeyPairAndLinkProviderKeysToAvatarParams.AvatarEmail, providerTypeToLinkTo, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPublicKey, generateKeyPairAndLinkProviderKeysToAvatarParams.ShowPrivateKey);
            else
                return new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        // ── GET ───────────────────────────────────────────────────────────────

        [Authorize]
        [HttpGet("get-provider-unique-storage-key-for-avatar-by-id")]
        public OASISResult<string> GetProviderUniqueStorageKeyForAvatarById(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            (bool isValid, ProviderType providerType, Guid avatarID, string errorMessage) = ValidateParams(providerKeyForAvatarParams);
            return isValid ? KeyManager.GetProviderUniqueStorageKeyForAvatarById(avatarID, providerType) : new OASISResult<string>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpGet("get-provider-unique-storage-key-for-avatar-by-username")]
        public OASISResult<string> GetProviderUniqueStorageKeyForAvatarByUsername(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            (bool isValid, ProviderType providerType, Guid avatarID, string errorMessage) = ValidateParams(providerKeyForAvatarParams);
            return isValid ? KeyManager.GetProviderUniqueStorageKeyForAvatarByUsername(providerKeyForAvatarParams.AvatarUsername, providerType) : new OASISResult<string>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpGet("get-provider-unique-storage-key-for-avatar-by-email")]
        public OASISResult<string> GetProviderUniqueStorageKeyForAvatarByEmail(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            (bool isValid, ProviderType providerType, Guid avatarID, string errorMessage) = ValidateParams(providerKeyForAvatarParams);
            return isValid ? KeyManager.GetProviderUniqueStorageKeyForAvatarByEmail(providerKeyForAvatarParams.AvatarEmail, providerType) : new OASISResult<string>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpGet("get-provider-private-key-for-avatar-by-id")]
        public OASISResult<List<string>> GetProviderPrivateKeyForAvatarById(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            (bool isValid, ProviderType providerType, Guid avatarID, string errorMessage) = ValidateParams(providerKeyForAvatarParams);
            return isValid ? KeyManager.GetProviderPrivateKeysForAvatarById(avatarID, providerType) : new OASISResult<List<string>>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpGet("get-provider-private-key-for-avatar-by-username")]
        public OASISResult<List<string>> GetProviderPrivateKeyForAvatarByUsername(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            (bool isValid, ProviderType providerType, Guid avatarID, string errorMessage) = ValidateParams(providerKeyForAvatarParams);
            return isValid ? KeyManager.GetProviderPrivateKeysForAvatarByUsername(providerKeyForAvatarParams.AvatarUsername, providerType) : new OASISResult<List<string>>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpGet("get-provider-public-keys-for-avatar-by-id")]
        public OASISResult<List<string>> GetProviderPublicKeysForAvatarById(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            (bool isValid, ProviderType providerType, Guid avatarID, string errorMessage) = ValidateParams(providerKeyForAvatarParams);
            return isValid ? KeyManager.GetProviderPublicKeysForAvatarById(avatarID, providerType) : new OASISResult<List<string>>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpGet("get-provider-public-keys-for-avatar-by-username")]
        public OASISResult<List<string>> GetProviderPublicKeysForAvatarByUsername(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            (bool isValid, ProviderType providerType, Guid avatarID, string errorMessage) = ValidateParams(providerKeyForAvatarParams);
            return isValid ? KeyManager.GetProviderPublicKeysForAvatarByUsername(providerKeyForAvatarParams.AvatarUsername, providerType) : new OASISResult<List<string>>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpGet("get-provider-public-keys-for-avatar-by-email")]
        public OASISResult<List<string>> GetProviderPublicKeysForAvatarByEmail(ProviderKeyForAvatarParams providerKeyForAvatarParams)
        {
            (bool isValid, ProviderType providerType, Guid avatarID, string errorMessage) = ValidateParams(providerKeyForAvatarParams);
            return isValid ? KeyManager.GetProviderPublicKeysForAvatarByEmail(providerKeyForAvatarParams.AvatarEmail, providerType) : new OASISResult<List<string>>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpGet("get-all-provider-public-keys-for-avatar-by-id/{id}")]
        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarById(Guid id)
        {
            return KeyManager.GetAllProviderPublicKeysForAvatarById(id);
        }

        [Authorize]
        [HttpGet("get-all-provider-public-keys-for-avatar-by-username/{username}")]
        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarByUsername(string username)
        {
            return KeyManager.GetAllProviderPublicKeysForAvatarByUsername(username);
        }

        [Authorize]
        [HttpGet("get-all-provider-public-keys-for-avatar-by-email/{email}")]
        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarByEmail(string email)
        {
            return KeyManager.GetAllProviderPublicKeysForAvatarByEmail(email);
        }

        [Authorize]
        [HttpGet("get-all-provider-private-keys-for-avatar-by-id/{id}")]
        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPrivateKeysForAvatarById(Guid id)
        {
            return KeyManager.GetAllProviderPrivateKeysForAvatarById(id);
        }

        [Authorize]
        [HttpGet("get-all-provider-private-keys-for-avatar-by-username/{username}")]
        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPrivateKeysForAvatarByUsername(string username)
        {
            return KeyManager.GetAllProviderPrivateKeysForAvatarByUsername(username);
        }

        [Authorize]
        [HttpGet("get-all-provider-unique-storage-keys-for-avatar-by-id/{id}")]
        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatarById(Guid id)
        {
            return KeyManager.GetAllProviderUniqueStorageKeysForAvatarById(id);
        }

        [Authorize]
        [HttpGet("get-all-provider-unique-storage-keys-for-avatar-by-username/{username}")]
        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatarByUsername(string username)
        {
            return KeyManager.GetAllProviderUniqueStorageKeysForAvatarByUsername(username);
        }

        [Authorize]
        [HttpGet("get-all-provider-unique-storage-keys-for-avatar-by-email/{email}")]
        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatarByEmail(string email)
        {
            return KeyManager.GetAllProviderUniqueStorageKeysForAvatarByEmail(email);
        }

        [Authorize]
        [HttpGet("get-avatar-id-for-provider-unique-storage-key/{providerKey}")]
        public OASISResult<Guid> GetAvatarIdForProviderUniqueStorageKey(string providerKey)
        {
            return KeyManager.GetAvatarIdForProviderUniqueStorageKey(providerKey);
        }

        [Authorize]
        [HttpGet("get-avatar-username-for-provider-unique-storage-key/{providerKey}")]
        public OASISResult<string> GetAvatarUsernameForProviderUniqueStorageKey(string providerKey)
        {
            return KeyManager.GetAvatarUsernameForProviderUniqueStorageKey(providerKey);
        }

        [Authorize]
        [HttpGet("get-avatar-email-for-provider-unique-storage-key/{providerKey}")]
        public OASISResult<string> GetAvatarEmailForProviderUniqueStorageKey(string providerKey)
        {
            return KeyManager.GetAvatarEmailForProviderUniqueStorageKey(providerKey);
        }

        [Authorize]
        [HttpGet("get-avatar-for-provider-unique-storage-key/{providerKey}")]
        public OASISResult<IAvatar> GetAvatarForProviderUniqueStorageKey(string providerKey)
        {
            return KeyManager.GetAvatarForProviderUniqueStorageKey(providerKey);
        }

        [Authorize]
        [HttpGet("get-avatar-id-for-provider-public-key/{providerKey}")]
        public OASISResult<Guid> GetAvatarIdForProviderPublicKey(string providerKey)
        {
            return KeyManager.GetAvatarIdForProviderPublicKey(providerKey);
        }

        [Authorize]
        [HttpGet("get-avatar-username-for-provider-public-key/{providerKey}")]
        public OASISResult<string> GetAvatarUsernameForProviderPublicKey(string providerKey)
        {
            return KeyManager.GetAvatarUsernameForProviderPublicKey(providerKey);
        }

        [Authorize]
        [HttpGet("get-avatar-email-for-provider-public-key/{providerKey}")]
        public OASISResult<string> GetAvatarEmailForProviderPublicKey(string providerKey)
        {
            return KeyManager.GetAvatarEmailForProviderPublicKey(providerKey);
        }

        [Authorize]
        [HttpGet("get-avatar-for-provider-public-key/{providerKey}")]
        public OASISResult<IAvatar> GetAvatarForProviderPublicKey(string providerKey)
        {
            return KeyManager.GetAvatarForProviderPublicKey(providerKey);
        }

        // ── GENERATE ──────────────────────────────────────────────────────────

        [Authorize]
        [HttpPost("generate-keypair-for-provider/{providerType}")]
        public OASISResult<IKeyPairAndWallet> GenerateKeyPairForProvider(ProviderType providerType)
        {
            return KeyManager.GenerateKeyPairWithWalletAddress(providerType);
        }

        [Authorize]
        [HttpPost("get-private-wifi/{source}")]
        public OASISResult<string> GetPrivateWif(byte[] source)
        {
            return KeyManager.GetPrivateWif(source);
        }

        [Authorize]
        [HttpPost("get-public-wifi")]
        public OASISResult<string> GetPublicWif(WifParams wifParams)
        {
            return KeyManager.GetPublicWif(wifParams.PublicKey, wifParams.Prefix);
        }

        [Authorize]
        [HttpPost("decode-private-wif/{data}")]
        public OASISResult<byte[]> DecodePrivateWif(string data)
        {
            return KeyManager.DecodePrivateWif(data);
        }

        [Authorize]
        [HttpPost("base58-check-decode/{data}")]
        public OASISResult<byte[]> Base58CheckDecode(string data)
        {
            return KeyManager.Base58CheckDecode(data);
        }

        [Authorize]
        [HttpPost("encode-signature/{source}")]
        public OASISResult<string> EncodeSignature(byte[] source)
        {
            return KeyManager.EncodeSignature(source);
        }

        // ── CREATE / UPDATE / DELETE (CRUD) ───────────────────────────────────

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
                    MetaData = new Dictionary<string, object> { ["oasisKeyRecord"] = true, ["keyType"] = keyRequest.Type }
                };
                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon, AvatarId);
                if (saveResult.IsError)
                    return new OASISResult<KeyInfo> { IsError = true, Message = saveResult.Message };
                return new OASISResult<KeyInfo> { Result = new KeyInfo { Id = saveResult.Result.Id, Name = saveResult.Result.Name, Type = keyRequest.Type, CreatedAt = saveResult.Result.CreatedDate, IsActive = true }, IsError = false, Message = "Key created successfully" };
            }
            catch (Exception ex)
            {
                return new OASISResult<KeyInfo> { IsError = true, Message = $"Error creating key: {ex.Message}", Exception = ex };
            }
        }

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
                return new OASISResult<KeyInfo> { Result = new KeyInfo { Id = saveResult.Result.Id, Name = saveResult.Result.Name, Type = keyRequest.Type, CreatedAt = saveResult.Result.CreatedDate, UpdatedAt = saveResult.Result.ModifiedDate == DateTime.MinValue ? (DateTime?)null : saveResult.Result.ModifiedDate, IsActive = saveResult.Result.IsActive }, IsError = false, Message = "Key updated successfully" };
            }
            catch (Exception ex)
            {
                return new OASISResult<KeyInfo> { IsError = true, Message = $"Error updating key: {ex.Message}", Exception = ex };
            }
        }

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

        [Authorize]
        [HttpGet("stats")]
        public async Task<OASISResult<Dictionary<string, object>>> GetKeyStats()
        {
            try
            {
                var holonsResult = await HolonManager.Instance.LoadHolonsForParentAsync(AvatarId);
                if (holonsResult.IsError)
                    return new OASISResult<Dictionary<string, object>> { IsError = true, Message = holonsResult.Message };
                var keyHolons = (holonsResult.Result ?? Enumerable.Empty<IHolon>()).Where(h => h.MetaData != null && h.MetaData.ContainsKey("oasisKeyRecord")).ToList();
                var keyTypeGroups = keyHolons.GroupBy(h => h.MetaData.ContainsKey("keyType") ? h.MetaData["keyType"]?.ToString() ?? "Unknown" : "Unknown").ToDictionary(g => g.Key, g => g.Count());
                var stats = new Dictionary<string, object> { ["totalKeys"] = keyHolons.Count, ["activeKeys"] = keyHolons.Count(h => h.IsActive), ["inactiveKeys"] = keyHolons.Count(h => !h.IsActive), ["keyTypes"] = keyTypeGroups };
                return new OASISResult<Dictionary<string, object>> { Result = stats, IsError = false, Message = "Key statistics retrieved successfully" };
            }
            catch (Exception ex)
            {
                return new OASISResult<Dictionary<string, object>> { IsError = true, Message = $"Error retrieving key statistics: {ex.Message}", Exception = ex };
            }
        }

        // ── WALLET ADDRESS ────────────────────────────────────────────────────

        [Authorize]
        [HttpPost("link-provider-wallet-address-to-avatar-by-id")]
        public OASISResult<IProviderWallet> LinkProviderWalletAddressToAvatarById(LinkProviderKeyToAvatarParams p)
        {
            (bool isValid, ProviderType providerTypeToLinkTo, Guid avatarID, string errorMessage) = ValidateParams(p);
            return isValid ? KeyManager.LinkProviderWalletAddressToAvatarById(p.WalletId, avatarID, providerTypeToLinkTo, p.WalletAddress, ProviderType.Default) : new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpPost("link-provider-wallet-address-to-avatar-by-username")]
        public OASISResult<IProviderWallet> LinkProviderWalletAddressToAvatarByUsername(LinkProviderKeyToAvatarParams p)
        {
            (bool isValid, ProviderType providerTypeToLinkTo, Guid avatarID, string errorMessage) = ValidateParams(p);
            return isValid ? KeyManager.LinkProviderWalletAddressToAvatarByUsername(p.WalletId, p.AvatarUsername, providerTypeToLinkTo, p.WalletAddress, ProviderType.Default) : new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpPost("link-provider-wallet-address-to-avatar-by-email")]
        public OASISResult<IProviderWallet> LinkProviderWalletAddressToAvatarByEmail(LinkProviderKeyToAvatarParams p)
        {
            (bool isValid, ProviderType providerTypeToLinkTo, Guid avatarID, string errorMessage) = ValidateParams(p);
            return isValid ? KeyManager.LinkProviderWalletAddressToAvatarByEmail(p.WalletId, p.AvatarEmail, providerTypeToLinkTo, p.WalletAddress, ProviderType.Default) : new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpPost("generate-keypair-with-wallet-address-and-link-provider-keys-to-avatar-by-id")]
        public OASISResult<IProviderWallet> GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarById(LinkProviderKeyToAvatarParams p)
        {
            (bool isValid, ProviderType providerTypeToLinkTo, Guid avatarID, string errorMessage) = ValidateParams(p);
            return isValid ? KeyManager.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarById(avatarID, providerTypeToLinkTo, p.ShowPublicKey, p.ShowPrivateKey, p.ShowSecretRecoveryWords) : new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpPost("generate-keypair-with-wallet-address-and-link-provider-keys-to-avatar-by-username")]
        public OASISResult<IProviderWallet> GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByUsername(LinkProviderKeyToAvatarParams p)
        {
            (bool isValid, ProviderType providerTypeToLinkTo, Guid avatarID, string errorMessage) = ValidateParams(p);
            return isValid ? KeyManager.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByUsername(p.AvatarUsername, providerTypeToLinkTo, p.ShowPublicKey, p.ShowPrivateKey, p.ShowSecretRecoveryWords) : new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpPost("generate-keypair-with-wallet-address-and-link-provider-keys-to-avatar-by-email")]
        public OASISResult<IProviderWallet> GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByEmail(LinkProviderKeyToAvatarParams p)
        {
            (bool isValid, ProviderType providerTypeToLinkTo, Guid avatarID, string errorMessage) = ValidateParams(p);
            return isValid ? KeyManager.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByEmail(p.AvatarEmail, providerTypeToLinkTo, p.ShowPublicKey, p.ShowPrivateKey, p.ShowSecretRecoveryWords) : new OASISResult<IProviderWallet>() { IsError = true, Message = errorMessage };
        }

        [Authorize]
        [HttpPost("generate-keypair-with-wallet-address-for-provider/{providerType}")]
        public OASISResult<IKeyPairAndWallet> GenerateKeyPairWithWalletAddressForProvider(ProviderType providerType)
        {
            return KeyManager.GenerateKeyPairWithWalletAddress(providerType);
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private (bool, ProviderType, Guid, string) ValidateParams(ProviderKeyForAvatarParams linkProviderKeyToAvatarParams)
        {
            if (string.IsNullOrEmpty(linkProviderKeyToAvatarParams.AvatarID) && string.IsNullOrEmpty(linkProviderKeyToAvatarParams.AvatarUsername) && string.IsNullOrEmpty(linkProviderKeyToAvatarParams.AvatarEmail))
                return (false, ProviderType.None, Guid.Empty, $"You need to either pass in a valid Avatar ID, Avatar Username or Avatar Email.");

            if (!Enum.TryParse(typeof(ProviderType), linkProviderKeyToAvatarParams.ProviderType, out object providerTypeToLinkTo))
                return (false, ProviderType.None, Guid.Empty, $"The given ProviderType param {linkProviderKeyToAvatarParams.ProviderType} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}");

            Guid avatarID = Guid.Empty;
            if (!string.IsNullOrEmpty(linkProviderKeyToAvatarParams.AvatarID) && !Guid.TryParse(linkProviderKeyToAvatarParams.AvatarID, out avatarID))
                return (false, ProviderType.None, Guid.Empty, $"The given AvatarID {linkProviderKeyToAvatarParams.AvatarID} is not a valid Guid.");

            return (true, (ProviderType)providerTypeToLinkTo, avatarID, null);
        }
    }

    public class KeyInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateKeyRequest
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class UpdateKeyRequest
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
