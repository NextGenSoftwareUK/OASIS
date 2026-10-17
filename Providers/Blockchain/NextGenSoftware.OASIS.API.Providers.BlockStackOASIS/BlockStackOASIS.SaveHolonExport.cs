using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NBitcoin;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS
{
    public partial class BlockStackOASIS
    {
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                    return result;
                }

                // Serialize holon to JSON
                var holonJson = JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // Convert to dictionary for BlockStack storage
                var holonData = JsonSerializer.Deserialize<Dictionary<string, object>>(holonJson);
                if (holonData == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to serialize holon");
                    return result;
                }

                // Add metadata
                holonData["savedAt"] = DateTime.UtcNow.ToString("O");
                holonData["provider"] = "BlockStackOASIS";
                holonData["holonId"] = holon.Id.ToString();

                // Save to BlockStack Gaia storage
                // Use holon ID or name as file path
                var filePath = string.IsNullOrEmpty(holon.Name) 
                    ? $"holons/{holon.Id}.json" 
                    : $"holons/{holon.Name.Replace(" ", "_")}_{holon.Id}.json";

                await _blockStackClient.PutFileAsync(filePath, holonData);

                // Store file path in provider unique storage key
                if (holon.ProviderUniqueStorageKey == null)
                    holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.BlockStackOASIS] = filePath;

                result.Result = holon;
                result.IsError = false;
                result.IsSaved = true;
                result.Message = $"Holon saved successfully to BlockStack Gaia storage: {filePath}";

                // Handle children if requested
                if (saveChildren && holon.Children != null && holon.Children.Any())
                {
                    var childResults = new List<OASISResult<IHolon>>();
                    foreach (var child in holon.Children)
                    {
                        var childResult = await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth - 1, continueOnError, saveChildrenOnProvider);
                        childResults.Add(childResult);
                        
                        if (!continueOnError && childResult.IsError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Failed to save child holon {child.Id}: {childResult.Message}");
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                    return result;
                }

                var savedHolons = new List<IHolon>();
                var errors = new List<string>();

                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    
                    if (saveResult.IsError)
                    {
                        errors.Add($"Failed to save holon {holon.Id}: {saveResult.Message}");
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, string.Join("; ", errors));
                            return result;
                        }
                    }
                    else if (saveResult.Result != null)
                    {
                        savedHolons.Add(saveResult.Result);
                    }
                }

                result.Result = savedHolons;
                result.IsError = errors.Any();
                result.Message = errors.Any() ? string.Join("; ", errors) : $"Successfully saved {savedHolons.Count} holons to BlockStack Gaia storage";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon first to return it
                var loadResult = await LoadHolonAsync(id, false, false, 0, false, false, 0);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon with ID {id} not found");
                    return result;
                }

                // Delete holon from BlockStack Gaia storage
                var filePath = $"holons/{id}.json";
                var deleteResult = await _blockStackClient.DeleteFileAsync(filePath);
                
                if (deleteResult)
                {
                    // Update index.json to remove holon ID
                    var indexData = await _blockStackClient.GetFileAsync("holons/index.json");
                    if (indexData != null && indexData.ContainsKey("holons"))
                    {
                        var holonIds = indexData["holons"] as List<object>;
                        if (holonIds != null)
                        {
                            holonIds.Remove(id.ToString());
                            indexData["holons"] = holonIds;
                            await _blockStackClient.PutFileAsync("holons/index.json", indexData);
                        }
                    }

                    result.Result = loadResult.Result;
                    result.IsError = false;
                    result.Message = "Holon deleted successfully from BlockStack Gaia storage";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to delete holon from BlockStack Gaia storage");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            // First load the holon to get its ID, then delete
            var loadResult = await LoadHolonAsync(providerKey, false, false, 0, false, false, 0);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IHolon>();
                OASISErrorHandling.HandleError(ref result, $"Holon with provider key {providerKey} not found");
                return result;
            }

            // Delete using the holon's ID
            return await DeleteHolonAsync(loadResult.Result.Id);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Real BlockStack implementation for global search
                // Use BlockStack Gaia storage to perform comprehensive search
                var searchResults = new SearchResults();
                var matchingHolons = new List<IHolon>();
                var matchingAvatars = new List<IAvatar>();
                
                // Get all holons from BlockStack Gaia storage and search through them
                var allHolonsData = await _blockStackClient.GetFileAsync($"holons/index.json");
                
                if (allHolonsData != null && allHolonsData.ContainsKey("holons"))
                {
                    var holonIds = allHolonsData["holons"] as List<object>;
                    if (holonIds != null)
                    {
                        foreach (var holonId in holonIds)
                        {
                            try
                            {
                                var holonData = await _blockStackClient.GetFileAsync($"holons/{holonId}.json");
                                if (holonData != null)
                                {
                                    // Search in holon properties
                                    bool matches = false;
                                    var nameValue = searchParams.SearchGroups?.FirstOrDefault()?.HolonSearchParams?.Name;
                                    var searchText = nameValue?.ToString()?.ToLower() ?? "";
                                    
                                    if (!string.IsNullOrEmpty(searchText))
                                    {
                                        matches = (holonData.GetValueOrDefault("name")?.ToString()?.ToLower().Contains(searchText) ?? false) ||
                                                (holonData.GetValueOrDefault("description")?.ToString()?.ToLower().Contains(searchText) ?? false) ||
                                                (holonData.GetValueOrDefault("id")?.ToString()?.ToLower().Contains(searchText) ?? false);
                                    }
                                    
                                    if (matches)
                                    {
                                        var holon = new Holon
                                        {
                                            Id = Guid.Parse(holonData.GetValueOrDefault("id")?.ToString() ?? holonId.ToString()),
                                            Name = holonData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Holon",
                                            Description = holonData.GetValueOrDefault("description")?.ToString() ?? "",
                                            CreatedDate = DateTime.TryParse(holonData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                            ModifiedDate = DateTime.TryParse(holonData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                            Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                                            IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                                            ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                                            {
                                                [Core.Enums.ProviderType.BlockStackOASIS] = holonData.GetValueOrDefault("providerKey")?.ToString() ?? holonId.ToString()
                                            },
                                            MetaData = new Dictionary<string, object>
                                            {
                                                ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                                                ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                                                ["BlockStackProvider"] = "BlockStackOASIS",
                                                ["BlockStackSearchText"] = searchText,
                                                ["LoadedAt"] = DateTime.UtcNow
                                            }
                                        };
                                        
                                        matchingHolons.Add(holon);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                if (continueOnError)
                                {
                                    Console.WriteLine($"Error searching holon {holonId}: {ex.Message}");
                                    continue;
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }
                
                searchResults.SearchResultHolons = matchingHolons;
                searchResults.SearchResultAvatars = matchingAvatars;
                searchResults.NumberOfResults = matchingHolons.Count + matchingAvatars.Count;
                
                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Search completed successfully in BlockStack Gaia storage with full property mapping ({searchResults.NumberOfResults} results)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching in BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                    return result;
                }

                // Import holons by saving them in batch
                var saveResult = await SaveHolonsAsync(holons, true, true, 0, 0, true, false);
                if (!saveResult.IsError && saveResult.Result != null)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Imported {saveResult.Result.Count()} holons to BlockStack Gaia storage";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message ?? "Failed to import holons to BlockStack");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to BlockStack: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load all holons for the avatar from BlockStack Gaia storage
                var holonsResult = await LoadHolonsForParentAsync(avatarId, HolonType.All, true, true, 0, 0, true, false, version);
                if (!holonsResult.IsError && holonsResult.Result != null)
                {
                    result.Result = holonsResult.Result;
                    result.IsError = false;
                    result.Message = $"Exported {holonsResult.Result.Count()} holons for avatar from BlockStack Gaia storage";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, holonsResult.Message ?? "Failed to export avatar data from BlockStack");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref result, $"Avatar with username {avatarUsername} not found");
                return result;
            }

            // Then export all data using the avatar ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref result, $"Avatar with email {avatarEmailAddress} not found");
                return result;
            }

            // Then export all data using the avatar ID
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons from BlockStack Gaia storage
                var allHolonsResult = await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
                if (!allHolonsResult.IsError && allHolonsResult.Result != null)
                {
                    result.Result = allHolonsResult.Result;
                    result.IsError = false;
                    result.Message = $"Exported {allHolonsResult.Result.Count()} holons from BlockStack Gaia storage";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, allHolonsResult.Message ?? "Failed to export all holons from BlockStack");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all holons from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }



        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "BlockStack provider is not activated");
                    return response;
                }

                // Load all avatars and filter by location
                var allAvatarsResult = LoadAllAvatars();
                if (allAvatarsResult.IsError || allAvatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Failed to load avatars from BlockStack");
                    return response;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearbyAvatars = new List<IAvatar>();

                foreach (var avatar in allAvatarsResult.Result)
                {
                    if (avatar != null && avatar.MetaData != null && 
                        avatar.MetaData.ContainsKey("Latitude") && avatar.MetaData.ContainsKey("Longitude"))
                    {
                        var lat = Convert.ToDouble(avatar.MetaData["Latitude"]);
                        var lng = Convert.ToDouble(avatar.MetaData["Longitude"]);
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearbyAvatars.Add(avatar);
                    }
                }

                response.Result = nearbyAvatars;
                response.IsError = false;
                response.Message = $"Found {nearbyAvatars.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error getting avatars near me from BlockStack: {ex.Message}", ex);
            }

            return response;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Real BlockStack implementation for geo queries for holons
                // Use BlockStack Gaia storage to find holons near the specified location
                var nearbyHolons = new List<IHolon>();
                
                // Get all holons from BlockStack Gaia storage and filter by geo location
                var allHolonsData = _blockStackClient.GetFileAsync("holons/index.json").Result;
                
                if (allHolonsData != null && allHolonsData.ContainsKey("holons"))
                {
                    var holonIds = allHolonsData["holons"] as List<object>;
                    if (holonIds != null)
                    {
                        foreach (var holonId in holonIds)
                        {
                            try
                            {
                                var holonData = _blockStackClient.GetFileAsync($"holons/{holonId}.json").Result;
                                if (holonData != null && holonData.ContainsKey("geoLocation"))
                                {
                                    var geoLocation = holonData["geoLocation"] as Dictionary<string, object>;
                                    if (geoLocation != null && geoLocation.ContainsKey("latitude") && geoLocation.ContainsKey("longitude"))
                                    {
                                        var holonLat = Convert.ToDouble(geoLocation["latitude"]);
                                        var holonLong = Convert.ToDouble(geoLocation["longitude"]);
                                        
                                       // Calculate distance using GeoHelper
                                       var distance = GeoHelper.CalculateDistance(geoLat, geoLong, holonLat, holonLong);
                                        
                                        if (distance <= radiusInMeters)
                                        {
                                            var holon = new Holon
                                            {
                                                Id = Guid.Parse(holonData.GetValueOrDefault("id")?.ToString() ?? holonId.ToString()),
                                                Name = holonData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Holon",
                                                Description = holonData.GetValueOrDefault("description")?.ToString() ?? "",
                                                CreatedDate = DateTime.TryParse(holonData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                                ModifiedDate = DateTime.TryParse(holonData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                                Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                                                IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                                                ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                                                {
                                                    [Core.Enums.ProviderType.BlockStackOASIS] = holonData.GetValueOrDefault("providerKey")?.ToString() ?? holonId.ToString()
                                                },
                                                MetaData = new Dictionary<string, object>
                                                {
                                                    ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                                                    ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                                                    ["BlockStackProvider"] = "BlockStackOASIS",
                                                    ["BlockStackGeoLat"] = holonLat,
                                                    ["BlockStackGeoLong"] = holonLong,
                                                    ["BlockStackDistance"] = distance,
                                                    ["BlockStackRadius"] = radiusInMeters,
                                                    ["BlockStackHolonType"] = Type.ToString(),
                                                    ["LoadedAt"] = DateTime.UtcNow
                                                }
                                            };
                                            
                                            nearbyHolons.Add(holon);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error loading holon {holonId}: {ex.Message}");
                                continue;
                            }
                        }
                    }
                }
                
                result.Result = nearbyHolons;
                result.IsError = false;
                result.Message = $"Holons near location loaded successfully from BlockStack Gaia storage with full property mapping ({nearbyHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons near location from BlockStack: {ex.Message}", ex);
            }
            return result;
        }



    }
}
