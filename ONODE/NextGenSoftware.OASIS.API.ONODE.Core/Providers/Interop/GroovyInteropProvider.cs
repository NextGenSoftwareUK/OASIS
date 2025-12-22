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
    /// Groovy interop provider
    /// Extracts function signatures from Groovy source code
    /// Groovy compiles to JVM bytecode, so can also use Java provider for compiled libraries
    /// </summary>
    public class GroovyInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, GroovyLibraryInfo> _loadedScripts;
        private readonly object _lockObject = new object();
        private bool _initialized = false;
        private bool _groovyAvailable = false;
        private string _groovyPath = null;

        private class GroovyLibraryInfo
        {
            public string ScriptPath { get; set; }
            public string ScriptContent { get; set; }
        }

        public InteropProviderType ProviderType => InteropProviderType.Java; // JVM-based

        public string[] SupportedExtensions => new[]
        {
            ".groovy", ".gvy", ".gy"
        };

        public GroovyInteropProvider()
        {
            _loadedScripts = new Dictionary<string, GroovyLibraryInfo>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                _groovyAvailable = TryDetectGroovy();
                _initialized = true;
                result.Result = true;
                
                if (_groovyAvailable)
                {
                    result.Message = $"Groovy interop provider initialized with Groovy runtime ({_groovyPath}). Both signature extraction and code execution are available.";
                }
                else
                {
                    result.Message = "Groovy interop provider initialized. Function signatures will be extracted from source code. Groovy runtime not found - code execution disabled. Install Groovy (https://groovy-lang.org/) for full support.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing Groovy provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private bool TryDetectGroovy()
        {
            var commonPaths = new[]
            {
                "groovy",
                @"C:\groovy\bin\groovy.bat",
                @"/usr/bin/groovy",
                @"/usr/local/bin/groovy",
                @"/opt/homebrew/bin/groovy"
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
                                _groovyPath = path;
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
                    OASISErrorHandling.HandleError(ref result, "Groovy file not found.");
                    return Task.FromResult(result);
                }

                var scriptContent = File.ReadAllText(libraryPath);
                var libraryId = Guid.NewGuid().ToString();
                var libraryName = Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedScripts[libraryId] = new GroovyLibraryInfo
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

                result.Message = "Groovy library loaded. Function signatures will be extracted from source code.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading Groovy library: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error unloading Groovy library: {ex.Message}", ex);
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

                    if (!_groovyAvailable)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            "Groovy runtime is not available. Cannot execute Groovy code. " +
                            "Install Groovy (https://groovy-lang.org/) for code execution. " +
                            "Signature extraction works without runtime, but code execution requires it.");
                        return Task.FromResult(result);
                    }

                    return ExecuteGroovyFunctionAsync<T>(scriptInfo, functionName, parameters);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking Groovy function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteGroovyFunctionAsync<T>(GroovyLibraryInfo scriptInfo, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                var tempScript = Path.GetTempFileName() + ".groovy";
                var scriptDir = Path.GetDirectoryName(scriptInfo.ScriptPath);
                var scriptFile = Path.GetFileName(scriptInfo.ScriptPath);

                var scriptBuilder = new StringBuilder();
                scriptBuilder.AppendLine("import groovy.json.JsonBuilder");
                scriptBuilder.AppendLine($"evaluate(new File('{scriptFile}'))");
                scriptBuilder.AppendLine($"def result = {functionName}({BuildGroovyParameters(parameters)})");
                scriptBuilder.AppendLine("def json = new JsonBuilder(result)");
                scriptBuilder.AppendLine("println(json.toString())");

                File.WriteAllText(tempScript, scriptBuilder.ToString());

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = _groovyPath,
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
                                OASISErrorHandling.HandleError(ref result, $"Groovy execution error: {error}");
                                return Task.FromResult(result);
                            }

                            result.Result = ParseGroovyOutput<T>(output);
                            result.Message = $"Groovy function '{functionName}' executed successfully.";
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
                OASISErrorHandling.HandleError(ref result, $"Error executing Groovy function '{functionName}': {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private string BuildGroovyParameters(object[] parameters)
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
                    paramStrings.Add($"new groovy.json.JsonSlurper().parseText('{JsonSerializer.Serialize(param).Replace("'", "\\'")}')");
            }

            return string.Join(", ", paramStrings);
        }

        private T ParseGroovyOutput<T>(string output)
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

                    var signatures = ParseGroovySignatures(scriptInfo.ScriptContent);
                    var functionNames = signatures.Select(s => s.FunctionName).ToList();

                    result.Result = functionNames;
                    result.Message = functionNames.Count > 0
                        ? $"Found {functionNames.Count} methods in Groovy library."
                        : "No methods found in Groovy library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Groovy functions: {ex.Message}", ex);
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
                    if (!_loadedScripts.TryGetValue(libraryId, out var scriptInfo))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    var metadata = new LibraryMetadata
                    {
                        LibraryName = "Groovy Library",
                        Language = "Groovy",
                        Framework = "JVM",
                        CustomProperties = new Dictionary<string, object>()
                    };

                    result.Result = metadata;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Groovy metadata: {ex.Message}", ex);
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

                    var signatures = ParseGroovySignatures(scriptInfo.ScriptContent);

                    result.Result = signatures;
                    result.Message = signatures.Count > 0
                        ? $"Found {signatures.Count} method signatures in Groovy library."
                        : "No method signatures found in Groovy library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Groovy function signatures: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error disposing Groovy provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParseGroovySignatures(string scriptContent)
        {
            var signatures = new List<IFunctionSignature>();

            if (string.IsNullOrWhiteSpace(scriptContent))
                return signatures;

            try
            {
                // Parse Groovy method definitions: def methodName(param1, param2 = default) { ... }
                // or: Type methodName(param1, param2) { ... }
                var methodPattern = @"(?:def|\w+)\s+(\w+)\s*\(([^)]*)\)\s*\{";
                var methodMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    methodPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in methodMatches)
                {
                    var methodName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Value;

                    var parameters = ParseGroovyParameters(paramsStr);

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = methodName,
                        ReturnType = "object", // Groovy is dynamically typed
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

        private List<IParameterInfo> ParseGroovyParameters(string paramsStr)
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
                // Groovy parameter: name or name = default or Type name
                var defaultMatch = System.Text.RegularExpressions.Regex.Match(paramPart, @"^(\w+)\s*(?::\s*(\w+))?(?:\s*=\s*(.+))?$");
                if (defaultMatch.Success)
                {
                    var name = defaultMatch.Groups[1].Value;
                    var hasDefault = defaultMatch.Groups[3].Success;

                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = name,
                        Type = "object",
                        IsOptional = hasDefault,
                        DefaultValue = hasDefault ? defaultMatch.Groups[3].Value.Trim() : null
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
    }
}

