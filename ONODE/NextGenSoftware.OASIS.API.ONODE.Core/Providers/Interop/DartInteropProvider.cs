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
    /// Dart interop provider
    /// Extracts function signatures from Dart source code
    /// </summary>
    public class DartInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, DartLibraryInfo> _loadedScripts;
        private readonly object _lockObject = new object();
        private bool _initialized = false;
        private bool _dartAvailable = false;
        private string _dartPath = null;

        private class DartLibraryInfo
        {
            public string ScriptPath { get; set; }
            public string ScriptContent { get; set; }
        }

        public InteropProviderType ProviderType => InteropProviderType.Dart;

        public string[] SupportedExtensions => new[]
        {
            ".dart"
        };

        public DartInteropProvider()
        {
            _loadedScripts = new Dictionary<string, string>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                _dartAvailable = TryDetectDart();
                _initialized = true;
                result.Result = true;
                
                if (_dartAvailable)
                {
                    result.Message = $"Dart interop provider initialized with Dart runtime ({_dartPath}). Both signature extraction and code execution are available.";
                }
                else
                {
                    result.Message = "Dart interop provider initialized. Function signatures will be extracted from source code. Dart runtime not found - code execution disabled. Install Dart SDK for full support.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing Dart provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private bool TryDetectDart()
        {
            var commonPaths = new[]
            {
                "dart",
                @"C:\tools\dart-sdk\bin\dart.exe",
                @"/usr/bin/dart",
                @"/usr/local/bin/dart",
                @"/opt/homebrew/bin/dart"
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
                                _dartPath = path;
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
                    OASISErrorHandling.HandleError(ref result, "Dart file not found.");
                    return Task.FromResult(result);
                }

                var scriptContent = File.ReadAllText(libraryPath);
                var libraryId = Guid.NewGuid().ToString();
                var libraryName = Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedScripts[libraryId] = new DartLibraryInfo
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

                result.Message = "Dart library loaded. Function signatures will be extracted from source code.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading Dart library: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error unloading Dart library: {ex.Message}", ex);
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

                    if (!_dartAvailable)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            "Dart runtime is not available. Cannot execute Dart code. " +
                            "Install Dart SDK (https://dart.dev/get-dart) for code execution. " +
                            "Signature extraction works without runtime, but code execution requires it.");
                        return Task.FromResult(result);
                    }

                    return ExecuteDartFunctionAsync<T>(scriptInfo, functionName, parameters);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking Dart function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteDartFunctionAsync<T>(DartLibraryInfo scriptInfo, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                var tempScript = Path.GetTempFileName() + ".dart";
                var scriptDir = Path.GetDirectoryName(scriptInfo.ScriptPath);
                var scriptFile = Path.GetFileName(scriptInfo.ScriptPath);

                var scriptBuilder = new StringBuilder();
                scriptBuilder.AppendLine("import 'dart:convert';");
                scriptBuilder.AppendLine($"import '{scriptFile}';");
                scriptBuilder.AppendLine($"void main() {{");
                scriptBuilder.AppendLine($"  var result = {functionName}({BuildDartParameters(parameters)});");
                scriptBuilder.AppendLine($"  print(jsonEncode(result));");
                scriptBuilder.AppendLine($"}}");

                File.WriteAllText(tempScript, scriptBuilder.ToString());

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = _dartPath,
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
                                OASISErrorHandling.HandleError(ref result, $"Dart execution error: {error}");
                                return Task.FromResult(result);
                            }

                            result.Result = ParseDartOutput<T>(output);
                            result.Message = $"Dart function '{functionName}' executed successfully.";
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
                OASISErrorHandling.HandleError(ref result, $"Error executing Dart function '{functionName}': {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private string BuildDartParameters(object[] parameters)
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
                    paramStrings.Add($"jsonDecode('{JsonSerializer.Serialize(param).Replace("'", "\\'")}')");
            }

            return string.Join(", ", paramStrings);
        }

        private T ParseDartOutput<T>(string output)
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

                    var signatures = ParseDartSignatures(scriptInfo.ScriptContent);
                    var functionNames = signatures.Select(s => s.FunctionName).ToList();

                    result.Result = functionNames;
                    result.Message = functionNames.Count > 0
                        ? $"Found {functionNames.Count} functions in Dart library."
                        : "No functions found in Dart library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Dart functions: {ex.Message}", ex);
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
                        LibraryName = "Dart Library",
                        Language = "Dart",
                        Framework = "Dart",
                        CustomProperties = new Dictionary<string, object>()
                    };

                    result.Result = metadata;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Dart metadata: {ex.Message}", ex);
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

                    var signatures = ParseDartSignatures(scriptInfo.ScriptContent);

                    result.Result = signatures;
                    result.Message = signatures.Count > 0
                        ? $"Found {signatures.Count} function signatures in Dart library."
                        : "No function signatures found in Dart library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Dart function signatures: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error disposing Dart provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParseDartSignatures(string scriptContent)
        {
            var signatures = new List<IFunctionSignature>();

            if (string.IsNullOrWhiteSpace(scriptContent))
                return signatures;

            try
            {
                // Parse Dart function definitions: ReturnType functionName(Type1 param1, Type2 param2) { ... }
                // or: functionName(Type1 param1, Type2 param2) { ... }
                var functionPattern = @"(?:(\w+)\s+)?(\w+)\s*\(([^)]*)\)\s*(?::\s*(\w+))?\s*\{";
                var functionMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    functionPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in functionMatches)
                {
                    var returnType = match.Groups[1].Success ? match.Groups[1].Value : "void";
                    var functionName = match.Groups[2].Value;
                    var paramsStr = match.Groups[3].Value;
                    var explicitReturnType = match.Groups[4].Success ? match.Groups[4].Value : returnType;

                    var parameters = ParseDartParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = functionName,
                        ReturnType = MapDartTypeToCSharp(explicitReturnType),
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

        private List<IParameterInfo> ParseDartParameters(string paramsStr)
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
                // Dart parameter: Type name or Type? name or Type name = default
                var paramMatch = System.Text.RegularExpressions.Regex.Match(
                    paramPart,
                    @"^(\w+)\??\s+(\w+)(?:\s*=\s*(.+))?$");

                if (paramMatch.Success)
                {
                    var type = paramMatch.Groups[1].Value;
                    var name = paramMatch.Groups[2].Value;
                    var hasDefault = paramMatch.Groups[3].Success;

                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = name,
                        Type = MapDartTypeToCSharp(type),
                        IsOptional = paramPart.Contains("?") || hasDefault,
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

        private string MapDartTypeToCSharp(string dartType)
        {
            if (string.IsNullOrWhiteSpace(dartType))
                return "object";

            return dartType.Trim() switch
            {
                "String" => "string",
                "int" => "int",
                "double" => "double",
                "bool" => "bool",
                "List" => "object[]",
                "Map" => "Dictionary<string, object>",
                "void" => "void",
                "dynamic" => "object",
                _ => "object"
            };
        }
    }
}

