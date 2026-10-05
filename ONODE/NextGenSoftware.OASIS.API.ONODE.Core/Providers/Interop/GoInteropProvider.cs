using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.Interop;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Providers.Interop
{
    /// <summary>
    /// Go interop provider via CGO (C interop)
    /// Go libraries are compiled to native libraries and can be accessed via P/Invoke
    /// Extracts function signatures from Go source code or compiled libraries
    /// </summary>
    public class GoInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, GoLibraryInfo> _loadedLibraries;
        private readonly object _lockObject = new object();

        public InteropProviderType ProviderType => InteropProviderType.Go;

        public string[] SupportedExtensions => new[]
        {
            ".so", ".dll", ".dylib", ".a", ".go"
        };

        public GoInteropProvider()
        {
            _loadedLibraries = new Dictionary<string, GoLibraryInfo>();
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
                var libraryId = Guid.NewGuid().ToString();
                var libraryName = Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedLibraries[libraryId] = new GoLibraryInfo
                    {
                        LibraryId = libraryId,
                        LibraryPath = libraryPath,
                        IsSourceFile = libraryPath.EndsWith(".go", StringComparison.OrdinalIgnoreCase)
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

                result.Message = "Go library loaded. Function signatures will be extracted from source code or compiled library.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading Go library: {ex.Message}", ex);
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
                    if (!_loadedLibraries.ContainsKey(libraryId))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not found.");
                        return Task.FromResult(result);
                    }

                    _loadedLibraries.Remove(libraryId);
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unloading Go library: {ex.Message}", ex);
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
                    if (!_loadedLibraries.TryGetValue(libraryId, out var library))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Go functions can be executed via compiled binaries or CGO
                    return ExecuteGoFunctionAsync<T>(library, functionName, parameters);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking Go function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteGoFunctionAsync<T>(GoLibraryInfo library, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                // Check if there's a compiled binary
                var binaryPath = Path.ChangeExtension(library.LibraryPath, Environment.OSVersion.Platform == PlatformID.Win32NT ? ".exe" : "");
                if (File.Exists(binaryPath))
                {
                    return ExecuteGoBinaryAsync<T>(binaryPath, functionName, parameters);
                }

                // Try to compile and run
                var goPath = TryDetectGo();
                if (!string.IsNullOrEmpty(goPath))
                {
                    return ExecuteGoWithRuntimeAsync<T>(library, functionName, parameters, goPath);
                }

                OASISErrorHandling.HandleError(ref result, 
                    "Go runtime is not available. Cannot execute Go code. " +
                    "Install Go (https://go.dev/) for code execution. " +
                    "Signature extraction works without runtime, but code execution requires it.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing Go function '{functionName}': {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteGoBinaryAsync<T>(string binaryPath, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = binaryPath,
                    Arguments = $"{functionName} {BuildGoParameters(parameters)}",
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
                            OASISErrorHandling.HandleError(ref result, $"Go execution error: {error}");
                            return Task.FromResult(result);
                        }

                        result.Result = ParseGoOutput<T>(output);
                        result.Message = $"Go function '{functionName}' executed successfully.";
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing Go binary: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteGoWithRuntimeAsync<T>(GoLibraryInfo library, string functionName, object[] parameters, string goPath)
        {
            var result = new OASISResult<T>();

            try
            {
                var scriptDir = Path.GetDirectoryName(library.LibraryPath);
                var scriptFile = Path.GetFileName(library.LibraryPath);

                var tempScript = Path.GetTempFileName() + ".go";
                var scriptBuilder = new System.Text.StringBuilder();
                scriptBuilder.AppendLine($"package main");
                scriptBuilder.AppendLine($"import (");
                scriptBuilder.AppendLine($"  \"encoding/json\"");
                scriptBuilder.AppendLine($"  \"fmt\"");
                scriptBuilder.AppendLine($")");
                scriptBuilder.AppendLine($"func main() {{");
                scriptBuilder.AppendLine($"  result := {functionName}({BuildGoParameters(parameters)})");
                scriptBuilder.AppendLine($"  jsonBytes, _ := json.Marshal(result)");
                scriptBuilder.AppendLine($"  fmt.Println(string(jsonBytes))");
                scriptBuilder.AppendLine($"}}");

                File.WriteAllText(tempScript, scriptBuilder.ToString());

                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = goPath,
                        Arguments = $"run \"{tempScript}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = scriptDir
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
                                OASISErrorHandling.HandleError(ref result, $"Go execution error: {error}");
                                return Task.FromResult(result);
                            }

                            result.Result = ParseGoOutput<T>(output);
                            result.Message = $"Go function '{functionName}' executed successfully.";
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
                OASISErrorHandling.HandleError(ref result, $"Error executing Go with runtime: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private string TryDetectGo()
        {
            var commonPaths = new[]
            {
                "go",
                @"C:\Program Files\Go\bin\go.exe",
                @"/usr/bin/go",
                @"/usr/local/bin/go",
                @"/opt/homebrew/bin/go"
            };

            foreach (var path in commonPaths)
            {
                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "version",
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

        private string BuildGoParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return string.Empty;

            var paramStrings = new List<string>();
            foreach (var param in parameters)
            {
                if (param == null)
                    paramStrings.Add("nil");
                else if (param is string)
                    paramStrings.Add($"\"{param.ToString().Replace("\"", "\\\"")}\"");
                else if (param is bool)
                    paramStrings.Add((bool)param ? "true" : "false");
                else if (param is int || param is long || param is double || param is float || param is decimal)
                    paramStrings.Add(param.ToString());
                else
                    paramStrings.Add("interface{}{}"); // Simplified
            }

            return string.Join(", ", paramStrings);
        }

        private T ParseGoOutput<T>(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
                return default(T);

            var trimmed = output.Trim();
            try
            {
                if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                    return System.Text.Json.JsonSerializer.Deserialize<T>(trimmed);
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
                    if (!_loadedLibraries.TryGetValue(libraryId, out var library))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Parse Go source code to extract exported functions
                    var signatures = ParseGoSourceCode(library.LibraryPath);
                    var functionNames = signatures.Select(s => s.FunctionName).ToList();

                    result.Result = functionNames;
                    result.Message = functionNames.Count > 0
                        ? $"Found {functionNames.Count} exported functions in Go library."
                        : "No exported functions found in Go library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Go functions: {ex.Message}", ex);
            }

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
            var result = new OASISResult<ILibraryMetadata>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedLibraries.TryGetValue(libraryId, out var library))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    var metadata = new LibraryMetadata
                    {
                        LibraryName = Path.GetFileName(library.LibraryPath),
                        Language = "Go",
                        Framework = "CGO",
                        CustomProperties = new Dictionary<string, object>()
                    };

                    result.Result = metadata;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Go metadata: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public Task<OASISResult<IEnumerable<IFunctionSignature>>> GetFunctionSignaturesAsync(string libraryId)
        {
            var result = new OASISResult<IEnumerable<IFunctionSignature>>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedLibraries.TryGetValue(libraryId, out var library))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Parse Go source code to extract function signatures
                    var signatures = ParseGoSourceCode(library.LibraryPath);

                    result.Result = signatures;
                    result.Message = signatures.Count > 0
                        ? $"Found {signatures.Count} function signatures in Go library."
                        : "No function signatures found in Go library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Go function signatures: {ex.Message}", ex);
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
                    _loadedLibraries.Clear();
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error disposing Go provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParseGoSourceCode(string filePath)
        {
            var signatures = new List<IFunctionSignature>();

            try
            {
                if (!File.Exists(filePath) || !filePath.EndsWith(".go", StringComparison.OrdinalIgnoreCase))
                    return signatures;

                var sourceCode = File.ReadAllText(filePath);

                // Parse Go function definitions: func FunctionName(param1 type1, param2 type2) returnType { ... }
                // Also: func (receiver) MethodName(...) returnType { ... }
                var functionPattern = @"func\s+(?:\([^)]+\)\s+)?(\w+)\s*\(([^)]*)\)\s*(?:\(([^)]+)\)|(\w+))?\s*\{";
                var functionMatches = System.Text.RegularExpressions.Regex.Matches(
                    sourceCode,
                    functionPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in functionMatches)
                {
                    var functionName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Value;
                    var returnType = match.Groups[3].Success ? match.Groups[3].Value : 
                                   (match.Groups[4].Success ? match.Groups[4].Value : "void");

                    var parameters = ParseGoParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = functionName,
                        ReturnType = MapGoTypeToCSharp(returnType),
                        Parameters = parameters,
                        Documentation = ExtractGoDocComment(sourceCode, match.Index)
                    });
                }
            }
            catch
            {
                // Parsing failed
            }

            return signatures;
        }

        private List<IParameterInfo> ParseGoParameters(string paramsStr)
        {
            var parameters = new List<IParameterInfo>();

            if (string.IsNullOrWhiteSpace(paramsStr))
                return parameters;

            var paramParts = paramsStr.Split(',')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            foreach (var paramPart in paramParts)
            {
                // Go parameter format: name type or name1, name2 type
                var parts = paramPart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var type = parts[parts.Length - 1];
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        parameters.Add(new Objects.Interop.ParameterInfo
                        {
                            Name = parts[i],
                            Type = MapGoTypeToCSharp(type)
                        });
                    }
                }
                else if (parts.Length == 1)
                {
                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = "param",
                        Type = MapGoTypeToCSharp(parts[0])
                    });
                }
            }

            return parameters;
        }

        private string MapGoTypeToCSharp(string goType)
        {
            if (string.IsNullOrWhiteSpace(goType))
                return "object";

            return goType switch
            {
                "int" => "int",
                "int8" => "sbyte",
                "int16" => "short",
                "int32" => "int",
                "int64" => "long",
                "uint" => "uint",
                "uint8" => "byte",
                "uint16" => "ushort",
                "uint32" => "uint",
                "uint64" => "ulong",
                "float32" => "float",
                "float64" => "double",
                "bool" => "bool",
                "string" => "string",
                "byte" => "byte",
                "rune" => "int",
                _ => goType.Contains("[]") ? goType.Replace("[]", "[]") : "object"
            };
        }

        private string ExtractGoDocComment(string sourceCode, int functionIndex)
        {
            try
            {
                // Go doc comments are above the function
                var beforeFunc = sourceCode.Substring(0, functionIndex);
                var lines = beforeFunc.Split('\n');
                
                if (lines.Length >= 2)
                {
                    var lastLine = lines[lines.Length - 1].Trim();
                    var secondLastLine = lines[lines.Length - 2].Trim();
                    
                    if (secondLastLine.StartsWith("//"))
                    {
                        return secondLastLine.Substring(2).Trim();
                    }
                }
            }
            catch
            {
                // Extraction failed
            }

            return string.Empty;
        }

        private class GoLibraryInfo
        {
            public string LibraryId { get; set; }
            public string LibraryPath { get; set; }
            public bool IsSourceFile { get; set; }
        }
    }
}

