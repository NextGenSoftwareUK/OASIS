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
    /// Julia interop provider
    /// Extracts function signatures from Julia source code
    /// </summary>
    public class JuliaInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, JuliaLibraryInfo> _loadedScripts;
        private readonly object _lockObject = new object();
        private bool _initialized = false;
        private bool _juliaAvailable = false;
        private string _juliaPath = null;

        private class JuliaLibraryInfo
        {
            public string ScriptPath { get; set; }
            public string ScriptContent { get; set; }
        }

        public InteropProviderType ProviderType => InteropProviderType.Julia;

        public string[] SupportedExtensions => new[]
        {
            ".jl"
        };

        public JuliaInteropProvider()
        {
            _loadedScripts = new Dictionary<string, string>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                _juliaAvailable = TryDetectJulia();
                _initialized = true;
                result.Result = true;
                
                if (_juliaAvailable)
                {
                    result.Message = $"Julia interop provider initialized with Julia runtime ({_juliaPath}). Both signature extraction and code execution are available.";
                }
                else
                {
                    result.Message = "Julia interop provider initialized. Function signatures will be extracted from source code. Julia runtime not found - code execution disabled. Install Julia (https://julialang.org/) for full support.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing Julia provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private bool TryDetectJulia()
        {
            var commonPaths = new[]
            {
                "julia",
                @"C:\Users\%USERNAME%\AppData\Local\Programs\Julia\*\bin\julia.exe",
                @"/usr/bin/julia",
                @"/usr/local/bin/julia",
                @"/opt/homebrew/bin/julia"
            };

            foreach (var path in commonPaths)
            {
                try
                {
                    if (path.Contains("*") || path.Contains("%"))
                    {
                        var expandedPath = Environment.ExpandEnvironmentVariables(path);
                        var dir = Path.GetDirectoryName(expandedPath);
                        var pattern = Path.GetFileName(expandedPath);
                        if (Directory.Exists(dir))
                        {
                            var matches = Directory.GetFiles(dir, pattern);
                            if (matches.Length > 0)
                            {
                                _juliaPath = matches[0];
                                return true;
                            }
                        }
                        continue;
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "--version",
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
                                _juliaPath = path;
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
                    OASISErrorHandling.HandleError(ref result, "Julia file not found.");
                    return Task.FromResult(result);
                }

                var scriptContent = File.ReadAllText(libraryPath);
                var libraryId = Guid.NewGuid().ToString();
                var libraryName = Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedScripts[libraryId] = new JuliaLibraryInfo
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

                result.Message = "Julia library loaded. Function signatures will be extracted from source code.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading Julia library: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error unloading Julia library: {ex.Message}", ex);
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

                    if (!_juliaAvailable)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            "Julia runtime is not available. Cannot execute Julia code. " +
                            "Install Julia (https://julialang.org/) for code execution. " +
                            "Signature extraction works without runtime, but code execution requires it.");
                        return Task.FromResult(result);
                    }

                    return ExecuteJuliaFunctionAsync<T>(scriptInfo, functionName, parameters);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking Julia function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteJuliaFunctionAsync<T>(JuliaLibraryInfo scriptInfo, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                var tempScript = Path.GetTempFileName() + ".jl";
                var scriptDir = Path.GetDirectoryName(scriptInfo.ScriptPath);
                var scriptFile = Path.GetFileName(scriptInfo.ScriptPath);

                var scriptBuilder = new StringBuilder();
                scriptBuilder.AppendLine("using JSON");
                scriptBuilder.AppendLine($"include(\"{scriptFile}\")");
                scriptBuilder.AppendLine($"result = {functionName}({BuildJuliaParameters(parameters)})");
                scriptBuilder.AppendLine("println(JSON.json(result))");

                File.WriteAllText(tempScript, scriptBuilder.ToString());

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = _juliaPath,
                        Arguments = $"\"{tempScript}\"",
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
                                OASISErrorHandling.HandleError(ref result, $"Julia execution error: {error}");
                                return Task.FromResult(result);
                            }

                            result.Result = ParseJuliaOutput<T>(output);
                            result.Message = $"Julia function '{functionName}' executed successfully.";
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
                OASISErrorHandling.HandleError(ref result, $"Error executing Julia function '{functionName}': {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private string BuildJuliaParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return string.Empty;

            var paramStrings = new List<string>();
            foreach (var param in parameters)
            {
                if (param == null)
                    paramStrings.Add("nothing");
                else if (param is string)
                    paramStrings.Add($"\"{param.ToString().Replace("\"", "\\\"")}\"");
                else if (param is bool)
                    paramStrings.Add((bool)param ? "true" : "false");
                else if (param is int || param is long || param is double || param is float || param is decimal)
                    paramStrings.Add(param.ToString());
                else
                    paramStrings.Add($"JSON.parse(\"{JsonSerializer.Serialize(param).Replace("\"", "\\\"")}\")");
            }

            return string.Join(", ", paramStrings);
        }

        private T ParseJuliaOutput<T>(string output)
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

                    var signatures = ParseJuliaSignatures(scriptInfo.ScriptContent);
                    var functionNames = signatures.Select(s => s.FunctionName).ToList();

                    result.Result = functionNames;
                    result.Message = functionNames.Count > 0
                        ? $"Found {functionNames.Count} functions in Julia library."
                        : "No functions found in Julia library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Julia functions: {ex.Message}", ex);
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
                        LibraryName = "Julia Library",
                        Language = "Julia",
                        Framework = "Julia",
                        CustomProperties = new Dictionary<string, object>()
                    };

                    result.Result = metadata;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Julia metadata: {ex.Message}", ex);
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

                    var signatures = ParseJuliaSignatures(scriptInfo.ScriptContent);

                    result.Result = signatures;
                    result.Message = signatures.Count > 0
                        ? $"Found {signatures.Count} function signatures in Julia library."
                        : "No function signatures found in Julia library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Julia function signatures: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error disposing Julia provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParseJuliaSignatures(string scriptContent)
        {
            var signatures = new List<IFunctionSignature>();

            if (string.IsNullOrWhiteSpace(scriptContent))
                return signatures;

            try
            {
                // Parse Julia function definitions: function function_name(param1::Type1, param2::Type2 = default) ... end
                var functionPattern = @"function\s+(\w+)\s*\(([^)]*)\)";
                var functionMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    functionPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in functionMatches)
                {
                    var functionName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Value;

                    var parameters = ParseJuliaParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = functionName,
                        ReturnType = "Any", // Julia is dynamically typed but can have type annotations
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

        private List<IParameterInfo> ParseJuliaParameters(string paramsStr)
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
                // Julia parameter: name or name::Type or name = default or name::Type = default
                var paramMatch = System.Text.RegularExpressions.Regex.Match(
                    paramPart,
                    @"^(\w+)(?:::(.+?))?(?:\s*=\s*(.+))?$");

                if (paramMatch.Success)
                {
                    var name = paramMatch.Groups[1].Value;
                    var type = paramMatch.Groups[2].Success ? paramMatch.Groups[2].Value.Trim() : "Any";
                    var hasDefault = paramMatch.Groups[3].Success;

                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = name,
                        Type = MapJuliaTypeToCSharp(type),
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

        private string MapJuliaTypeToCSharp(string juliaType)
        {
            if (string.IsNullOrWhiteSpace(juliaType) || juliaType == "Any")
                return "object";

            return juliaType.Trim() switch
            {
                "String" => "string",
                "Int" or "Int64" => "long",
                "Int32" => "int",
                "Float64" => "double",
                "Float32" => "float",
                "Bool" => "bool",
                "Array" => "object[]",
                _ => "object"
            };
        }
    }
}

