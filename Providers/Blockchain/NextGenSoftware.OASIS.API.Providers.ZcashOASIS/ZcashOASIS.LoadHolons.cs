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
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Services.Zcash;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Models;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS
{
    public partial class ZcashOASIS
    {
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

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

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                // For Zcash, we would query all holons from the blockchain
                // This is a simplified implementation - in production would query Zcash for all holon transactions
                // For now, return empty list as Zcash doesn't have a direct "get all" method
                // In production, would:
                // 1. Query Zcash for all transactions with holon metadata
                // 2. Decrypt shielded transaction memos
                // 3. Deserialize holon data
                // 4. Filter by type if needed
                
                result.Result = new List<IHolon>();
                result.IsError = false;
                result.Message = "LoadAllHolons: Zcash requires querying blockchain transactions (simplified implementation)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                var savedHolon = await _zcashRepository.SaveHolonAsync(holon);
                result.Result = savedHolon;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
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
            var savedHolons = new List<IHolon>();
            foreach (var holon in holons)
            {
                var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (saveResult.IsError && !continueOnError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }
                if (!saveResult.IsError && saveResult.Result != null)
                {
                    savedHolons.Add(saveResult.Result);
                }
            }
            result.Result = savedHolons;
            result.IsError = false;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon first
                var holonResult = await LoadHolonAsync(id);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found");
                    return result;
                }

                // For Zcash, deletion would involve marking the holon as deleted in metadata
                // and creating a new transaction indicating deletion
                // For now, mark as deleted in metadata
                holonResult.Result.MetaData = holonResult.Result.MetaData ?? new Dictionary<string, object>();
                holonResult.Result.MetaData["Deleted"] = true;
                holonResult.Result.MetaData["DeletedDate"] = DateTime.UtcNow.ToString("o");

                // Save updated holon
                var saveResult = await SaveHolonAsync(holonResult.Result);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }

                result.Result = saveResult.Result;
                result.IsError = false;
                result.Message = "Holon marked as deleted in Zcash";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon by provider key first
                var holonResult = await LoadHolonAsync(providerKey);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found by provider key");
                    return result;
                }

                // Mark as deleted
                holonResult.Result.MetaData = holonResult.Result.MetaData ?? new Dictionary<string, object>();
                holonResult.Result.MetaData["Deleted"] = true;
                holonResult.Result.MetaData["DeletedDate"] = DateTime.UtcNow.ToString("o");

                // Save updated holon
                var saveResult = await SaveHolonAsync(holonResult.Result);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }

                result.Result = saveResult.Result;
                result.IsError = false;
                result.Message = "Holon marked as deleted in Zcash by provider key";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons collection is null");
                    return result;
                }

                // Save all holons
                var saveResult = await SaveHolonsAsync(holons);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Imported {saveResult.Result?.Count() ?? 0} holons to Zcash";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar
                var avatarResult = await LoadAvatarAsync(avatarId, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                // Load all holons for this avatar (as parent)
                var holonsResult = await LoadHolonsForParentAsync(avatarId);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, holonsResult.Message);
                    return result;
                }

                // Combine avatar and holons
                var allData = new List<IHolon> { avatarResult.Result as IHolon };
                if (holonsResult.Result != null)
                {
                    allData.AddRange(holonsResult.Result);
                }

                result.Result = allData;
                result.IsError = false;
                result.Message = $"Exported {allData.Count} holons for avatar";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Zcash provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by username
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
                    return result;
                }

                // Load all holons for this avatar
                var holonsResult = await LoadHolonsForParentAsync(avatarResult.Result.Id);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, holonsResult.Message);
                    return result;
                }

                // Combine avatar and holons
                var allData = new List<IHolon> { avatarResult.Result as IHolon };
                if (holonsResult.Result != null)
                {
                    allData.AddRange(holonsResult.Result);
                }

                result.Result = allData;
                result.IsError = false;
                result.Message = $"Exported {allData.Count} holons for avatar by username";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

    }
}
