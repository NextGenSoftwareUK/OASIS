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
    /// TypeScript interop provider
    /// Extracts function signatures from TypeScript source code
    /// </summary>
    public class TypeScriptInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, TypeScriptLibraryInfo> _loadedScripts;
        private readonly object _lockObject = new object();
        private bool _initialized = false;
        private bool _tscAvailable = false;
        private bool _nodeAvailable = false;
        private string _tscPath = null;
        private string _nodePath = null;

        private class TypeScriptLibraryInfo
        {
            public string ScriptPath { get; set; }
            public string ScriptContent { get; set; }
        }

        public InteropProviderType ProviderType => InteropProviderType.TypeScript;

        public string[] SupportedExtensions => new[]
        {
            ".ts", ".tsx"
        };

        public TypeScriptInteropProvider()
        {
            _loadedScripts = new Dictionary<string, string>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                _tscAvailable = TryDetectTypeScript();
                _nodeAvailable = TryDetectNodeJs();
                _initialized = true;
                result.Result = true;
                
                if (_tscAvailable && _nodeAvailable)
                {
                    result.Message = $"TypeScript interop provider initialized with TypeScript compiler ({_tscPath}) and Node.js ({_nodePath}). Both signature extraction and code execution are available.";
                }
                else
                {
                    result.Message = "TypeScript interop provider initialized. Function signatures will be extracted from source code. TypeScript compiler or Node.js not found - code execution disabled. Install TypeScript (npm install -g typescript) and Node.js for full support.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing TypeScript provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private bool TryDetectTypeScript()
        {
            var commonPaths = new[]
            {
                "tsc",
                "npx tsc"
            };

            foreach (var path in commonPaths)
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = path.Contains(" ") ? path.Split(' ')[0] : path,
                        Arguments = path.Contains(" ") ? string.Join(" ", path.Split(' ').Skip(1)) + " --version" : "--version",
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
                                _tscPath = path;
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

        private bool TryDetectNodeJs()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "node",
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
                            _nodePath = "node";
                            return true;
                        }
                    }
                }
            }
            catch
            {
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
                    OASISErrorHandling.HandleError(ref result, "TypeScript file not found.");
                    return Task.FromResult(result);
                }

                var scriptContent = File.ReadAllText(libraryPath);
                var libraryId = Guid.NewGuid().ToString();
                var libraryName = Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedScripts[libraryId] = new TypeScriptLibraryInfo
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

                result.Message = "TypeScript library loaded. Function signatures will be extracted from source code.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading TypeScript library: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error unloading TypeScript library: {ex.Message}", ex);
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

                    if (!_tscAvailable || !_nodeAvailable)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            "TypeScript compiler or Node.js runtime is not available. Cannot execute TypeScript code. " +
                            "Install TypeScript (npm install -g typescript) and Node.js for code execution. " +
                            "Signature extraction works without runtime, but code execution requires it.");
                        return Task.FromResult(result);
                    }

                    return ExecuteTypeScriptFunctionAsync<T>(scriptInfo, functionName, parameters);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking TypeScript function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteTypeScriptFunctionAsync<T>(TypeScriptLibraryInfo scriptInfo, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                var scriptDir = Path.GetDirectoryName(scriptInfo.ScriptPath);
                var scriptFile = Path.GetFileName(scriptInfo.ScriptPath);
                var compiledJs = Path.Combine(scriptDir, Path.GetFileNameWithoutExtension(scriptFile) + ".js");
                var tempScript = Path.GetTempFileName() + ".js";

                // Compile TypeScript to JavaScript
                var compileInfo = new ProcessStartInfo
                {
                    FileName = _tscPath.Contains(" ") ? _tscPath.Split(' ')[0] : _tscPath,
                    Arguments = $"{(_tscPath.Contains(" ") ? string.Join(" ", _tscPath.Split(' ').Skip(1)) + " " : "")}\"{scriptInfo.ScriptPath}\" --outDir \"{scriptDir}\"",
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
                            OASISErrorHandling.HandleError(ref result, $"TypeScript compilation error: {error}");
                            return Task.FromResult(result);
                        }
                    }
                }

                // Execute compiled JavaScript
                var scriptBuilder = new StringBuilder();
                scriptBuilder.AppendLine($"const lib = require('./{Path.GetFileName(compiledJs)}');");
                scriptBuilder.AppendLine($"const result = lib.{functionName}({BuildJavaScriptParameters(parameters)});");
                scriptBuilder.AppendLine("console.log(JSON.stringify(result));");

                File.WriteAllText(tempScript, scriptBuilder.ToString());

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = _nodePath,
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
                                OASISErrorHandling.HandleError(ref result, $"TypeScript execution error: {error}");
                                return Task.FromResult(result);
                            }

                            result.Result = ParseTypeScriptOutput<T>(output);
                            result.Message = $"TypeScript function '{functionName}' executed successfully.";
                        }
                    }
                }
                finally
                {
                    try { File.Delete(tempScript); } catch { }
                    try { File.Delete(compiledJs); } catch { }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing TypeScript function '{functionName}': {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private string BuildJavaScriptParameters(object[] parameters)
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
                else if (param is bool || param is int || param is long || param is double || param is float || param is decimal)
                    paramStrings.Add(param.ToString());
                else
                    paramStrings.Add(JsonSerializer.Serialize(param));
            }

            return string.Join(", ", paramStrings);
        }

        private T ParseTypeScriptOutput<T>(string output)
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
                    if (!_loadedScripts.TryGetValue(libraryId, out var scriptContent))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    var signatures = ParseTypeScriptSignatures(scriptContent);
                    var functionNames = signatures.Select(s => s.FunctionName).ToList();

                    result.Result = functionNames;
                    result.Message = functionNames.Count > 0
                        ? $"Found {functionNames.Count} functions in TypeScript library."
                        : "No functions found in TypeScript library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting TypeScript functions: {ex.Message}", ex);
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
                        LibraryName = "TypeScript Library",
                        Language = "TypeScript",
                        Framework = "TypeScript",
                        CustomProperties = new Dictionary<string, object>()
                    };

                    result.Result = metadata;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting TypeScript metadata: {ex.Message}", ex);
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
                    if (!_loadedScripts.TryGetValue(libraryId, out var scriptContent))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    var signatures = ParseTypeScriptSignatures(scriptContent);

                    result.Result = signatures;
                    result.Message = signatures.Count > 0
                        ? $"Found {signatures.Count} function signatures in TypeScript library."
                        : "No function signatures found in TypeScript library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting TypeScript function signatures: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error disposing TypeScript provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParseTypeScriptSignatures(string scriptContent)
        {
            var signatures = new List<IFunctionSignature>();

            if (string.IsNullOrWhiteSpace(scriptContent))
                return signatures;

            try
            {
                // Parse TypeScript function declarations with types
                // function name(param1: type1, param2: type2): returnType { ... }
                var functionPattern = @"function\s+(\w+)\s*\(([^)]*)\)\s*(?::\s*([^\{]+))?";
                var functionMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    functionPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in functionMatches)
                {
                    var functionName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Value;
                    var returnType = match.Groups[3].Success ? match.Groups[3].Value.Trim() : "any";

                    var parameters = ParseTypeScriptParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = functionName,
                        ReturnType = MapTypeScriptTypeToCSharp(returnType),
                        Parameters = parameters,
                        Documentation = ExtractTSDocComment(scriptContent, match.Index)
                    });
                }

                // Parse arrow functions with types: const name = (param1: type1) => type2
                var arrowPattern = @"(?:const|let|var|export\s+(?:const|let|var)?)\s+(\w+)\s*[:=]\s*(?:\(([^)]*)\)|(\w+))\s*(?::\s*([^=]+))?\s*=>";
                var arrowMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    arrowPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in arrowMatches)
                {
                    var functionName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Success ? match.Groups[2].Value : "";
                    var returnType = match.Groups[4].Success ? match.Groups[4].Value.Trim() : "any";

                    var parameters = ParseTypeScriptParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = functionName,
                        ReturnType = MapTypeScriptTypeToCSharp(returnType),
                        Parameters = parameters
                    });
                }

                // Parse class methods: methodName(param1: type1): returnType { ... }
                var methodPattern = @"(?:public|private|protected)?\s*(\w+)\s*\(([^)]*)\)\s*(?::\s*([^\{]+))?";
                var methodMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    methodPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in methodMatches)
                {
                    var methodName = match.Groups[1].Value;
                    if (methodName == "constructor" || methodName == "get" || methodName == "set")
                        continue;

                    var paramsStr = match.Groups[2].Value;
                    var returnType = match.Groups[3].Success ? match.Groups[3].Value.Trim() : "any";

                    var parameters = ParseTypeScriptParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = methodName,
                        ReturnType = MapTypeScriptTypeToCSharp(returnType),
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

        private List<IParameterInfo> ParseTypeScriptParameters(string paramsStr)
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
                // TypeScript parameter: name: type or name?: type or name: type = default
                var optionalMatch = System.Text.RegularExpressions.Regex.Match(paramPart, @"^(\w+)\??\s*:\s*([^=]+)(?:\s*=\s*(.+))?$");
                if (optionalMatch.Success)
                {
                    var name = optionalMatch.Groups[1].Value;
                    var type = optionalMatch.Groups[2].Value.Trim();
                    var hasDefault = optionalMatch.Groups[3].Success;

                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = name,
                        Type = MapTypeScriptTypeToCSharp(type),
                        IsOptional = paramPart.Contains("?") || hasDefault,
                        DefaultValue = hasDefault ? optionalMatch.Groups[3].Value.Trim() : null
                    });
                }
                else
                {
                    // Simple parameter name
                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = paramPart,
                        Type = "object"
                    });
                }
            }

            return parameters;
        }

        private string MapTypeScriptTypeToCSharp(string tsType)
        {
            if (string.IsNullOrWhiteSpace(tsType))
                return "object";

            return tsType.Trim() switch
            {
                "string" => "string",
                "number" => "double",
                "boolean" => "bool",
                "any" => "object",
                "void" => "void",
                "null" => "object",
                "undefined" => "object",
                _ => tsType.Contains("[]") ? tsType.Replace("[]", "[]") : "object"
            };
        }

        private string ExtractTSDocComment(string scriptContent, int functionIndex)
        {
            try
            {
                var beforeFunc = scriptContent.Substring(0, functionIndex);
                var docMatch = System.Text.RegularExpressions.Regex.Match(
                    beforeFunc,
                    @"/\*\*[\s\S]*?\*/",
                    System.Text.RegularExpressions.RegexOptions.RightToLeft);

                if (docMatch.Success)
                {
                    var doc = docMatch.Value;
                    var descMatch = System.Text.RegularExpressions.Regex.Match(doc, @"\*\s+(.+)");
                    if (descMatch.Success)
                    {
                        return descMatch.Groups[1].Value.Trim();
                    }
                }
            }
            catch
            {
                // Extraction failed
            }

            return string.Empty;
        }
    }
}

