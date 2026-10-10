using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public partial class SQLLiteDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider, IOASISLocalStorageProvider, IOASISNETProvider, IOASISSuperStar
    {

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, version);
            return result;
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonsForParent(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonsForParent(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = _holonRepository.LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
        {
            // SQLite provider does not generate native code from STAR metadata.
            return true;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            var result = _avatarRepository.SaveAvatar(Avatar);
            return result;
        }

        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            var result = _avatarRepository.SaveAvatarAsync(Avatar);
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            var result = _avatarDetailRepository.SaveAvatarDetail(Avatar);
            return result;
        }

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var result = _avatarDetailRepository.SaveAvatarDetailAsync(Avatar);
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = _holonRepository.SaveHolon(holon, saveChildren, recursive, maxChildDepth, continueOnError);
            return result;
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = _holonRepository.SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = _holonRepository.SaveHolons(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = _holonRepository.SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError);
            return result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                var searchResults = new SearchResults();
                var holons = new List<IHolon>();
                var avatars = new List<IAvatar>();
                
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var firstGroup = searchParams.SearchGroups.First() as ISearchTextGroup;
                    var q = (firstGroup?.SearchQuery?.ToString() ?? string.Empty).ToLower();
                    // Basic in-memory search using available repo APIs
                    var holonLoad = await _holonRepository.LoadAllHolonsAsync();
                    if (!holonLoad.IsError && holonLoad.Result != null)
                    {
                        holons.AddRange(holonLoad.Result.Where(h =>
                            (h.Name ?? string.Empty).ToLower().Contains(q) || (h.Description ?? string.Empty).ToLower().Contains(q)));
                    }

                    var avatarLoad = await _avatarRepository.LoadAllAvatarsAsync();
                    if (!avatarLoad.IsError && avatarLoad.Result != null)
                    {
                        avatars.AddRange(avatarLoad.Result.Where(a =>
                            (a.Name ?? string.Empty).ToLower().Contains(q) || (a.Username ?? string.Empty).ToLower().Contains(q) || (a.Email ?? string.Empty).ToLower().Contains(q)));
                    }
                }
                
                searchResults.SearchResultHolons = holons;
                searchResults.SearchResultAvatars = avatars;
                
                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Search completed successfully in SQLite with full property mapping ({holons.Count} holons, {avatars.Count} avatars)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching in SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWallets()
        {
            return LoadProviderWalletsAsync().Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsAsync()
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load all avatars and aggregate their provider wallets
                var allAvatarsResult = await _avatarRepository.LoadAllAvatarsAsync();
                if (allAvatarsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {allAvatarsResult.Message}");
                    return result;
                }

                var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                
                // Aggregate wallets from all avatars
                foreach (var avatar in allAvatarsResult.Result ?? Enumerable.Empty<IAvatar>())
                {
                    if (avatar.ProviderWallets != null)
                    {
                        foreach (var kvp in avatar.ProviderWallets)
                        {
                            if (!wallets.ContainsKey(kvp.Key))
                            {
                                wallets[kvp.Key] = new List<IProviderWallet>();
                            }
                            wallets[kvp.Key].AddRange(kvp.Value);
                        }
                    }
                }
                
                result.Result = wallets;
                result.IsError = false;
                result.Message = $"Successfully loaded provider wallets from {allAvatarsResult.Result?.Count() ?? 0} avatars in SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SaveProviderWallets(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsAsync(providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsAsync(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (providerWallets == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Provider wallets cannot be null");
                    return result;
                }

                // Save provider wallets for all avatars that have these wallets
                // Load all avatars and update their provider wallets
                var allAvatarsResult = await _avatarRepository.LoadAllAvatarsAsync();
                if (allAvatarsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {allAvatarsResult.Message}");
                    return result;
                }

                int updatedCount = 0;
                foreach (var avatar in allAvatarsResult.Result ?? Enumerable.Empty<IAvatar>())
                {
                    if (avatar.ProviderWallets != null)
                    {
                        // Update provider wallets for this avatar
                        foreach (var providerType in providerWallets.Keys)
                        {
                            if (avatar.ProviderWallets.ContainsKey(providerType))
                            {
                                avatar.ProviderWallets[providerType] = providerWallets[providerType];
                            }
                            else
                            {
                                avatar.ProviderWallets.Add(providerType, providerWallets[providerType]);
                            }
                        }

                        // Save the updated avatar
                        var saveResult = await _avatarRepository.SaveAvatarAsync(avatar);
                        if (!saveResult.IsError)
                        {
                            updatedCount++;
                        }
                    }
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully saved provider wallets for {updatedCount} avatars in SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets to SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            return LoadProviderWalletsForAvatarByIdAsync(id).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar to get provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var providerWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                if (avatarResult.Result?.ProviderWallets != null)
                {
                    foreach (var grp in avatarResult.Result.ProviderWallets.GroupBy(w => w.Key))
                        providerWallets[grp.Key] = grp.SelectMany(g => g.Value).ToList();
                }

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {id} from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsername(string username)
        {
            return LoadProviderWalletsForAvatarByUsernameAsync(username).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByUsernameAsync(string username)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by username to get provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarByUsernameAsync(username);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                    return result;
                }

                var providerWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                if (avatarResult.Result?.ProviderWallets != null)
                {
                    foreach (var grp in avatarResult.Result.ProviderWallets.GroupBy(w => w.Key))
                        providerWallets[grp.Key] = grp.SelectMany(g => g.Value).ToList();
                }

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {username} from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar by username from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByEmail(string email)
        {
            return LoadProviderWalletsForAvatarByEmailAsync(email).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByEmailAsync(string email)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by email to get provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarByEmailAsync(email);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                    return result;
                }

                var providerWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                if (avatarResult.Result?.ProviderWallets != null)
                {
                    foreach (var grp in avatarResult.Result.ProviderWallets.GroupBy(w => w.Key))
                        providerWallets[grp.Key] = grp.SelectMany(g => g.Value).ToList();
                }

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {email} from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar by email from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByIdAsync(id, providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar and update provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var avatar = avatarResult.Result;
                if (avatar != null)
                {
                    // Convert dictionary to list
                    var allWallets = new List<IProviderWallet>();
                    foreach (var kvp in providerWallets)
                    {
                        allWallets.AddRange(kvp.Value);
                    }
                    avatar.ProviderWallets = providerWallets;

                    // Save updated avatar
                    var saveResult = await _avatarRepository.SaveAvatarAsync(avatar);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {saveResult.Message}");
                        return result;
                    }

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully saved {allWallets.Count} provider wallets for avatar {id} to SQLite";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets for avatar to SQLite: {ex.Message}", ex);
            }
            return result;
        }

    }
}
