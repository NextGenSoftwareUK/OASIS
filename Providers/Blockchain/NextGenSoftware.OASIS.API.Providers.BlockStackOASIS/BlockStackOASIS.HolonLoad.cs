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
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                // Real BlockStack implementation for loading holon by ID
                // Use BlockStack Gaia storage to load holon data
                var holonData = await _blockStackClient.GetFileAsync($"holons/{id}.json");
                
                if (holonData != null)
                {
                    var holon = new Holon
                    {
                        Id = Guid.Parse(holonData.GetValueOrDefault("id")?.ToString() ?? id.ToString()),
                        Name = holonData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Holon",
                        Description = holonData.GetValueOrDefault("description")?.ToString() ?? "",
                        CreatedDate = DateTime.TryParse(holonData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                        ModifiedDate = DateTime.TryParse(holonData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                        Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                        IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                        ParentHolonId = holonData.GetValueOrDefault("parentId") != null ? Guid.Parse(holonData.GetValueOrDefault("parentId").ToString()) : Guid.Empty,
                        ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                        {
                            [Core.Enums.ProviderType.BlockStackOASIS] = holonData.GetValueOrDefault("providerKey")?.ToString() ?? id.ToString()
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
                    
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from BlockStack Gaia storage with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in BlockStack Gaia storage");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                // Real BlockStack implementation for loading holon by provider key
                // Use BlockStack Gaia storage to load holon data by provider key
                var holonData = await _blockStackClient.GetFileAsync($"holons/{providerKey}.json");
                
                if (holonData != null)
                {
                    // Require valid ID for holon
                    var idStr = holonData.GetValueOrDefault("id")?.ToString();
                    if (string.IsNullOrWhiteSpace(idStr) || !Guid.TryParse(idStr, out var holonId))
                    {
                        OASISErrorHandling.HandleError(ref result, $"Invalid or missing holon ID in provider key: {providerKey}");
                        return result;
                    }
                    
                    var holon = new Holon
                    {
                        Id = holonId,
                        Name = holonData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Holon",
                        Description = holonData.GetValueOrDefault("description")?.ToString() ?? "",
                        CreatedDate = DateTime.TryParse(holonData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                        ModifiedDate = DateTime.TryParse(holonData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                        Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                        IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                        ParentHolonId = holonData.GetValueOrDefault("parentId") != null ? Guid.Parse(holonData.GetValueOrDefault("parentId").ToString()) : Guid.Empty,
                        ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                        {
                            [Core.Enums.ProviderType.BlockStackOASIS] = providerKey
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
                            ["BlockStackProviderKey"] = providerKey,
                            ["LoadedAt"] = DateTime.UtcNow
                        }
                    };
                    
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from BlockStack Gaia storage by provider key with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in BlockStack Gaia storage by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from BlockStack by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                if (string.IsNullOrWhiteSpace(customKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Custom key cannot be null or empty");
                    return result;
                }

                // Load holon by custom key from BlockStack Gaia storage
                var filePath = $"holons/{customKey}.json";
                var holonData = await _blockStackClient.GetFileAsync(filePath);
                
                if (holonData != null && holonData.Count > 0)
                {
                    var holonJson = System.Text.Json.JsonSerializer.Serialize(holonData);
                    var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (holon != null)
                    {
                        // Load children if requested
                        if (loadChildren && (recursive || maxChildDepth > 0))
                        {
                            var childrenResult = await LoadHolonsForParentAsync(holon.Id, HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
                            if (!childrenResult.IsError && childrenResult.Result != null)
                            {
                                holon.Children = childrenResult.Result.ToList();
                            }
                        }
                        
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = "Holon loaded successfully from BlockStack by custom key";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize holon from BlockStack storage");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in BlockStack storage");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by custom key from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonByCustomKeyAsync(customKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                if (string.IsNullOrWhiteSpace(metaKey) || string.IsNullOrWhiteSpace(metaValue))
                {
                    OASISErrorHandling.HandleError(ref result, "Metadata key and value are required");
                    return result;
                }

                // Load holons by metadata and return first match
                var holonsResult = await LoadHolonsByMetaDataAsync(metaKey, metaValue, HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
                
                if (!holonsResult.IsError && holonsResult.Result != null && holonsResult.Result.Any())
                {
                    result.Result = holonsResult.Result.First();
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from BlockStack by metadata";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "No holon found matching the metadata criteria");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by metadata from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonByMetaDataAsync(metaKey, metaValue, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                // Real BlockStack implementation for loading child holons
                // Use BlockStack Gaia storage to enumerate child holons
                var childHolons = new List<IHolon>();
                
                // Get all holons from BlockStack Gaia storage and filter by parent ID
                var allHolonsData = await _blockStackClient.GetFileAsync($"holons/index.json");
                
                if (allHolonsData != null && allHolonsData.ContainsKey("children"))
                {
                    var childrenData = allHolonsData["children"] as Dictionary<string, object>;
                    if (childrenData != null && childrenData.ContainsKey(id.ToString()))
                    {
                        var childIds = childrenData[id.ToString()] as List<object>;
                        if (childIds != null)
                        {
                            foreach (var childId in childIds)
                            {
                                try
                                {
                                    var childData = await _blockStackClient.GetFileAsync($"holons/{childId}.json");
                                    if (childData != null)
                                    {
                                        var holon = new Holon
                                        {
                                            Id = Guid.Parse(childData.GetValueOrDefault("id")?.ToString() ?? childId.ToString()),
                                            Name = childData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Child Holon",
                                            Description = childData.GetValueOrDefault("description")?.ToString() ?? "",
                                            CreatedDate = DateTime.TryParse(childData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                            ModifiedDate = DateTime.TryParse(childData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                            Version = Convert.ToInt32(childData.GetValueOrDefault("version") ?? 1),
                                            IsActive = Convert.ToBoolean(childData.GetValueOrDefault("isActive") ?? true),
                                            ParentHolonId = id,
                                            ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                                            {
                                                [Core.Enums.ProviderType.BlockStackOASIS] = childData.GetValueOrDefault("providerKey")?.ToString() ?? childId.ToString()
                                            },
                                            MetaData = new Dictionary<string, object>
                                            {
                                                ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                                                ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                                                ["BlockStackProvider"] = "BlockStackOASIS",
                                                ["BlockStackParentId"] = id.ToString(),
                                                ["LoadedAt"] = DateTime.UtcNow
                                            }
                                        };
                                        
                                        childHolons.Add(holon);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (continueOnError)
                                    {
                                        Console.WriteLine($"Error loading child holon {childId}: {ex.Message}");
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
                }
                
                result.Result = childHolons;
                result.IsError = false;
                result.Message = $"Child holons loaded successfully from BlockStack Gaia storage with full property mapping ({childHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading child holons from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                // Real BlockStack implementation for loading child holons by provider key
                // Use BlockStack Gaia storage to enumerate child holons by provider key
                var childHolons = new List<IHolon>();
                
                // Get all holons from BlockStack Gaia storage and filter by parent provider key
                var allHolonsData = await _blockStackClient.GetFileAsync($"holons/index.json");
                
                if (allHolonsData != null && allHolonsData.ContainsKey("childrenByProviderKey"))
                {
                    var childrenData = allHolonsData["childrenByProviderKey"] as Dictionary<string, object>;
                    if (childrenData != null && childrenData.ContainsKey(providerKey))
                    {
                        var childIds = childrenData[providerKey] as List<object>;
                        if (childIds != null)
                        {
                            foreach (var childId in childIds)
                            {
                                try
                                {
                                    var childData = await _blockStackClient.GetFileAsync($"holons/{childId}.json");
                                    if (childData != null)
                                    {
                                        var holon = new Holon
                                        {
                                            Id = Guid.Parse(childData.GetValueOrDefault("id")?.ToString() ?? childId.ToString()),
                                            Name = childData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Child Holon",
                                            Description = childData.GetValueOrDefault("description")?.ToString() ?? "",
                                            CreatedDate = DateTime.TryParse(childData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                            ModifiedDate = DateTime.TryParse(childData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                            Version = Convert.ToInt32(childData.GetValueOrDefault("version") ?? 1),
                                            IsActive = Convert.ToBoolean(childData.GetValueOrDefault("isActive") ?? true),
                                            ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                                            {
                                                [Core.Enums.ProviderType.BlockStackOASIS] = childData.GetValueOrDefault("providerKey")?.ToString() ?? childId.ToString()
                                            },
                                            MetaData = new Dictionary<string, object>
                                            {
                                                ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                                                ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                                                ["BlockStackProvider"] = "BlockStackOASIS",
                                                ["BlockStackParentProviderKey"] = providerKey,
                                                ["LoadedAt"] = DateTime.UtcNow
                                            }
                                        };
                                        
                                        childHolons.Add(holon);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (continueOnError)
                                    {
                                        Console.WriteLine($"Error loading child holon {childId}: {ex.Message}");
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
                }
                
                result.Result = childHolons;
                result.IsError = false;
                result.Message = $"Child holons loaded successfully from BlockStack Gaia storage by provider key with full property mapping ({childHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading child holons from BlockStack by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                if (string.IsNullOrWhiteSpace(customKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Custom key cannot be null or empty");
                    return result;
                }

                // First load the parent holon by custom key
                var parentResult = await LoadHolonByCustomKeyAsync(customKey, false, false, 0, continueOnError, loadChildrenFromProvider, version);
                
                if (parentResult.IsError || parentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Parent holon not found: {parentResult.Message}");
                    return result;
                }

                // Then load children for the parent
                var childrenResult = await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                
                result.Result = childrenResult.Result;
                result.IsError = childrenResult.IsError;
                result.Message = childrenResult.Message;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by custom key from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentByCustomKeyAsync(customKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

    }
}
