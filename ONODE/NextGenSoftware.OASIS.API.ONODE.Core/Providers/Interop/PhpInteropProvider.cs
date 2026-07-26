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
    /// PHP interop provider
    /// Extracts function signatures from PHP source code
    /// </summary>
    public class PhpInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, PhpLibraryInfo> _loadedScripts;
        private readonly object _lockObject = new object();
        private bool _initialized = false;
        private bool _phpAvailable = false;
        private string _phpPath = null;

        private class PhpLibraryInfo
        {
            public string ScriptPath { get; set; }
            public string ScriptContent { get; set; }
        }

        public InteropProviderType ProviderType => InteropProviderType.PHP;

        public string[] SupportedExtensions => new[]
        {
            ".php"
        };

        public PhpInteropProvider()
        {
            _loadedScripts = new Dictionary<string, PhpLibraryInfo>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                _phpAvailable = TryDetectPhp();
                _initialized = true;
                result.Result = true;
                
                if (_phpAvailable)
                {
                    result.Message = $"PHP interop provider initialized with PHP runtime ({_phpPath}). Both signature extraction and code execution are available.";
                }
                else
                {
                    result.Message = "PHP interop provider initialized. Function signatures will be extracted from source code. PHP runtime not found - code execution disabled. Install PHP for full support.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing PHP provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private bool TryDetectPhp()
        {
            var commonPaths = new[]
            {
                "php",
                @"C:\php\php.exe",
                @"/usr/bin/php",
                @"/usr/local/bin/php",
                @"/opt/homebrew/bin/php"
            };

            foreach (var path in commonPaths)
            {
                try
                {
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
                                _phpPath = path;
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
                    OASISErrorHandling.HandleError(ref result, "PHP file not found.");
                    return Task.FromResult(result);
                }

                var scriptContent = File.ReadAllText(libraryPath);
                var libraryId = Guid.NewGuid().ToString();
                var libraryName = Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedScripts[libraryId] = new PhpLibraryInfo
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

                result.Message = "PHP library loaded. Function signatures will be extracted from source code.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading PHP library: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error unloading PHP library: {ex.Message}", ex);
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

                    if (!_phpAvailable)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            "PHP runtime is not available. Cannot execute PHP code. " +
                            "Install PHP (https://www.php.net/) for code execution. " +
                            "Signature extraction works without runtime, but code execution requires it.");
                        return Task.FromResult(result);
                    }

                    return ExecutePhpFunctionAsync<T>(scriptInfo, functionName, parameters);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking PHP function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecutePhpFunctionAsync<T>(PhpLibraryInfo scriptInfo, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                var tempScript = Path.GetTempFileName() + ".php";
                var scriptDir = Path.GetDirectoryName(scriptInfo.ScriptPath);
                var scriptFile = Path.GetFileName(scriptInfo.ScriptPath);

                var scriptBuilder = new StringBuilder();
                scriptBuilder.AppendLine("<?php");
                scriptBuilder.AppendLine($"require_once '{scriptFile}';");
                scriptBuilder.AppendLine($"$result = {functionName}({BuildPhpParameters(parameters)});");
                scriptBuilder.AppendLine("echo json_encode($result);");

                File.WriteAllText(tempScript, scriptBuilder.ToString());

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = _phpPath,
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
                                OASISErrorHandling.HandleError(ref result, $"PHP execution error: {error}");
                                return Task.FromResult(result);
                            }

                            result.Result = ParsePhpOutput<T>(output);
                            result.Message = $"PHP function '{functionName}' executed successfully.";
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
                OASISErrorHandling.HandleError(ref result, $"Error executing PHP function '{functionName}': {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private string BuildPhpParameters(object[] parameters)
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
                    paramStrings.Add($"json_decode('{JsonSerializer.Serialize(param).Replace("'", "\\'")}')");
            }

            return string.Join(", ", paramStrings);
        }

        private T ParsePhpOutput<T>(string output)
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

                    var signatures = ParsePhpSignatures(scriptInfo.ScriptContent);
                    var functionNames = signatures.Select(s => s.FunctionName).ToList();

                    result.Result = functionNames;
                    result.Message = functionNames.Count > 0
                        ? $"Found {functionNames.Count} functions in PHP library."
                        : "No functions found in PHP library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting PHP functions: {ex.Message}", ex);
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
                        LibraryName = "PHP Library",
                        Language = "PHP",
                        Framework = "PHP",
                        CustomProperties = new Dictionary<string, object>()
                    };

                    result.Result = metadata;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting PHP metadata: {ex.Message}", ex);
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

                    var signatures = ParsePhpSignatures(scriptInfo.ScriptContent);

                    result.Result = signatures;
                    result.Message = signatures.Count > 0
                        ? $"Found {signatures.Count} function signatures in PHP library."
                        : "No function signatures found in PHP library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting PHP function signatures: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error disposing PHP provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParsePhpSignatures(string scriptContent)
        {
            var signatures = new List<IFunctionSignature>();

            if (string.IsNullOrWhiteSpace(scriptContent))
                return signatures;

            try
            {
                // Parse PHP function definitions: function functionName($param1, $param2 = default): returnType
                var functionPattern = @"function\s+(\w+)\s*\(([^)]*)\)\s*(?::\s*([^\{]+))?";
                var functionMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    functionPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in functionMatches)
                {
                    var functionName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Value;
                    var returnType = match.Groups[3].Success ? match.Groups[3].Value.Trim() : "mixed";

                    var parameters = ParsePhpParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = functionName,
                        ReturnType = MapPhpTypeToCSharp(returnType),
                        Parameters = parameters,
                        Documentation = ExtractPhpDocComment(scriptContent, match.Index)
                    });
                }

                // Parse PHP class methods: public function methodName($param): returnType
                var methodPattern = @"(?:public|private|protected|static)?\s*function\s+(\w+)\s*\(([^)]*)\)\s*(?::\s*([^\{]+))?";
                var methodMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    methodPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in methodMatches)
                {
                    var methodName = match.Groups[1].Value;
                    if (methodName == "__construct" || methodName == "__destruct")
                        continue;

                    var paramsStr = match.Groups[2].Value;
                    var returnType = match.Groups[3].Success ? match.Groups[3].Value.Trim() : "mixed";

                    var parameters = ParsePhpParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = methodName,
                        ReturnType = MapPhpTypeToCSharp(returnType),
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

        private List<IParameterInfo> ParsePhpParameters(string paramsStr)
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
                // PHP parameter: $name or $name = default or ?Type $name or Type $name
                var paramMatch = System.Text.RegularExpressions.Regex.Match(
                    paramPart,
                    @"^(?:\?)?\s*(\w+)?\s*\$(\w+)(?:\s*=\s*(.+))?$");

                if (paramMatch.Success)
                {
                    var type = paramMatch.Groups[1].Success ? paramMatch.Groups[1].Value : "mixed";
                    var name = paramMatch.Groups[2].Value;
                    var hasDefault = paramMatch.Groups[3].Success;

                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = name,
                        Type = MapPhpTypeToCSharp(type),
                        IsOptional = hasDefault || paramPart.Contains("?"),
                        DefaultValue = hasDefault ? paramMatch.Groups[3].Value.Trim() : null
                    });
                }
                else
                {
                    // Simple $name
                    var simpleMatch = System.Text.RegularExpressions.Regex.Match(paramPart, @"\$(\w+)");
                    if (simpleMatch.Success)
                    {
                        parameters.Add(new Objects.Interop.ParameterInfo
                        {
                            Name = simpleMatch.Groups[1].Value,
                            Type = "object"
                        });
                    }
                }
            }

            return parameters;
        }

        private string MapPhpTypeToCSharp(string phpType)
        {
            if (string.IsNullOrWhiteSpace(phpType))
                return "object";

            return phpType.ToLower() switch
            {
                "string" => "string",
                "int" or "integer" => "int",
                "float" or "double" => "double",
                "bool" or "boolean" => "bool",
                "array" => "object[]",
                "object" => "object",
                "mixed" => "object",
                "void" => "void",
                _ => "object"
            };
        }

        private string ExtractPhpDocComment(string scriptContent, int functionIndex)
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

