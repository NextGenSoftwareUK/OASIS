using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.Interop;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Providers.Interop
{
    /// <summary>
    /// P/Invoke provider for native C/C++/Rust libraries
    /// Best performance - direct native code execution
    /// </summary>
    public class NativePInvokeProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, NativeLibraryInfo> _loadedLibraries;
        private readonly Dictionary<string, Dictionary<string, Delegate>> _functionCache;
        private readonly object _lockObject = new object();

        public InteropProviderType ProviderType => InteropProviderType.NativePInvoke;

        public string[] SupportedExtensions => new[]
        {
            ".dll", ".so", ".dylib", ".a", ".lib"
        };

        public NativePInvokeProvider()
        {
            _loadedLibraries = new Dictionary<string, NativeLibraryInfo>();
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
                    _loadedLibraries[libraryId] = new NativeLibraryInfo
                    {
                        Handle = handle,
                        LibraryPath = libraryPath
                    };
                    _functionCache[libraryId] = new Dictionary<string, Delegate>();
                }

                result.Result = new Objects.Interop.LoadedLibrary
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
                    if (!_loadedLibraries.TryGetValue(libraryId, out var libraryInfo))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not found.");
                        return Task.FromResult(result);
                    }

                    bool success;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        success = FreeLibrary(libraryInfo.Handle);
                    }
                    else
                    {
                        success = dlclose(libraryInfo.Handle) == 0;
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
            var result = new OASISResult<IEnumerable<string>>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedLibraries.TryGetValue(libraryId, out var libraryInfo))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    var functions = new List<string>();

                    // Try to extract function names from the library
                    // This works by attempting to resolve common function name patterns
                    // For production, you'd use tools like dumpbin (Windows) or nm (Linux/Mac)
                    functions.AddRange(ExtractFunctionNamesFromLibrary(libraryInfo.Handle, libraryInfo.LibraryPath));

                    // Also check for metadata files (.def, .json, etc.)
                    functions.AddRange(ExtractFunctionsFromMetadata(libraryInfo.LibraryPath));

                    result.Result = functions.Distinct().ToList();
                    result.Message = $"Found {result.Result.Count()} functions in native library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting native functions: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<string> ExtractFunctionNamesFromLibrary(IntPtr handle, string libraryPath)
        {
            var functions = new List<string>();

            try
            {
                // Try common function name patterns
                // In production, use platform-specific tools:
                // Windows: dumpbin /exports library.dll
                // Linux/Mac: nm -D library.so | grep " T "

                // For now, check if metadata was provided in options
                // Function name extraction from native libraries
                var commonNames = new[] { "Initialize", "Process", "Calculate", "Execute", "Run", "Start", "Stop" };
                
                foreach (var name in commonNames)
                {
                    IntPtr funcPtr;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        funcPtr = GetProcAddress(handle, name);
                    }
                    else
                    {
                        funcPtr = dlsym(handle, name);
                    }

                    if (funcPtr != IntPtr.Zero)
                    {
                        functions.Add(name);
                    }
                }
            }
            catch
            {
                // Extraction failed, continue
            }

            return functions;
        }

        private List<string> ExtractFunctionsFromMetadata(string libraryPath)
        {
            var functions = new List<string>();

            try
            {
                var dir = System.IO.Path.GetDirectoryName(libraryPath);
                var name = System.IO.Path.GetFileNameWithoutExtension(libraryPath);

                // Check for .def file (Windows module definition file)
                var defFile = System.IO.Path.Combine(dir, name + ".def");
                if (System.IO.File.Exists(defFile))
                {
                    var defContent = System.IO.File.ReadAllText(defFile);
                    var exports = System.Text.RegularExpressions.Regex.Matches(defContent, @"^\s*(\w+)\s*", System.Text.RegularExpressions.RegexOptions.Multiline);
                    foreach (System.Text.RegularExpressions.Match match in exports)
                    {
                        functions.Add(match.Groups[1].Value);
                    }
                }

                // Check for JSON metadata file
                var jsonFile = System.IO.Path.Combine(dir, name + ".metadata.json");
                if (System.IO.File.Exists(jsonFile))
                {
                    var jsonContent = System.IO.File.ReadAllText(jsonFile);
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
                    if (jsonDoc.RootElement.TryGetProperty("functions", out var funcsArray))
                    {
                        foreach (var func in funcsArray.EnumerateArray())
                        {
                            if (func.TryGetProperty("name", out var nameProp))
                            {
                                functions.Add(nameProp.GetString());
                            }
                        }
                    }
                }
            }
            catch
            {
                // Metadata extraction failed, continue
            }

            return functions;
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

        public Task<OASISResult<IEnumerable<IFunctionSignature>>> GetFunctionSignaturesAsync(string libraryId)
        {
            var result = new OASISResult<IEnumerable<IFunctionSignature>>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedLibraries.TryGetValue(libraryId, out var libraryInfo))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Extract function signatures from metadata files
                    var signatures = ExtractSignaturesFromMetadata(libraryInfo.LibraryPath);
                    
                    result.Result = signatures;
                    result.Message = $"Found {signatures.Count} function signatures in native library metadata.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting native function signatures: {ex.Message}", ex);
            }

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
                        var libraryInfo = _loadedLibraries[libraryId];
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            FreeLibrary(libraryInfo.Handle);
                        }
                        else
                        {
                            dlclose(libraryInfo.Handle);
                        }
                    }
                    _loadedLibraries.Clear();
                    _functionCache.Clear();
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error disposing provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ExtractSignaturesFromMetadata(string libraryPath)
        {
            var signatures = new List<IFunctionSignature>();

            try
            {
                var dir = System.IO.Path.GetDirectoryName(libraryPath);
                var name = System.IO.Path.GetFileNameWithoutExtension(libraryPath);

                // Check for JSON metadata file with function signatures
                var jsonFile = System.IO.Path.Combine(dir, name + ".metadata.json");
                if (System.IO.File.Exists(jsonFile))
                {
                    var jsonContent = System.IO.File.ReadAllText(jsonFile);
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
                    
                    if (jsonDoc.RootElement.TryGetProperty("functions", out var funcsArray))
                    {
                        foreach (var func in funcsArray.EnumerateArray())
                        {
                            var signature = new Objects.Interop.FunctionSignature
                            {
                                FunctionName = func.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "Unknown",
                                ReturnType = func.TryGetProperty("returnType", out var retType) ? retType.GetString() : "object",
                                Documentation = func.TryGetProperty("description", out var desc) ? desc.GetString() : string.Empty
                            };

                            var parameters = new List<IParameterInfo>();
                            if (func.TryGetProperty("parameters", out var paramsArray))
                            {
                                foreach (var param in paramsArray.EnumerateArray())
                                {
                                    parameters.Add(new Objects.Interop.ParameterInfo
                                    {
                                        Name = param.TryGetProperty("name", out var pName) ? pName.GetString() : "param",
                                        Type = param.TryGetProperty("type", out var pType) ? pType.GetString() : "object",
                                        IsOptional = param.TryGetProperty("optional", out var opt) && opt.GetBoolean()
                                    });
                                }
                            }
                            signature.Parameters = parameters;
                            signatures.Add(signature);
                        }
                    }
                }
            }
            catch
            {
                // Metadata extraction failed
            }

            return signatures;
        }

        private OASISResult<Delegate> GetOrCreateDelegate<T>(string libraryId, string functionName)
        {
            var result = new OASISResult<Delegate>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedLibraries.TryGetValue(libraryId, out var libraryInfo))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return result;
                    }

                    var handle = libraryInfo.Handle;

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

                    // Create delegate for native function invocation
                    // Function signature metadata should be provided via library metadata
                    var delegateType = typeof(Func<T>);
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

}

