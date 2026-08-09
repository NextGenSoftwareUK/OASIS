//using System.Text.Json;
//using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS
{
    public partial class LocalFileOASIS
    {
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonFilePath = Path.Combine(_holonDirectory, $"{id}.json");
                if (File.Exists(holonFilePath))
                {
                    // Load holon first to return it
                    var loadResult = await LoadHolonAsync(id, false, false, 0, true, false, 0);
                    if (!loadResult.IsError && loadResult.Result != null)
                    {
                        // Delete the file
                        File.Delete(holonFilePath);
                        
                        result.Result = loadResult.Result;
                        result.IsError = false;
                        result.IsSaved = true;
                        result.Message = "Holon deleted successfully";
                    }
                    else
                    {
                        // File exists but couldn't load it, delete anyway
                        File.Delete(holonFilePath);
                        result.IsError = false;
                        result.Message = "Holon file deleted (but could not be loaded)";
                    }
                }
                else
                {
                    result.IsError = false;
                    result.Message = "Holon file not found";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            // Load holon by provider key first
            var loadResult = await LoadHolonAsync(providerKey, false, false, 0, true, false, 0);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IHolon>();
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key: {loadResult.Message}");
                return result;
            }

            // Delete using the loaded holon's ID
            return await DeleteHolonAsync(loadResult.Result.Id);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                    return result;
                }

                // Ensure holon directory exists
                if (!Directory.Exists(_holonDirectory))
                    Directory.CreateDirectory(_holonDirectory);

                int importedCount = 0;
                foreach (var holon in holons)
                {
                    try
                    {
                        var saveResult = await SaveHolonAsync(holon, true, true, 10, true, false);
                        if (!saveResult.IsError)
                        {
                            importedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingManager.Log($"Error importing holon {holon.Id}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                    }
                }

                result.Result = true;
                result.IsError = false;
                result.IsSaved = true;
                result.Message = $"Imported {importedCount} holons successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar
                var avatarResult = await LoadAvatarAsync(avatarId, version);
                var allData = new List<IHolon>();

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    allData.Add(avatarResult.Result as IHolon);
                }

                // Load all holons for this avatar (as parent)
                var holonsResult = await LoadHolonsForParentAsync(avatarId, HolonType.All, true, true, 10, 0, true, false, version);
                if (!holonsResult.IsError && holonsResult.Result != null)
                {
                    allData.AddRange(holonsResult.Result);
                }

                result.Result = allData;
                result.IsError = false;
                result.Message = $"Exported {allData.Count} holons for avatar";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            // Load avatar by username first
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                return result;
            }

            // Export using the loaded avatar's ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            // Load avatar by email first
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                return result;
            }

            // Export using the loaded avatar's ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load all avatars and holons
                var avatarsResult = await LoadAllAvatarsAsync(version);
                var holonsResult = await LoadAllHolonsAsync(HolonType.All, false, false, 0, 0, true, false, version);

                var allData = new List<IHolon>();
                if (!avatarsResult.IsError && avatarsResult.Result != null)
                {
                    allData.AddRange(avatarsResult.Result.Cast<IHolon>());
                }
                if (!holonsResult.IsError && holonsResult.Result != null)
                {
                    allData.AddRange(holonsResult.Result);
                }

                result.Result = allData;
                result.IsError = false;
                result.Message = $"Exported {allData.Count} holons";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (searchParams == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Search parameters cannot be null");
                    return result;
                }

                var searchResults = new SearchResults
                {
                    SearchResultAvatars = new List<IAvatar>(),
                    SearchResultHolons = new List<IHolon>()
                };

                // Process search groups
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    foreach (var searchGroup in searchParams.SearchGroups)
                    {
                        // Search holons if requested
                        if (searchGroup.SearchHolons && searchGroup.HolonSearchParams != null)
                        {
                            var holonsToSearch = new List<IHolon>();

                            // Load holons based on parent ID if specified
                            if (searchParams.ParentId != Guid.Empty)
                            {
                                var parentHolonsResult = await LoadHolonsForParentAsync(
                                    searchParams.ParentId, 
                                    searchGroup.HolonType, 
                                    loadChildren, 
                                    searchParams.Recursive, 
                                    maxChildDepth, 
                                    0, 
                                    continueOnError, 
                                    false, 
                                    version);
                                if (!parentHolonsResult.IsError && parentHolonsResult.Result != null)
                                {
                                    holonsToSearch.AddRange(parentHolonsResult.Result);
                                }
                            }
                            else
                            {
                                // Load all holons of the specified type
                                var allHolonsResult = await LoadAllHolonsAsync(
                                    searchGroup.HolonType, 
                                    loadChildren, 
                                    recursive, 
                                    maxChildDepth, 
                                    0, 
                                    continueOnError, 
                                    false, 
                                    version);
                                if (!allHolonsResult.IsError && allHolonsResult.Result != null)
                                {
                                    holonsToSearch.AddRange(allHolonsResult.Result);
                                }
                            }

                            // Filter holons based on search criteria
                            foreach (var holon in holonsToSearch)
                            {
                                bool matches = true;

                                // Filter by avatar ID if specified
                                if (searchParams.SearchOnlyForCurrentAvatar && searchParams.AvatarId != Guid.Empty)
                                {
                                    if (holon.CreatedByAvatarId != searchParams.AvatarId)
                                    {
                                        matches = false;
                                    }
                                }

                                if (matches)
                                {
                                    searchResults.SearchResultHolons.Add(holon);
                                }
                            }
                        }

                        // Search avatars if requested
                        if (searchGroup.SearchAvatars && searchGroup.AvatarSearchParams != null)
                        {
                            var avatarsToSearch = new List<IAvatar>();
                            
                            // Load all avatars
                            var allAvatarsResult = await LoadAllAvatarsAsync(version);
                            if (!allAvatarsResult.IsError && allAvatarsResult.Result != null)
                            {
                                avatarsToSearch.AddRange(allAvatarsResult.Result);
                            }

                            // Filter avatars based on search criteria
                            foreach (var avatar in avatarsToSearch)
                            {
                                bool matches = true;

                                // Filter by avatar ID if specified
                                if (searchParams.SearchOnlyForCurrentAvatar && searchParams.AvatarId != Guid.Empty)
                                {
                                    if (avatar.Id != searchParams.AvatarId)
                                    {
                                        matches = false;
                                    }
                                }

                                if (matches)
                                {
                                    searchResults.SearchResultAvatars.Add(avatar);
                                }
                            }
                        }
                    }
                }

                searchResults.NumberOfResults = searchResults.SearchResultAvatars.Count + searchResults.SearchResultHolons.Count;
                searchResults.NumberOfDuplicates = 0; // LocalFile doesn't track duplicates

                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Found {searchResults.NumberOfResults} matching results";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error performing search: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        //public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();

        //    try
        //    {
        //        using FileStream createStream = File.Create(GetWalletFilePath(id));
        //        await JsonSerializer.SerializeAsync<object>(createStream, providerWallets);
        //        await createStream.DisposeAsync();
        //        result.Result = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in SaveProviderWalletsAsync method in LocalFileOASIS Provider saving wallets. Reason: {ex.Message}", ex);
        //    }

        //    return result;
        //}

    }
}
