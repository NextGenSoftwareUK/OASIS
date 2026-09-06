using System;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS
{
    public partial class ArweaveOASIS
    {
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();

            try
            {
                var tags = new Dictionary<string, string>
                {
                    { "OASIS-Type", "AvatarDetail" },
                    { "OASIS-Id", avatarDetail.Id.ToString() },
                    { "OASIS-Username", avatarDetail.Username ?? "" },
                    { "OASIS-Email", avatarDetail.Email ?? "" }
                };

                var uploadResult = await UploadJsonToArweaveAsync(avatarDetail, $"AvatarDetail_{avatarDetail.Id}", tags);

                if (uploadResult.IsError || string.IsNullOrEmpty(uploadResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar detail to Arweave. Reason: {uploadResult.Message}");
                    return result;
                }

                avatarDetail.ProviderUniqueStorageKey[Core.Enums.ProviderType.ArweaveOASIS] = uploadResult.Result;
                result.Result = avatarDetail;
                result.Message = $"Avatar detail saved to Arweave permanently. TxId: {uploadResult.Result}";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                var tags = new Dictionary<string, string>
                {
                    { "OASIS-Type", "Holon" },
                    { "OASIS-Id", holon.Id.ToString() },
                    { "OASIS-HolonType", holon.HolonType.ToString() }
                };

                if (holon.ParentHolonId != Guid.Empty)
                    tags["OASIS-ParentId"] = holon.ParentHolonId.ToString();

                var uploadResult = await UploadJsonToArweaveAsync(holon, $"Holon_{holon.Id}", tags);

                if (uploadResult.IsError || string.IsNullOrEmpty(uploadResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holon to Arweave. Reason: {uploadResult.Message}");
                    return result;
                }

                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ArweaveOASIS] = uploadResult.Result;
                result.Result = holon;
                result.Message = $"Holon saved to Arweave permanently. TxId: {uploadResult.Result}";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                var saved = new List<IHolon>();
                var errors = new List<string>();

                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (saveResult.IsError)
                    {
                        errors.Add($"Failed to save holon {holon.Id}: {saveResult.Message}");
                        if (!continueOnError) break;
                    }
                    else
                    {
                        saved.Add(saveResult.Result);
                    }
                }

                result.Result = saved;

                if (errors.Any())
                {
                    result.Message = $"Saved {saved.Count} holons with {errors.Count} errors";
                    if (!continueOnError && errors.Any())
                        OASISErrorHandling.HandleError(ref result, string.Join("; ", errors));
                }
                else
                {
                    result.Message = $"All {saved.Count} holons saved successfully to Arweave";
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var txIds = await _arweaveService.QueryByTagsAsync(new Dictionary<string, string>
                {
                    { "OASIS-Type", "Holon" },
                    { "OASIS-Id", id.ToString() }
                });

                if (txIds == null || !txIds.Any())
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon with ID {id} not found in Arweave.");
                    return result;
                }

                var data = await _arweaveService.GetTransactionDataAsync(txIds.First());
                var holon = JsonConvert.DeserializeObject<Holon>(Encoding.UTF8.GetString(data));
                result.Result = holon;
                result.IsError = false;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                if (string.IsNullOrEmpty(providerKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Provider key (Arweave TxId) cannot be null or empty.");
                    return result;
                }

                var data = await _arweaveService.GetTransactionDataAsync(providerKey);

                if (data == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to retrieve holon from Arweave for TxId: {providerKey}");
                    return result;
                }

                var holon = JsonConvert.DeserializeObject<Holon>(Encoding.UTF8.GetString(data));
                result.Result = holon;
                result.IsError = false;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            // Arweave data is permanent — deletion is not possible on-chain.
            // We record a tombstone transaction to mark it as deleted logically.
            var result = new OASISResult<IHolon>();

            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var txIds = await _arweaveService.QueryByTagsAsync(new Dictionary<string, string>
                {
                    { "OASIS-Type", "Holon" },
                    { "OASIS-Id", id.ToString() }
                });

                if (txIds == null || !txIds.Any())
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon with ID {id} not found in Arweave.");
                    return result;
                }

                var data = await _arweaveService.GetTransactionDataAsync(txIds.First());
                IHolon holon = null;
                if (data != null)
                    holon = JsonConvert.DeserializeObject<Holon>(Encoding.UTF8.GetString(data));

                // Post a tombstone so queries know the holon is deleted
                await _arweaveService.PostTransactionAsync(
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { deleted = true, id = id })),
                    "application/json",
                    new Dictionary<string, string>
                    {
                        { "OASIS-Type", "Holon-Tombstone" },
                        { "OASIS-Id", id.ToString() }
                    });

                result.Result = holon;
                result.IsDeleted = true;
                result.IsError = false;
                result.Message = "Holon logically deleted in Arweave (tombstone written; original data is permanent).";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();

            try
            {
                if (string.IsNullOrWhiteSpace(providerKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Provider key (Arweave TxId) is required.");
                    return result;
                }

                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                IHolon holon = null;
                try
                {
                    var data = await _arweaveService.GetTransactionDataAsync(providerKey);
                    if (data != null)
                        holon = JsonConvert.DeserializeObject<Holon>(Encoding.UTF8.GetString(data));
                }
                catch { /* best effort */ }

                await _arweaveService.PostTransactionAsync(
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { deleted = true, txId = providerKey })),
                    "application/json",
                    new Dictionary<string, string>
                    {
                        { "OASIS-Type", "Holon-Tombstone" },
                        { "OASIS-Original-TxId", providerKey }
                    });

                result.Result = holon;
                result.IsDeleted = true;
                result.IsError = false;
                result.Message = "Holon logically deleted in Arweave (tombstone written; original data is permanent).";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                var saveResult = await SaveHolonsAsync(holons);

                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to import holons to Arweave. Reason: {saveResult.Message}");
                    return result;
                }

                result.Result = true;
                result.Message = $"Successfully imported {saveResult.Result.Count()} holons to Arweave";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var allResult = await LoadAllHolonsAsync();
                if (allResult.IsError || allResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons from Arweave: {allResult.Message}");
                    return result;
                }

                var filtered = allResult.Result
                    .Where(h => HolonMatchesAvatarId(h, avatarId))
                    .ToList();

                result.Result = filtered;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Exported {filtered.Count} holons for avatar {avatarId} from Arweave.";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar with username {avatarUsername} not found in Arweave.");
                    return result;
                }

                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar username from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar with email {avatarEmailAddress} not found in Arweave.");
                    return result;
                }

                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar email from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            return await LoadAllHolonsAsync();
        }

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars from Arweave: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Arweave. Reason: {e}");
            }

            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                var holonsResult = LoadAllHolons(type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons from Arweave: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Arweave. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();

            try
            {
                if (searchParams == null)
                {
                    OASISErrorHandling.HandleError(ref result, "SearchParams cannot be null");
                    return result;
                }

                var tags = new Dictionary<string, string> { { "App-Name", "OASIS" } };

                var txIds = await _arweaveService.QueryByTagsAsync(tags);
                var foundHolons = new List<IHolon>();

                foreach (var txId in txIds ?? new List<string>())
                {
                    try
                    {
                        var data = await _arweaveService.GetTransactionDataAsync(txId);
                        if (data == null) continue;

                        var holon = JsonConvert.DeserializeObject<Holon>(Encoding.UTF8.GetString(data));

                        if (holon != null && MatchesSearchCriteria(holon, searchParams))
                            foundHolons.Add(holon);
                    }
                    catch
                    {
                        if (!continueOnError) throw;
                    }
                }

                var searchResults = new SearchResults { SearchResultHolons = foundHolons };
                result.Result = searchResults;
                result.IsError = false;
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SearchAsync in ArweaveOASIS. Reason: {e}");
            }

            return result;
        }

        private bool MatchesSearchCriteria(IHolon holon, ISearchParams searchParams)
        {
            if (holon == null || searchParams == null) return false;

            if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
            {
                var g = searchParams.SearchGroups.First();

                if (g is ISearchTextGroup textGroup && !string.IsNullOrWhiteSpace(textGroup.SearchQuery))
                {
                    var q = textGroup.SearchQuery.ToLower();
                    if (!((holon.Name ?? "").ToLower().Contains(q) || (holon.Description ?? "").ToLower().Contains(q)))
                        return false;
                }

                if (g.HolonType != HolonType.All && holon.HolonType != g.HolonType)
                    return false;
            }

            return true;
        }

        private static bool HolonMatchesAvatarId(IHolon holon, Guid avatarId)
        {
            if (holon == null) return false;

            try
            {
                var prop = holon.GetType().GetProperty("CreatedByAvatarId");
                if (prop != null && prop.PropertyType == typeof(Guid))
                    return (Guid)prop.GetValue(holon) == avatarId;
            }
            catch { }

            if (holon.MetaData != null &&
                holon.MetaData.TryGetValue("CreatedByAvatarId", out var val) &&
                Guid.TryParse(val?.ToString(), out var parsed))
                return parsed == avatarId;

            return false;
        }
    }
}
