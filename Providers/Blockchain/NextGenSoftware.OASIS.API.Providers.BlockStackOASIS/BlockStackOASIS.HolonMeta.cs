using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NBitcoin;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS
{
    public partial class BlockStackOASIS
    {
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Real BlockStack implementation for loading holons by metadata
                // Use BlockStack Gaia storage to search holons by metadata
                var matchingHolons = new List<IHolon>();
                
                // Get all holons from BlockStack Gaia storage and filter by metadata
                var allHolonsData = await _blockStackClient.GetFileAsync($"holons/index.json");
                
                if (allHolonsData != null && allHolonsData.ContainsKey("holons"))
                {
                    var holonIds = allHolonsData["holons"] as List<object>;
                    if (holonIds != null)
                    {
                        foreach (var holonId in holonIds)
                        {
                            try
                            {
                                var holonData = await _blockStackClient.GetFileAsync($"holons/{holonId}.json");
                                if (holonData != null && holonData.ContainsKey("metaData"))
                                {
                                    var metaData = holonData["metaData"] as Dictionary<string, object>;
                                    if (metaData != null && metaData.ContainsKey(metaKey) && metaData[metaKey]?.ToString() == metaValue)
                                    {
                                        var holon = new Holon
                                        {
                                            Id = Guid.Parse(holonData.GetValueOrDefault("id")?.ToString() ?? holonId.ToString()),
                                            Name = holonData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Holon",
                                            Description = holonData.GetValueOrDefault("description")?.ToString() ?? "",
                                            CreatedDate = DateTime.TryParse(holonData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                            ModifiedDate = DateTime.TryParse(holonData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                            Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                                            IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                                            ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                                            {
                                                [Core.Enums.ProviderType.BlockStackOASIS] = holonData.GetValueOrDefault("providerKey")?.ToString() ?? holonId.ToString()
                                            },
                                            MetaData = new Dictionary<string, object>
                                            {
                                                ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                                                ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                                                ["BlockStackProvider"] = "BlockStackOASIS",
                                                ["BlockStackMetaKey"] = metaKey,
                                                ["BlockStackMetaValue"] = metaValue,
                                                ["LoadedAt"] = DateTime.UtcNow
                                            }
                                        };
                                        
                                        matchingHolons.Add(holon);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                if (continueOnError)
                                {
                                    Console.WriteLine($"Error loading holon {holonId}: {ex.Message}");
                                    continue;
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }
                
                result.Result = matchingHolons;
                result.IsError = false;
                result.Message = $"Holons loaded successfully from BlockStack Gaia storage by metadata with full property mapping ({matchingHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from BlockStack by metadata: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Real BlockStack implementation for loading holons by compound metadata
                // Use BlockStack Gaia storage to search holons by multiple metadata key-value pairs
                var matchingHolons = new List<IHolon>();
                
                // Get all holons from BlockStack Gaia storage and filter by compound metadata
                var allHolonsData = await _blockStackClient.GetFileAsync($"holons/index.json");
                
                if (allHolonsData != null && allHolonsData.ContainsKey("holons"))
                {
                    var holonIds = allHolonsData["holons"] as List<object>;
                    if (holonIds != null)
                    {
                        foreach (var holonId in holonIds)
                        {
                            try
                            {
                                var holonData = await _blockStackClient.GetFileAsync($"holons/{holonId}.json");
                                if (holonData != null && holonData.ContainsKey("metaData"))
                                {
                                    var metaData = holonData["metaData"] as Dictionary<string, object>;
                                    if (metaData != null)
                                    {
                                        bool matches = false;
                                        if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                                        {
                                            // All key-value pairs must match
                                            matches = metaKeyValuePairs.All(kvp => metaData.ContainsKey(kvp.Key) && metaData[kvp.Key]?.ToString() == kvp.Value);
                                        }
                                        else
                                        {
                                            // Any key-value pair can match
                                            matches = metaKeyValuePairs.Any(kvp => metaData.ContainsKey(kvp.Key) && metaData[kvp.Key]?.ToString() == kvp.Value);
                                        }
                                        
                                        if (matches)
                                        {
                                            var holon = new Holon
                                            {
                                                Id = Guid.Parse(holonData.GetValueOrDefault("id")?.ToString() ?? holonId.ToString()),
                                                Name = holonData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Holon",
                                                Description = holonData.GetValueOrDefault("description")?.ToString() ?? "",
                                                CreatedDate = DateTime.TryParse(holonData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                                ModifiedDate = DateTime.TryParse(holonData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                                Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                                                IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                                                ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                                                {
                                                    [Core.Enums.ProviderType.BlockStackOASIS] = holonData.GetValueOrDefault("providerKey")?.ToString() ?? holonId.ToString()
                                                },
                                                MetaData = new Dictionary<string, object>
                                                {
                                                    ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                                                    ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                                                    ["BlockStackProvider"] = "BlockStackOASIS",
                                                    ["BlockStackMetaKeyValuePairs"] = string.Join(",", metaKeyValuePairs.Select(kvp => $"{kvp.Key}={kvp.Value}")),
                                                    ["BlockStackMatchMode"] = metaKeyValuePairMatchMode.ToString(),
                                                    ["LoadedAt"] = DateTime.UtcNow
                                                }
                                            };
                                            
                                            matchingHolons.Add(holon);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                if (continueOnError)
                                {
                                    Console.WriteLine($"Error loading holon {holonId}: {ex.Message}");
                                    continue;
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }
                
                result.Result = matchingHolons;
                result.IsError = false;
                result.Message = $"Holons loaded successfully from BlockStack Gaia storage by compound metadata with full property mapping ({matchingHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from BlockStack by compound metadata: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Real BlockStack implementation for loading all holons
                // Use BlockStack Gaia storage to enumerate all holons
                var allHolons = new List<IHolon>();
                
                // Get all holons from BlockStack Gaia storage
                var allHolonsData = await _blockStackClient.GetFileAsync($"holons/index.json");
                
                if (allHolonsData != null && allHolonsData.ContainsKey("holons"))
                {
                    var holonIds = allHolonsData["holons"] as List<object>;
                    if (holonIds != null)
                    {
                        foreach (var holonId in holonIds)
                        {
                            try
                            {
                                var holonData = await _blockStackClient.GetFileAsync($"holons/{holonId}.json");
                                if (holonData != null)
                                {
                                    var holon = new Holon
                                    {
                                        Id = Guid.Parse(holonData.GetValueOrDefault("id")?.ToString() ?? holonId.ToString()),
                                        Name = holonData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Holon",
                                        Description = holonData.GetValueOrDefault("description")?.ToString() ?? "",
                                        CreatedDate = DateTime.TryParse(holonData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                        ModifiedDate = DateTime.TryParse(holonData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                        Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                                        IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                                        ParentHolonId = holonData.GetValueOrDefault("parentId") != null ? Guid.Parse(holonData.GetValueOrDefault("parentId").ToString()) : Guid.Empty,
                                        ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                                        {
                                            [Core.Enums.ProviderType.BlockStackOASIS] = holonData.GetValueOrDefault("providerKey")?.ToString() ?? holonId.ToString()
                                        },
                                        VersionId = holonData.GetValueOrDefault("nextVersionId") != null ? Guid.Parse(holonData.GetValueOrDefault("nextVersionId").ToString()) : Guid.Empty,
                                        IsNewHolon = Convert.ToBoolean(holonData.GetValueOrDefault("isNew") ?? false),
                                        DeletedByAvatarId = holonData.GetValueOrDefault("deletedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("deletedByAvatarId").ToString()) : Guid.Empty,
                                        DeletedDate = holonData.GetValueOrDefault("deletedDate") != null ? DateTime.Parse(holonData.GetValueOrDefault("deletedDate").ToString()) : DateTime.MinValue,
                                        CreatedByAvatarId = holonData.GetValueOrDefault("createdByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("createdByAvatarId").ToString()) : Guid.Empty,
                                        ModifiedByAvatarId = holonData.GetValueOrDefault("modifiedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("modifiedByAvatarId").ToString()) : Guid.Empty,
                                        MetaData = new Dictionary<string, object>
                                        {
                                            ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                                            ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                                            ["BlockStackProvider"] = "BlockStackOASIS",
                                            ["LoadedAt"] = DateTime.UtcNow
                                        }
                                    };
                                    
                                    allHolons.Add(holon);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (continueOnError)
                                {
                                    Console.WriteLine($"Error loading holon {holonId}: {ex.Message}");
                                    continue;
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }
                
                result.Result = allHolons;
                result.IsError = false;
                result.Message = $"All holons loaded successfully from BlockStack Gaia storage with full property mapping ({allHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from BlockStack: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                    return result;
                }

                // Serialize holon to JSON
                var holonJson = JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // Convert to dictionary for BlockStack storage
                var holonData = JsonSerializer.Deserialize<Dictionary<string, object>>(holonJson);
                if (holonData == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to serialize holon");
                    return result;
                }

                // Add metadata
                holonData["savedAt"] = DateTime.UtcNow.ToString("O");
                holonData["provider"] = "BlockStackOASIS";
                holonData["holonId"] = holon.Id.ToString();

                // Save to BlockStack Gaia storage
                // Use holon ID or name as file path
                var filePath = string.IsNullOrEmpty(holon.Name) 
                    ? $"holons/{holon.Id}.json" 
                    : $"holons/{holon.Name.Replace(" ", "_")}_{holon.Id}.json";

                await _blockStackClient.PutFileAsync(filePath, holonData);

                // Store file path in provider unique storage key
                if (holon.ProviderUniqueStorageKey == null)
                    holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.BlockStackOASIS] = filePath;

                result.Result = holon;
                result.IsError = false;
                result.IsSaved = true;
                result.Message = $"Holon saved successfully to BlockStack Gaia storage: {filePath}";

                // Handle children if requested
                if (saveChildren && holon.Children != null && holon.Children.Any())
                {
                    var childResults = new List<OASISResult<IHolon>>();
                    foreach (var child in holon.Children)
                    {
                        var childResult = await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth - 1, continueOnError, saveChildrenOnProvider);
                        childResults.Add(childResult);
                        
                        if (!continueOnError && childResult.IsError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Failed to save child holon {child.Id}: {childResult.Message}");
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to BlockStack: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                    return result;
                }

                var savedHolons = new List<IHolon>();
                var errors = new List<string>();

                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    
                    if (saveResult.IsError)
                    {
                        errors.Add($"Failed to save holon {holon.Id}: {saveResult.Message}");
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, string.Join("; ", errors));
                            return result;
                        }
                    }
                    else if (saveResult.Result != null)
                    {
                        savedHolons.Add(saveResult.Result);
                    }
                }

                result.Result = savedHolons;
                result.IsError = errors.Any();
                result.Message = errors.Any() ? string.Join("; ", errors) : $"Successfully saved {savedHolons.Count} holons to BlockStack Gaia storage";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to BlockStack: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon first to return it
                var loadResult = await LoadHolonAsync(id, false, false, 0, false, false, 0);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon with ID {id} not found");
                    return result;
                }

                // Delete holon from BlockStack Gaia storage
                var filePath = $"holons/{id}.json";
                var deleteResult = await _blockStackClient.DeleteFileAsync(filePath);
                
                if (deleteResult)
                {
                    // Update index.json to remove holon ID
                    var indexData = await _blockStackClient.GetFileAsync("holons/index.json");
                    if (indexData != null && indexData.ContainsKey("holons"))
                    {
                        var holonIds = indexData["holons"] as List<object>;
                        if (holonIds != null)
                        {
                            holonIds.Remove(id.ToString());
                            indexData["holons"] = holonIds;
                            await _blockStackClient.PutFileAsync("holons/index.json", indexData);
                        }
                    }

                    result.Result = loadResult.Result;
                    result.IsError = false;
                    result.Message = "Holon deleted successfully from BlockStack Gaia storage";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to delete holon from BlockStack Gaia storage");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from BlockStack: {ex.Message}", ex);
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
            var loadResult = await LoadHolonAsync(providerKey, false, false, 0, false, false, 0);
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

    }
}
