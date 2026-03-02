using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.Interop;
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
        private readonly HotReloadManager _hotReloadManager;
        private readonly PerformanceCache _performanceCache;
        private readonly LibraryDependencyResolver _dependencyResolver;
        private readonly object _lockObject = new object();

        public LibraryInteropManager()
        {
            _providers = new Dictionary<InteropProviderType, ILibraryInteropProvider>();
            _loadedLibraries = new Dictionary<string, ILoadedLibrary>();
            _libraryProviderMap = new Dictionary<string, InteropProviderType>();
            _hotReloadManager = new HotReloadManager();
            _performanceCache = new PerformanceCache();
            _dependencyResolver = new LibraryDependencyResolver();
        }

        /// <summary>
        /// Gets the performance cache instance
        /// </summary>
        public PerformanceCache PerformanceCache => _performanceCache;

        /// <summary>
        /// Gets the dependency resolver instance
        /// </summary>
        public LibraryDependencyResolver DependencyResolver => _dependencyResolver;

        /// <summary>
        /// Gets the hot reload manager instance
        /// </summary>
        public HotReloadManager HotReloadManager => _hotReloadManager;

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

                // Resolve dependencies if specified in options
                var dependencies = options?.ContainsKey("Dependencies") == true
                    ? options["Dependencies"] as IEnumerable<string>
                    : null;

                if (dependencies != null)
                {
                    var libraryName = Path.GetFileNameWithoutExtension(libraryPath);
                    _dependencyResolver.RegisterLibrary("temp_" + Guid.NewGuid().ToString(), libraryName, dependencies);
                }

                // Load library
                var loadResult = await provider.LoadLibraryAsync(libraryPath, options);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                    return result;
                }

                var loadedLibrary = loadResult.Result;

                // Register with dependency resolver
                var libName = Path.GetFileNameWithoutExtension(libraryPath);
                _dependencyResolver.RegisterLibrary(loadedLibrary.LibraryId, libName, dependencies);

                // Cache loaded library
                lock (_lockObject)
                {
                    _loadedLibraries[loadedLibrary.LibraryId] = loadedLibrary;
                    _libraryProviderMap[loadedLibrary.LibraryId] = providerType.Value;
                }

                // Enable hot reload if requested
                if (options?.ContainsKey("EnableHotReload") == true && 
                    options["EnableHotReload"] is bool enableHotReload && 
                    enableHotReload)
                {
                    _hotReloadManager.EnableHotReload(
                        loadedLibrary.LibraryId,
                        libraryPath,
                        async (path) => await ReloadLibraryInternalAsync(loadedLibrary.LibraryId, path, providerType.Value));
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
            var stopwatch = Stopwatch.StartNew();
            var functionKey = $"{libraryId}:{functionName}";

            try
            {
                // Check cache first (if enabled)
                var cacheKey = PerformanceCache.GenerateCacheKey(libraryId, functionName, parameters);
                var cachedResult = _performanceCache.GetCached<T>(cacheKey);
                if (!cachedResult.IsError && cachedResult.Result != null)
                {
                    stopwatch.Stop();
                    _performanceCache.RecordInvocation(functionKey, stopwatch.Elapsed, true);
                    return cachedResult;
                }

                // Get provider for this library
                if (!_libraryProviderMap.TryGetValue(libraryId, out var providerType))
                {
                    OASISErrorHandling.HandleError(ref result, "Library not found or not loaded.");
                    stopwatch.Stop();
                    _performanceCache.RecordInvocation(functionKey, stopwatch.Elapsed, false);
                    return result;
                }

                if (!_providers.TryGetValue(providerType, out var provider))
                {
                    OASISErrorHandling.HandleError(ref result, $"Provider not found for library: {providerType}");
                    stopwatch.Stop();
                    _performanceCache.RecordInvocation(functionKey, stopwatch.Elapsed, false);
                    return result;
                }

                // Invoke function
                var invokeResult = await provider.InvokeAsync<T>(libraryId, functionName, parameters);
                stopwatch.Stop();
                
                // Record metrics
                _performanceCache.RecordInvocation(functionKey, stopwatch.Elapsed, !invokeResult.IsError);

                // Cache result if successful
                if (!invokeResult.IsError && invokeResult.Result != null)
                {
                    _performanceCache.CacheResult(cacheKey, invokeResult.Result);
                }

                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(invokeResult, result);
                if (!invokeResult.IsError)
                {
                    result.Result = invokeResult.Result;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _performanceCache.RecordInvocation(functionKey, stopwatch.Elapsed, false);
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
            var stopwatch = Stopwatch.StartNew();
            var functionKey = $"{libraryId}:{functionName}";

            try
            {
                // Check cache first (if enabled)
                var cacheKey = PerformanceCache.GenerateCacheKey(libraryId, functionName, parameters);
                var cachedResult = _performanceCache.GetCached<object>(cacheKey);
                if (!cachedResult.IsError && cachedResult.Result != null)
                {
                    stopwatch.Stop();
                    _performanceCache.RecordInvocation(functionKey, stopwatch.Elapsed, true);
                    return cachedResult;
                }

                // Get provider for this library
                if (!_libraryProviderMap.TryGetValue(libraryId, out var providerType))
                {
                    OASISErrorHandling.HandleError(ref result, "Library not found or not loaded.");
                    stopwatch.Stop();
                    _performanceCache.RecordInvocation(functionKey, stopwatch.Elapsed, false);
                    return result;
                }

                if (!_providers.TryGetValue(providerType, out var provider))
                {
                    OASISErrorHandling.HandleError(ref result, $"Provider not found for library: {providerType}");
                    stopwatch.Stop();
                    _performanceCache.RecordInvocation(functionKey, stopwatch.Elapsed, false);
                    return result;
                }

                // Invoke function
                var invokeResult = await provider.InvokeAsync(libraryId, functionName, parameters);
                stopwatch.Stop();
                
                // Record metrics
                _performanceCache.RecordInvocation(functionKey, stopwatch.Elapsed, !invokeResult.IsError);

                // Cache result if successful
                if (!invokeResult.IsError && invokeResult.Result != null)
                {
                    _performanceCache.CacheResult(cacheKey, invokeResult.Result);
                }

                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(invokeResult, result);
                if (!invokeResult.IsError)
                {
                    result.Result = invokeResult.Result;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _performanceCache.RecordInvocation(functionKey, stopwatch.Elapsed, false);
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
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(functionsResult, result);
                if (!functionsResult.IsError)
                {
                    result.Result = functionsResult.Result;
                }
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
            // Note: Most providers are auto-detected via SupportedExtensions above
            return extension switch
            {
                ".dll" => InteropProviderType.DotNet, // Could also be native on Windows
                ".so" => InteropProviderType.NativePInvoke, // Linux native
                ".dylib" => InteropProviderType.NativePInvoke, // macOS native
                ".py" => InteropProviderType.Python,
                ".js" or ".mjs" or ".cjs" => InteropProviderType.JavaScript,
                ".ts" or ".tsx" => InteropProviderType.JavaScript, // TypeScript
                ".wasm" => InteropProviderType.WebAssembly,
                ".jar" or ".class" => InteropProviderType.Java,
                ".kt" or ".kts" => InteropProviderType.Java, // Kotlin (JVM)
                ".scala" or ".sc" => InteropProviderType.Java, // Scala (JVM)
                ".groovy" or ".gvy" or ".gy" => InteropProviderType.Java, // Groovy (JVM)
                ".clj" or ".cljs" or ".cljc" => InteropProviderType.Java, // Clojure (JVM)
                ".go" => InteropProviderType.Go,
                ".rb" or ".ruby" => InteropProviderType.JavaScript, // Ruby
                ".php" => InteropProviderType.JavaScript, // PHP
                ".lua" => InteropProviderType.JavaScript, // Lua
                ".pl" or ".pm" or ".perl" => InteropProviderType.JavaScript, // Perl
                ".dart" => InteropProviderType.JavaScript, // Dart
                ".r" or ".R" => InteropProviderType.JavaScript, // R
                ".jl" => InteropProviderType.JavaScript, // Julia
                ".sh" or ".bash" or ".zsh" or ".fish" => InteropProviderType.JavaScript, // Shell Script
                ".ps1" or ".psm1" or ".psd1" => InteropProviderType.JavaScript, // PowerShell
                ".a" => InteropProviderType.NativePInvoke, // Static library
                ".lib" => InteropProviderType.NativePInvoke, // Windows static library
                ".proto" or ".grpc" => InteropProviderType.Grpc,
                _ => InteropProviderType.Auto
            };
        }

        /// <summary>
        /// Get library metadata including function signatures
        /// </summary>
        public async Task<OASISResult<ILibraryMetadata>> GetLibraryMetadataAsync(string libraryId)
        {
            var result = new OASISResult<ILibraryMetadata>();

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

                var metadataResult = await provider.GetLibraryMetadataAsync(libraryId);
                if (metadataResult.IsError)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(metadataResult, result);
                    return result;
                }

                // If metadata doesn't have function signatures, try to get them
                if (metadataResult.Result != null && 
                    (metadataResult.Result.FunctionSignatures == null || !metadataResult.Result.FunctionSignatures.Any()))
                {
                    var signaturesResult = await provider.GetFunctionSignaturesAsync(libraryId);
                    if (!signaturesResult.IsError && signaturesResult.Result != null)
                    {
                        // Create new metadata with signatures
                        var metadata = new Objects.Interop.LibraryMetadata
                        {
                            LibraryName = metadataResult.Result.LibraryName,
                            Version = metadataResult.Result.Version,
                            Description = metadataResult.Result.Description,
                            Author = metadataResult.Result.Author,
                            Language = metadataResult.Result.Language,
                            Framework = metadataResult.Result.Framework,
                            AvailableFunctions = metadataResult.Result.AvailableFunctions,
                            FunctionSignatures = signaturesResult.Result,
                            CustomProperties = metadataResult.Result.CustomProperties
                        };
                        result.Result = metadata;
                    }
                    else
                    {
                        result.Result = metadataResult.Result;
                    }
                }
                else
                {
                    result.Result = metadataResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting library metadata: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get function signatures for a library (parameter types, return types)
        /// Used for generating strongly-typed proxy methods
        /// </summary>
        public async Task<OASISResult<IEnumerable<IFunctionSignature>>> GetFunctionSignaturesAsync(string libraryId)
        {
            var result = new OASISResult<IEnumerable<IFunctionSignature>>();

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

                var signaturesResult = await provider.GetFunctionSignaturesAsync(libraryId);
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(signaturesResult, result);
                if (!signaturesResult.IsError)
                {
                    result.Result = signaturesResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting function signatures: {ex.Message}", ex);
            }

            return result;
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

        /// <summary>
        /// Reloads a library (internal method for hot reload)
        /// </summary>
        private async Task<OASISResult<bool>> ReloadLibraryInternalAsync(string libraryId, string libraryPath, InteropProviderType providerType)
        {
            var result = new OASISResult<bool>();

            try
            {
                // Unload existing library
                var unloadResult = await UnloadLibraryAsync(libraryId);
                if (unloadResult.IsError)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(unloadResult, result);
                    return result;
                }

                // Reload library
                var loadResult = await LoadLibraryAsync(libraryPath, providerType);
                if (loadResult.IsError)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                    return result;
                }

                result.Result = true;
                result.Message = $"Library {libraryId} reloaded successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error reloading library: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Manually reloads a library
        /// </summary>
        public async Task<OASISResult<bool>> ReloadLibraryAsync(string libraryId)
        {
            var result = new OASISResult<bool>();

            try
            {
                ILoadedLibrary library;
                InteropProviderType providerType;
                
                lock (_lockObject)
                {
                    if (!_loadedLibraries.TryGetValue(libraryId, out library))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not found.");
                        return result;
                    }

                    if (!_libraryProviderMap.TryGetValue(libraryId, out providerType))
                    {
                        OASISErrorHandling.HandleError(ref result, "Provider type not found for library.");
                        return result;
                    }
                }

                return await ReloadLibraryInternalAsync(libraryId, library.LibraryPath, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error reloading library: {ex.Message}", ex);
            }

            return result;
        }
    }
}

