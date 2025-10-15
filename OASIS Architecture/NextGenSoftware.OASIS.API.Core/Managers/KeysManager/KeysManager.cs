//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.OASIS.API.DNA;
//using NextGenSoftware.Utilities;

//namespace NextGenSoftware.OASIS.API.Core.Managers
//{
//    public partial class KeysManager : OASISManager
//    {
//        private static KeysManager _instance;
//        private readonly object _lockObject = new object();
//        private readonly Dictionary<Guid, List<Key>> _avatarKeys;
//        private readonly Dictionary<Guid, List<KeyUsage>> _keyUsage;

//        public static KeysManager Instance
//        {
//            get
//            {
//                if (_instance == null)
//                    _instance = new KeysManager(ProviderManager.Instance.CurrentStorageProvider);
//                return _instance;
//            }
//        }

//        public KeysManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
//        {
//            _avatarKeys = new Dictionary<Guid, List<Key>>();
//            _keyUsage = new Dictionary<Guid, List<KeyUsage>>();
//        }

//        public async Task<OASISResult<List<Key>>> GetAllKeysAsync(Guid avatarId)
//        {
//            var result = new OASISResult<List<Key>>();
//            try
//            {
//                if (_avatarKeys.TryGetValue(avatarId, out var keys))
//                {
//                    result.Result = keys.ToList();
//                    result.Message = "Keys retrieved successfully.";
//                }
//                else
//                {
//                    result.Result = new List<Key>();
//                    result.Message = "No keys found for this avatar.";
//                }
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Message = $"Error retrieving keys: {ex.Message}";
//                result.Exception = ex;
//            }
//            return await Task.FromResult(result);
//        }

//        public async Task<OASISResult<Key>> GenerateKeyAsync(Guid avatarId, KeyType keyType, string name = null, Dictionary<string, object> metadata = null)
//        {
//            var result = new OASISResult<Key>();
//            try
//            {
//                var key = new Key
//                {
//                    Id = Guid.NewGuid(),
//                    AvatarId = avatarId,
//                    KeyType = keyType,
//                    Name = name ?? $"{keyType} Key {DateTime.UtcNow:yyyy-MM-dd}",
//                    PublicKey = GeneratePublicKey(),
//                    PrivateKey = GeneratePrivateKey(),
//                    CreatedAt = DateTime.UtcNow,
//                    IsActive = true,
//                    UsageCount = 0,
//                    Metadata = metadata ?? new Dictionary<string, object>()
//                };

//                lock (_lockObject)
//                {
//                    if (!_avatarKeys.ContainsKey(avatarId))
//                    {
//                        _avatarKeys[avatarId] = new List<Key>();
//                    }
//                    _avatarKeys[avatarId].Add(key);
//                }

//                result.Result = key;
//                result.Message = "Key generated successfully.";
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Result = null;
//                result.Message = $"Error generating key: {ex.Message}";
//                result.Exception = ex;
//            }
//            return await Task.FromResult(result);
//        }

//        public async Task<OASISResult<bool>> UseKeyAsync(Guid avatarId, Guid keyId, string purpose = null)
//        {
//            var result = new OASISResult<bool>();
//            try
//            {
//                if (_avatarKeys.TryGetValue(avatarId, out var keys))
//                {
//                    var key = keys.FirstOrDefault(k => k.Id == keyId);
//                    if (key != null && key.IsActive)
//                    {
//                        key.UsageCount++;
//                        key.LastUsedAt = DateTime.UtcNow;

//                        // Record usage
//                        var usage = new KeyUsage
//                        {
//                            Id = Guid.NewGuid(),
//                            KeyId = keyId,
//                            AvatarId = avatarId,
//                            Purpose = purpose,
//                            UsedAt = DateTime.UtcNow
//                        };

//                        lock (_lockObject)
//                        {
//                            if (!_keyUsage.ContainsKey(avatarId))
//                            {
//                                _keyUsage[avatarId] = new List<KeyUsage>();
//                            }
//                            _keyUsage[avatarId].Add(usage);
//                        }

//                        result.Result = true;
//                        result.Message = "Key used successfully.";
//                    }
//                    else
//                    {
//                        result.IsError = true;
//                        result.Result = false;
//                        result.Message = "Key not found or inactive.";
//                    }
//                }
//                else
//                {
//                    result.IsError = true;
//                    result.Result = false;
//                    result.Message = "No keys found for this avatar.";
//                }
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Result = false;
//                result.Message = $"Error using key: {ex.Message}";
//                result.Exception = ex;
//            }
//            return await Task.FromResult(result);
//        }

//        public async Task<OASISResult<bool>> DeactivateKeyAsync(Guid avatarId, Guid keyId)
//        {
//            var result = new OASISResult<bool>();
//            try
//            {
//                if (_avatarKeys.TryGetValue(avatarId, out var keys))
//                {
//                    var key = keys.FirstOrDefault(k => k.Id == keyId);
//                    if (key != null)
//                    {
//                        key.IsActive = false;
//                        key.DeactivatedAt = DateTime.UtcNow;

//                        result.Result = true;
//                        result.Message = "Key deactivated successfully.";
//                    }
//                    else
//                    {
//                        result.IsError = true;
//                        result.Result = false;
//                        result.Message = "Key not found.";
//                    }
//                }
//                else
//                {
//                    result.IsError = true;
//                    result.Result = false;
//                    result.Message = "No keys found for this avatar.";
//                }
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Result = false;
//                result.Message = $"Error deactivating key: {ex.Message}";
//                result.Exception = ex;
//            }
//            return await Task.FromResult(result);
//        }

//        public async Task<OASISResult<List<KeyUsage>>> GetKeyUsageHistoryAsync(Guid avatarId, int limit = 50, int offset = 0)
//        {
//            var result = new OASISResult<List<KeyUsage>>();
//            try
//            {
//                if (_keyUsage.TryGetValue(avatarId, out var usage))
//                {
//                    result.Result = usage
//                        .OrderByDescending(u => u.UsedAt)
//                        .Skip(offset)
//                        .Take(limit)
//                        .ToList();
//                    result.Message = "Key usage history retrieved successfully.";
//                }
//                else
//                {
//                    result.Result = new List<KeyUsage>();
//                    result.Message = "No key usage history found for this avatar.";
//                }
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Message = $"Error retrieving key usage history: {ex.Message}";
//                result.Exception = ex;
//            }
//            return await Task.FromResult(result);
//        }

//        #region Competition Tracking

//        private async Task UpdateKeyCompetitionScoresAsync(Guid avatarId, KeyType keyType)
//        {
//            try
//            {
//                var competitionManager = CompetitionManager.Instance;
                
//                // Calculate score based on key type and usage
//                var score = CalculateKeyScore(keyType);
                
//                // Update social activity competition scores
//                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Daily, score);
//                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Weekly, score);
//                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Monthly, score);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error updating key competition scores: {ex.Message}");
//            }
//        }

//        private long CalculateKeyScore(KeyType keyType)
//        {
//            return keyType switch
//            {
//                KeyType.Authentication => 5,
//                KeyType.Encryption => 10,
//                KeyType.Signing => 15,
//                KeyType.Access => 20,
//                KeyType.Master => 50,
//                KeyType.System => 25,
//                _ => 1
//            };
//        }

//        public async Task<OASISResult<Dictionary<string, object>>> GetKeyStatsAsync(Guid avatarId)
//        {
//            var result = new OASISResult<Dictionary<string, object>>();
//            try
//            {
//                var keys = _avatarKeys.GetValueOrDefault(avatarId, new List<Key>());
//                var usage = _keyUsage.GetValueOrDefault(avatarId, new List<KeyUsage>());

//                var totalKeys = keys.Count;
//                var activeKeys = keys.Count(k => k.IsActive);
//                var totalUsage = usage.Count;
//                var keyTypeDistribution = keys.GroupBy(k => k.KeyType).ToDictionary(g => g.Key.ToString(), g => g.Count());

//                var stats = new Dictionary<string, object>
//                {
//                    ["totalKeys"] = totalKeys,
//                    ["activeKeys"] = activeKeys,
//                    ["inactiveKeys"] = totalKeys - activeKeys,
//                    ["totalUsage"] = totalUsage,
//                    ["keyTypeDistribution"] = keyTypeDistribution,
//                    ["averageUsagePerKey"] = totalKeys > 0 ? (double)totalUsage / totalKeys : 0,
//                    ["mostUsedKeyType"] = keyTypeDistribution.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "None",
//                    ["totalScore"] = keys.Sum(k => CalculateKeyScore(k.KeyType) * k.UsageCount)
//                };

//                result.Result = stats;
//                result.Message = "Key statistics retrieved successfully.";
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Message = $"Error retrieving key statistics: {ex.Message}";
//                result.Exception = ex;
//            }
//            return await Task.FromResult(result);
//        }

//        #endregion

//        #region Helper Methods

//        private string GeneratePublicKey()
//        {
//            // In a real implementation, this would generate a proper cryptographic key
//            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
//        }

//        private string GeneratePrivateKey()
//        {
//            // In a real implementation, this would generate a proper cryptographic key
//            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
//        }

//        #endregion
//    }

//    public class Key
//    {
//        public Guid Id { get; set; }
//        public Guid AvatarId { get; set; }
//        public KeyType KeyType { get; set; }
//        public string Name { get; set; }
//        public string PublicKey { get; set; }
//        public string PrivateKey { get; set; }
//        public DateTime CreatedAt { get; set; }
//        public DateTime? LastUsedAt { get; set; }
//        public DateTime? DeactivatedAt { get; set; }
//        public bool IsActive { get; set; }
//        public int UsageCount { get; set; }
//        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
//    }

//    public class KeyUsage
//    {
//        public Guid Id { get; set; }
//        public Guid KeyId { get; set; }
//        public Guid AvatarId { get; set; }
//        public string Purpose { get; set; }
//        public DateTime UsedAt { get; set; }
//    }

//    public enum KeyType
//    {
//        Authentication,
//        Encryption,
//        Signing,
//        Access,
//        Master,
//        System,
//        Custom
//    }
//}
