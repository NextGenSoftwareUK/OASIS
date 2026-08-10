using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS
{
    public partial class AztecOASIS
    {
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load avatar detail as separate object (HolonType.AvatarDetail)
                var holonsResult = await LoadHolonsByMetaDataAsync("Username", avatarUsername, HolonType.AvatarDetail);
                if (holonsResult.IsError || holonsResult.Result == null || !holonsResult.Result.Any())
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail not found by username");
                    return result;
                }

                var avatarDetail = ConvertHolonToAvatarDetail(holonsResult.Result.First());
                result.Result = avatarDetail;
                result.IsError = false;
                result.Message = "Avatar detail loaded successfully from Aztec by username";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load avatar details as separate objects (holons with HolonType.AvatarDetail)
                var holonsResult = await LoadAllHolonsAsync(HolonType.AvatarDetail);
                var avatarDetails = new List<IAvatarDetail>();
                if (!holonsResult.IsError && holonsResult.Result != null)
                {
                    foreach (var holon in holonsResult.Result)
                    {
                        var detail = ConvertHolonToAvatarDetail(holon);
                        if (detail != null)
                            avatarDetails.Add(detail);
                    }
                }

                result.Result = avatarDetails;
                result.IsError = false;
                result.Message = $"Loaded {avatarDetails.Count} avatar details from Aztec";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Save avatar as holon to Aztec
                var holon = Avatar as IHolon ?? new Holon
                {
                    Id = Avatar.Id,
                    Name = Avatar.Username,
                    Description = Avatar.Email,
                    HolonType = HolonType.Avatar
                };

                var saveResult = await SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }

                result.Result = Avatar;
                result.IsError = false;
                result.Message = "Avatar saved successfully to Aztec";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar) => SaveAvatarAsync(Avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Convert avatar detail to holon and save
                var holon = ConvertAvatarDetailToHolon(Avatar);
                var saveResult = await SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }

                result.Result = Avatar;
                result.IsError = false;
                result.Message = "Avatar detail saved successfully to Aztec";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar) => SaveAvatarDetailAsync(Avatar).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Delete holon (avatar is stored as holon)
                var deleteResult = await DeleteHolonAsync(id);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, deleteResult.Message);
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = "Avatar deleted successfully from Aztec";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Delete holon by provider key
                var deleteResult = await DeleteHolonAsync(providerKey);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, deleteResult.Message);
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = "Avatar deleted successfully from Aztec by provider key";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load avatar by email first
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmail);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
                    return result;
                }

                // Delete the avatar
                var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                result.Result = deleteResult.Result;
                result.IsError = deleteResult.IsError;
                result.Message = deleteResult.Message;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load avatar by username first
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
                    return result;
                }

                // Delete the avatar
                var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                result.Result = deleteResult.Result;
                result.IsError = deleteResult.IsError;
                result.Message = deleteResult.Message;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Use LoadHolonsByMetaData to search
                var holons = new List<IHolon>();
                
                if (searchParams != null && searchParams.SearchGroups != null)
                {
                    foreach (var group in searchParams.SearchGroups)
                    {
                        // SearchGroups can be different types (SearchTextGroup, SearchNumberGroup, etc.)
                        if (group is ISearchTextGroup textGroup && !string.IsNullOrWhiteSpace(textGroup.SearchQuery))
                        {
                            // Use SearchQuery to search across multiple fields
                            var holonsResult = await LoadHolonsByMetaDataAsync("Name", textGroup.SearchQuery, HolonType.All);
                            if (!holonsResult.IsError && holonsResult.Result != null)
                            {
                                holons.AddRange(holonsResult.Result);
                            }
                            
                            // Also search in description
                            var descHolonsResult = await LoadHolonsByMetaDataAsync("Description", textGroup.SearchQuery, HolonType.All);
                            if (!descHolonsResult.IsError && descHolonsResult.Result != null)
                            {
                                holons.AddRange(descHolonsResult.Result);
                            }
                        }
                    }
                }

                // Remove duplicates
                holons = holons.GroupBy(h => h.Id).Select(g => g.First()).ToList();

                result.Result = new SearchResults
                {
                    SearchResultHolons = holons,
                    NumberOfResults = holons.Count
                };
                result.IsError = false;
                result.Message = $"Found {holons.Count} results";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                result.Result = await _aztecRepository.LoadHolonAsync(id);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                result.Result = await _aztecRepository.LoadHolonByProviderKeyAsync(providerKey);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load holons by parent ID in metadata
                var holonsResult = await LoadHolonsByMetaDataAsync("ParentId", id.ToString(), type);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, holonsResult.Message);
                    return result;
                }

                result.Result = holonsResult.Result ?? new List<IHolon>();
                result.IsError = false;
                result.Message = $"Loaded {result.Result.Count()} holons for parent";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load holons by parent provider key in metadata
                var holonsResult = await LoadHolonsByMetaDataAsync("ParentProviderKey", providerKey, type);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, holonsResult.Message);
                    return result;
                }

                result.Result = holonsResult.Result ?? new List<IHolon>();
                result.IsError = false;
                result.Message = $"Loaded {result.Result.Count()} holons for parent by provider key";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load all holons and filter by metadata
                var allHolonsResult = await LoadAllHolonsAsync(type);
                if (allHolonsResult.IsError || allHolonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {allHolonsResult.Message}");
                    return result;
                }

                // Filter by metadata
                var filteredHolons = allHolonsResult.Result.Where(h => 
                    h.MetaData != null && 
                    h.MetaData.TryGetValue(metaKey, out var value) && 
                    value?.ToString() == metaValue
                ).ToList();

                result.Result = filteredHolons;
                result.IsError = false;
                result.Message = $"Found {filteredHolons.Count} holons matching metadata";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load all holons and filter by metadata dictionary
                var allHolonsResult = await LoadAllHolonsAsync(type);
                if (allHolonsResult.IsError || allHolonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {allHolonsResult.Message}");
                    return result;
                }

                // Filter by metadata dictionary based on match mode
                IEnumerable<IHolon> filteredHolons = allHolonsResult.Result;
                
                if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                {
                    // All key-value pairs must match
                    filteredHolons = filteredHolons.Where(h =>
                        h.MetaData != null &&
                        metaKeyValuePairs.All(kvp =>
                            h.MetaData.TryGetValue(kvp.Key, out var value) &&
                            value?.ToString() == kvp.Value
                        )
                    );
                }
                else
                {
                    // Any key-value pair can match
                    filteredHolons = filteredHolons.Where(h =>
                        h.MetaData != null &&
                        metaKeyValuePairs.Any(kvp =>
                            h.MetaData.TryGetValue(kvp.Key, out var value) &&
                            value?.ToString() == kvp.Value
                        )
                    );
                }

                result.Result = filteredHolons.ToList();
                result.IsError = false;
                result.Message = $"Found {result.Result.Count()} holons matching metadata dictionary";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    }
}
