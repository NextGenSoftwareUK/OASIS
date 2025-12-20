using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop
{
    /// <summary>
    /// Manages interoperable libraries from any language/framework
    /// Routes calls to appropriate interop providers for best performance
    /// </summary>
    public class LibraryInteropManager
    {
        private readonly Dictionary<InteropProviderType, ILibraryInteropProvider> _providers;
        private readonly Dictionary<string, ILoadedLibrary> _loadedLibraries;
        private readonly Dictionary<string, InteropProviderType> _libraryProviderMap;
        private readonly object _lockObject = new object();

        public LibraryInteropManager()
        {
            _providers = new Dictionary<InteropProviderType, ILibraryInteropProvider>();
            _loadedLibraries = new Dictionary<string, ILoadedLibrary>();
            _libraryProviderMap = new Dictionary<string, InteropProviderType>();
        }

        /// <summary>
        /// Register an interop provider
        /// </summary>
        public async Task<OASISResult<bool>> RegisterProviderAsync(ILibraryInteropProvider provider)
        {
            var result = new OASISResult<bool>();

            try
            {
                if (provider == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Provider cannot be null.");
                    return result;
                }

                // Initialize provider
                var initResult = await provider.InitializeAsync();
                if (initResult.IsError)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(initResult, result);
                    return result;
                }

                lock (_lockObject)
                {
                    _providers[provider.ProviderType] = provider;
                }

                result.Result = true;
                result.Message = $"Provider {provider.ProviderType} registered successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error registering provider: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Load a library (auto-detects provider type from file extension)
        /// </summary>
        public async Task<OASISResult<ILoadedLibrary>> LoadLibraryAsync(
            string libraryPath, 
            InteropProviderType? providerType = null,
            Dictionary<string, object> options = null)
        {
            var result = new OASISResult<ILoadedLibrary>();

            try
            {
                if (string.IsNullOrWhiteSpace(libraryPath) || !File.Exists(libraryPath))
                {
                    OASISErrorHandling.HandleError(ref result, "Library file not found.");
                    return result;
                }

                // Auto-detect provider type if not specified
                if (!providerType.HasValue)
                {
                    providerType = DetectProviderType(libraryPath);
                }

                if (!providerType.HasValue)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not detect provider type for library.");
                    return result;
                }

                // Get appropriate provider
                if (!_providers.TryGetValue(providerType.Value, out var provider))
                {
                    OASISErrorHandling.HandleError(ref result, $"No provider registered for type: {providerType.Value}");
                    return result;
                }

                // Load library
                var loadResult = await provider.LoadLibraryAsync(libraryPath, options);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                    return result;
                }

                var loadedLibrary = loadResult.Result;

                // Cache loaded library
                lock (_lockObject)
                {
                    _loadedLibraries[loadedLibrary.LibraryId] = loadedLibrary;
                    _libraryProviderMap[loadedLibrary.LibraryId] = providerType.Value;
                }

                result.Result = loadedLibrary;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading library: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Invoke a function in a loaded library
        /// </summary>
        public async Task<OASISResult<T>> InvokeAsync<T>(
            string libraryId, 
            string functionName, 
            params object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                // Get provider for this library
                if (!_libraryProviderMap.TryGetValue(libraryId, out var providerType))
                {
                    OASISErrorHandling.HandleError(ref result, "Library not found or not loaded.");
                    return result;
                }

                if (!_providers.TryGetValue(providerType, out var provider))
                {
                    OASISErrorHandling.HandleError(ref result, $"Provider not found for library: {providerType}");
                    return result;
                }

                // Invoke function
                var invokeResult = await provider.InvokeAsync<T>(libraryId, functionName, parameters);
                OASISResultHelper.CopyResult(invokeResult, result);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking function: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Invoke a function with dynamic return type
        /// </summary>
        public async Task<OASISResult<object>> InvokeAsync(
            string libraryId, 
            string functionName, 
            params object[] parameters)
        {
            var result = new OASISResult<object>();

            try
            {
                // Get provider for this library
                if (!_libraryProviderMap.TryGetValue(libraryId, out var providerType))
                {
                    OASISErrorHandling.HandleError(ref result, "Library not found or not loaded.");
                    return result;
                }

                if (!_providers.TryGetValue(providerType, out var provider))
                {
                    OASISErrorHandling.HandleError(ref result, $"Provider not found for library: {providerType}");
                    return result;
                }

                // Invoke function
                var invokeResult = await provider.InvokeAsync(libraryId, functionName, parameters);
                OASISResultHelper.CopyResult(invokeResult, result);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking function: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Unload a library
        /// </summary>
        public async Task<OASISResult<bool>> UnloadLibraryAsync(string libraryId)
        {
            var result = new OASISResult<bool>();

            try
            {
                if (!_libraryProviderMap.TryGetValue(libraryId, out var providerType))
                {
                    OASISErrorHandling.HandleError(ref result, "Library not found.");
                    return result;
                }

                if (!_providers.TryGetValue(providerType, out var provider))
                {
                    OASISErrorHandling.HandleError(ref result, $"Provider not found: {providerType}");
                    return result;
                }

                // Unload library
                var unloadResult = await provider.UnloadLibraryAsync(libraryId);
                if (unloadResult.IsError)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(unloadResult, result);
                    return result;
                }

                // Remove from cache
                lock (_lockObject)
                {
                    _loadedLibraries.Remove(libraryId);
                    _libraryProviderMap.Remove(libraryId);
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unloading library: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get available functions in a library
        /// </summary>
        public async Task<OASISResult<IEnumerable<string>>> GetAvailableFunctionsAsync(string libraryId)
        {
            var result = new OASISResult<IEnumerable<string>>();

            try
            {
                if (!_libraryProviderMap.TryGetValue(libraryId, out var providerType))
                {
                    OASISErrorHandling.HandleError(ref result, "Library not found.");
                    return result;
                }

                if (!_providers.TryGetValue(providerType, out var provider))
                {
                    OASISErrorHandling.HandleError(ref result, $"Provider not found: {providerType}");
                    return result;
                }

                var functionsResult = await provider.GetAvailableFunctionsAsync(libraryId);
                OASISResultHelper.CopyResult(functionsResult, result);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting functions: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Auto-detect provider type from file extension
        /// </summary>
        private InteropProviderType? DetectProviderType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            // Check all registered providers for supported extensions
            foreach (var provider in _providers.Values)
            {
                if (provider.SupportedExtensions != null && 
                    provider.SupportedExtensions.Any(ext => extension.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    return provider.ProviderType;
                }
            }

            // Default detection based on extension
            return extension switch
            {
                ".dll" => InteropProviderType.DotNet, // Could also be native on Windows
                ".so" => InteropProviderType.NativePInvoke, // Linux native
                ".dylib" => InteropProviderType.NativePInvoke, // macOS native
                ".py" => InteropProviderType.Python,
                ".js" => InteropProviderType.JavaScript,
                ".wasm" => InteropProviderType.WebAssembly,
                ".jar" => InteropProviderType.Java,
                ".a" => InteropProviderType.NativePInvoke, // Static library
                ".lib" => InteropProviderType.NativePInvoke, // Windows static library
                _ => InteropProviderType.Auto
            };
        }

        /// <summary>
        /// Get all loaded libraries
        /// </summary>
        public IEnumerable<ILoadedLibrary> GetLoadedLibraries()
        {
            lock (_lockObject)
            {
                return _loadedLibraries.Values.ToList();
            }
        }

        /// <summary>
        /// Check if library is loaded
        /// </summary>
        public bool IsLibraryLoaded(string libraryId)
        {
            lock (_lockObject)
            {
                return _loadedLibraries.ContainsKey(libraryId);
            }
        }
    }
}

