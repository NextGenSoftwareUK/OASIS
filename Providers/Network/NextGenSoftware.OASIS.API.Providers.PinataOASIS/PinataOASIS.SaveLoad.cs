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
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            
            try
            {
                // Upload avatar detail data to Pinata
                var uploadResult = await UploadJsonToPinataAsync(avatarDetail, $"AvatarDetail_{avatarDetail.Id}");
                
                if (uploadResult.IsError || string.IsNullOrEmpty(uploadResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar detail to Pinata. Reason: {uploadResult.Message}");
                    return result;
                }

                // Store the IPFS hash as the provider key
                avatarDetail.ProviderUniqueStorageKey[Core.Enums.ProviderType.PinataOASIS] = uploadResult.Result;
                
                result.Result = avatarDetail;
                result.Message = "Avatar detail saved to Pinata successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to Pinata. Reason: {e}");
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
                // Upload holon data to Pinata
                var uploadResult = await UploadJsonToPinataAsync(holon, $"Holon_{holon.Id}");
                
                if (uploadResult.IsError || string.IsNullOrEmpty(uploadResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holon to Pinata. Reason: {uploadResult.Message}");
                    return result;
                }

                // Store the IPFS hash as the provider key
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.PinataOASIS] = uploadResult.Result;
                
                result.Result = holon;
                result.Message = "Holon saved to Pinata successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to Pinata. Reason: {e}");
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
                var savedHolons = new List<IHolon>();
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
                        savedHolons.Add(saveResult.Result);
                    }
                }

                result.Result = savedHolons;
                
                if (errors.Any())
                {
                    result.Message = $"Saved {savedHolons.Count} holons with {errors.Count} errors";
                    if (!continueOnError && errors.Any())
                    {
                        OASISErrorHandling.HandleError(ref result, string.Join("; ", errors));
                    }
                }
                else
                {
                    result.Message = $"All {savedHolons.Count} holons saved successfully";
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to Pinata. Reason: {e}");
            }

            return result;
        }


        //public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, int version = 0)
        //{
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();
            
        //    try
        //    {
        //        // Download holon data from Pinata using providerKey as IPFS hash
        //        var downloadResult = await DownloadFileFromPinataAsync(providerKey);
                
        //        if (downloadResult.IsError || downloadResult.Result == null)
        //        {
        //            OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Pinata. Reason: {downloadResult.Message}");
        //            return result;
        //        }

        //        // Deserialize holon data
        //        var holonJson = Encoding.UTF8.GetString(downloadResult.Result);
        //        var holon = JsonConvert.DeserializeObject<Holon>(holonJson);
                
        //        result.Result = holon;
        //        result.Message = "Holon loaded from Pinata successfully";
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error loading holon from Pinata. Reason: {e}");
        //    }

        //    return result;
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, string propertyName, int version = 0)
        //{
        //    OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
        //    OASISErrorHandling.HandleError(ref result, "LoadHolonsForParent not implemented for PinataOASIS - Pinata is primarily for file storage");
        //    return result;
        //}

        //public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, string propertyName, int version = 0)
        //{
        //    OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
        //    OASISErrorHandling.HandleError(ref result, "LoadHolonsForParentAsync not implemented for PinataOASIS - Pinata is primarily for file storage");
        //    return result;
        //}


        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                var lookup = await FindPinnedHolonByIdAsync(id);
                if (lookup.IsError)
                {
                    result.IsError = lookup.IsError;
                    result.Message = lookup.Message;
                    result.Exception = lookup.Exception;
                    return result;
                }

                if (string.IsNullOrWhiteSpace(lookup.Result.PinHash) || lookup.Result.Holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon with ID {id} not found in Pinata.");
                    return result;
                }

                var unpinOk = await _pinataService.UnpinFileAsync(lookup.Result.PinHash);
                if (!unpinOk)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unpin holon {id} from Pinata.");
                    return result;
                }

                result.Result = lookup.Result.Holon;
                result.IsDeleted = true;
                result.DeletedCount = 1;
                result.IsError = false;
                result.Message = "Holon unpinned (deleted) from Pinata successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Pinata: {ex.Message}", ex);
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
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                if (string.IsNullOrWhiteSpace(providerKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Provider key (IPFS hash) is required.");
                    return result;
                }

                // Capture current content before unpinning (best-effort)
                IHolon holon = null;
                try
                {
                    var content = await _pinataService.GetFileContentAsync(providerKey);
                    if (!string.IsNullOrWhiteSpace(content))
                        holon = JsonConvert.DeserializeObject<Holon>(content);
                }
                catch { /* best effort */ }

                var unpinOk = await _pinataService.UnpinFileAsync(providerKey);
                if (!unpinOk)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unpin holon {providerKey} from Pinata.");
                    return result;
                }

                result.Result = holon;
                result.IsDeleted = true;
                result.DeletedCount = 1;
                result.IsError = false;
                result.Message = "Holon unpinned (deleted) from Pinata successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Pinata: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to import holons to Pinata. Reason: {saveResult.Message}");
                    return result;
                }

                result.Result = true;
                result.Message = $"Successfully imported {saveResult.Result.Count()} holons to Pinata";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Pinata. Reason: {e}");
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
                await EnsureActivatedForPinataAsync(result);
                if (result.IsError) return result;

                var holonsResult = await LoadAllHolonsAsync();
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons from Pinata: {holonsResult.Message}");
                    return result;
                }

                var filtered = holonsResult.Result.Where(h => HolonMatchesAvatarId(h, avatarId)).ToList();
                result.Result = filtered;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Exported {filtered.Count} holons for avatar {avatarId} from Pinata.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar from Pinata: {ex.Message}", ex);
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
                if (string.IsNullOrWhiteSpace(avatarUsername))
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar username is required.");
                    return result;
                }

                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar with username {avatarUsername} not found in Pinata: {avatarResult.Message}");
                    return result;
                }

                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar username from Pinata: {ex.Message}", ex);
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
                if (string.IsNullOrWhiteSpace(avatarEmailAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar email is required.");
                    return result;
                }

                var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar with email {avatarEmailAddress} not found in Pinata: {avatarResult.Message}");
                    return result;
                }

                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar email from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        // IOASISNETProvider implementation
        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Pinata provider: {activateResult.Message}");
                        return result;
                    }
                }

                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
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
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Pinata: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Pinata provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
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
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Pinata: {ex.Message}", ex);
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
            string errorMessage = "Error in SearchAsync method in PinataOASIS Provider. Reason: ";

            try
            {
                if (searchParams == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} SearchParams cannot be null");
                    return result;
                }

                var searchResults = new SearchResults();
                var foundHolons = new List<IHolon>();

                // Real Pinata implementation: Search through Pinata files using metadata
                var files = await _pinataService.GetFilesAsync();
                
                foreach (var file in files)
                {
                    try
                    {
                        // Real Pinata implementation: Download and parse the file content
                        var content = await _pinataService.GetFileContentAsync(file.IpfsPinHash);
                        var holon = JsonConvert.DeserializeObject<Holon>(content);
                        
                        if (holon != null && MatchesSearchCriteria(holon, searchParams))
                        {
                            foundHolons.Add(holon);
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

                searchResults.SearchResultHolons = foundHolons.ToList();
                result.Result = searchResults;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        private bool MatchesSearchCriteria(IHolon holon, ISearchParams searchParams)
        {
            if (holon == null || searchParams == null)
                return false;

            if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
            {
                var firstGroup = searchParams.SearchGroups.First();
                if (firstGroup is ISearchTextGroup textGroup && !string.IsNullOrWhiteSpace(textGroup.SearchQuery))
                {
                    var q = textGroup.SearchQuery.ToLower();
                    if (!((holon.Name ?? string.Empty).ToLower().Contains(q) || (holon.Description ?? string.Empty).ToLower().Contains(q)))
                        return false;
                }
            }

            if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
            {
                var g = searchParams.SearchGroups.First();
                if (g.HolonType != HolonType.All && holon.HolonType != g.HolonType)
                    return false;
            }

            if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
            {
                var g = searchParams.SearchGroups.First();
                // MetaData is a boolean flag indicating whether to search metadata
                if (g.HolonSearchParams?.MetaData == true && holon.MetaData != null)
                {
                    // MetaData search enabled - metadata exists on the holon
                    // Additional metadata filtering logic would go here if needed
                }
            }

            return true;
        }

        // Missing abstract method implementations
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in LoadHolonAsync method in PinataOASIS Provider. Reason: ";

            try
            {
                if (id == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Holon ID cannot be empty");
                    return result;
                }

                // Search for the holon file in Pinata by ID
                // Real Pinata implementation: Load child holons from Pinata files
                var files = await _pinataService.GetFilesAsync();
                var holonFile = files.FirstOrDefault(f => f.ToString().Contains(id.ToString()));
                
                if (holonFile == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Holon with ID {id} not found in Pinata");
                    return result;
                }

                // Download and deserialize the holon
                var content = await _pinataService.GetFileContentAsync(holonFile.IpfsPinHash);
                if (string.IsNullOrEmpty(content))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to retrieve holon content from Pinata");
                    return result;
                }
                var holon = JsonConvert.DeserializeObject<Holon>(content);
                
                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to deserialize holon {id}");
                    return result;
                }

                result.Result = holon;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in LoadHolonAsync method in PinataOASIS Provider. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(providerKey))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Provider key cannot be null or empty");
                    return result;
                }

                // Use providerKey as IPFS hash to directly fetch the file
                var content = await _pinataService.GetFileContentAsync(providerKey);
                if (string.IsNullOrEmpty(content))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to retrieve holon content from Pinata using provider key");
                    return result;
                }
                var holon = JsonConvert.DeserializeObject<Holon>(content);
                
                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to deserialize holon with provider key {providerKey}");
                    return result;
                }

                result.Result = holon;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

    }
}
