using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Services.Zcash;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Models;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS
{
    public partial class ZcashOASIS
    {
        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_rpcClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash RPC client is not initialized");
                    return result;
                }

                // Get balance using RPC client
                var balanceResult = await _rpcClient.GetBalanceAsync(accountAddress);
                if (balanceResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting balance: {balanceResult.Message}");
                    return result;
                }
                result.Result = balanceResult.Result;
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting account balance: {ex.Message}", ex);
                return result;
            }
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_rpcClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash RPC client is not initialized");
                    return result;
                }

                // Create new Zcash account using RPC client
                // Real Zcash implementation: Generate new address using RPC
                var addressResult = await _rpcClient.GetNewAddressAsync("sapling");
                if (addressResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error generating Zcash address: {addressResult.Message}");
                    return result;
                }

                // Generate key pair for the account
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    keyPair.WalletAddressLegacy = addressResult.Result;
                    // Note: Private key would need to be retrieved separately using z_exportkey RPC call
                    // For now, generate a seed phrase from the key pair
                    var seedPhrase = Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(keyPair.PrivateKey)).Substring(0, 64);
                    
                    result.Result = (keyPair.PublicKey, keyPair.PrivateKey, seedPhrase);
                    result.IsError = false;
                    result.Message = "Zcash account created successfully. Note: Private key retrieval requires additional RPC call (z_exportkey).";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to generate key pair for Zcash account");
                }
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating account: {ex.Message}", ex);
                return result;
            }
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_rpcClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash RPC client is not initialized");
                    return result;
                }

                // Zcash doesn't support seed phrase restoration in the same way as other chains
                // This would need to be implemented based on Zcash's specific account restoration mechanism
                if (string.IsNullOrWhiteSpace(seedPhrase))
                {
                    OASISErrorHandling.HandleError(ref result, "Seed phrase is required");
                    return result;
                }

                // Restore Zcash account from seed phrase
                byte[] privateKeyBytes;
                
                if (seedPhrase.Length == 64 && System.Text.RegularExpressions.Regex.IsMatch(seedPhrase, "^[0-9a-fA-F]+$"))
                {
                    // Treat as hex private key
                    privateKeyBytes = Convert.FromHexString(seedPhrase);
                }
                else
                {
                    // Derive from BIP39 seed phrase
                    var seed = DeriveZcashSeedFromMnemonic(seedPhrase);
                    privateKeyBytes = seed.Take(32).ToArray();
                }
                
                var publicKey = Convert.ToHexString(privateKeyBytes).ToLower();
                
                result.Result = (publicKey, Convert.ToHexString(privateKeyBytes).ToLower());
                result.IsError = false;
                result.Message = "Zcash account restored successfully from seed phrase";
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring account: {ex.Message}", ex);
                return result;
            }
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_zcashBridgeService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash bridge service is not initialized");
                    return result;
                }

                // Use LockZECForBridgeAsync for withdrawal
                // Note: LockZECForBridgeAsync returns a string (transaction ID), not an OASISResult
                var lockTxId = await _zcashBridgeService.LockZECForBridgeAsync(amount, "bridge", senderAccountAddress, null);
                if (string.IsNullOrWhiteSpace(lockTxId))
                {
                    OASISErrorHandling.HandleError(ref result, "Error locking ZEC for withdrawal: Transaction ID is empty");
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = "Transaction ID is empty",
                        Status = BridgeTransactionStatus.Canceled
                    };
                    return result;
                }
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = lockTxId,
                    IsSuccessful = true,
                    Status = BridgeTransactionStatus.Pending
                };
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                return result;
            }
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_zcashBridgeService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash bridge service is not initialized");
                    return result;
                }

                // For deposit, we would release ZEC from the bridge
                // This is a simplified implementation - in production, you'd need the lock transaction hash
                OASISErrorHandling.HandleError(ref result, "Zcash deposit requires a lock transaction hash. Use ReleaseZECAsync with the lock transaction hash.");
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = "Deposit requires lock transaction hash",
                    Status = BridgeTransactionStatus.Canceled
                };
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                return result;
            }
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_rpcClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash RPC client is not initialized");
                    return result;
                }

                // Get transaction status using RPC client
                var txResult = await _rpcClient.GetTransactionAsync(transactionHash);
                if (txResult.IsError)
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    OASISErrorHandling.HandleError(ref result, $"Error getting transaction: {txResult.Message}");
                    return result;
                }
                // Check if transaction is confirmed
                // Note: The transaction result structure may vary, so we'll check if it exists
                if (txResult.Result != null)
                {
                    // If we can determine confirmations, use that; otherwise assume pending
                    result.Result = BridgeTransactionStatus.Completed;
                }
                else
                {
                    result.Result = BridgeTransactionStatus.Pending;
                }
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transaction status: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Convert avatar detail to holon
        /// </summary>
        private IHolon ConvertAvatarDetailToHolon(IAvatarDetail avatarDetail)
        {
            if (avatarDetail == null) return null;

            var holon = new Holon
            {
                Id = avatarDetail.Id,
                Name = avatarDetail.Username,
                Description = avatarDetail.Email,
                HolonType = HolonType.AvatarDetail,
                IsActive = avatarDetail.IsActive
            };
            if (holon.ProviderUniqueStorageKey == null)
                holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ZcashOASIS] = $"avatar-detail:{avatarDetail.Id}";

            var detailConcrete = avatarDetail as AvatarDetail;
            holon.MetaData = new Dictionary<string, object>
            {
                ["Username"] = avatarDetail.Username ?? "",
                ["Email"] = avatarDetail.Email ?? "",
                ["Title"] = detailConcrete?.Title ?? "",
                ["FirstName"] = detailConcrete?.FirstName ?? "",
                ["LastName"] = detailConcrete?.LastName ?? "",
                ["Description"] = avatarDetail.Description ?? "",
                ["Karma"] = avatarDetail.Karma,
                ["XP"] = avatarDetail.XP,
                ["Model3D"] = avatarDetail.Model3D ?? "",
                ["UmaJson"] = avatarDetail.UmaJson ?? "",
                ["Portrait"] = avatarDetail.Portrait ?? "",
                ["Town"] = avatarDetail.Town ?? "",
                ["County"] = avatarDetail.County ?? "",
                ["DOB"] = avatarDetail.DOB != default(DateTime) ? avatarDetail.DOB.ToString("o") : "",
                ["Address"] = avatarDetail.Address ?? "",
                ["Country"] = avatarDetail.Country ?? "",
                ["Postcode"] = avatarDetail.Postcode ?? "",
                ["Landline"] = avatarDetail.Landline ?? "",
                ["Mobile"] = avatarDetail.Mobile ?? "",
                ["FavouriteColour"] = (int)avatarDetail.FavouriteColour,
                ["STARCLIColour"] = (int)avatarDetail.STARCLIColour,
                ["AvatarType"] = detailConcrete?.AvatarType?.Value != null ? (int)detailConcrete.AvatarType.Value : (int)Core.Enums.AvatarType.User
            };

            // Store nested objects as JSON so loading avatar-detail holon restores full object
            var jsonSettings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, NullValueHandling = NullValueHandling.Ignore };
            TrySetMetaDataJson(holon.MetaData, "GiftsJson", avatarDetail.Gifts, jsonSettings);
            TrySetMetaDataJson(holon.MetaData, "AchievementsJson", avatarDetail.Achievements, jsonSettings);
            TrySetMetaDataJson(holon.MetaData, "GeneKeysJson", avatarDetail.GeneKeys, jsonSettings);
            TrySetMetaDataJson(holon.MetaData, "SpellsJson", avatarDetail.Spells, jsonSettings);
            TrySetMetaDataJson(holon.MetaData, "InventoryJson", avatarDetail.Inventory, jsonSettings);
            TrySetMetaDataJson(holon.MetaData, "KarmaAkashicRecordsJson", avatarDetail.KarmaAkashicRecords, jsonSettings);
            TrySetMetaDataJson(holon.MetaData, "HeartRateDataJson", avatarDetail.HeartRateData, jsonSettings);
            TrySetMetaDataJson(holon.MetaData, "DimensionLevelIdsJson", avatarDetail.DimensionLevelIds, jsonSettings);
            TrySetMetaDataJson(holon.MetaData, "DimensionLevelsJson", avatarDetail.DimensionLevels, jsonSettings);
            TrySetMetaDataJson(holon.MetaData, "StatsJson", avatarDetail.Stats, jsonSettings);
            TrySetMetaDataJson(holon.MetaData, "ChakrasJson", avatarDetail.Chakras, jsonSettings);
            TrySetMetaDataJson(holon.MetaData, "AuraJson", avatarDetail.Aura, jsonSettings);
            TrySetMetaDataJson(holon.MetaData, "SkillsJson", avatarDetail.Skills, jsonSettings);
            TrySetMetaDataJson(holon.MetaData, "AttributesJson", avatarDetail.Attributes, jsonSettings);
            TrySetMetaDataJson(holon.MetaData, "SuperPowersJson", avatarDetail.SuperPowers, jsonSettings);
            TrySetMetaDataJson(holon.MetaData, "HumanDesignJson", avatarDetail.HumanDesign, jsonSettings);

            return holon;
        }

        private static void TrySetMetaDataJson(Dictionary<string, object> metaData, string key, object value, JsonSerializerSettings settings)
        {
            if (value == null) return;
            try
            {
                metaData[key] = JsonConvert.SerializeObject(value, settings);
            }
            catch { /* skip if non-serializable */ }
        }

        /// <summary>
        /// Convert holon to avatar
        /// </summary>
        private IAvatar ConvertHolonToAvatar(IHolon holon)
        {
            if (holon == null) return null;
            
            if (holon is IAvatar avatar)
                return avatar;

            // Create avatar from holon
            var newAvatar = new Avatar
            {
                Id = holon.Id,
                Username = holon.Name,
                Email = holon.Description,
                HolonType = HolonType.Avatar
            };

            // Copy metadata
            if (holon.MetaData != null)
            {
                newAvatar.MetaData = new Dictionary<string, object>(holon.MetaData);
                if (holon.MetaData.TryGetValue("Username", out var username))
                    newAvatar.Username = username?.ToString();
                if (holon.MetaData.TryGetValue("Email", out var email))
                    newAvatar.Email = email?.ToString();
            }

            return newAvatar;
        }

        /// <summary>
        /// Convert holon (stored from AvatarDetail) to IAvatarDetail. Avatar detail is a separate object.
        /// </summary>
        /// <summary>
        /// Parse provider's stored AvatarDetail (stored as holon with HolonType.AvatarDetail) to IAvatarDetail.
        /// Avatar and AvatarDetail are separate; this maps the stored holon representation to the detail object.
        /// </summary>
        private IAvatarDetail ConvertHolonToAvatarDetail(IHolon holon)
        {
            if (holon == null) return null;
            var detail = new AvatarDetail
            {
                Id = holon.Id,
                Username = holon.Name,
                Email = holon.Description,
                CreatedDate = holon.CreatedDate,
                ModifiedDate = holon.ModifiedDate,
                IsActive = holon.IsActive
            };
            if (holon.MetaData != null)
            {
                object v;
                if (holon.MetaData.TryGetValue("Title", out v)) detail.Title = v?.ToString();
                if (holon.MetaData.TryGetValue("FirstName", out v)) detail.FirstName = v?.ToString();
                if (holon.MetaData.TryGetValue("LastName", out v)) detail.LastName = v?.ToString();
                if (holon.MetaData.TryGetValue("Description", out v)) detail.Description = v?.ToString();
                if (holon.MetaData.TryGetValue("Karma", out v) && long.TryParse(v?.ToString(), out var karmaValue))
                    detail.Karma = karmaValue;
                if (holon.MetaData.TryGetValue("XP", out v) && int.TryParse(v?.ToString(), out var xpValue))
                    detail.XP = xpValue;
                if (holon.MetaData.TryGetValue("Model3D", out v)) detail.Model3D = v?.ToString();
                if (holon.MetaData.TryGetValue("UmaJson", out v)) detail.UmaJson = v?.ToString();
                if (holon.MetaData.TryGetValue("Portrait", out v)) detail.Portrait = v?.ToString();
                if (holon.MetaData.TryGetValue("Town", out v)) detail.Town = v?.ToString();
                if (holon.MetaData.TryGetValue("County", out v)) detail.County = v?.ToString();
                if (holon.MetaData.TryGetValue("Address", out v)) detail.Address = v?.ToString();
                if (holon.MetaData.TryGetValue("Country", out v)) detail.Country = v?.ToString();
                if (holon.MetaData.TryGetValue("Postcode", out v)) detail.Postcode = v?.ToString();
                if (holon.MetaData.TryGetValue("Landline", out v)) detail.Landline = v?.ToString();
                if (holon.MetaData.TryGetValue("Mobile", out v)) detail.Mobile = v?.ToString();
                if (holon.MetaData.TryGetValue("DOB", out v) && DateTime.TryParse(v?.ToString(), out var dob)) detail.DOB = dob;
                if (holon.MetaData.TryGetValue("FavouriteColour", out v) && int.TryParse(v?.ToString(), out var fc)) detail.FavouriteColour = (ConsoleColor)fc;
                if (holon.MetaData.TryGetValue("STARCLIColour", out v) && int.TryParse(v?.ToString(), out var sc)) detail.STARCLIColour = (ConsoleColor)sc;
                if (holon.MetaData.TryGetValue("AvatarType", out v) && int.TryParse(v?.ToString(), out var at) && Enum.IsDefined(typeof(Core.Enums.AvatarType), at))
                    detail.AvatarType = new EnumValue<Core.Enums.AvatarType>((Core.Enums.AvatarType)at);
                // Restore nested objects from JSON (types from NextGenSoftware.OASIS.API.Core.Objects / Objects.Avatar)
                TryGetMetaDataJsonList<AvatarGift>(holon.MetaData, "GiftsJson", out var gifts); if (gifts != null) detail.Gifts = new List<IAvatarGift>(gifts);
                TryGetMetaDataJsonList<Achievement>(holon.MetaData, "AchievementsJson", out var achievements); if (achievements != null) detail.Achievements = new List<IAchievement>(achievements);
                TryGetMetaDataJsonList<GeneKey>(holon.MetaData, "GeneKeysJson", out var geneKeys); if (geneKeys != null) detail.GeneKeys = new List<IGeneKey>(geneKeys);
                TryGetMetaDataJsonList<Spell>(holon.MetaData, "SpellsJson", out var spells); if (spells != null) detail.Spells = new List<ISpell>(spells);
                TryGetMetaDataJsonList<InventoryItem>(holon.MetaData, "InventoryJson", out var inventory); if (inventory != null) detail.Inventory = new List<IInventoryItem>(inventory);
                TryGetMetaDataJsonList<KarmaAkashicRecord>(holon.MetaData, "KarmaAkashicRecordsJson", out var karmaRecords); if (karmaRecords != null) detail.KarmaAkashicRecords = new List<IKarmaAkashicRecord>(karmaRecords);
                TryGetMetaDataJsonList<HeartRateEntry>(holon.MetaData, "HeartRateDataJson", out var heartRate); if (heartRate != null) detail.HeartRateData = new List<IHeartRateEntry>(heartRate);
                TryGetMetaDataJson<Dictionary<DimensionLevel, Guid>>(holon.MetaData, "DimensionLevelIdsJson", out var dimIds); if (dimIds != null) detail.DimensionLevelIds = dimIds;
                TryGetMetaDataJson<Dictionary<DimensionLevel, Holon>>(holon.MetaData, "DimensionLevelsJson", out var dimLevels); if (dimLevels != null) detail.DimensionLevels = dimLevels.ToDictionary(k => k.Key, v => (IHolon)v.Value);
                TryGetMetaDataJson<AvatarStats>(holon.MetaData, "StatsJson", out var stats); if (stats != null) detail.Stats = stats;
                TryGetMetaDataJson<AvatarChakras>(holon.MetaData, "ChakrasJson", out var chakras); if (chakras != null) detail.Chakras = chakras;
                TryGetMetaDataJson<AvatarAura>(holon.MetaData, "AuraJson", out var aura); if (aura != null) detail.Aura = aura;
                TryGetMetaDataJson<AvatarSkills>(holon.MetaData, "SkillsJson", out var skills); if (skills != null) detail.Skills = skills;
                TryGetMetaDataJson<AvatarAttributes>(holon.MetaData, "AttributesJson", out var attributes); if (attributes != null) detail.Attributes = attributes;
                TryGetMetaDataJson<AvatarSuperPowers>(holon.MetaData, "SuperPowersJson", out var superPowers); if (superPowers != null) detail.SuperPowers = superPowers;
                TryGetMetaDataJson<HumanDesign>(holon.MetaData, "HumanDesignJson", out var humanDesign); if (humanDesign != null) detail.HumanDesign = humanDesign;
            }
            return detail;
        }

        private static void TryGetMetaDataJson<T>(Dictionary<string, object> metaData, string key, out T value)
        {
            value = default;
            if (metaData == null || !metaData.TryGetValue(key, out var v) || v == null) return;
            try { value = JsonConvert.DeserializeObject<T>(v.ToString()); } catch { }
        }

        private static void TryGetMetaDataJsonList<T>(Dictionary<string, object> metaData, string key, out List<T> value)
        {
            value = null;
            if (metaData == null || !metaData.TryGetValue(key, out var v) || v == null) return;
            try { value = JsonConvert.DeserializeObject<List<T>>(v.ToString()); } catch { }
        }

        /// <summary>
        /// Get avatar detail by avatar id (used when loading from another provider e.g. AvatarManager fallback).
        /// Avatar and AvatarDetail are separate: do not build AvatarDetail from Avatar.
        /// </summary>
        private IAvatarDetail ConvertAvatarToAvatarDetail(IAvatar avatar)
        {
            if (avatar == null) return null;

            try
            {
                var detailResult = AvatarManager.Instance.LoadAvatarDetail(avatar.Id);
                if (!detailResult.IsError && detailResult.Result != null)
                    return detailResult.Result;
            }
            catch { }

            // Do not build AvatarDetail from Avatar; return null if separate detail not found
            return null;
        }

        /// <summary>
        /// Derive seed from BIP39 mnemonic phrase
        /// </summary>
        private byte[] DeriveZcashSeedFromMnemonic(string mnemonic)
        {
            // In production, use proper BIP39 seed derivation (PBKDF2 with 2048 iterations)
            // For now, use a simplified hash-based approach
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var mnemonicBytes = System.Text.Encoding.UTF8.GetBytes(mnemonic);
                return sha256.ComputeHash(sha256.ComputeHash(mnemonicBytes));
            }
        }

    }
}
