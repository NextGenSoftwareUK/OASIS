using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS
{
    public partial class TRONOASIS
    {
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Query TRON smart contract for all holons
                var contractAddress = GetOASISContractAddress();
                var functionName = "getAllHolons";
                var parameters = new object[] { (int)type };

                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var holons = ParseTRONToHolons(contractResult.Result);
                    if (holons != null)
                    {
                        result.Result = holons.Where(h => type == HolonType.All || h.HolonType == type).ToList();
                        result.IsError = false;
                        result.Message = $"Loaded {result.Result.Count()} holons from TRON blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "No holons found in TRON blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from TRON: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                    return result;
                }

                // Get wallet for the holon
                var walletResult = await GetWalletAddressForAvatar(holon.CreatedByAvatarId != Guid.Empty ? holon.CreatedByAvatarId : holon.Id);
                if (walletResult.IsError || string.IsNullOrWhiteSpace(walletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for holon");
                    return result;
                }

                // Save holon to TRON smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "saveHolon";
                var holonJson = JsonSerializer.Serialize(holon);
                var parameters = new object[]
                {
                    holon.Id.ToString(),
                    holon.Name ?? "",
                    holon.Description ?? "",
                    (int)holon.HolonType,
                    holon.ParentHolonId.ToString(),
                    holonJson
                };

                var contractResult = await CallContractAsync(contractAddress, functionName, parameters, walletResult.Result);
                if (!contractResult.IsError)
                {
                    result.Result = holon;
                    result.IsError = false;
                    result.IsSaved = true;
                    result.Message = "Holon saved successfully to TRON blockchain";

                    // Handle children if requested
                    if (saveChildren && holon.Children != null && holon.Children.Any())
                    {
                        foreach (var child in holon.Children)
                        {
                            var childResult = await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth - 1, continueOnError, saveChildrenOnProvider);
                            if (!continueOnError && childResult.IsError)
                            {
                                OASISErrorHandling.HandleError(ref result, $"Failed to save child holon {child.Id}: {childResult.Message}");
                                return result;
                            }
                        }
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holon to TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to TRON: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                    return result;
                }

                var savedHolons = new List<IHolon>();
                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (!saveResult.IsError && saveResult.Result != null)
                    {
                        savedHolons.Add(saveResult.Result);
                    }
                    else if (!continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to save holon {holon.Id}: {saveResult.Message}");
                        return result;
                    }
                }

                result.Result = savedHolons;
                result.IsError = false;
                result.Message = $"Saved {savedHolons.Count} holons to TRON blockchain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to TRON: {ex.Message}", ex);
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
                // First load the holon to return it
                var loadResult = await LoadHolonAsync(id);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon with ID {id} not found");
                    return result;
                }

                // Get wallet for the holon
                var walletResult = await GetWalletAddressForAvatar(loadResult.Result.CreatedByAvatarId != Guid.Empty ? loadResult.Result.CreatedByAvatarId : id);
                if (walletResult.IsError || string.IsNullOrWhiteSpace(walletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for holon deletion");
                    return result;
                }

                // Delete holon from TRON smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "deleteHolon";
                var parameters = new object[] { id.ToString() };

                var contractResult = await CallContractAsync(contractAddress, functionName, parameters, walletResult.Result);
                if (!contractResult.IsError)
                {
                    result.Result = loadResult.Result;
                    result.IsError = false;
                    result.Message = "Holon deleted successfully from TRON blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from TRON: {ex.Message}", ex);
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
            var loadResult = await LoadHolonAsync(providerKey);
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
                if (searchParams == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Search parameters cannot be null");
                    return result;
                }

                // Extract search query from SearchGroups
                string searchQuery = null;
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var firstGroup = searchParams.SearchGroups.FirstOrDefault();
                    if (firstGroup is ISearchTextGroup textGroup && !string.IsNullOrWhiteSpace(textGroup.SearchQuery))
                    {
                        searchQuery = textGroup.SearchQuery;
                    }
                }

                // Real TRON implementation - search through holons and avatars
                var searchResults = new SearchResults();
                var matchingHolons = new List<IHolon>();
                var matchingAvatars = new List<IAvatar>();

                // Search holons by calling smart contract search function or loading all and filtering
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    var contractAddress = GetOASISContractAddress();
                    var functionName = "searchHolons";
                    var parameters = new object[] { searchQuery };

                    var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                    if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                    {
                        var holons = ParseTRONToHolons(contractResult.Result);
                        if (holons != null)
                        {
                            matchingHolons.AddRange(holons);
                        }
                    }
                    else
                    {
                        // Fallback: Load all holons and filter
                        var allHolonsResult = await LoadAllHolonsAsync(HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
                        if (!allHolonsResult.IsError && allHolonsResult.Result != null)
                        {
                            foreach (var holon in allHolonsResult.Result)
                            {
                                if (holon.Name?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true ||
                                    holon.Description?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    matchingHolons.Add(holon);
                                }
                            }
                        }
                    }

                    // Search avatars by loading all and filtering
                    var allAvatarsResult = await LoadAllAvatarsAsync(version);
                    if (!allAvatarsResult.IsError && allAvatarsResult.Result != null)
                    {
                        foreach (var avatar in allAvatarsResult.Result)
                        {
                            if (avatar.Username?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true ||
                                avatar.Email?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true ||
                                $"{avatar.FirstName} {avatar.LastName}".Trim().Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                            {
                                matchingAvatars.Add(avatar);
                            }
                        }
                    }
                }

                searchResults.SearchResultHolons = matchingHolons;
                searchResults.SearchResultAvatars = matchingAvatars;
                searchResults.NumberOfResults = matchingHolons.Count + matchingAvatars.Count;

                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Search completed: Found {matchingHolons.Count} holons and {matchingAvatars.Count} avatars";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching TRON blockchain: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            // Import holons by saving them in batch
            var saveResult = await SaveHolonsAsync(holons, true, true, 0, 0, true, false);
            var result = new OASISResult<bool>();
            if (!saveResult.IsError && saveResult.Result != null)
            {
                result.Result = true;
                result.IsError = false;
                result.Message = $"Imported {saveResult.Result.Count()} holons to TRON blockchain";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, saveResult.Message ?? "Failed to import holons to TRON");
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
                // Query TRON smart contract for holons created by this avatar
                var contractAddress = GetOASISContractAddress();
                var functionName = "getHolonsByAvatar";
                var parameters = new object[] { avatarId.ToString() };

                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var holons = ParseTRONToHolons(contractResult.Result);
                    if (holons != null)
                    {
                        result.Result = holons.ToList();
                        result.IsError = false;
                        result.Message = $"Exported {result.Result.Count()} holons for avatar {avatarId} from TRON blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "No holons found for avatar in TRON blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export data from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data from TRON: {ex.Message}", ex);
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
            // Export all holons - delegate to LoadAllHolonsAsync
            return await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }



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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
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
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from TRON: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
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
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from TRON: {ex.Message}", ex);
            }
            return result;
        }

    }
}
