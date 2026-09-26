using System;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.PinataOASIS
{
    public partial class PinataOASIS
    {
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            string errorMessage = "Error in LoadHolonsForParentAsync method in PinataOASIS Provider. Reason: ";

            try
            {
                if (id == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Parent ID cannot be empty");
                    return result;
                }

                var childHolons = new List<IHolon>();
                // Real Pinata implementation: Load child holons from Pinata files
                var files = await _pinataService.GetFilesAsync();
                
                foreach (var file in files)
                {
                    try
                    {
                        // Real Pinata implementation: Get file content from Pinata IPFS
                        var content = await _pinataService.GetFileContentAsync(file.IpfsPinHash);
                        var holon = JsonConvert.DeserializeObject<Holon>(content);
                        
                        if (holon != null && holon.ParentHolonId == id && 
                            (type == HolonType.All || holon.HolonType == type))
                        {
                            childHolons.Add(holon);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (continueOnError)
                        {
                            LoggingManager.Log($"Error processing Pinata file: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                            continue;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                result.Result = childHolons;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
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

                var parentHolonResult = await LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
                if (parentHolonResult.IsError || parentHolonResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Parent holon not found in Pinata: {parentHolonResult.Message}");
                    return result;
                }

                return await LoadHolonsForParentAsync(parentHolonResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Pinata: {ex.Message}", ex);
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

                var holonsResult = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons from Pinata: {holonsResult.Message}");
                    return result;
                }

                var filtered = holonsResult.Result.Where(h => HolonMetaMatches(h, metaKey, metaValue)).ToList();
                result.Result = filtered;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Found {filtered.Count} holons matching metadata '{metaKey}'='{metaValue}'.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Pinata: {ex.Message}", ex);
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

                var holonsResult = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons from Pinata: {holonsResult.Message}");
                    return result;
                }

                bool Matches(IHolon h)
                {
                    if (h?.MetaData == null) return false;
                    if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                        return metaKeyValuePairs.All(kvp => HolonMetaMatches(h, kvp.Key, kvp.Value));
                    return metaKeyValuePairs.Any(kvp => HolonMetaMatches(h, kvp.Key, kvp.Value));
                }

                var filtered = holonsResult.Result.Where(Matches).ToList();
                result.Result = filtered;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Found {filtered.Count} holons matching metadata pairs.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from Pinata: {ex.Message}", ex);
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
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                var pins = await _pinataService.GetFilesAsync();
                var holons = new List<IHolon>();

                foreach (var pin in pins)
                {
                    if (string.IsNullOrWhiteSpace(pin?.IpfsPinHash)) continue;
                    try
                    {
                        var content = await _pinataService.GetFileContentAsync(pin.IpfsPinHash);
                        if (string.IsNullOrWhiteSpace(content)) continue;

                        var holon = JsonConvert.DeserializeObject<Holon>(content);
                        if (holon == null) continue;

                        if (type != HolonType.All && holon.HolonType != type) continue;
                        if (version > 0 && holon.Version != version) continue;

                        holons.Add(holon);
                    }
                    catch
                    {
                        // ignore non-holon pins
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Loaded {holons.Count} holons from Pinata.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from Pinata: {ex.Message}", ex);
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
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                var pins = await _pinataService.GetFilesAsync();
                var avatarDetails = new List<IAvatarDetail>();

                foreach (var pin in pins)
                {
                    if (string.IsNullOrWhiteSpace(pin?.IpfsPinHash)) continue;
                    try
                    {
                        var content = await _pinataService.GetFileContentAsync(pin.IpfsPinHash);
                        if (string.IsNullOrWhiteSpace(content)) continue;

                        var avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(content);
                        if (avatarDetail == null) continue;
                        if (version > 0 && avatarDetail.Version != version) continue;

                        avatarDetails.Add(avatarDetail);
                    }
                    catch
                    {
                        // ignore non-avatar-detail pins
                    }
                }

                result.Result = avatarDetails;
                result.IsLoaded = true;
                result.IsError = false;
                result.Message = $"Loaded {avatarDetails.Count} avatar details from Pinata.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Pinata: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Pinata: {allResult.Message}");
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
                result.Message = "Avatar detail loaded successfully by email from Pinata.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Pinata: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Pinata: {allResult.Message}");
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
                result.Message = "Avatar detail loaded successfully by username from Pinata.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Pinata: {ex.Message}", ex);
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

        private static bool HolonMatchesAvatarId(IHolon holon, Guid avatarId)
        {
            if (holon == null) return false;

            // Prefer strongly-typed property if present
            try
            {
                var prop = holon.GetType().GetProperty("CreatedByAvatarId");
                if (prop != null && prop.PropertyType == typeof(Guid))
                {
                    var value = (Guid)prop.GetValue(holon);
                    return value == avatarId;
                }
            }
            catch { }

            // Fallback: metadata
            if (holon.MetaData != null &&
                holon.MetaData.TryGetValue("CreatedByAvatarId", out var val) &&
                Guid.TryParse(val?.ToString(), out var parsed))
            {
                return parsed == avatarId;
            }

            return false;
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
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                // Find avatar pin by scanning pinned avatar JSONs (reliable across Pinata query syntax changes)
                var pins = await _pinataService.GetFilesAsync();
                foreach (var pin in pins)
                {
                    if (string.IsNullOrWhiteSpace(pin?.IpfsPinHash)) continue;

                    try
                    {
                        var content = await _pinataService.GetFileContentAsync(pin.IpfsPinHash);
                        if (string.IsNullOrWhiteSpace(content)) continue;

                        var avatar = JsonConvert.DeserializeObject<Avatar>(content);
                        if (avatar != null && avatar.Id == id)
                        {
                            var unpinOk = await _pinataService.UnpinFileAsync(pin.IpfsPinHash);
                            if (!unpinOk)
                            {
                                OASISErrorHandling.HandleError(ref result, $"Failed to unpin avatar {id} from Pinata.");
                                return result;
                            }

                            result.Result = true;
                            result.IsDeleted = true;
                            result.DeletedCount = 1;
                            result.IsError = false;
                            result.Message = "Avatar unpinned (deleted) from Pinata successfully.";
                            return result;
                        }
                    }
                    catch
                    {
                        // ignore non-avatar pins
                    }
                }

                OASISErrorHandling.HandleError(ref result, $"Avatar with ID {id} not found in Pinata.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Pinata: {ex.Message}", ex);
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
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                if (string.IsNullOrWhiteSpace(providerKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Provider key (IPFS hash) is required.");
                    return result;
                }

                var unpinOk = await _pinataService.UnpinFileAsync(providerKey);
                if (!unpinOk)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unpin avatar {providerKey} from Pinata.");
                    return result;
                }

                result.Result = true;
                result.IsDeleted = true;
                result.DeletedCount = 1;
                result.IsError = false;
                result.Message = "Avatar unpinned (deleted) from Pinata successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Pinata: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Avatar with email {avatarEmail} not found in Pinata.");
                    return result;
                }

                if (avatarResult.Result.ProviderUniqueStorageKey != null &&
                    avatarResult.Result.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.PinataOASIS, out var providerKey) &&
                    !string.IsNullOrWhiteSpace(providerKey))
                {
                    return await DeleteAvatarAsync(providerKey, softDelete);
                }

                // Fall back to scanning pins by content if provider key missing
                return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from Pinata: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Avatar with username {avatarUsername} not found in Pinata.");
                    return result;
                }

                if (avatarResult.Result.ProviderUniqueStorageKey != null &&
                    avatarResult.Result.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.PinataOASIS, out var providerKey) &&
                    !string.IsNullOrWhiteSpace(providerKey))
                {
                    return await DeleteAvatarAsync(providerKey, softDelete);
                }

                return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from Pinata: {ex.Message}", ex);
            }
            return result;
        }


        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                // Export = enumerate pins and return all holons we can deserialize.
                var pins = await _pinataService.GetFilesAsync();
                var holons = new List<IHolon>();

                foreach (var pin in pins)
                {
                    if (string.IsNullOrWhiteSpace(pin?.IpfsPinHash)) continue;
                    try
                    {
                        var content = await _pinataService.GetFileContentAsync(pin.IpfsPinHash);
                        if (string.IsNullOrWhiteSpace(content)) continue;

                        var holon = JsonConvert.DeserializeObject<Holon>(content);
                        if (holon != null)
                            holons.Add(holon);
                    }
                    catch
                    {
                        // ignore non-holon pins
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Exported {holons.Count} holons from Pinata.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting holons from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        private async Task EnsureActivatedForPinataAsync<T>(OASISResult<T> result)
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate PinataOASIS Provider: {activateResult.Message}");
            }
        }

        private async Task<OASISResult<(string PinHash, IHolon Holon)>> FindPinnedHolonByIdAsync(Guid id)
        {
            var result = new OASISResult<(string PinHash, IHolon Holon)>();
            try
            {
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                var pins = await _pinataService.GetFilesAsync();
                foreach (var pin in pins)
                {
                    if (string.IsNullOrWhiteSpace(pin?.IpfsPinHash)) continue;

                    try
                    {
                        var content = await _pinataService.GetFileContentAsync(pin.IpfsPinHash);
                        if (string.IsNullOrWhiteSpace(content)) continue;

                        var holon = JsonConvert.DeserializeObject<Holon>(content);
                        if (holon != null && holon.Id == id)
                        {
                            result.Result = (pin.IpfsPinHash, holon);
                            result.IsError = false;
                            return result;
                        }
                    }
                    catch
                    {
                        // ignore non-holon pins
                    }
                }

                OASISErrorHandling.HandleError(ref result, $"Holon with ID {id} not found in Pinata.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching holon pins in Pinata: {ex.Message}", ex);
            }
            return result;
        }
    }

    // Helper classes for Pinata API responses
    public class PinataFileResponse
    {
        [JsonProperty("IpfsHash")]
        public string IpfsHash { get; set; }

        [JsonProperty("PinSize")]
        public int PinSize { get; set; }

        [JsonProperty("Timestamp")]
        public string Timestamp { get; set; }
    }

    public class PinataPinResponse
    {
        public string IpfsHash { get; set; }
        public int PinSize { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsDuplicate { get; set; }
    }

    public interface IPinataService
    {
        Task<List<PinataPin>> GetFilesAsync();
        Task<string> GetFileContentAsync(string hash);
        Task<PinataPinResponse> PinFileAsync(string filePath);
        Task<PinataPinResponse> PinJsonAsync(object jsonObject);
        Task<bool> UnpinFileAsync(string hash);
    }

    public class PinataService : IPinataService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _secretKey;
        private readonly string _jwt;

        public PinataService(string apiKey, string secretKey, string jwt = null)
        {
            _apiKey = apiKey;
            _secretKey = secretKey;
            _jwt = jwt;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("pinata_api_key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("pinata_secret_api_key", _secretKey);
        }

        public async Task<List<PinataPin>> GetFilesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://api.pinata.cloud/data/pinList");
                var content = await response.Content.ReadAsStringAsync();
                var pinList = JsonConvert.DeserializeObject<PinataPinListResponse>(content);
                return pinList?.Rows ?? new List<PinataPin>();
            }
            catch
            {
                return new List<PinataPin>();
            }
        }

        public async Task<string> GetFileContentAsync(string hash)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://gateway.pinata.cloud/ipfs/{hash}");
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<PinataPinResponse> PinFileAsync(string filePath)
        {
            try
            {
                var formData = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                formData.Add(fileContent, "file", System.IO.Path.GetFileName(filePath));

                var response = await _httpClient.PostAsync("https://api.pinata.cloud/pinning/pinFileToIPFS", formData);
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PinataPinResponse>(content);
            }
            catch
            {
                return new PinataPinResponse();
            }
        }

        public async Task<PinataPinResponse> PinJsonAsync(object jsonObject)
        {
            try
            {
                var json = JsonConvert.SerializeObject(jsonObject);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://api.pinata.cloud/pinning/pinJSONToIPFS", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PinataPinResponse>(responseContent);
            }
            catch
            {
                return new PinataPinResponse();
            }
        }

        public async Task<bool> UnpinFileAsync(string hash)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"https://api.pinata.cloud/pinning/unpin/{hash}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
