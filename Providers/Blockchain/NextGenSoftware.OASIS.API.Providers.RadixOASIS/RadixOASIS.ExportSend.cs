using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Oracle;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Extensions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

public partial class RadixOASIS
{
    public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
    }

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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await HolonManager.Instance.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version);
            }

            // Query holon by ID from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "get_holon_by_id",
                args = new[] { id.ToString(), version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var holonJson = response.Result.GetRawText();
                var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonJson);
                if (holon != null)
                {
                    // Load children if requested
                    if (loadChildren && (maxChildDepth == 0 || maxChildDepth > 0))
                    {
                        var childrenResult = await LoadHolonsForParentAsync(id, HolonType.All, loadChildren, recursive, maxChildDepth - 1, 0, continueOnError, loadChildrenFromProvider, version);
                        if (!childrenResult.IsError && childrenResult.Result != null)
                        {
                            holon.Children = childrenResult.Result.ToList();
                        }
                    }

                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Successfully loaded holon from Radix";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holon from Radix response");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holon from Radix: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await HolonManager.Instance.LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
            }

            // Query holon by provider key from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "get_holon_by_provider_key",
                args = new[] { providerKey, version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var holonJson = response.Result.GetRawText();
                var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonJson);
                if (holon != null)
                {
                    // Load children if requested
                    if (loadChildren && (maxChildDepth == 0 || maxChildDepth > 0))
                    {
                        var childrenResult = await LoadHolonsForParentAsync(holon.Id, HolonType.All, loadChildren, recursive, maxChildDepth - 1, 0, continueOnError, loadChildrenFromProvider, version);
                        if (!childrenResult.IsError && childrenResult.Result != null)
                        {
                            holon.Children = childrenResult.Result.ToList();
                        }
                    }

                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Successfully loaded holon by provider key from Radix";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holon from Radix response");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load holon by provider key from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await HolonManager.Instance.LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, curentChildDepth, HolonType.All, version, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
            }

            // Query holons for parent from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "get_holons_for_parent",
                args = new[] { id.ToString(), type.ToString(), version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var holons = new List<IHolon>();
                foreach (var holonElement in response.Result.EnumerateArray())
                {
                    var holonJson = holonElement.GetRawText();
                    var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonJson);
                    if (holon != null)
                    {
                        // Load children recursively if requested
                        if (loadChildren && recursive && (maxChildDepth == 0 || curentChildDepth < maxChildDepth))
                        {
                            var childrenResult = await LoadHolonsForParentAsync(holon.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth + 1, continueOnError, loadChildrenFromProvider, version);
                            if (!childrenResult.IsError && childrenResult.Result != null)
                            {
                                holon.Children = childrenResult.Result.ToList();
                            }
                        }
                        holons.Add(holon);
                    }
                }
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons for parent from Radix";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load holons for parent from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Radix: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await HolonManager.Instance.LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, curentChildDepth, HolonType.All, version, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
            }

            // First load the parent holon to get its ID
            var parentResult = await LoadHolonAsync(providerKey, false, false, 0, continueOnError, loadChildrenFromProvider, version);
            if (parentResult.IsError || parentResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load parent holon: {parentResult.Message}");
                return result;
            }

            // Then load children using the parent ID
            return await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await HolonManager.Instance.LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, curentChildDepth, HolonType.All, version, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
            }

            // Serialize metadata to JSON for query
            var metadataJson = System.Text.Json.JsonSerializer.Serialize(metaKeyValuePairs);
            var matchModeStr = metaKeyValuePairMatchMode.ToString();

            // Query holons by metadata from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "get_holons_by_metadata",
                args = new[] { metadataJson, matchModeStr, type.ToString(), version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var holons = new List<IHolon>();
                foreach (var holonElement in response.Result.EnumerateArray())
                {
                    var holonJson = holonElement.GetRawText();
                    var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonJson);
                    if (holon != null)
                    {
                        // Load children recursively if requested
                        if (loadChildren && recursive && (maxChildDepth == 0 || curentChildDepth < maxChildDepth))
                        {
                            var childrenResult = await LoadHolonsForParentAsync(holon.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth + 1, continueOnError, loadChildrenFromProvider, version);
                            if (!childrenResult.IsError && childrenResult.Result != null)
                            {
                                holon.Children = childrenResult.Result.ToList();
                            }
                        }
                        holons.Add(holon);
                    }
                }
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons by metadata from Radix";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load holons by metadata from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        // Convert single key-value pair to dictionary and use the main method
        var metaKeyValuePairs = new Dictionary<string, string> { { metaKey, metaValue } };
        return await LoadHolonsByMetaDataAsync(metaKeyValuePairs, MetaKeyValuePairMatchMode.All, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        // RadixOASIS focuses on bridge operations - delegate storage to ProviderManager
        return HolonManager.Instance.LoadHolonsByMetaData(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
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
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Radix provider: {activateResult.Message}");
                    return result;
                }
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await HolonManager.Instance.LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
            }

            // Query all holons from Radix OASIS blueprint component using Gateway API
            var url = $"{_config.HostUri}/state/entity/component/{Uri.EscapeDataString(_config.OasisBlueprintAddress)}";
            var queryData = new
            {
                network = _config.NetworkId == 1 ? "mainnet" : "stokenet",
                method = "get_all_holons",
                args = new[] { type.ToString(), version.ToString() }
            };

            var response = await HttpClientHelper.PostAsync<object, System.Text.Json.JsonElement>(
                _httpClient,
                url,
                queryData);

            if (!response.IsError && response.Result.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var holons = new List<IHolon>();
                foreach (var holonElement in response.Result.EnumerateArray())
                {
                    var holonJson = holonElement.GetRawText();
                    var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonJson);
                    if (holon != null)
                    {
                        // Load children recursively if requested
                        if (loadChildren && recursive && (maxChildDepth == 0 || curentChildDepth < maxChildDepth))
                        {
                            var childrenResult = await LoadHolonsForParentAsync(holon.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth + 1, continueOnError, loadChildrenFromProvider, version);
                            if (!childrenResult.IsError && childrenResult.Result != null)
                            {
                                holon.Children = childrenResult.Result.ToList();
                            }
                        }
                        holons.Add(holon);
                    }
                }
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons from Radix";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load holons from Radix: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons from Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

}
