using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS
{
    public partial class ThreeFoldOASIS
    {
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
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
                        result.Message = $"Successfully saved {savedHolons.Count} holons to ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize saved holons from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to ThreeFold Grid: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/holons/{id}");

                if (response.IsSuccessStatusCode)
                {
                    result.Result = null; // Holon deleted
                    result.IsError = false;
                    result.Message = "Holon deleted successfully from ThreeFold Grid";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from ThreeFold Grid: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/holons/by-provider-key/{Uri.EscapeDataString(providerKey)}");

                if (response.IsSuccessStatusCode)
                {
                    result.Result = null; // Holon deleted
                    result.IsError = false;
                    result.Message = "Holon deleted successfully from ThreeFold Grid by provider key";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from ThreeFold Grid by provider key: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (searchParams == null)
                {
                    OASISErrorHandling.HandleError(ref result, "SearchParams cannot be null");
                    return result;
                }

                string searchText = null;
                HolonType holonType = HolonType.All;
                Dictionary<string, string> metaData = null;

                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var firstGroup = searchParams.SearchGroups.First();
                    holonType = firstGroup.HolonType;

                    if (firstGroup is ISearchTextGroup textGroup)
                        searchText = textGroup.SearchQuery;

                    if (firstGroup.HolonSearchParams != null && firstGroup.HolonSearchParams.MetaData)
                    {
                        metaData = new Dictionary<string, string>();
                    }
                }

                var searchRequest = new
                {
                    searchText = searchText,
                    holonType = holonType.ToString(),
                    metaData = metaData,
                    version = version
                };

                var jsonContent = JsonSerializer.Serialize(searchRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/search", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var searchResults = JsonSerializer.Deserialize<SearchResults>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (searchResults != null)
                    {
                        result.Result = searchResults;
                        result.IsError = false;
                        result.Message = $"Search completed successfully on ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize search results from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching on ThreeFold Grid: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
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
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/import", content);

                if (response.IsSuccessStatusCode)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully imported {holons.Count()} holons to ThreeFold Grid";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to ThreeFold Grid: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/export/avatar/{avatarId}?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var holons = JsonSerializer.Deserialize<List<Holon>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (holons != null)
                    {
                        result.Result = holons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully exported {holons.Count} holons for avatar {avatarId} from ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize exported holons from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from ThreeFold Grid: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/export/avatar/username/{Uri.EscapeDataString(avatarUsername)}?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var holons = JsonSerializer.Deserialize<List<Holon>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (holons != null)
                    {
                        result.Result = holons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully exported {holons.Count} holons for avatar {avatarUsername} from ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize exported holons from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from ThreeFold Grid: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/export/avatar/email/{Uri.EscapeDataString(avatarEmailAddress)}?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var holons = JsonSerializer.Deserialize<List<Holon>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (holons != null)
                    {
                        result.Result = holons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully exported {holons.Count} holons for avatar {avatarEmailAddress} from ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize exported holons from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from ThreeFold Grid: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/export/all?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var holons = JsonSerializer.Deserialize<List<Holon>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (holons != null)
                    {
                        result.Result = holons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully exported {holons.Count} holons from ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize exported holons from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from ThreeFold Grid: {ex.Message}", ex);
            }
            return result;
        }

    }
}
