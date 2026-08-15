using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Cryptography.ECDSA;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Rijndael256;
using static NextGenSoftware.Utilities.KeyHelper;
using KeyPair = NextGenSoftware.OASIS.API.Core.Objects.KeyPair;
using Rijndael = Rijndael256.Rijndael;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class KeyManager
    {
        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderWalletAddressesForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.WalletAddress != null)
                            result.Result[provider].Add(wallet.WalletAddress);
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderWalletAddressesForAvatarByEmail loading avatar with email {email}. Reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.PublicKey != null)
                            result.Result[provider].Add(wallet.PublicKey);
                    }

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderPublicKeysForAvatarById loading avatar with avatarId {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarByUsername(string username, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.PublicKey != null)
                            result.Result[provider].Add(wallet.PublicKey);
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderPublicKeysForAvatarByUsername loading avatar with username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.PublicKey != null)
                            result.Result[provider].Add(wallet.PublicKey);
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderPublicKeysForAvatarByEmail loading avatar with email {email}. Reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPrivateKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();

            if (AvatarManager.LoggedInAvatar.Id != avatarId)
                OASISErrorHandling.HandleError(ref result, "An error occured in GetAllProviderPrivateKeysForAvatarById. You can only retreive your own private keys, not another persons avatar.");
            else
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, false, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    result.Result = new Dictionary<ProviderType, List<string>>();

                    foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                    {
                        //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PrivateKey).ToList();

                        //for (int i = 0; i < result.Result[provider].Count; i++)
                        //{
                        //    if (result.Result[provider][i] == null)
                        //        result.Result[provider].Remove(result.Result[provider][i]);
                        //}

                        foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                        {
                            if (!result.Result.ContainsKey(provider))
                                result.Result[provider] = new List<string>();

                            if (wallet.PrivateKey != null)
                                result.Result[provider].Add(wallet.PrivateKey);
                        }

                        // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
                        for (int i = 0; i < result.Result[provider].Count; i++)
                        {
                            if (result.Result[provider][i] != null)
                                result.Result[provider][i] = Rijndael256.Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(result.Result[provider][i], OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"An error occured in GetAllProviderPrivateKeysForAvatarById, the avatar with id {avatarId} could not be loaded. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPrivateKeysForAvatarByUsername(string username, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();

            if (AvatarManager.LoggedInAvatar == null)
                OASISErrorHandling.HandleError(ref result, "Error occured in GetAllProviderPrivateKeysForAvatarByUsername, AvatarManager.LoggedInAvatar is null!");
            
            else if (AvatarManager.LoggedInAvatar.Username != username)
                OASISErrorHandling.HandleError(ref result, "Error occured in GetAllProviderPrivateKeysForAvatarByUsername, you can only retreive your own private keys, not another persons avatar.");
            else
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, true, false, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    result.Result = new Dictionary<ProviderType, List<string>>();

                    foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                    {
                        //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PrivateKey).ToList();

                        //for (int i = 0; i < result.Result[provider].Count; i++)
                        //{
                        //    if (result.Result[provider][i] == null)
                        //        result.Result[provider].Remove(result.Result[provider][i]);
                        //}

                        foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                        {
                            if (!result.Result.ContainsKey(provider))
                                result.Result[provider] = new List<string>();

                            if (wallet.PrivateKey != null)
                                result.Result[provider].Add(wallet.PrivateKey);
                        }

                        // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
                        for (int i = 0; i < result.Result[provider].Count; i++)
                        {
                            if (result.Result[provider][i] != null)
                                result.Result[provider][i] = Rijndael256.Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(result.Result[provider][i], OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"An error occured in GetAllProviderPrivateKeysForAvatarByUsername, the avatar with username {username} could not be loaded. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        //public OASISResult<Dictionary<ProviderType, string>> GetAllProviderPrivateKeysForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();

        //    if (AvatarManager.LoggedInAvatar.Email != email)
        //        OASISErrorHandling.HandleError(ref result, "Error occured in GetAllProviderPrivateKeysForAvatarByEmail, you can only retreive your own private keys, not another persons avatar.");
        //    else
        //    {
        //        OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, providerType);

        //        if (!avatarResult.IsError && avatarResult.Result != null)
        //        {
        //            result.Result = avatarResult.Result.ProviderPrivateKey;

        //            // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
        //            foreach (ProviderType privateKeyProviderType in result.Result.Keys)
        //                result.Result[privateKeyProviderType] = Rijndael.Decrypt(result.Result[privateKeyProviderType], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
        //                //result.Result[privateKeyProviderType] = StringCipher.Decrypt(result.Result[privateKeyProviderType]);
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"An error occured in GetAllProviderPrivateKeysForAvatarByEmail, the avatar with email {email} could not be loaded. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
        //    }

        //    return result;
        //}

        public OASISResult<string> GetPrivateWif(byte[] source)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                result.Result = WifUtility.GetPrivateWif(source);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetPrivateWif. Reason: {ex}", ex);
            }

            return result;
        }

        public OASISResult<string> GetPublicWif(byte[] publicKey, string prefix)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                result.Result = WifUtility.GetPublicWif(publicKey, prefix);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetPublicWif. Reason: {ex}", ex);
            }

            return result; ;
        }

        public OASISResult<byte[]> DecodePrivateWif(string data)
        {
            OASISResult<byte[]> result = new OASISResult<byte[]>();

            try
            {
                result.Result = WifUtility.DecodePrivateWif(data);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in DecodePrivateWif. Reason: {ex}", ex);
            }

            return result;
        }

        public OASISResult<byte[]> Base58CheckDecode(string data)
        {
            OASISResult<byte[]> result = new OASISResult<byte[]>();

            try
            {
                result.Result = WifUtility.Base58CheckDecode(data);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in Base58CheckDecode. Reason: {ex}", ex);
            }

            return result;
        }

        public OASISResult<string> EncodeSignature(byte[] source)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                result.Result = WifUtility.EncodeSignature(source);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in EncodeSignature. Reason: {ex}", ex);
            }

            return result;
        }

        //TODO: Key Management System (KMS) to be implemented in future release.
        //public async Task<OASISResult<List<Key>>> GetAllKeysAsync(Guid avatarId)
        //{
        //    var result = new OASISResult<List<Key>>();
        //    try
        //    {
        //        if (_avatarKeys.TryGetValue(avatarId, out var keys))
        //        {
        //            result.Result = keys.ToList();
        //            result.Message = "Keys retrieved successfully.";
        //        }
        //        else
        //        {
        //            result.Result = new List<Key>();
        //            result.Message = "No keys found for this avatar.";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        result.IsError = true;
        //        result.Message = $"Error retrieving keys: {ex.Message}";
        //        result.Exception = ex;
        //    }
        //    return await Task.FromResult(result);
        //}

        //public async Task<OASISResult<Key>> GenerateKeyAsync(Guid avatarId, KeyType keyType, string name = null, Dictionary<string, object> metadata = null)
        //{
        //    var result = new OASISResult<Key>();
        //    try
        //    {
        //        var key = new Key
        //        {
        //            Id = Guid.NewGuid(),
        //            AvatarId = avatarId,
        //            KeyType = keyType,
        //            Name = name ?? $"{keyType} Key {DateTime.UtcNow:yyyy-MM-dd}",
        //            PublicKey = GeneratePublicKey(),
        //            PrivateKey = GeneratePrivateKey(),
        //            CreatedAt = DateTime.UtcNow,
        //            IsActive = true,
        //            UsageCount = 0,
        //            Metadata = metadata ?? new Dictionary<string, object>()
        //        };

        //        lock (_lockObject)
        //        {
        //            if (!_avatarKeys.ContainsKey(avatarId))
        //            {
        //                _avatarKeys[avatarId] = new List<Key>();
        //            }
        //            _avatarKeys[avatarId].Add(key);
        //        }

        //        result.Result = key;
        //        result.Message = "Key generated successfully.";
        //    }
        //    catch (Exception ex)
        //    {
        //        result.IsError = true;
        //        result.Result = null;
        //        result.Message = $"Error generating key: {ex.Message}";
        //        result.Exception = ex;
        //    }
        //    return await Task.FromResult(result);
        //}

        //public async Task<OASISResult<bool>> UseKeyAsync(Guid avatarId, Guid keyId, string purpose = null)
        //{
        //    var result = new OASISResult<bool>();
        //    try
        //    {
        //        if (_avatarKeys.TryGetValue(avatarId, out var keys))
        //        {
        //            var key = keys.FirstOrDefault(k => k.Id == keyId);
        //            if (key != null && key.IsActive)
        //            {
        //                key.UsageCount++;
        //                key.LastUsedAt = DateTime.UtcNow;

        //                // Record usage
        //                var usage = new KeyUsage
        //                {
        //                    Id = Guid.NewGuid(),
        //                    KeyId = keyId,
        //                    AvatarId = avatarId,
        //                    Purpose = purpose,
        //                    UsedAt = DateTime.UtcNow
        //                };

        //                lock (_lockObject)
        //                {
        //                    if (!_keyUsage.ContainsKey(avatarId))
        //                    {
        //                        _keyUsage[avatarId] = new List<KeyUsage>();
        //                    }
        //                    _keyUsage[avatarId].Add(usage);
        //                }

        //                result.Result = true;
        //                result.Message = "Key used successfully.";
        //            }
        //            else
        //            {
        //                result.IsError = true;
        //                result.Result = false;
        //                result.Message = "Key not found or inactive.";
        //            }
        //        }
        //        else
        //        {
        //            result.IsError = true;
        //            result.Result = false;
        //            result.Message = "No keys found for this avatar.";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        result.IsError = true;
        //        result.Result = false;
        //        result.Message = $"Error using key: {ex.Message}";
        //        result.Exception = ex;
        //    }
        //    return await Task.FromResult(result);
        //}

        //public async Task<OASISResult<bool>> DeactivateKeyAsync(Guid avatarId, Guid keyId)
        //{
        //    var result = new OASISResult<bool>();
        //    try
        //    {
        //        if (_avatarKeys.TryGetValue(avatarId, out var keys))
        //        {
        //            var key = keys.FirstOrDefault(k => k.Id == keyId);
        //            if (key != null)
        //            {
        //                key.IsActive = false;
        //                key.DeactivatedAt = DateTime.UtcNow;

        //                result.Result = true;
        //                result.Message = "Key deactivated successfully.";
        //            }
        //            else
        //            {
        //                result.IsError = true;
        //                result.Result = false;
        //                result.Message = "Key not found.";
        //            }
        //        }
        //        else
        //        {
        //            result.IsError = true;
        //            result.Result = false;
        //            result.Message = "No keys found for this avatar.";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        result.IsError = true;
        //        result.Result = false;
        //        result.Message = $"Error deactivating key: {ex.Message}";
        //        result.Exception = ex;
        //    }
        //    return await Task.FromResult(result);
        //}

        //public async Task<OASISResult<List<KeyUsage>>> GetKeyUsageHistoryAsync(Guid avatarId, int limit = 50, int offset = 0)
        //{
        //    var result = new OASISResult<List<KeyUsage>>();
        //    try
        //    {
        //        if (_keyUsage.TryGetValue(avatarId, out var usage))
        //        {
        //            result.Result = usage
        //                .OrderByDescending(u => u.UsedAt)
        //                .Skip(offset)
        //                .Take(limit)
        //                .ToList();
        //            result.Message = "Key usage history retrieved successfully.";
        //        }
        //        else
        //        {
        //            result.Result = new List<KeyUsage>();
        //            result.Message = "No key usage history found for this avatar.";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        result.IsError = true;
        //        result.Message = $"Error retrieving key usage history: {ex.Message}";
        //        result.Exception = ex;
        //    }
        //    return await Task.FromResult(result);
        //}

        //#region Competition Tracking

        //private async Task UpdateKeyCompetitionScoresAsync(Guid avatarId, KeyType keyType)
        //{
        //    try
        //    {
        //        var competitionManager = CompetitionManager.Instance;

        //        // Calculate score based on key type and usage
        //        var score = CalculateKeyScore(keyType);

        //        // Update social activity competition scores
        //        await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Daily, score);
        //        await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Weekly, score);
        //        await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Monthly, score);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error updating key competition scores: {ex.Message}");
        //    }
        //}

        private long CalculateKeyScore(KeyType keyType)
        {
            return keyType switch
            {
                KeyType.Authentication => 5,
                KeyType.Encryption => 10,
                KeyType.Signing => 15,
                KeyType.Access => 20,
                KeyType.Master => 50,
                KeyType.System => 25,
                _ => 1
            };
        }

        public async Task<OASISResult<Dictionary<string, object>>> GetKeyStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var keys = _avatarKeys.GetValueOrDefault(avatarId, new List<Key>());
                var usage = _keyUsage.GetValueOrDefault(avatarId, new List<KeyUsage>());

                var totalKeys = keys.Count;
                var activeKeys = keys.Count(k => k.IsActive);
                var totalUsage = usage.Count;
                var keyTypeDistribution = keys.GroupBy(k => k.KeyType).ToDictionary(g => g.Key.ToString(), g => g.Count());

                var stats = new Dictionary<string, object>
                {
                    ["totalKeys"] = totalKeys,
                    ["activeKeys"] = activeKeys,
                    ["inactiveKeys"] = totalKeys - activeKeys,
                    ["totalUsage"] = totalUsage,
                    ["keyTypeDistribution"] = keyTypeDistribution,
                    ["averageUsagePerKey"] = totalKeys > 0 ? (double)totalUsage / totalKeys : 0,
                    ["mostUsedKeyType"] = keyTypeDistribution.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "None",
                    ["totalScore"] = keys.Sum(k => CalculateKeyScore(k.KeyType) * k.UsageCount)
                };

                result.Result = stats;
                result.Message = "Key statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving key statistics: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        //#endregion

        //#region Helper Methods

        //private string GeneratePublicKey()
        //{
        //    // In a real implementation, this would generate a proper cryptographic key
        //    return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        //}

        //private string GeneratePrivateKey()
        //{
        //    // In a real implementation, this would generate a proper cryptographic key
        //    return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        //}

        //#endregion

        private OASISResult<string> GetProviderUniqueStorageKeyForAvatar(IAvatar avatar, string key, Dictionary<string, string> dictionaryCache, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();

            if (avatar != null)
            {
                if (avatar.ProviderUniqueStorageKey.ContainsKey(providerType))
                {
                    dictionaryCache[key] = avatar.ProviderUniqueStorageKey[providerType];
                    result.Result = dictionaryCache[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("The avatar with id ", avatar.Id, " and username ", avatar.Username, " has not been linked to the ", Enum.GetName(providerType), " provider."));
            }
            else
                OASISErrorHandling.HandleError(ref result, string.Concat("The avatar with id ", avatar.Id, " and username ", avatar.Username, " was not found."));

            //result.Result = dictionaryCache[key];
            return result;
        }
    }
}
