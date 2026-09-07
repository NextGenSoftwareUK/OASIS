using System;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS
{
    public partial class ArweaveOASIS
    {
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var tags = new Dictionary<string, string>
                {
                    { "OASIS-Type", "Holon" },
                    { "OASIS-ParentId", id.ToString() }
                };

                var txIds = await _arweaveService.QueryByTagsAsync(tags);
                var holons = new List<IHolon>();

                foreach (var txId in txIds ?? new List<string>())
                {
                    try
                    {
                        var data = await _arweaveService.GetTransactionDataAsync(txId);
                        if (data == null) continue;

                        var holon = JsonConvert.DeserializeObject<Holon>(Encoding.UTF8.GetString(data));

                        if (holon != null && (type == HolonType.All || holon.HolonType == type))
                            holons.Add(holon);
                    }
                    catch
                    {
                        if (!continueOnError) throw;
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Loaded {holons.Count} child holons for parent {id} from Arweave.";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (string.IsNullOrWhiteSpace(providerKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Provider key is required.");
                    return result;
                }

                var parentResult = await LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
                if (parentResult.IsError || parentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Parent holon not found in Arweave: {parentResult.Message}");
                    return result;
                }

                return await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (string.IsNullOrWhiteSpace(metaKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Meta key is required.");
                    return result;
                }

                var allResult = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                if (allResult.IsError || allResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons from Arweave: {allResult.Message}");
                    return result;
                }

                var filtered = allResult.Result
                    .Where(h => HolonMetaMatches(h, metaKey, metaValue))
                    .ToList();

                result.Result = filtered;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Found {filtered.Count} holons matching metadata '{metaKey}'='{metaValue}' in Arweave.";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (metaKeyValuePairs == null || metaKeyValuePairs.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Meta key/value pairs are required.");
                    return result;
                }

                var allResult = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                if (allResult.IsError || allResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons from Arweave: {allResult.Message}");
                    return result;
                }

                bool Matches(IHolon h)
                {
                    if (h?.MetaData == null) return false;
                    if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                        return metaKeyValuePairs.All(kvp => HolonMetaMatches(h, kvp.Key, kvp.Value));
                    return metaKeyValuePairs.Any(kvp => HolonMetaMatches(h, kvp.Key, kvp.Value));
                }

                var filtered = allResult.Result.Where(Matches).ToList();
                result.Result = filtered;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Found {filtered.Count} holons matching metadata pairs in Arweave.";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var tags = new Dictionary<string, string>
                {
                    { "OASIS-Type", "Holon" },
                    { "App-Name", "OASIS" }
                };

                var txIds = await _arweaveService.QueryByTagsAsync(tags);
                var holons = new List<IHolon>();

                foreach (var txId in txIds ?? new List<string>())
                {
                    try
                    {
                        var data = await _arweaveService.GetTransactionDataAsync(txId);
                        if (data == null) continue;

                        var holon = JsonConvert.DeserializeObject<Holon>(Encoding.UTF8.GetString(data));
                        if (holon == null) continue;

                        if (type != HolonType.All && holon.HolonType != type) continue;
                        if (version > 0 && holon.Version != version) continue;

                        holons.Add(holon);
                    }
                    catch
                    {
                        if (!continueOnError) throw;
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Loaded {holons.Count} holons from Arweave.";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();

            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var txIds = await _arweaveService.QueryByTagsAsync(new Dictionary<string, string>
                {
                    { "OASIS-Type", "AvatarDetail" }
                });

                var avatarDetails = new List<IAvatarDetail>();

                foreach (var txId in txIds ?? new List<string>())
                {
                    try
                    {
                        var data = await _arweaveService.GetTransactionDataAsync(txId);
                        if (data == null) continue;

                        var avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(Encoding.UTF8.GetString(data));
                        if (avatarDetail == null) continue;
                        if (version > 0 && avatarDetail.Version != version) continue;

                        avatarDetails.Add(avatarDetail);
                    }
                    catch { /* ignore non-avatar-detail transactions */ }
                }

                result.Result = avatarDetails;
                result.IsLoaded = true;
                result.IsError = false;
                result.Message = $"Loaded {avatarDetails.Count} avatar details from Arweave.";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();

            try
            {
                if (string.IsNullOrWhiteSpace(avatarEmail))
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar email is required.");
                    return result;
                }

                var allResult = await LoadAllAvatarDetailsAsync(version);
                if (allResult.IsError || allResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Arweave: {allResult.Message}");
                    return result;
                }

                var match = allResult.Result.FirstOrDefault(a => string.Equals(a.Email, avatarEmail, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"No avatar detail found with email: {avatarEmail}");
                    return result;
                }

                result.Result = match;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = "Avatar detail loaded by email from Arweave.";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();

            try
            {
                if (string.IsNullOrWhiteSpace(avatarUsername))
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar username is required.");
                    return result;
                }

                var allResult = await LoadAllAvatarDetailsAsync(version);
                if (allResult.IsError || allResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Arweave: {allResult.Message}");
                    return result;
                }

                var match = allResult.Result.FirstOrDefault(a => string.Equals(a.Username, avatarUsername, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"No avatar detail found with username: {avatarUsername}");
                    return result;
                }

                result.Result = match;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = "Avatar detail loaded by username from Arweave.";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();

            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Arweave is permanent — post a tombstone
                await _arweaveService.PostTransactionAsync(
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { deleted = true, id = id })),
                    "application/json",
                    new Dictionary<string, string>
                    {
                        { "OASIS-Type", "Avatar-Tombstone" },
                        { "OASIS-Id", id.ToString() }
                    });

                result.Result = true;
                result.IsDeleted = true;
                result.IsError = false;
                result.Message = "Avatar logically deleted in Arweave (tombstone written; original data is permanent).";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();

            try
            {
                if (string.IsNullOrWhiteSpace(providerKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Provider key (Arweave TxId) is required.");
                    return result;
                }

                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                await _arweaveService.PostTransactionAsync(
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { deleted = true, txId = providerKey })),
                    "application/json",
                    new Dictionary<string, string>
                    {
                        { "OASIS-Type", "Avatar-Tombstone" },
                        { "OASIS-Original-TxId", providerKey }
                    });

                result.Result = true;
                result.IsDeleted = true;
                result.IsError = false;
                result.Message = "Avatar logically deleted in Arweave (tombstone written; original data is permanent).";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();

            try
            {
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmail);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar with email {avatarEmail} not found in Arweave.");
                    return result;
                }

                if (avatarResult.Result.ProviderUniqueStorageKey != null &&
                    avatarResult.Result.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.ArweaveOASIS, out var providerKey) &&
                    !string.IsNullOrWhiteSpace(providerKey))
                    return await DeleteAvatarAsync(providerKey, softDelete);

                return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();

            try
            {
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar with username {avatarUsername} not found in Arweave.");
                    return result;
                }

                if (avatarResult.Result.ProviderUniqueStorageKey != null &&
                    avatarResult.Result.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.ArweaveOASIS, out var providerKey) &&
                    !string.IsNullOrWhiteSpace(providerKey))
                    return await DeleteAvatarAsync(providerKey, softDelete);

                return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from Arweave. Reason: {e}");
            }

            return result;
        }

        private static bool HolonMetaMatches(IHolon holon, string metaKey, string metaValue)
        {
            if (holon?.MetaData == null || string.IsNullOrWhiteSpace(metaKey)) return false;
            if (!holon.MetaData.TryGetValue(metaKey, out var val)) return false;
            if (metaValue == null) return val != null;
            return string.Equals(val?.ToString(), metaValue, StringComparison.OrdinalIgnoreCase);
        }
    }
}
