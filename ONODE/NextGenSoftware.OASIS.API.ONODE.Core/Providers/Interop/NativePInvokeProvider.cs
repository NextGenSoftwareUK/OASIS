using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Providers.Interop
{
    /// <summary>
    /// P/Invoke provider for native C/C++/Rust libraries
    /// Best performance - direct native code execution
    /// </summary>
    public class NativePInvokeProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, IntPtr> _loadedLibraries;
        private readonly Dictionary<string, Dictionary<string, Delegate>> _functionCache;
        private readonly object _lockObject = new object();

        public InteropProviderType ProviderType => InteropProviderType.NativePInvoke;

        public string[] SupportedExtensions => new[]
        {
            ".dll", ".so", ".dylib", ".a", ".lib"
        };

        public NativePInvokeProvider()
        {
            _loadedLibraries = new Dictionary<string, IntPtr>();
            _functionCache = new Dictionary<string, Dictionary<string, Delegate>>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool> { Result = true };
            return Task.FromResult(result);
        }

        public Task<OASISResult<ILoadedLibrary>> LoadLibraryAsync(string libraryPath, Dictionary<string, object> options = null)
        {
            var result = new OASISResult<ILoadedLibrary>();

            try
            {
                IntPtr handle;

                // Load library based on platform
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    handle = LoadLibrary(libraryPath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    handle = dlopen(libraryPath, RTLD_NOW);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    handle = dlopen(libraryPath, RTLD_NOW);
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Unsupported platform for native library loading.");
                    return Task.FromResult(result);
                }

                if (handle == IntPtr.Zero)
                {
                    var error = GetLastError();
                    OASISErrorHandling.HandleError(ref result, $"Failed to load native library: {error}");
                    return Task.FromResult(result);
                }

                var libraryId = Guid.NewGuid().ToString();
                var libraryName = System.IO.Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedLibraries[libraryId] = handle;
                    _functionCache[libraryId] = new Dictionary<string, Delegate>();
                }

                result.Result = new LoadedLibrary
                {
                    LibraryId = libraryId,
                    LibraryPath = libraryPath,
                    LibraryName = libraryName,
                    ProviderType = ProviderType,
                    LoadedAt = DateTime.UtcNow,
                    Metadata = options ?? new Dictionary<string, object>()
                };
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading native library: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public Task<OASISResult<bool>> UnloadLibraryAsync(string libraryId)
        {
            var result = new OASISResult<bool>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedLibraries.TryGetValue(libraryId, out var handle))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not found.");
                        return Task.FromResult(result);
                    }

                    bool success;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        success = FreeLibrary(handle);
                    }
                    else
                    {
                        success = dlclose(handle) == 0;
                    }

                    if (!success)
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to unload library.");
                        return Task.FromResult(result);
                    }

                    _loadedLibraries.Remove(libraryId);
                    _functionCache.Remove(libraryId);
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unloading library: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public Task<OASISResult<T>> InvokeAsync<T>(string libraryId, string functionName, params object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                var delegateResult = GetOrCreateDelegate<T>(libraryId, functionName);
                if (delegateResult.IsError)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(delegateResult, result);
                    return Task.FromResult(result);
                }

                var func = delegateResult.Result;
                var returnValue = func.DynamicInvoke(parameters);

                if (returnValue is T typedValue)
                {
                    result.Result = typedValue;
                }
                else if (returnValue != null)
                {
                    result.Result = (T)Convert.ChangeType(returnValue, typeof(T));
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking native function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public Task<OASISResult<object>> InvokeAsync(string libraryId, string functionName, params object[] parameters)
        {
            var result = new OASISResult<object>();

            try
            {
                // For dynamic invocation, we need to know the return type
                // This is a simplified version - in practice, you'd need function signatures
                var delegateResult = GetOrCreateDelegate<object>(libraryId, functionName);
                if (delegateResult.IsError)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(delegateResult, result);
                    return Task.FromResult(result);
                }

                var func = delegateResult.Result;
                result.Result = func.DynamicInvoke(parameters);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking native function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public Task<OASISResult<IEnumerable<string>>> GetAvailableFunctionsAsync(string libraryId)
        {
            // Native libraries don't have reflection, so this would need to be provided via metadata
            var result = new OASISResult<IEnumerable<string>>
            {
                Result = new List<string>(),
                Message = "Native libraries require function metadata. Use library metadata to get available functions."
            };
            return Task.FromResult(result);
        }

        public bool IsLibraryLoaded(string libraryId)
        {
            lock (_lockObject)
            {
                return _loadedLibraries.ContainsKey(libraryId);
            }
        }

        public Task<OASISResult<ILibraryMetadata>> GetLibraryMetadataAsync(string libraryId)
        {
            // Native libraries don't have built-in metadata
            var result = new OASISResult<ILibraryMetadata>
            {
                Message = "Native libraries require external metadata. Use library DNA/configuration files."
            };
            return Task.FromResult(result);
        }

        public Task<OASISResult<bool>> DisposeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                lock (_lockObject)
                {
                    foreach (var libraryId in _loadedLibraries.Keys.ToList())
                    {
                        UnloadLibraryAsync(libraryId).Wait();
                    }
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error disposing provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private OASISResult<Delegate> GetOrCreateDelegate<T>(string libraryId, string functionName)
        {
            var result = new OASISResult<Delegate>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedLibraries.TryGetValue(libraryId, out var handle))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return result;
                    }

                    // Check cache
                    if (_functionCache.TryGetValue(libraryId, out var functions) &&
                        functions.TryGetValue(functionName, out var cachedDelegate))
                    {
                        result.Result = cachedDelegate;
                        return result;
                    }

                    // Get function pointer
                    IntPtr funcPtr;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        funcPtr = GetProcAddress(handle, functionName);
                    }
                    else
                    {
                        funcPtr = dlsym(handle, functionName);
                    }

                    if (funcPtr == IntPtr.Zero)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Function '{functionName}' not found in library.");
                        return result;
                    }

                    // Create delegate (simplified - in practice, you'd need proper function signatures)
                    // This is a placeholder - actual implementation would require function signature metadata
                    var delegateType = typeof(Func<T>); // Simplified
                    var del = Marshal.GetDelegateForFunctionPointer(funcPtr, delegateType);

                    if (functions == null)
                    {
                        functions = new Dictionary<string, Delegate>();
                        _functionCache[libraryId] = functions;
                    }

                    functions[functionName] = del;
                    result.Result = del;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting delegate: {ex.Message}", ex);
            }

            return result;
        }

        // P/Invoke declarations
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("libdl.so.2")]
        private static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl.so.2")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl.so.2")]
        private static extern int dlclose(IntPtr handle);

        private const int RTLD_NOW = 2;

        private static string GetLastError()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"Windows error: {Marshal.GetLastWin32Error()}";
            }
            else
            {
                var error = dlerror();
                return error ?? "Unknown error";
            }
        }

        [DllImport("libdl.so.2")]
        private static extern string dlerror();
    }

    // Helper class
    internal class LoadedLibrary : ILoadedLibrary
    {
        public string LibraryId { get; set; }
        public string LibraryPath { get; set; }
        public string LibraryName { get; set; }
        public InteropProviderType ProviderType { get; set; }
        public DateTime LoadedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}

