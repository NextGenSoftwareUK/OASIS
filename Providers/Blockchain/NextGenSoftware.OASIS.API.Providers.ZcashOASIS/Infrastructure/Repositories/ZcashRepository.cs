using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Services.Zcash;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Repositories
{
    public class ZcashRepository : IZcashRepository
    {
        private readonly ZcashRPCClient _rpcClient;
        private readonly string _holonStorageAddress; // Address used for storing holons
        private const int MAX_MEMO_SIZE = 512; // Zcash memo field size limit in bytes

        public ZcashRepository(ZcashRPCClient rpcClient)
        {
            _rpcClient = rpcClient;
            // In production, this would be configured or generated
            _holonStorageAddress = Environment.GetEnvironmentVariable("ZCASH_HOLON_STORAGE_ADDRESS") ?? "zt1test";
        }

        public async Task<IHolon> LoadHolonAsync(Guid id)
        {
            // Load holon by GUID
            // Strategy: Search for transaction memos containing the holon ID
            // In production, would maintain an index or scan transactions
            
            try
            {
                // For now, try to load by searching transactions
                // In production, would use an index or query by transaction ID stored in provider key
                var transactions = await _rpcClient.ListTransactionsAsync(100); // Get recent transactions
                
                if (transactions.Result != null)
                {
                    foreach (var tx in transactions.Result)
                    {
                        if (tx.ContainsKey("memo") && tx["memo"] != null)
                        {
                            var memo = tx["memo"].ToString();
                            var holon = DeserializeHolonFromMemo(memo);
                            if (holon != null && holon.Id == id)
                            {
                                // Set provider key to transaction ID
                                if (holon.ProviderUniqueStorageKey == null)
                                {
                                    holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                                }
                                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ZcashOASIS] = tx.ContainsKey("txid") ? tx["txid"]?.ToString() ?? "" : "";
                                return holon;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error and return null
                // In production, would use proper logging
            }

            return null;
        }

        public async Task<IHolon> LoadHolonByProviderKeyAsync(string providerKey)
        {
            // Load holon by transaction ID (provider key)
            try
            {
                var txResult = await _rpcClient.GetTransactionAsync(providerKey);
                if (!txResult.IsError && txResult.Result != null)
                {
                    var tx = txResult.Result as Dictionary<string, object>;
                    if (tx != null && tx.ContainsKey("memo"))
                    {
                        var memo = tx["memo"]?.ToString();
                        if (!string.IsNullOrEmpty(memo))
                        {
                            var holon = DeserializeHolonFromMemo(memo);
                            if (holon != null)
                            {
                                if (holon.ProviderUniqueStorageKey == null)
                                {
                                    holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                                }
                                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ZcashOASIS] = providerKey;
                                return holon;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error
            }

            return null;
        }

        public async Task<IHolon> SaveHolonAsync(IHolon holon)
        {
            // Save holon to Zcash blockchain using shielded transaction memo
            try
            {
                // Serialize holon to JSON
                var holonJson = JsonConvert.SerializeObject(holon, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                });

                // Handle memo size limit (512 bytes)
                // For large holons, we may need to compress or split
                var memoBytes = Encoding.UTF8.GetBytes(holonJson);
                
                if (memoBytes.Length > MAX_MEMO_SIZE)
                {
                    // Compress or truncate for now
                    // In production, would use IPFS for large holons and store CID in memo
                    holonJson = CompressHolonJson(holonJson);
                    memoBytes = Encoding.UTF8.GetBytes(holonJson);
                    
                    if (memoBytes.Length > MAX_MEMO_SIZE)
                    {
                        // Still too large - store reference only
                        holonJson = $"{{\"id\":\"{holon.Id}\",\"ref\":\"ipfs_cid_placeholder\"}}";
                    }
                }

                // Convert to base64 for memo (Zcash memos are hex-encoded, but we'll use base64 for JSON)
                var memo = Convert.ToBase64String(Encoding.UTF8.GetBytes(holonJson));

                // Create shielded transaction with holon data in memo
                // Use self-transfer with minimal amount (0.00001 ZEC)
                var txResult = await _rpcClient.SendShieldedTransactionAsync(
                    fromAddress: _holonStorageAddress,
                    toAddress: _holonStorageAddress,
                    amount: 0.00001m,
                    memo: memo
                );

                if (!txResult.IsError && !string.IsNullOrEmpty(txResult.Result))
                {
                    // Store transaction ID as provider key
                    if (holon.ProviderUniqueStorageKey == null)
                    {
                        holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                    }
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ZcashOASIS] = txResult.Result;
                    
                    // Update timestamps
                    if (holon.CreatedDate == default(DateTime))
                    {
                        holon.CreatedDate = DateTime.UtcNow;
                    }
                    holon.ModifiedDate = DateTime.UtcNow;
                    
                    return holon;
                }
            }
            catch (Exception ex)
            {
                // Log error
                throw new Exception($"Failed to save holon to Zcash: {ex.Message}", ex);
            }

            return null;
        }

        public async Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid parentId)
        {
            // Load child holons for a given parent
            var holons = new List<IHolon>();
            
            try
            {
                // Search transactions for holons with matching parent ID
                var transactions = await _rpcClient.ListTransactionsAsync(1000);
                
                if (transactions.Result != null)
                {
                    foreach (var tx in transactions.Result)
                    {
                        if (tx.ContainsKey("memo") && tx["memo"] != null)
                        {
                            var memo = tx["memo"].ToString();
                            var holon = DeserializeHolonFromMemo(memo);
                            if (holon != null && holon.ParentHolonId == parentId)
                            {
                                if (holon.ProviderUniqueStorageKey == null)
                                {
                                    holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                                }
                                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ZcashOASIS] = tx.ContainsKey("txid") ? tx["txid"]?.ToString() ?? "" : "";
                                holons.Add(holon);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error
            }

            return holons;
        }

        public async Task<IEnumerable<IHolon>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue)
        {
            // Load holons matching metadata key-value pair
            var holons = new List<IHolon>();
            
            try
            {
                var transactions = await _rpcClient.ListTransactionsAsync(1000);
                
                if (transactions.Result != null)
                {
                    foreach (var tx in transactions.Result)
                    {
                        if (tx.ContainsKey("memo") && tx["memo"] != null)
                        {
                            var memo = tx["memo"].ToString();
                            var holon = DeserializeHolonFromMemo(memo);
                            if (holon != null && holon.MetaData != null && 
                                holon.MetaData.ContainsKey(metaKey) &&
                                holon.MetaData[metaKey]?.ToString() == metaValue)
                            {
                                if (holon.ProviderUniqueStorageKey == null)
                                {
                                    holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                                }
                                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ZcashOASIS] = tx.ContainsKey("txid") ? tx["txid"]?.ToString() ?? "" : "";
                                holons.Add(holon);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error
            }

            return holons;
        }

        public async Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            // Mark holon as deleted by creating a deletion marker transaction
            try
            {
                var holon = await LoadHolonAsync(id);
                if (holon == null)
                {
                    return false;
                }

                if (softDelete)
                {
                    // Create deletion marker holon
                    var deletionMarker = new Holon
                    {
                        Id = Guid.NewGuid(),
                        Name = $"deletion_marker_{id}",
                        Description = "Holon deletion marker",
                        HolonType = Core.Enums.HolonType.Index,
                        ParentHolonId = id,
                        MetaData = new Dictionary<string, object>
                        {
                            { "deleted_holon_id", id.ToString() },
                            { "deletion_type", "soft_delete" },
                            { "deleted_at", DateTime.UtcNow.ToString("O") }
                        }
                    };

                    await SaveHolonAsync(deletionMarker);
                    return true;
                }
                else
                {
                    // Hard delete - cannot actually delete from blockchain, but mark as hard deleted
                    var deletionMarker = new Holon
                    {
                        Id = Guid.NewGuid(),
                        Name = $"hard_deletion_marker_{id}",
                        Description = "Holon hard deletion marker",
                        HolonType = Core.Enums.HolonType.Index,
                        ParentHolonId = id,
                        MetaData = new Dictionary<string, object>
                        {
                            { "deleted_holon_id", id.ToString() },
                            { "deletion_type", "hard_delete" },
                            { "deleted_at", DateTime.UtcNow.ToString("O") }
                        }
                    };

                    await SaveHolonAsync(deletionMarker);
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private IHolon DeserializeHolonFromMemo(string memo)
        {
            try
            {
                // Decode base64 memo
                var memoBytes = Convert.FromBase64String(memo);
                var holonJson = Encoding.UTF8.GetString(memoBytes);
                
                // Deserialize holon
                var holon = JsonConvert.DeserializeObject<Holon>(holonJson);
                return holon;
            }
            catch
            {
                // Try direct JSON if not base64
                try
                {
                    var holon = JsonConvert.DeserializeObject<Holon>(memo);
                    return holon;
                }
                catch
                {
                    return null;
                }
            }
        }

        private string CompressHolonJson(string json)
        {
            // Simple compression: remove whitespace and null values
            // In production, would use proper compression or IPFS for large holons
            try
            {
                var obj = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Newtonsoft.Json.Formatting.None
                });
            }
            catch
            {
                return json;
            }
        }

        public async Task<Guid?> GetAvatarIdByUsernameAsync(string username)
        {
            // Load index holon for username lookup
            var indexHolons = await LoadHolonsByMetaDataAsync("index_type", "username");
            var indexHolon = indexHolons.FirstOrDefault(h => 
                h.MetaData != null && 
                h.MetaData.ContainsKey("username") && 
                h.MetaData["username"]?.ToString() == username);
            
            if (indexHolon != null && indexHolon.MetaData != null && indexHolon.MetaData.ContainsKey("avatar_id"))
            {
                if (Guid.TryParse(indexHolon.MetaData["avatar_id"]?.ToString(), out Guid avatarId))
                {
                    return avatarId;
                }
            }
            
            return null;
        }

        public async Task<Guid?> GetAvatarIdByEmailAsync(string email)
        {
            // Load index holon for email lookup
            var indexHolons = await LoadHolonsByMetaDataAsync("index_type", "email");
            var indexHolon = indexHolons.FirstOrDefault(h => 
                h.MetaData != null && 
                h.MetaData.ContainsKey("email") && 
                h.MetaData["email"]?.ToString() == email);
            
            if (indexHolon != null && indexHolon.MetaData != null && indexHolon.MetaData.ContainsKey("avatar_id"))
            {
                if (Guid.TryParse(indexHolon.MetaData["avatar_id"]?.ToString(), out Guid avatarId))
                {
                    return avatarId;
                }
            }
            
            return null;
        }

        public async Task<bool> SaveAvatarIndexAsync(string indexType, string indexValue, Guid avatarId)
        {
            // Create index holon for username/email lookup
            var indexHolon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = $"avatar_index_{indexType}_{indexValue}",
                Description = $"Avatar index for {indexType}: {indexValue}",
                HolonType = Core.Enums.HolonType.Index,
                MetaData = new Dictionary<string, object>
                {
                    { "index_type", indexType },
                    { indexType, indexValue },
                    { "avatar_id", avatarId.ToString() }
                }
            };

            var saved = await SaveHolonAsync(indexHolon);
            return saved != null;
        }
    }
}

