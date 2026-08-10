using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using LockWeb3TokenRequest = NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.LockWeb3TokenRequest;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.Infrastructure.Services.Miden;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.Models;
using NextGenSoftware.OASIS.Common;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.MidenOASIS
{
    public partial class MidenOASIS
    {
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Delete avatar by ID from Miden API
                var apiResult = await _apiClient.PostAsync<bool>($"/api/avatars/delete/{id}", new { softDelete });
                
                if (!apiResult.IsError)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = "Successfully deleted avatar by ID from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar by ID from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by ID from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query holons by metadata from Miden API
                var requestPayload = new
                {
                    metadata = metaKeyValuePairs,
                    matchMode = metaKeyValuePairMatchMode.ToString(),
                    holonType = type.ToString(),
                    version = version
                };
                
                var apiResult = await _apiClient.PostAsync<List<Holon>>("/api/holons/search/metadata", requestPayload);
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    var holons = apiResult.Result.Where(h => type == HolonType.All || h.HolonType == type).Cast<IHolon>();
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count()} holons by metadata from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons by metadata from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            return GenerateKeyPairAsync().Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Generate key pair: use KeyHelper (IKeyPairAndWallet from Utilities via KeyManager/Core); API could be used with a matching DTO if needed
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    result.Result = keyPair;
                    result.IsError = false;
                    result.Message = "Key pair generated successfully for Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to generate key pair from Miden");
                }
            }
            catch (Exception ex)
            {
                // Fallback: Use KeyHelper if API call fails
                try
                {
                    var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                    if (keyPair != null)
                    {
                        result.Result = keyPair;
                        result.IsError = false;
                        result.Message = "Key pair generated successfully using KeyHelper (fallback)";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
                    }
                }
                catch
                {
                    OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
                }
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query all avatar details from Miden API
                var apiResult = await _apiClient.GetAsync<List<AvatarDetail>>($"/api/avatars/details?version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result.Cast<IAvatarDetail>();
                    result.IsError = false;
                    result.Message = $"Successfully loaded {apiResult.Result.Count} avatar details from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar details from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons from Miden API
                var apiResult = await _apiClient.GetAsync<List<Holon>>($"/api/holons/export?version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result.Cast<IHolon>();
                    result.IsError = false;
                    result.Message = $"Successfully exported {apiResult.Result.Count} holons from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export all holons from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all holons from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                    return result;
                }

                // Import holons to Miden API
                var apiResult = await _apiClient.PostAsync<bool>("/api/holons/import", holons);
                
                if (!apiResult.IsError)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = $"Successfully imported {holons.Count()} holons to Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to import holons to Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (searchParams == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Search parameters cannot be null");
                    return result;
                }

                // Build search request payload
                var searchPayload = new
                {
                    query = searchParams is ISearchTextGroup textGroup ? textGroup.SearchQuery : "",
                    version = version
                };

                // Search holons and avatars from Miden API
                var apiResult = await _apiClient.PostAsync<SearchResults>("/api/search", searchPayload);
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result;
                    result.IsError = false;
                    result.Message = $"Successfully searched Miden: found {apiResult.Result.SearchResultAvatars?.Count() ?? 0} avatars and {apiResult.Result.SearchResultHolons?.Count() ?? 0} holons";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to search Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching Miden: {ex.Message}", ex);
            }
            return result;
        }

    }
}
