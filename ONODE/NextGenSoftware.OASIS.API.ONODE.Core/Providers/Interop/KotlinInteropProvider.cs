using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.Interop;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Providers.Interop
{
    /// <summary>
    /// Kotlin interop provider
    /// Extracts function signatures from Kotlin source code
    /// Kotlin compiles to JVM bytecode, so can also use Java provider for compiled libraries
    /// </summary>
    public class KotlinInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, KotlinLibraryInfo> _loadedScripts;
        private readonly object _lockObject = new object();
        private bool _initialized = false;
        private bool _kotlinAvailable = false;
        private bool _javaAvailable = false;
        private string _kotlinPath = null;
        private string _javaPath = null;

        private class KotlinLibraryInfo
        {
            public string ScriptPath { get; set; }
            public string ScriptContent { get; set; }
        }

        public InteropProviderType ProviderType => InteropProviderType.Kotlin;

        public string[] SupportedExtensions => new[]
        {
            ".kt", ".kts"
        };

        public KotlinInteropProvider()
        {
            _loadedScripts = new Dictionary<string, string>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                _kotlinAvailable = TryDetectKotlin();
                _javaAvailable = TryDetectJava();
                _initialized = true;
                result.Result = true;
                
                if (_kotlinAvailable && _javaAvailable)
                {
                    result.Message = $"Kotlin interop provider initialized with Kotlin compiler ({_kotlinPath}) and Java runtime ({_javaPath}). Both signature extraction and code execution are available.";
                }
                else
                {
                    result.Message = "Kotlin interop provider initialized. Function signatures will be extracted from source code. Kotlin compiler or Java runtime not found - code execution disabled. Install Kotlin and Java for full support.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing Kotlin provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private bool TryDetectKotlin()
        {
            var commonPaths = new[]
            {
                "kotlinc",
                "kotlin",
                @"C:\Program Files\JetBrains\Kotlin\kotlinc\bin\kotlinc.bat",
                @"/usr/bin/kotlinc",
                @"/usr/local/bin/kotlinc"
            };

            foreach (var path in commonPaths)
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            process.WaitForExit(2000);
                            if (process.ExitCode == 0)
                            {
                                _kotlinPath = path;
                                return true;
                            }
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return false;
        }

        private bool TryDetectJava()
        {
            var commonPaths = new[]
            {
                "java",
                @"C:\Program Files\Java\*\bin\java.exe"
            };

            foreach (var path in commonPaths)
            {
                try
                {
                    if (path.Contains("*"))
                    {
                        var dir = Path.GetDirectoryName(path);
                        var pattern = Path.GetFileName(path);
                        if (Directory.Exists(dir))
                        {
                            var matches = Directory.GetFiles(dir, pattern);
                            if (matches.Length > 0)
                            {
                                _javaPath = matches[0];
                                return true;
                            }
                        }
                        continue;
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            process.WaitForExit(2000);
                            if (process.ExitCode == 0)
                            {
                                _javaPath = path;
                                return true;
                            }
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return false;
        }

        public Task<OASISResult<ILoadedLibrary>> LoadLibraryAsync(string libraryPath, Dictionary<string, object> options = null)
        {
            var result = new OASISResult<ILoadedLibrary>();

            try
            {
                if (!File.Exists(libraryPath))
                {
                    OASISErrorHandling.HandleError(ref result, "Kotlin file not found.");
                    return Task.FromResult(result);
                }

                var scriptContent = File.ReadAllText(libraryPath);
                var libraryId = Guid.NewGuid().ToString();
                var libraryName = Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedScripts[libraryId] = new KotlinLibraryInfo
                    {
                        ScriptPath = libraryPath,
                        ScriptContent = scriptContent
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

                result.Message = "Kotlin library loaded. Function signatures will be extracted from source code.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading Kotlin library: {ex.Message}", ex);
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
                    if (!_loadedScripts.ContainsKey(libraryId))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not found.");
                        return Task.FromResult(result);
                    }

                    _loadedScripts.Remove(libraryId);
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unloading Kotlin library: {ex.Message}", ex);
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
                    if (!_loadedScripts.TryGetValue(libraryId, out var scriptInfo))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    if (!_kotlinAvailable || !_javaAvailable)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            "Kotlin compiler or Java runtime is not available. Cannot execute Kotlin code. " +
                            "Install Kotlin (https://kotlinlang.org/) and Java JDK for code execution. " +
                            "Signature extraction works without runtime, but code execution requires it.");
                        return Task.FromResult(result);
                    }

                    return ExecuteKotlinFunctionAsync<T>(scriptInfo, functionName, parameters);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking Kotlin function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteKotlinFunctionAsync<T>(KotlinLibraryInfo scriptInfo, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                var scriptDir = Path.GetDirectoryName(scriptInfo.ScriptPath);
                var scriptFile = Path.GetFileName(scriptInfo.ScriptPath);
                var compiledJar = Path.Combine(scriptDir, Path.GetFileNameWithoutExtension(scriptFile) + ".jar");

                // Compile Kotlin to JAR
                var compileInfo = new ProcessStartInfo
                {
                    FileName = _kotlinPath,
                    Arguments = $"-include-runtime -d \"{compiledJar}\" \"{scriptInfo.ScriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = scriptDir
                };

                using (var compileProcess = Process.Start(compileInfo))
                {
                    if (compileProcess != null)
                    {
                        compileProcess.WaitForExit(30000);
                        if (compileProcess.ExitCode != 0)
                        {
                            var error = compileProcess.StandardError.ReadToEnd();
                            OASISErrorHandling.HandleError(ref result, $"Kotlin compilation error: {error}");
                            return Task.FromResult(result);
                        }
                    }
                }

                // Execute compiled JAR
                var startInfo = new ProcessStartInfo
                {
                    FileName = _javaPath,
                    Arguments = $"-jar \"{compiledJar}\" {functionName} {BuildKotlinParameters(parameters)}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = scriptDir
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();
                        process.WaitForExit(30000);

                        if (process.ExitCode != 0)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Kotlin execution error: {error}");
                            return Task.FromResult(result);
                        }

                        result.Result = ParseKotlinOutput<T>(output);
                        result.Message = $"Kotlin function '{functionName}' executed successfully.";
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing Kotlin function '{functionName}': {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private string BuildKotlinParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return string.Empty;

            var paramStrings = new List<string>();
            foreach (var param in parameters)
            {
                if (param == null)
                    paramStrings.Add("null");
                else if (param is string)
                    paramStrings.Add($"\"{param.ToString().Replace("\"", "\\\"")}\"");
                else if (param is bool)
                    paramStrings.Add((bool)param ? "true" : "false");
                else if (param is int || param is long || param is double || param is float || param is decimal)
                    paramStrings.Add(param.ToString());
                else
                    paramStrings.Add(JsonSerializer.Serialize(param));
            }

            return string.Join(" ", paramStrings);
        }

        private T ParseKotlinOutput<T>(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
                return default(T);

            var trimmed = output.Trim();
            try
            {
                if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                    return JsonSerializer.Deserialize<T>(trimmed);
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
                    if (!_loadedScripts.TryGetValue(libraryId, out var scriptInfo))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    var signatures = ParseKotlinSignatures(scriptInfo.ScriptContent);
                    var functionNames = signatures.Select(s => s.FunctionName).ToList();

                    result.Result = functionNames;
                    result.Message = functionNames.Count > 0
                        ? $"Found {functionNames.Count} functions in Kotlin library."
                        : "No functions found in Kotlin library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Kotlin functions: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public bool IsLibraryLoaded(string libraryId)
        {
            lock (_lockObject)
            {
                return _loadedScripts.ContainsKey(libraryId);
            }
        }

        public Task<OASISResult<ILibraryMetadata>> GetLibraryMetadataAsync(string libraryId)
        {
            var result = new OASISResult<ILibraryMetadata>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedScripts.TryGetValue(libraryId, out var scriptContent))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    var metadata = new LibraryMetadata
                    {
                        LibraryName = "Kotlin Library",
                        Language = "Kotlin",
                        Framework = "JVM",
                        CustomProperties = new Dictionary<string, object>()
                    };

                    result.Result = metadata;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Kotlin metadata: {ex.Message}", ex);
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
                    if (!_loadedScripts.TryGetValue(libraryId, out var scriptInfo))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    var signatures = ParseKotlinSignatures(scriptInfo.ScriptContent);

                    result.Result = signatures;
                    result.Message = signatures.Count > 0
                        ? $"Found {signatures.Count} function signatures in Kotlin library."
                        : "No function signatures found in Kotlin library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Kotlin function signatures: {ex.Message}", ex);
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
                    _loadedScripts.Clear();
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error disposing Kotlin provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParseKotlinSignatures(string scriptContent)
        {
            var signatures = new List<IFunctionSignature>();

            if (string.IsNullOrWhiteSpace(scriptContent))
                return signatures;

            try
            {
                // Parse Kotlin function definitions: fun functionName(param1: Type1, param2: Type2 = default): ReturnType
                var functionPattern = @"fun\s+(?:[\w.]+\.)?(\w+)\s*\(([^)]*)\)\s*(?::\s*([^\{]+))?";
                var functionMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    functionPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in functionMatches)
                {
                    var functionName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Value;
                    var returnType = match.Groups[3].Success ? match.Groups[3].Value.Trim() : "Unit";

                    var parameters = ParseKotlinParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = functionName,
                        ReturnType = MapKotlinTypeToCSharp(returnType),
                        Parameters = parameters
                    });
                }
            }
            catch
            {
                // Parsing failed
            }

            return signatures;
        }

        private List<IParameterInfo> ParseKotlinParameters(string paramsStr)
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
                // Kotlin parameter: name: Type or name: Type = default
                var paramMatch = System.Text.RegularExpressions.Regex.Match(
                    paramPart,
                    @"^(\w+)\s*:\s*([^=]+)(?:\s*=\s*(.+))?$");

                if (paramMatch.Success)
                {
                    var name = paramMatch.Groups[1].Value;
                    var type = paramMatch.Groups[2].Value.Trim();
                    var hasDefault = paramMatch.Groups[3].Success;

                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = name,
                        Type = MapKotlinTypeToCSharp(type),
                        IsOptional = hasDefault,
                        DefaultValue = hasDefault ? paramMatch.Groups[3].Value.Trim() : null
                    });
                }
                else
                {
                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = paramPart,
                        Type = "object"
                    });
                }
            }

            return parameters;
        }

        private string MapKotlinTypeToCSharp(string kotlinType)
        {
            if (string.IsNullOrWhiteSpace(kotlinType))
                return "object";

            return kotlinType.Trim() switch
            {
                "String" => "string",
                "Int" => "int",
                "Long" => "long",
                "Double" => "double",
                "Float" => "float",
                "Boolean" => "bool",
                "Byte" => "byte",
                "Short" => "short",
                "Char" => "char",
                "Unit" => "void",
                "Any" => "object",
                _ => kotlinType.Contains("Array") || kotlinType.Contains("List") ? "object[]" : "object"
            };
        }
    }
}

