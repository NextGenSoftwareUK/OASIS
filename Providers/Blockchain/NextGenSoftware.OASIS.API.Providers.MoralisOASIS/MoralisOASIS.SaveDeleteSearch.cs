using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
// using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request; // Removed - use Requests (plural) instead
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MoralisOASIS
{
    public partial class MoralisOASIS
    {
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon first to return it
                var loadResult = await LoadHolonAsync(id, false, false, 0, true, false, 0);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon {id} not found for deletion");
                    return result;
                }

                var holon = loadResult.Result;

                // Real Moralis implementation - delete holon from contract if available
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "deleteHolon",
                        abi = GetOASISContractABI(),
                        @params = new { holonId = id.ToString() }
                    };

                    var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = $"Holon {id} deleted successfully from Moralis contract";
                        return result;
                    }
                }

                // IPFS is immutable, so we can't actually delete files
                // Instead, we mark it as deleted in a new transaction or return a warning
                OASISErrorHandling.HandleWarning(ref result, "IPFS is immutable. Holon cannot be deleted from IPFS. Use contract deletion or mark as deleted in metadata.");
                result.Result = holon;
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
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon first to return it
                var loadResult = await LoadHolonAsync(providerKey, false, false, 0, true, false, 0);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon with provider key {providerKey} not found for deletion");
                    return result;
                }

                var holon = loadResult.Result;

                // Real Moralis implementation - delete holon by provider key from contract if available
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "deleteHolonByProviderKey",
                        abi = GetOASISContractABI(),
                        @params = new { providerKey = providerKey }
                    };

                    var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = $"Holon with provider key {providerKey} deleted successfully from Moralis contract";
                        return result;
                    }
                }

                // IPFS is immutable, so we can't actually delete files
                OASISErrorHandling.HandleWarning(ref result, "IPFS is immutable. Holon cannot be deleted from IPFS. Use contract deletion or mark as deleted in metadata.");
                result.Result = holon;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        // Import/Export Methods
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                    return result;
                }

                // Real Moralis implementation - import holons by saving them all to IPFS
                var savedCount = 0;
                var errors = new List<string>();

                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, true, true, 0, true, false);
                    if (!saveResult.IsError && saveResult.Result != null)
                    {
                        savedCount++;
                    }
                    else
                    {
                        errors.Add($"Failed to import holon {holon.Id}: {saveResult.Message}");
                    }
                }

                result.Result = savedCount == holons.Count();
                result.IsError = errors.Any();
                result.Message = errors.Any()
                    ? $"Imported {savedCount} of {holons.Count()} holons. Errors: {string.Join("; ", errors)}"
                    : $"Successfully imported {savedCount} holons to Moralis IPFS";
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

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Real Moralis implementation - export all holons for avatar by loading all holons and filtering by CreatedByAvatarId
                var allHolonsResult = await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
                if (allHolonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons: {allHolonsResult.Message}");
                    return result;
                }

                var avatarHolons = allHolonsResult.Result?.Where(h => h.CreatedByAvatarId == id).ToList() ?? new List<IHolon>();
                
                result.Result = avatarHolons;
                result.IsError = false;
                result.Message = $"Exported {avatarHolons.Count} holons for avatar {id} from Moralis";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data for avatar by ID: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(id, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by username first
                var avatarResult = await LoadAvatarByUsernameAsync(username, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar not found by username: {avatarResult.Message}");
                    return result;
                }

                // Export all data for the avatar
                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data for avatar by username: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(username, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by email first
                var avatarResult = await LoadAvatarByEmailAsync(email, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar not found by email: {avatarResult.Message}");
                    return result;
                }

                // Export all data for the avatar
                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data for avatar by email: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(email, version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Real Moralis implementation - export all holons
                return await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
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

        // Search Methods
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (searchParams == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Search parameters cannot be null");
                    return result;
                }

                // Real Moralis implementation - search through holons and avatars
                var searchResults = new SearchResults();
                var matchingHolons = new List<IHolon>();
                var matchingAvatars = new List<IAvatar>();

                // Load all holons and filter by search criteria
                var allHolonsResult = await LoadAllHolonsAsync(HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
                if (!allHolonsResult.IsError && allHolonsResult.Result != null)
                {
                    var searchQuery = searchParams.SearchGroups?.OfType<ISearchTextGroup>().FirstOrDefault()?.SearchQuery ?? "";
                    foreach (var holon in allHolonsResult.Result)
                    {
                        bool matches = false;
                        
                        if (!string.IsNullOrEmpty(searchQuery) && 
                            holon.Name?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true)
                            matches = true;
                        
                        if (!matches && !string.IsNullOrEmpty(searchQuery) && 
                            holon.Description?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true)
                            matches = true;

                        if (!matches && holon.MetaData != null && searchQuery != null)
                            matches = holon.MetaData.Values.Any(v => v?.ToString()?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true);

                        if (matches)
                        {
                            matchingHolons.Add(holon);
                        }
                    }
                }

                // Load all avatars and filter by search criteria
                var allAvatarsResult = await LoadAllAvatarsAsync(version);
                if (!allAvatarsResult.IsError && allAvatarsResult.Result != null)
                {
                    var searchQueryAvatars = searchParams.SearchGroups?.OfType<ISearchTextGroup>().FirstOrDefault()?.SearchQuery ?? "";
                    foreach (var avatar in allAvatarsResult.Result)
                    {
                        bool matches = false;
                        
                        if (!string.IsNullOrEmpty(searchQueryAvatars) && 
                            avatar.Username?.Contains(searchQueryAvatars, StringComparison.OrdinalIgnoreCase) == true)
                            matches = true;
                        
                        if (!matches && !string.IsNullOrEmpty(searchQueryAvatars) && 
                            avatar.Email?.Contains(searchQueryAvatars, StringComparison.OrdinalIgnoreCase) == true)
                            matches = true;

                        if (!matches && !string.IsNullOrEmpty(searchQueryAvatars))
                        {
                            var avatarDetail = avatar as AvatarDetail;
                            var fullName = $"{avatarDetail?.FirstName} {avatarDetail?.LastName}".Trim();
                            if (fullName.Contains(searchQueryAvatars, StringComparison.OrdinalIgnoreCase))
                                matches = true;
                        }

                        if (matches)
                            matchingAvatars.Add(avatar);
                    }
                }

                searchResults.SearchResultHolons = matchingHolons;
                searchResults.SearchResultAvatars = matchingAvatars;

                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Search completed: Found {matchingHolons.Count} holons and {matchingAvatars.Count} avatars";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        // IOASISNETProvider Methods
        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAvatarsNearMeAsync(IAvatar avatar, double radiusKm)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar cannot be null");
                    return result;
                }

                // Real Moralis implementation - get avatars near location using avatar's coordinates
                if (avatar.MetaData != null && 
                    avatar.MetaData.ContainsKey("Latitude") && avatar.MetaData.ContainsKey("Longitude"))
                {
                    var lat = Convert.ToDouble(avatar.MetaData["Latitude"]);
                    var lon = Convert.ToDouble(avatar.MetaData["Longitude"]);
                    return await GetAvatarsNearMeAsync((long)(lat * 1000000), (long)(lon * 1000000), (int)(radiusKm * 1000));
                }

                // If no coordinates, return empty result
                result.Result = new List<IAvatar>();
                result.IsError = false;
                result.Message = "Avatar does not have location coordinates";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me: {ex.Message}", ex);
            }
            return result;
        }

    }
}
