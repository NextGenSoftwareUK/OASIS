using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS
{
    public partial class PLANOASIS
    {
        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        {
            return null;
        }

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var searchParams = new Dictionary<string, string>
                {
                    { "metaKey", metaKey },
                    { "metaValue", metaValue },
                    { "type", type.ToString() },
                    { "version", version.ToString() }
                };

                var queryString = string.Join("&", searchParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/search?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var holons = JsonSerializer.Deserialize<List<Holon>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (holons != null)
                    {
                        result.Result = holons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {holons.Count} holons by metadata from PLAN";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from PLAN API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var searchRequest = new
                {
                    metaKeyValuePairs = metaKeyValuePairs,
                    matchMode = metaKeyValuePairMatchMode.ToString(),
                    type = type.ToString(),
                    version = version
                };

                var jsonContent = JsonSerializer.Serialize(searchRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/holons/search-multiple", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var holons = JsonSerializer.Deserialize<List<Holon>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (holons != null)
                    {
                        result.Result = holons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {holons.Count} holons by multiple metadata from PLAN";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from PLAN API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by multiple metadata from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                    return result;
                }

                var jsonContent = JsonSerializer.Serialize(holon);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/holons", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var savedHolon = JsonSerializer.Deserialize<Holon>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (savedHolon != null)
                    {
                        result.Result = savedHolon;
                        result.IsError = false;
                        result.Message = "Holon saved successfully to PLAN";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize saved holon from PLAN API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to PLAN: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holons == null || !holons.Any())
                {
                    OASISErrorHandling.HandleError(ref result, "Holons collection cannot be null or empty");
                    return result;
                }

                var jsonContent = JsonSerializer.Serialize(holons);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/holons/batch", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var savedHolons = JsonSerializer.Deserialize<List<Holon>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (savedHolons != null)
                    {
                        result.Result = savedHolons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully saved {savedHolons.Count} holons to PLAN";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize saved holons from PLAN API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to PLAN: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/holons/{id}");

                if (response.IsSuccessStatusCode)
                {
                    result.Result = null; // Holon deleted
                    result.IsError = false;
                    result.Message = "Holon deleted successfully from PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/holons/by-provider-key/{Uri.EscapeDataString(providerKey)}");

                if (response.IsSuccessStatusCode)
                {
                    result.Result = null; // Holon deleted
                    result.IsError = false;
                    result.Message = "Holon deleted successfully from PLAN by provider key";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from PLAN by provider key: {ex.Message}", ex);
            }
            return result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var searchUrl = $"{_apiBaseUrl}/search";
                
                // Extract search query and holon type from SearchGroups if available
                string searchQuery = "";
                string holonTypeStr = "All";
                
                if (searchParams?.SearchGroups != null && searchParams.SearchGroups.Count > 0)
                {
                    var firstGroup = searchParams.SearchGroups[0];
                    holonTypeStr = firstGroup.HolonType.ToString();
                    
                    // Try to get SearchQuery from SearchTextGroup if available
                    if (firstGroup is SearchTextGroup textGroup)
                    {
                        searchQuery = textGroup.SearchQuery ?? "";
                    }
                }
                
                var searchRequest = new
                {
                    query = searchQuery,
                    holonType = holonTypeStr,
                    version = version
                };

                var content = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(searchUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                    var searchResults = new SearchResults
                    {
                        SearchResultHolons = new List<IHolon>(),
                        SearchResultAvatars = new List<IAvatar>(),
                        NumberOfResults = 0,
                        NumberOfDuplicates = 0
                    };

                    if (searchData?.ContainsKey("results") == true && searchData["results"] is JsonElement resultsArray)
                    {
                        foreach (var item in resultsArray.EnumerateArray())
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(item.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                Name = item.GetProperty("name").GetString() ?? "",
                                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : ""
                            };
                            searchResults.SearchResultHolons.Add(holon);
                        }
                        searchResults.NumberOfResults = searchResults.SearchResultHolons.Count;
                    }

                    result.Result = searchResults;
                    result.IsError = false;
                    result.Message = $"Search completed successfully, found {searchResults.NumberOfResults} results";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN search failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching PLAN: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var importUrl = $"{_apiBaseUrl}/import";
                var importedCount = 0;

                foreach (var holon in holons)
                {
                    var holonData = new
                    {
                        id = holon.Id.ToString(),
                        name = holon.Name,
                        description = holon.Description,
                        holonType = holon.HolonType.ToString(),
                        createdDate = holon.CreatedDate,
                        modifiedDate = holon.ModifiedDate,
                        version = holon.Version
                    };

                    var content = new StringContent(JsonSerializer.Serialize(holonData), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(importUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        importedCount++;
                    }
                    else if (!result.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to import holon {holon.Id}: {response.StatusCode}");
                        return result;
                    }
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {importedCount} holons to PLAN";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to PLAN: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var exportUrl = $"{_apiBaseUrl}/export/avatar/{avatarId}?version={version}";
                var response = await _httpClient.GetAsync(exportUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                    var holons = new List<IHolon>();
                    if (exportData?.ContainsKey("holons") == true && exportData["holons"] is JsonElement holonsArray)
                    {
                        foreach (var item in holonsArray.EnumerateArray())
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(item.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                Name = item.GetProperty("name").GetString() ?? "",
                                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                                Version = item.TryGetProperty("version", out var ver) ? ver.GetInt32() : version
                            };
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully exported {holons.Count} holons for avatar {avatarId} from PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN export failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var exportUrl = $"{_apiBaseUrl}/export/avatar/username/{avatarUsername}?version={version}";
                var response = await _httpClient.GetAsync(exportUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                    var holons = new List<IHolon>();
                    if (exportData?.ContainsKey("holons") == true && exportData["holons"] is JsonElement holonsArray)
                    {
                        foreach (var item in holonsArray.EnumerateArray())
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(item.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                Name = item.GetProperty("name").GetString() ?? "",
                                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                                Version = item.TryGetProperty("version", out var ver) ? ver.GetInt32() : version
                            };
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully exported {holons.Count} holons for avatar {avatarUsername} from PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN export failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var exportUrl = $"{_apiBaseUrl}/export/avatar/email/{Uri.EscapeDataString(avatarEmailAddress)}?version={version}";
                var response = await _httpClient.GetAsync(exportUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                    var holons = new List<IHolon>();
                    if (exportData?.ContainsKey("holons") == true && exportData["holons"] is JsonElement holonsArray)
                    {
                        foreach (var item in holonsArray.EnumerateArray())
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(item.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                Name = item.GetProperty("name").GetString() ?? "",
                                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                                Version = item.TryGetProperty("version", out var ver) ? ver.GetInt32() : version
                            };
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully exported {holons.Count} holons for avatar {avatarEmailAddress} from PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN export failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from PLAN: {ex.Message}", ex);
            }
            return result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var exportUrl = $"{_apiBaseUrl}/export/all?version={version}";
                var response = await _httpClient.GetAsync(exportUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                    var holons = new List<IHolon>();
                    if (exportData?.ContainsKey("holons") == true && exportData["holons"] is JsonElement holonsArray)
                    {
                        foreach (var item in holonsArray.EnumerateArray())
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(item.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                Name = item.GetProperty("name").GetString() ?? "",
                                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                                Version = item.TryGetProperty("version", out var ver) ? ver.GetInt32() : version
                            };
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully exported {holons.Count} holons from PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN export failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }



    }
}
