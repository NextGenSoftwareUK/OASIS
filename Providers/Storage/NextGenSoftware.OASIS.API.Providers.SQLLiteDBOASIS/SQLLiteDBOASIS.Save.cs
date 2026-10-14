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

        public OASISResult<bool> SaveProviderWalletsForAvatarByUsername(string username, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByUsernameAsync(username, providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByUsernameAsync(string username, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}", activateResult.Exception);
                        result.ErrorCode = activateResult.ErrorCode;
                        return result;
                    }
                }

                // Load avatar by username and update provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarByUsernameAsync(username);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}", avatarResult.Exception);
                    result.ErrorCode = avatarResult.ErrorCode;
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
                    var saveResult = await StoreLocalWalletsAsync(avatar.Id, providerWallets);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving local wallets: {saveResult.Message}", saveResult.Exception);
                        result.ErrorCode = saveResult.ErrorCode;
                        return result;
                    }

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully saved {allWallets.Count} provider wallets for avatar {username} to SQLite";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets for avatar by username to SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarByEmail(string email, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByEmailAsync(email, providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByEmailAsync(string email, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}", activateResult.Exception);
                        result.ErrorCode = activateResult.ErrorCode;
                        return result;
                    }
                }

                // Load avatar by email and update provider wallets
                var avatarResult = await _avatarRepository.LoadAvatarByEmailAsync(email);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}", avatarResult.Exception);
                    result.ErrorCode = avatarResult.ErrorCode;
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
                    var saveResult = await StoreLocalWalletsAsync(avatar.Id, providerWallets);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving local wallets: {saveResult.Message}", saveResult.Exception);
                        result.ErrorCode = saveResult.ErrorCode;
                        return result;
                    }

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully saved {allWallets.Count} provider wallets for avatar {email} to SQLite";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets for avatar by email to SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        // removed duplicate earlier Import/Export methods to resolve CS0111

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                var importedCount = 0;
                foreach (var holon in holons)
                {
                    var saveResult = await _holonRepository.SaveHolonAsync(holon);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error importing holon {holon.Id}: {saveResult.Message}");
                        return result;
                    }
                    importedCount++;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {importedCount} holons to SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Fallback: load all and filter by CreatedByAvatarId
                var allHolons = await _holonRepository.LoadAllHolonsAsync(version: version);
                if (allHolons.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading export data: {allHolons.Message}", allHolons.Exception);
                    result.ErrorCode = allHolons.ErrorCode;
                    return result;
                }
                var filtered = allHolons.Result?.Where(h => h.CreatedByAvatarId == avatarId) ?? Enumerable.Empty<IHolon>();
                result.Result = filtered;
                result.IsError = false;
                result.Message = $"Successfully exported {result.Result.Count()} holons for avatar {avatarId} from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by ID from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Fallback: load all and filter by CreatedByAvatarId (using CreatedByAvatar property)
                var allHolons = await _holonRepository.LoadAllHolonsAsync(version: version);
                if (allHolons.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading export data: {allHolons.Message}", allHolons.Exception);
                    result.ErrorCode = allHolons.ErrorCode;
                    return result;
                }
                var owner = await _avatarRepository.LoadAvatarByUsernameAsync(avatarUsername);
                if (owner.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error resolving export owner: {owner.Message}", owner.Exception);
                    result.ErrorCode = owner.ErrorCode;
                    return result;
                }
                var filtered = owner.Result == null ? Enumerable.Empty<IHolon>()
                    : allHolons.Result?.Where(h => h.CreatedByAvatarId == owner.Result.Id) ?? Enumerable.Empty<IHolon>();
                result.Result = filtered;
                result.IsError = false;
                result.Message = $"Successfully exported {result.Result.Count()} holons for avatar {avatarUsername} from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Fallback: load all and filter by CreatedByAvatarId (using CreatedByAvatar property)
                var allHolons = await _holonRepository.LoadAllHolonsAsync(version: version);
                if (allHolons.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading export data: {allHolons.Message}", allHolons.Exception);
                    result.ErrorCode = allHolons.ErrorCode;
                    return result;
                }
                var owner = await _avatarRepository.LoadAvatarByEmailAsync(avatarEmailAddress);
                if (owner.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error resolving export owner: {owner.Message}", owner.Exception);
                    result.ErrorCode = owner.ErrorCode;
                    return result;
                }
                var filtered = owner.Result == null ? Enumerable.Empty<IHolon>()
                    : allHolons.Result?.Where(h => h.CreatedByAvatarId == owner.Result.Id) ?? Enumerable.Empty<IHolon>();
                result.Result = filtered;
                result.IsError = false;
                result.Message = $"Successfully exported {result.Result.Count()} holons for avatar {avatarEmailAddress} from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons via current repository API
                var holons = await _holonRepository.LoadAllHolonsAsync();
                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Result?.Count() ?? 0} holons from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons by metadata from SQLite - using LoadAllHolons and filter
                var allHolons = await _holonRepository.LoadAllHolonsAsync(type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,version);
                var holons = new OASISResult<IEnumerable<IHolon>>();
                if (allHolons.IsError)
                {
                    holons.IsError = true;
                    holons.Message = allHolons.Message;
                }
                else
                {
                    var filtered = allHolons.Result?.Where(h => h.MetaData != null && h.MetaData.ContainsKey(metaKey) && h.MetaData[metaKey]?.ToString() == metaValue) ?? Enumerable.Empty<IHolon>();
                    holons.Result = filtered;
                    holons.IsError = false;
                }
                if (holons.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata: {holons.Message}");
                    return result;
                }

                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Result?.Count() ?? 0} holons by metadata from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from SQLite: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SQLite provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons by multiple metadata pairs from SQLite - using LoadAllHolons and filter
                var allHolons = await _holonRepository.LoadAllHolonsAsync(type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,version);
                var holons = new OASISResult<IEnumerable<IHolon>>();
                if (allHolons.IsError)
                {
                    holons.IsError = true;
                    holons.Message = allHolons.Message;
                }
                else
                {
                    var filtered = allHolons.Result?.Where(h => 
                    {
                        if (h.MetaData == null) return false;
                        
                        if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                        {
                            return metaKeyValuePairs.All(kvp => h.MetaData.ContainsKey(kvp.Key) && h.MetaData[kvp.Key]?.ToString() == kvp.Value);
                        }
                        else // Or
                        {
                            return metaKeyValuePairs.Any(kvp => h.MetaData.ContainsKey(kvp.Key) && h.MetaData[kvp.Key]?.ToString() == kvp.Value);
                        }
                    }) ?? Enumerable.Empty<IHolon>();
                    holons.Result = filtered;
                    holons.IsError = false;
                }
                if (holons.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs: {holons.Message}");
                    return result;
                }

                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Result?.Count() ?? 0} holons by metadata pairs from SQLite";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from SQLite: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }
    }
}
