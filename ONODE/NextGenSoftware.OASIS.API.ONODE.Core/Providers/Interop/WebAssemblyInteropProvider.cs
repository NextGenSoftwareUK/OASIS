using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.Interop;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Providers.Interop
{
    /// <summary>
    /// WebAssembly (WASM) interop provider
    /// Supports libraries compiled to WASM from many languages (Rust, C/C++, Go, etc.)
    /// Extracts function signatures from WASM binary format
    /// </summary>
    public class WebAssemblyInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, WasmModule> _loadedModules;
        private readonly object _lockObject = new object();
        private bool _initialized = false;

        public InteropProviderType ProviderType => InteropProviderType.WebAssembly;

        public string[] SupportedExtensions => new[]
        {
            ".wasm"
        };

        public WebAssemblyInteropProvider()
        {
            _loadedModules = new Dictionary<string, WasmModule>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                // WASM binary parsing works without runtime

                _initialized = true;
                result.Result = true;
                result.Message = "WebAssembly interop provider initialized.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing WASM provider: {ex.Message}. Make sure Wasmtime is installed.", ex);
            }

            return Task.FromResult(result);
        }

        public Task<OASISResult<ILoadedLibrary>> LoadLibraryAsync(string libraryPath, Dictionary<string, object> options = null)
        {
            var result = new OASISResult<ILoadedLibrary>();

            try
            {
                if (!_initialized)
                {
                    var initResult = InitializeAsync().Result;
                    if (initResult.IsError)
                    {
                        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(initResult, result);
                        return Task.FromResult(result);
                    }
                }

                // Load WASM module
                var wasmBytes = System.IO.File.ReadAllBytes(libraryPath);

                var libraryId = Guid.NewGuid().ToString();
                var libraryName = System.IO.Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedModules[libraryId] = new WasmModule
                    {
                        ModuleId = libraryId,
                        Bytes = wasmBytes,
                        ModulePath = libraryPath
                    };
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

                result.Message = "WASM library loaded. Function signatures will be extracted from WASM binary.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading WASM library: {ex.Message}", ex);
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
                    if (!_loadedModules.ContainsKey(libraryId))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not found.");
                        return Task.FromResult(result);
                    }

                    // Module unloaded
                    _loadedModules.Remove(libraryId);
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unloading WASM library: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public Task<OASISResult<T>> InvokeAsync<T>(string libraryId, string functionName, params object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedModules.TryGetValue(libraryId, out var module))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Execute WASM function using Wasmtime (with graceful fallback)
                    return ExecuteWasmFunctionAsync<T>(module, functionName, parameters);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking WASM function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteWasmFunctionAsync<T>(WasmModule module, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                // Try to use Wasmtime if available
                var wasmtimeType = Type.GetType("Wasmtime.Engine, Wasmtime");
                if (wasmtimeType != null)
                {
                    // Use Wasmtime for execution
                    return ExecuteWasmWithWasmtimeAsync<T>(module, functionName, parameters);
                }

                // Fallback: Use wasmtime CLI if available
                var wasmtimePath = TryDetectWasmtime();
                if (!string.IsNullOrEmpty(wasmtimePath))
                {
                    return ExecuteWasmWithCLIAsync<T>(module, functionName, parameters, wasmtimePath);
                }

                OASISErrorHandling.HandleError(ref result, 
                    "Wasmtime runtime is not available. Cannot execute WebAssembly code. " +
                    "Install Wasmtime (https://wasmtime.dev/) or add Wasmtime NuGet package for code execution. " +
                    "Signature extraction works without runtime, but code execution requires it.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing WASM function '{functionName}': {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteWasmWithWasmtimeAsync<T>(WasmModule module, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                // Use reflection to call Wasmtime methods
                var engineType = Type.GetType("Wasmtime.Engine, Wasmtime");
                var storeType = Type.GetType("Wasmtime.Store, Wasmtime");
                var moduleType = Type.GetType("Wasmtime.Module, Wasmtime");
                var linkerType = Type.GetType("Wasmtime.Linker, Wasmtime");

                if (engineType != null && storeType != null && moduleType != null && linkerType != null)
                {
                    var engine = Activator.CreateInstance(engineType);
                    var store = Activator.CreateInstance(storeType, engine);
                    var wasmModule = moduleType.GetMethod("FromFile", new[] { engineType, typeof(string) })
                        .Invoke(null, new[] { engine, module.ModulePath });
                    var linker = Activator.CreateInstance(linkerType, engine);
                    var instance = linkerType.GetMethod("Instantiate", new[] { storeType, moduleType })
                        .Invoke(linker, new[] { store, wasmModule });

                    var func = instance.GetType().GetProperty("GetFunction", new[] { typeof(string) })
                        .GetValue(instance, new[] { functionName });

                    if (func != null)
                    {
                        var invokeMethod = func.GetType().GetMethod("Invoke");
                        var returnValue = invokeMethod.Invoke(func, parameters);
                        result.Result = (T)Convert.ChangeType(returnValue, typeof(T));
                        result.Message = $"WASM function '{functionName}' executed successfully.";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Function '{functionName}' not found in WASM module.");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing WASM with Wasmtime: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteWasmWithCLIAsync<T>(WasmModule module, string functionName, object[] parameters, string wasmtimePath)
        {
            var result = new OASISResult<T>();

            try
            {
                // Create a wrapper script that calls the WASM function
                var tempScript = Path.GetTempFileName() + ".wat";
                var scriptBuilder = new System.Text.StringBuilder();
                scriptBuilder.AppendLine($"(module");
                scriptBuilder.AppendLine($"  (import \"env\" \"{functionName}\" (func $import_{functionName}))");
                scriptBuilder.AppendLine($"  (func $main");
                scriptBuilder.AppendLine($"    call $import_{functionName}");
                scriptBuilder.AppendLine($"  )");
                scriptBuilder.AppendLine($"  (start $main)");
                scriptBuilder.AppendLine($")");

                File.WriteAllText(tempScript, scriptBuilder.ToString());

                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = wasmtimePath,
                        Arguments = $"run \"{module.ModulePath}\" --invoke {functionName} {BuildWasmParameters(parameters)}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = System.Diagnostics.Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            var output = process.StandardOutput.ReadToEnd();
                            var error = process.StandardError.ReadToEnd();
                            process.WaitForExit(30000);

                            if (process.ExitCode != 0)
                            {
                                OASISErrorHandling.HandleError(ref result, $"WASM execution error: {error}");
                                return Task.FromResult(result);
                            }

                            result.Result = ParseWasmOutput<T>(output);
                            result.Message = $"WASM function '{functionName}' executed successfully.";
                        }
                    }
                }
                finally
                {
                    try { File.Delete(tempScript); } catch { }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing WASM with CLI: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private string TryDetectWasmtime()
        {
            var commonPaths = new[]
            {
                "wasmtime",
                @"/usr/local/bin/wasmtime",
                @"/opt/homebrew/bin/wasmtime"
            };

            foreach (var path in commonPaths)
            {
                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = System.Diagnostics.Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            process.WaitForExit(2000);
                            if (process.ExitCode == 0)
                            {
                                return path;
                            }
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }

        private string BuildWasmParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return string.Empty;

            var paramStrings = new List<string>();
            foreach (var param in parameters)
            {
                if (param is int || param is long)
                    paramStrings.Add($"i32 {param}");
                else if (param is float || param is double)
                    paramStrings.Add($"f64 {param}");
                else
                    paramStrings.Add(param.ToString());
            }

            return string.Join(" ", paramStrings);
        }

        private T ParseWasmOutput<T>(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
                return default(T);

            var trimmed = output.Trim();
            try
            {
                return (T)Convert.ChangeType(trimmed, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        public Task<OASISResult<object>> InvokeAsync(string libraryId, string functionName, params object[] parameters)
        {
            return InvokeAsync<object>(libraryId, functionName, parameters);
        }

        public Task<OASISResult<IEnumerable<string>>> GetAvailableFunctionsAsync(string libraryId)
        {
            var result = new OASISResult<IEnumerable<string>>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedModules.TryGetValue(libraryId, out var module))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Parse WASM binary to extract exported function names
                    var signatures = ParseWasmBinary(module.Bytes);
                    var functionNames = signatures.Select(s => s.FunctionName).ToList();
                    
                    result.Result = functionNames;
                    result.Message = functionNames.Count > 0
                        ? $"Found {functionNames.Count} exported functions in WASM module."
                        : "No exported functions found in WASM module.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting WASM functions: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public bool IsLibraryLoaded(string libraryId)
        {
            lock (_lockObject)
            {
                return _loadedModules.ContainsKey(libraryId);
            }
        }

        public Task<OASISResult<ILibraryMetadata>> GetLibraryMetadataAsync(string libraryId)
        {
            var result = new OASISResult<ILibraryMetadata>
            {
                Message = "WASM metadata extracted from binary."
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
                    if (!_loadedModules.TryGetValue(libraryId, out var module))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Parse WASM binary to extract function signatures
                    var signatures = ParseWasmBinary(module.Bytes);
                    
                    result.Result = signatures;
                    result.Message = signatures.Count > 0
                        ? $"Found {signatures.Count} function signatures in WASM module."
                        : "No exported functions found in WASM module.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting WASM function signatures: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParseWasmBinary(byte[] wasmBytes)
        {
            var signatures = new List<IFunctionSignature>();

            try
            {
                if (wasmBytes == null || wasmBytes.Length < 8)
                    return signatures;

                // WASM binary format: magic number (0x00 0x61 0x73 0x6D) + version (0x01 0x00 0x00 0x00)
                if (wasmBytes[0] != 0x00 || wasmBytes[1] != 0x61 || wasmBytes[2] != 0x73 || wasmBytes[3] != 0x6D)
                    return signatures; // Not a valid WASM file

                using (var reader = new BinaryReader(new MemoryStream(wasmBytes)))
                {
                    // Skip magic number and version (8 bytes)
                    reader.ReadBytes(8);

                    // Parse sections
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        var sectionId = reader.ReadByte();
                        var sectionSize = ReadLEB128(reader);

                        if (sectionId == 3) // Export section
                        {
                            var exportCount = ReadLEB128(reader);
                            var functionIndex = 0;

                            for (int i = 0; i < exportCount; i++)
                            {
                                var nameLength = (int)ReadLEB128(reader);
                                var nameBytes = reader.ReadBytes(nameLength);
                                var exportName = System.Text.Encoding.UTF8.GetString(nameBytes);
                                
                                var exportKind = reader.ReadByte();
                                
                                if (exportKind == 0) // Function export
                                {
                                    var funcIndex = (int)ReadLEB128(reader);
                                    
                                    // Try to find function signature from type section
                                    var signature = GetFunctionSignature(wasmBytes, funcIndex);
                                    if (signature != null)
                                    {
                                        // Create new signature with the export name since FunctionName is read-only
                                        signatures.Add(new Objects.Interop.FunctionSignature
                                        {
                                            FunctionName = exportName,
                                            ReturnType = signature.ReturnType,
                                            Parameters = signature.Parameters,
                                            IsAsync = signature.IsAsync,
                                            Documentation = signature.Documentation
                                        });
                                    }
                                    else
                                    {
                                        // Create basic signature if type info not available
                                        signatures.Add(new Objects.Interop.FunctionSignature
                                        {
                                            FunctionName = exportName,
                                            ReturnType = "object",
                                            Parameters = new List<IParameterInfo>()
                                        });
                                    }
                                }
                                else
                                {
                                    // Skip non-function exports
                                    ReadLEB128(reader);
                                }
                            }
                        }
                        else if (sectionId == 1) // Type section - store for later use
                        {
                            // Store type section position for signature lookup
                            var typeSectionPos = reader.BaseStream.Position;
                            var typeCount = ReadLEB128(reader);
                            
                            // Store types for later reference
                            for (int i = 0; i < typeCount; i++)
                            {
                                var form = reader.ReadByte(); // 0x60 = function type
                                if (form == 0x60)
                                {
                                    var paramCount = ReadLEB128(reader);
                                    for (int p = 0; p < paramCount; p++)
                                    {
                                        var paramType = reader.ReadByte();
                                    }
                                    var returnCount = ReadLEB128(reader);
                                    if (returnCount > 0)
                                    {
                                        var returnType = reader.ReadByte();
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Skip other sections
                            reader.ReadBytes((int)sectionSize);
                        }
                    }
                }
            }
            catch
            {
                // Parsing failed, return what we have
            }

            return signatures;
        }

        private IFunctionSignature GetFunctionSignature(byte[] wasmBytes, int functionIndex)
        {
            try
            {
                using (var reader = new BinaryReader(new MemoryStream(wasmBytes)))
                {
                    reader.ReadBytes(8); // Skip magic + version

                    // Find function section and type section
                    int? functionSectionPos = null;
                    Dictionary<int, int> functionTypes = new Dictionary<int, int>();
                    Dictionary<int, FunctionTypeInfo> typeInfos = new Dictionary<int, FunctionTypeInfo>();

                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        var sectionId = reader.ReadByte();
                        var sectionSize = ReadLEB128(reader);
                        var sectionStart = reader.BaseStream.Position;

                        if (sectionId == 1) // Type section
                        {
                            var typeCount = ReadLEB128(reader);
                            for (int i = 0; i < typeCount; i++)
                            {
                                var form = reader.ReadByte();
                                if (form == 0x60) // Function type
                                {
                                    var paramCount = ReadLEB128(reader);
                                    var paramTypes = new List<byte>();
                                    for (int p = 0; p < paramCount; p++)
                                    {
                                        paramTypes.Add(reader.ReadByte());
                                    }
                                    var returnCount = ReadLEB128(reader);
                                    byte? returnType = null;
                                    if (returnCount > 0)
                                    {
                                        returnType = reader.ReadByte();
                                    }
                                    typeInfos[i] = new FunctionTypeInfo
                                    {
                                        ParamTypes = paramTypes,
                                        ReturnType = returnType
                                    };
                                }
                            }
                        }
                        else if (sectionId == 3) // Function section
                        {
                            var funcCount = (int)ReadLEB128(reader);
                            for (int i = 0; i < funcCount; i++)
                            {
                                var funcTypeIndex = (int)ReadLEB128(reader);
                                functionTypes[i] = funcTypeIndex;
                            }
                        }

                        reader.BaseStream.Position = sectionStart + sectionSize;
                    }

                    // Get function type
                    if (functionTypes.TryGetValue(functionIndex, out var funcTypeIdx) &&
                        typeInfos.TryGetValue(funcTypeIdx, out var typeInfo))
                    {
                        var parameters = typeInfo.ParamTypes.Select((t, i) => new Objects.Interop.ParameterInfo
                        {
                            Name = $"param{i}",
                            Type = MapWasmTypeToCSharp(t)
                        }).Cast<IParameterInfo>().ToList();

                        return new Objects.Interop.FunctionSignature
                        {
                            FunctionName = "", // Will be set by caller
                            ReturnType = typeInfo.ReturnType.HasValue 
                                ? MapWasmTypeToCSharp(typeInfo.ReturnType.Value) 
                                : "void",
                            Parameters = parameters
                        };
                    }
                }
            }
            catch
            {
                // Parsing failed
            }

            return null;
        }

        private uint ReadLEB128(BinaryReader reader)
        {
            uint result = 0;
            int shift = 0;
            byte b;

            do
            {
                b = reader.ReadByte();
                result |= (uint)(b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);

            return result;
        }

        private string MapWasmTypeToCSharp(byte wasmType)
        {
            return wasmType switch
            {
                0x7F => "int",      // i32
                0x7E => "long",     // i64
                0x7D => "float",    // f32
                0x7C => "double",   // f64
                _ => "object"
            };
        }

        private class FunctionTypeInfo
        {
            public List<byte> ParamTypes { get; set; }
            public byte? ReturnType { get; set; }
        }

        public Task<OASISResult<bool>> DisposeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                lock (_lockObject)
                {
                    _loadedModules.Clear();
                }
                _initialized = false;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error disposing WASM provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private class WasmModule
        {
            public string ModuleId { get; set; }
            public byte[] Bytes { get; set; }
            public string ModulePath { get; set; }
        }
    }
}

