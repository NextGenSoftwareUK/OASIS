//using System.Text.Json;
//using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS
{
    public partial class LocalFileOASIS
    {
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                    return result;
                }

                // Ensure holon directory exists
                if (!Directory.Exists(_holonDirectory))
                    Directory.CreateDirectory(_holonDirectory);

                var holonFilePath = Path.Combine(_holonDirectory, $"{holon.Id}.json");
                var jsonContent = JsonConvert.SerializeObject(holon, Formatting.Indented);
                await File.WriteAllTextAsync(holonFilePath, jsonContent);

                // Save children if requested
                if (saveChildren && holon.Children != null && holon.Children.Any() && maxChildDepth > 0)
                {
                    foreach (var child in holon.Children)
                    {
                        var childResult = await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth - 1, continueOnError, saveChildrenOnProvider);
                        if (childResult.IsError && !continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Error saving child holon: {childResult.Message}");
                            return result;
                        }
                    }
                }

                result.Result = holon;
                result.IsError = false;
                result.IsSaved = true;
                result.Message = "Holon saved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                    return result;
                }

                var savedHolons = new List<IHolon>();
                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (saveResult.IsError)
                    {
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Error saving holon: {saveResult.Message}");
                            return result;
                        }
                    }
                    else
                    {
                        savedHolons.Add(saveResult.Result);
                    }
                }

                result.Result = savedHolons;
                result.IsError = false;
                result.IsSaved = true;
                result.Message = $"Saved {savedHolons.Count} holons successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonFilePath = Path.Combine(_holonDirectory, $"{id}.json");
                if (File.Exists(holonFilePath))
                {
                    var jsonContent = await File.ReadAllTextAsync(holonFilePath);
                    var holon = JsonConvert.DeserializeObject<Holon>(jsonContent);
                    
                    if (holon != null && holon.Version == version)
                    {
                        // Load children if requested
                        if (loadChildren && holon.Children != null && holon.Children.Any() && maxChildDepth > 0)
                        {
                            var loadedChildren = new List<IHolon>();
                            foreach (var child in holon.Children)
                            {
                                var childResult = await LoadHolonAsync(child.Id, loadChildren, recursive, maxChildDepth - 1, continueOnError, loadChildrenFromProvider, version);
                                if (!childResult.IsError && childResult.Result != null)
                                {
                                    loadedChildren.Add(childResult.Result);
                                }
                                else if (childResult.IsError && !continueOnError)
                                {
                                    OASISErrorHandling.HandleError(ref result, $"Error loading child holon: {childResult.Message}");
                                    return result;
                                }
                            }
                            holon.Children = loadedChildren;
                        }

                        result.Result = holon;
                        result.IsError = false;
                        result.IsLoaded = true;
                        result.Message = "Holon loaded successfully";
                    }
                    else
                    {
                        result.IsError = false;
                        result.IsLoaded = false;
                        result.Message = holon == null ? "Holon file corrupted" : $"Holon version {version} not found";
                    }
                }
                else
                {
                    result.IsError = false;
                    result.IsLoaded = false;
                    result.Message = "Holon file not found";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Search for holon by provider key in holon directory
                if (Directory.Exists(_holonDirectory))
                {
                    var jsonFiles = Directory.GetFiles(_holonDirectory, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var holon = JsonConvert.DeserializeObject<Holon>(jsonContent);
                            
                            if (holon != null && holon.ProviderUniqueStorageKey != null && 
                                holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.LocalFileOASIS) &&
                                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.LocalFileOASIS] == providerKey &&
                                holon.Version == version)
                            {
                                // Load children if requested
                                if (loadChildren && holon.Children != null && holon.Children.Any() && maxChildDepth > 0)
                                {
                                    var loadedChildren = new List<IHolon>();
                                    foreach (var child in holon.Children)
                                    {
                                        var childResult = await LoadHolonAsync(child.Id, loadChildren, recursive, maxChildDepth - 1, continueOnError, loadChildrenFromProvider, version);
                                        if (!childResult.IsError && childResult.Result != null)
                                        {
                                            loadedChildren.Add(childResult.Result);
                                        }
                                        else if (childResult.IsError && !continueOnError)
                                        {
                                            OASISErrorHandling.HandleError(ref result, $"Error loading child holon: {childResult.Message}");
                                            return result;
                                        }
                                    }
                                    holon.Children = loadedChildren;
                                }

                                result.Result = holon;
                                result.IsError = false;
                                result.IsLoaded = true;
                                result.Message = "Holon loaded successfully by provider key";
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Continue searching other files
                            LoggingManager.Log($"Error reading holon file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.IsError = false;
                result.IsLoaded = false;
                result.Message = "Holon not found by provider key";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load parent holon first
                var parentResult = await LoadHolonAsync(id, false, false, 0, continueOnError, loadChildrenFromProvider, version);
                if (parentResult.IsError || parentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading parent holon: {parentResult.Message}");
                    return result;
                }

                // Get children from parent holon
                var children = parentResult.Result.Children ?? new List<IHolon>();
                
                // Filter by type if specified
                if (type != HolonType.All)
                {
                    children = children.Where(c => c.HolonType == type).ToList();
                }

                // Load children recursively if requested
                if (loadChildren && children.Any() && maxChildDepth > curentChildDepth)
                {
                    var loadedChildren = new List<IHolon>();
                    foreach (var child in children)
                    {
                        var childResult = await LoadHolonAsync(child.Id, loadChildren, recursive, maxChildDepth - curentChildDepth - 1, continueOnError, loadChildrenFromProvider, version);
                        if (!childResult.IsError && childResult.Result != null)
                        {
                            loadedChildren.Add(childResult.Result);
                        }
                        else if (childResult.IsError && !continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Error loading child holon: {childResult.Message}");
                            return result;
                        }
                    }
                    children = loadedChildren;
                }

                result.Result = children;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Loaded {children.Count()} holons for parent";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // Load parent holon by provider key first
            var parentResult = await LoadHolonAsync(providerKey, false, false, 0, continueOnError, loadChildrenFromProvider, version);
            if (parentResult.IsError || parentResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref result, $"Error loading parent holon by provider key: {parentResult.Message}");
                return result;
            }

            // Use the parent's ID to load children
            return await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                var matchingHolons = new List<IHolon>();
                
                if (Directory.Exists(_holonDirectory))
                {
                    var jsonFiles = Directory.GetFiles(_holonDirectory, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var holon = JsonConvert.DeserializeObject<Holon>(jsonContent);
                            
                            if (holon != null && holon.Version == version &&
                                (type == HolonType.All || holon.HolonType == type) &&
                                holon.MetaData != null && holon.MetaData.ContainsKey(metaKey) &&
                                holon.MetaData[metaKey]?.ToString() == metaValue)
                            {
                                // Load children if requested
                                if (loadChildren && holon.Children != null && holon.Children.Any() && maxChildDepth > curentChildDepth)
                                {
                                    var loadedChildren = new List<IHolon>();
                                    foreach (var child in holon.Children)
                                    {
                                        var childResult = await LoadHolonAsync(child.Id, loadChildren, recursive, maxChildDepth - curentChildDepth - 1, continueOnError, loadChildrenFromProvider, version);
                                        if (!childResult.IsError && childResult.Result != null)
                                        {
                                            loadedChildren.Add(childResult.Result);
                                        }
                                        else if (childResult.IsError && !continueOnError)
                                        {
                                            OASISErrorHandling.HandleError(ref result, $"Error loading child holon: {childResult.Message}");
                                            return result;
                                        }
                                    }
                                    holon.Children = loadedChildren;
                                }

                                matchingHolons.Add(holon);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!continueOnError)
                            {
                                OASISErrorHandling.HandleError(ref result, $"Error reading holon file {file}: {ex.Message}", ex);
                                return result;
                            }
                            LoggingManager.Log($"Error reading holon file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.Result = matchingHolons;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Loaded {matchingHolons.Count} holons by metadata";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                var matchingHolons = new List<IHolon>();
                
                if (Directory.Exists(_holonDirectory))
                {
                    var jsonFiles = Directory.GetFiles(_holonDirectory, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var holon = JsonConvert.DeserializeObject<Holon>(jsonContent);
                            
                            if (holon != null && holon.Version == version &&
                                (type == HolonType.All || holon.HolonType == type) &&
                                holon.MetaData != null)
                            {
                                bool matches = false;
                                if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                                {
                                    matches = metaKeyValuePairs.All(kvp => holon.MetaData.ContainsKey(kvp.Key) && holon.MetaData[kvp.Key]?.ToString() == kvp.Value);
                                }
                                else // Or
                                {
                                    matches = metaKeyValuePairs.Any(kvp => holon.MetaData.ContainsKey(kvp.Key) && holon.MetaData[kvp.Key]?.ToString() == kvp.Value);
                                }

                                if (matches)
                                {
                                    // Load children if requested
                                    if (loadChildren && holon.Children != null && holon.Children.Any() && maxChildDepth > curentChildDepth)
                                    {
                                        var loadedChildren = new List<IHolon>();
                                        foreach (var child in holon.Children)
                                        {
                                            var childResult = await LoadHolonAsync(child.Id, loadChildren, recursive, maxChildDepth - curentChildDepth - 1, continueOnError, loadChildrenFromProvider, version);
                                            if (!childResult.IsError && childResult.Result != null)
                                            {
                                                loadedChildren.Add(childResult.Result);
                                            }
                                            else if (childResult.IsError && !continueOnError)
                                            {
                                                OASISErrorHandling.HandleError(ref result, $"Error loading child holon: {childResult.Message}");
                                                return result;
                                            }
                                        }
                                        holon.Children = loadedChildren;
                                    }

                                    matchingHolons.Add(holon);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!continueOnError)
                            {
                                OASISErrorHandling.HandleError(ref result, $"Error reading holon file {file}: {ex.Message}", ex);
                                return result;
                            }
                            LoggingManager.Log($"Error reading holon file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.Result = matchingHolons;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Loaded {matchingHolons.Count} holons by metadata pairs";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs: {ex.Message}", ex);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holons = new List<IHolon>();
                
                if (Directory.Exists(_holonDirectory))
                {
                    var jsonFiles = Directory.GetFiles(_holonDirectory, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var holon = JsonConvert.DeserializeObject<Holon>(jsonContent);
                            
                            if (holon != null && holon.Version == version &&
                                (type == HolonType.All || holon.HolonType == type))
                            {
                                // Load children if requested
                                if (loadChildren && holon.Children != null && holon.Children.Any() && maxChildDepth > curentChildDepth)
                                {
                                    var loadedChildren = new List<IHolon>();
                                    foreach (var child in holon.Children)
                                    {
                                        var childResult = await LoadHolonAsync(child.Id, loadChildren, recursive, maxChildDepth - curentChildDepth - 1, continueOnError, loadChildrenFromProvider, version);
                                        if (!childResult.IsError && childResult.Result != null)
                                        {
                                            loadedChildren.Add(childResult.Result);
                                        }
                                        else if (childResult.IsError && !continueOnError)
                                        {
                                            OASISErrorHandling.HandleError(ref result, $"Error loading child holon: {childResult.Message}");
                                            return result;
                                        }
                                    }
                                    holon.Children = loadedChildren;
                                }

                                holons.Add(holon);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!continueOnError)
                            {
                                OASISErrorHandling.HandleError(ref result, $"Error reading holon file {file}: {ex.Message}", ex);
                                return result;
                            }
                            LoggingManager.Log($"Error reading holon file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Loaded {holons.Count} holons";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

    }
}
