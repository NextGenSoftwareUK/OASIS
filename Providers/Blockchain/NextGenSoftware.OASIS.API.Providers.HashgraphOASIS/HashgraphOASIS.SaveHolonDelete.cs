using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
    public partial class HashgraphOASIS
    {
        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Save multiple holons to Hashgraph blockchain
                var saveRequest = new
                {
                    holons = holons.Select(h => new
                    {
                        id = h.Id.ToString(),
                        name = h.Name,
                        description = h.Description,
                        data = JsonSerializer.Serialize(h),
                        version = h.Version,
                        parentId = h.ParentHolonId.ToString(),
                        holonType = h.HolonType.ToString()
                    }).ToArray(),
                    saveChildren = saveChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    saveChildrenOnProvider = saveChildrenOnProvider
                };

                var jsonContent = JsonSerializer.Serialize(saveRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var saveResponse = await _httpClient.PostAsync("/api/v1/holons/batch", content);
                if (saveResponse.IsSuccessStatusCode)
                {
                    var responseContent = await saveResponse.Content.ReadAsStringAsync();
                    var saveData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var savedHolons = new List<IHolon>();
                    if (saveData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            savedHolons.Add(holon);
                        }
                    }

                    result.Result = savedHolons;
                    result.IsError = false;
                    result.Message = $"Successfully saved {savedHolons.Count} holons to Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holons to Hashgraph blockchain: {saveResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to Hashgraph blockchain: {ex.Message}", ex);
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
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Delete holon from Hashgraph blockchain
                var deleteRequest = new
                {
                    id = id.ToString(),
                    deleted = true,
                    deletedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var jsonContent = JsonSerializer.Serialize(deleteRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var deleteResponse = await _httpClient.PostAsync($"/api/v1/holons/{id}/delete", content);
                if (deleteResponse.IsSuccessStatusCode)
                {
                    var responseContent = await deleteResponse.Content.ReadAsStringAsync();
                    var deleteData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (deleteData.TryGetProperty("holon", out var holonElement))
                    {
                        var deletedHolon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                        result.Result = deletedHolon;
                        result.IsError = false;
                        result.Message = "Holon deleted successfully from Hashgraph blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Invalid response format from Hashgraph blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon from Hashgraph blockchain: {deleteResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Hashgraph blockchain: {ex.Message}", ex);
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
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Delete holon by provider key from Hashgraph blockchain
                var deleteRequest = new
                {
                    providerKey = providerKey,
                    deleted = true,
                    deletedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var jsonContent = JsonSerializer.Serialize(deleteRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var deleteResponse = await _httpClient.PostAsync($"/api/v1/holons/provider/{providerKey}/delete", content);
                if (deleteResponse.IsSuccessStatusCode)
                {
                    var responseContent = await deleteResponse.Content.ReadAsStringAsync();
                    var deleteData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (deleteData.TryGetProperty("holon", out var holonElement))
                    {
                        var deletedHolon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                        result.Result = deletedHolon;
                        result.IsError = false;
                        result.Message = "Holon deleted successfully from Hashgraph blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Invalid response format from Hashgraph blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon from Hashgraph blockchain: {deleteResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Hashgraph blockchain: {ex.Message}", ex);
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
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Search holons in Hashgraph blockchain
                var searchRequest = new
                {
                    searchParams = new
                    {
                        avatarId = searchParams.AvatarId,
                        searchOnlyForCurrentAvatar = searchParams.SearchOnlyForCurrentAvatar,
                        searchGroups = searchParams.SearchGroups
                    },
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    continueOnError = continueOnError,
                    version = version
                };

                var jsonContent = JsonSerializer.Serialize(searchRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var searchResponse = await _httpClient.PostAsync("/api/v1/search", content);
                if (searchResponse.IsSuccessStatusCode)
                {
                    var responseContent = await searchResponse.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var searchResults = new SearchResults();
                    if (searchData.TryGetProperty("results", out var resultsArray))
                    {
                        var holons = new List<IHolon>();
                        foreach (var holonElement in resultsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                        searchResults.SearchResultHolons = holons.ToList();
                        searchResults.NumberOfResults = holons.Count();
                    }

                    result.Result = searchResults;
                    result.IsError = false;
                    result.Message = "Search completed successfully on Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to search on Hashgraph blockchain: {searchResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching on Hashgraph blockchain: {ex.Message}", ex);
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
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Import holons to Hashgraph blockchain
                var importRequest = new
                {
                    holons = holons.Select(h => new
                    {
                        id = h.Id.ToString(),
                        name = h.Name,
                        description = h.Description,
                        data = JsonSerializer.Serialize(h),
                        version = h.Version,
                        parentId = h.ParentHolonId.ToString(),
                        holonType = h.HolonType.ToString()
                    }).ToArray()
                };

                var jsonContent = JsonSerializer.Serialize(importRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var importResponse = await _httpClient.PostAsync("/api/v1/import", content);
                if (importResponse.IsSuccessStatusCode)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully imported {holons.Count()} holons to Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to import holons to Hashgraph blockchain: {importResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Hashgraph blockchain: {ex.Message}", ex);
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
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all data for specific avatar from Hashgraph blockchain
                var exportRequest = new
                {
                    avatarId = avatarId.ToString(),
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    if (exportData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Avatar data export completed successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from Hashgraph blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Hashgraph blockchain: {ex.Message}", ex);
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
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all data for specific avatar by username from Hashgraph blockchain
                var exportRequest = new
                {
                    avatarUsername = avatarUsername,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar/username", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    if (exportData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Avatar data export completed successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from Hashgraph blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Hashgraph blockchain: {ex.Message}", ex);
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
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all data for specific avatar by email from Hashgraph blockchain
                var exportRequest = new
                {
                    avatarEmail = avatarEmailAddress,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar/email", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    if (exportData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Avatar data export completed successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from Hashgraph blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Hashgraph blockchain: {ex.Message}", ex);
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
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all data from Hashgraph blockchain
                var exportRequest = new
                {
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/api/v1/export", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    if (exportData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "All data export completed successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export all data from Hashgraph blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }



        // Removed explicit interface implementation that doesn't exist in the interface

    }
}
