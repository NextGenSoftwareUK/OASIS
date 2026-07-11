using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    /// <summary>
    /// Stores and retrieves per-avatar AI provider API keys encrypted as holon metadata in COSMIC ORM.
    /// Env vars always win at runtime; this provides a per-avatar fallback for multi-tenant deployments.
    /// </summary>
    public class KeyVaultManager : OASISManager
    {
        private const string MetaKeyPrefix = "web6_apikey_";

        public KeyVaultManager(Guid avatarId, OASISDNA dna = null) : base(avatarId, dna) { }

        /// <summary>Saves an encrypted provider API key for this avatar into COSMIC ORM metadata.</summary>
        public async Task<OASISResult<bool>> SaveProviderKeyAsync(string provider, string apiKey)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                string metaKey   = MetaKeyPrefix + provider.ToLowerInvariant();
                string encrypted = Encrypt(apiKey);

                var avatar = await NextGenSoftware.OASIS.API.Core.Managers.AvatarManager.Instance.LoadAvatarAsync(AvatarId);
                if (avatar.IsError || avatar.Result == null)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(avatar, result);

                avatar.Result.MetaData[metaKey] = encrypted;
                await NextGenSoftware.OASIS.API.Core.Managers.AvatarManager.Instance.SaveAvatarAsync(avatar.Result);
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"KeyVaultManager.SaveProviderKeyAsync error: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>Loads and decrypts the stored API key for this avatar + provider. Returns null if not found.</summary>
        public async Task<OASISResult<string>> LoadProviderKeyAsync(string provider)
        {
            OASISResult<string> result = new OASISResult<string>();
            try
            {
                string metaKey = MetaKeyPrefix + provider.ToLowerInvariant();

                var avatar = await NextGenSoftware.OASIS.API.Core.Managers.AvatarManager.Instance.LoadAvatarAsync(AvatarId);
                if (avatar.IsError || avatar.Result == null)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(avatar, result);

                if (avatar.Result.MetaData.TryGetValue(metaKey, out object encrypted) && encrypted != null)
                    result.Result = Decrypt(encrypted.ToString());
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"KeyVaultManager.LoadProviderKeyAsync error: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>Deletes the stored key for this avatar + provider.</summary>
        public async Task<OASISResult<bool>> DeleteProviderKeyAsync(string provider)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                string metaKey = MetaKeyPrefix + provider.ToLowerInvariant();

                var avatar = await NextGenSoftware.OASIS.API.Core.Managers.AvatarManager.Instance.LoadAvatarAsync(AvatarId);
                if (avatar.IsError || avatar.Result == null)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(avatar, result);

                avatar.Result.MetaData.Remove(metaKey);
                await NextGenSoftware.OASIS.API.Core.Managers.AvatarManager.Instance.SaveAvatarAsync(avatar.Result);
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"KeyVaultManager.DeleteProviderKeyAsync error: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>Lists all provider names that have a stored key for this avatar.</summary>
        public async Task<OASISResult<List<string>>> ListStoredProvidersAsync()
        {
            OASISResult<List<string>> result = new OASISResult<List<string>> { Result = new List<string>() };
            try
            {
                var avatar = await NextGenSoftware.OASIS.API.Core.Managers.AvatarManager.Instance.LoadAvatarAsync(AvatarId);
                if (avatar.IsError || avatar.Result == null)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(avatar, result);

                foreach (string key in avatar.Result.MetaData.Keys)
                    if (key.StartsWith(MetaKeyPrefix))
                        result.Result.Add(key.Substring(MetaKeyPrefix.Length));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"KeyVaultManager.ListStoredProvidersAsync error: {ex.Message}", ex);
            }
            return result;
        }

        // Minimal symmetric encryption using Base64 + XOR with a per-deployment key.
        // In production replace with AES-256-GCM using a real key from environment.
        private static string VaultKey => Environment.GetEnvironmentVariable("WEB6_VAULT_KEY") ?? "oasis-web6-default-vault-key-2026";

        private static string Encrypt(string plaintext)
        {
            byte[] data = Encoding.UTF8.GetBytes(plaintext);
            byte[] key  = Encoding.UTF8.GetBytes(VaultKey);
            for (int i = 0; i < data.Length; i++) data[i] ^= key[i % key.Length];
            return Convert.ToBase64String(data);
        }

        private static string Decrypt(string ciphertext)
        {
            byte[] data = Convert.FromBase64String(ciphertext);
            byte[] key  = Encoding.UTF8.GetBytes(VaultKey);
            for (int i = 0; i < data.Length; i++) data[i] ^= key[i % key.Length];
            return Encoding.UTF8.GetString(data);
        }
    }
}
