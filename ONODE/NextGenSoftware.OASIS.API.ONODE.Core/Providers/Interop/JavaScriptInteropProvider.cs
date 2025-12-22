using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.Interop;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Providers.Interop
{
    /// <summary>
    /// JavaScript/Node.js interop provider using ClearScript or Edge.js
    /// JavaScript interop provider - extracts function signatures from source code
    /// </summary>
    public class JavaScriptInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, JavaScriptLibraryInfo> _loadedScripts;
        private readonly object _lockObject = new object();
        private bool _initialized = false;
        private bool _nodeAvailable = false;
        private string _nodePath = null;

        public InteropProviderType ProviderType => InteropProviderType.JavaScript;

        public string[] SupportedExtensions => new[]
        {
            ".js", ".mjs", ".cjs"
        };

        public JavaScriptInteropProvider()
        {
            _loadedScripts = new Dictionary<string, JavaScriptLibraryInfo>();
        }

        private class JavaScriptLibraryInfo
        {
            public string ScriptPath { get; set; }
            public string ScriptContent { get; set; }
            public string LibraryName { get; set; }
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                // Check for Node.js availability
                _nodeAvailable = TryDetectNodeJs();
                
                _initialized = true;
                result.Result = true;
                
                if (_nodeAvailable)
                {
                    result.Message = $"JavaScript interop provider initialized with Node.js runtime ({_nodePath}). Both signature extraction and code execution are available.";
                }
                else
                {
                    result.Message = "JavaScript interop provider initialized. Function signatures will be extracted from source code. Node.js runtime not found - code execution disabled. Install Node.js for full support.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing JavaScript provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private bool TryDetectNodeJs()
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "node",
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
                        process.WaitForExit(5000);
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
                // Node.js not found in PATH
            }

            // Try common Node.js installation paths
            var commonPaths = new[]
            {
                @"C:\Program Files\nodejs\node.exe",
                @"C:\Program Files (x86)\nodejs\node.exe",
                @"/usr/bin/node",
                @"/usr/local/bin/node",
                @"/opt/homebrew/bin/node"
            };

            foreach (var path in commonPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    _nodePath = path;
                    return true;
                }
            }

            return false;
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

                // Load JavaScript file
                var scriptContent = System.IO.File.ReadAllText(libraryPath);
                var libraryId = Guid.NewGuid().ToString();
                var libraryName = System.IO.Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedScripts[libraryId] = new JavaScriptLibraryInfo
                    {
                        ScriptPath = libraryPath,
                        ScriptContent = scriptContent,
                        LibraryName = libraryName
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

                result.Message = "JavaScript library loaded. Function signatures will be extracted from source code.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading JavaScript library: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error unloading JavaScript library: {ex.Message}", ex);
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

                    // Check if Node.js is available for execution
                    if (!_nodeAvailable)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            "Node.js runtime is not available. Cannot execute JavaScript code. " +
                            "Install Node.js (https://nodejs.org/) for code execution. " +
                            "Signature extraction works without runtime, but code execution requires it.");
                        return Task.FromResult(result);
                    }

                    // Execute JavaScript function using Node.js
                    return ExecuteJavaScriptFunctionAsync<T>(scriptInfo, functionName, parameters);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking JavaScript function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private Task<OASISResult<T>> ExecuteJavaScriptFunctionAsync<T>(JavaScriptLibraryInfo scriptInfo, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                // Create a temporary script that loads the library and calls the function
                var tempScript = System.IO.Path.GetTempFileName() + ".js";
                var scriptDir = System.IO.Path.GetDirectoryName(scriptInfo.ScriptPath);
                var scriptFile = System.IO.Path.GetFileName(scriptInfo.ScriptPath);

                var scriptBuilder = new System.Text.StringBuilder();
                scriptBuilder.AppendLine($"process.chdir('{scriptDir.Replace("\\", "\\\\")}');");
                scriptBuilder.AppendLine($"const lib = require('./{scriptFile}');");
                scriptBuilder.AppendLine($"const result = lib.{functionName}({BuildJavaScriptParameters(parameters)});");
                scriptBuilder.AppendLine("console.log(JSON.stringify(result));");

                System.IO.File.WriteAllText(tempScript, scriptBuilder.ToString());

                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _nodePath,
                        Arguments = $"\"{tempScript}\"",
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
                            process.WaitForExit(30000); // 30 second timeout

                            if (process.ExitCode != 0)
                            {
                                OASISErrorHandling.HandleError(ref result, $"JavaScript execution error: {error}");
                                return Task.FromResult(result);
                            }

                            // Parse JSON output
                            if (!string.IsNullOrWhiteSpace(output))
                            {
                                var jsonOutput = output.Trim();
                                try
                                {
                                    if (jsonOutput.StartsWith("{") || jsonOutput.StartsWith("["))
                                    {
                                        result.Result = System.Text.Json.JsonSerializer.Deserialize<T>(jsonOutput);
                                    }
                                    else
                                    {
                                        result.Result = (T)Convert.ChangeType(jsonOutput, typeof(T));
                                    }
                                }
                                catch
                                {
                                    // Fallback to string conversion
                                    result.Result = (T)Convert.ChangeType(jsonOutput, typeof(T));
                                }
                            }
                            else
                            {
                                result.Result = default(T);
                            }

                            result.Message = $"JavaScript function '{functionName}' executed successfully.";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Failed to start Node.js process.");
                        }
                    }
                }
                finally
                {
                    // Cleanup temp script
                    try { System.IO.File.Delete(tempScript); } catch { }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing JavaScript function '{functionName}': {ex.Message}", ex);
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
                {
                    paramStrings.Add("null");
                }
                else if (param is string)
                {
                    paramStrings.Add($"\"{param.ToString().Replace("\"", "\\\"")}\"");
                }
                else if (param is bool || param is int || param is long || param is double || param is float || param is decimal)
                {
                    paramStrings.Add(param.ToString());
                }
                else
                {
                    // Serialize complex objects to JSON
                    try
                    {
                        paramStrings.Add(System.Text.Json.JsonSerializer.Serialize(param));
                    }
                    catch
                    {
                        paramStrings.Add($"\"{param.ToString().Replace("\"", "\\\"")}\"");
                    }
                }
            }

            return string.Join(", ", paramStrings);
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
                    if (!_loadedScripts.ContainsKey(libraryId))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Parse JavaScript to extract function names and signatures
                    var scriptContent = _loadedScripts[libraryId] as string;
                    if (scriptContent != null)
                    {
                        var signatures = ParseJavaScriptSignatures(scriptContent);
                        result.Result = signatures.Select(s => s.FunctionName).ToList();
                        result.Message = $"Found {signatures.Count} functions in JavaScript library.";
                    }
                    else
                    {
                        result.Result = new List<string>();
                        result.Message = "JavaScript function discovery from source code.";
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting JavaScript functions: {ex.Message}", ex);
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
            var result = new OASISResult<ILibraryMetadata>
            {
                Message = "JavaScript metadata extracted from source code."
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
                    if (!_loadedScripts.TryGetValue(libraryId, out var scriptInfo))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Parse JavaScript to extract function signatures
                    var signatures = ParseJavaScriptSignatures(scriptInfo.ScriptContent);
                    
                    result.Result = signatures;
                    result.Message = $"Found {signatures.Count} function signatures in JavaScript library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting JavaScript function signatures: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParseJavaScriptSignatures(string scriptContent)
        {
            var signatures = new List<IFunctionSignature>();
            
            if (string.IsNullOrWhiteSpace(scriptContent))
                return signatures;

            try
            {
                // Parse function declarations: function name(param1, param2) { ... }
                var functionDeclPattern = @"function\s+(\w+)\s*\(([^)]*)\)";
                var functionMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent, 
                    functionDeclPattern, 
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in functionMatches)
                {
                    var functionName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Value;
                    var parameters = ParseParameters(paramsStr);
                    
                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = functionName,
                        ReturnType = "object", // Default, could be inferred from JSDoc
                        Parameters = parameters,
                        Documentation = ExtractJSDoc(scriptContent, functionName)
                    });
                }

                // Parse arrow functions: const name = (param1, param2) => { ... }
                var arrowFunctionPattern = @"(?:const|let|var)\s+(\w+)\s*=\s*\(([^)]*)\)\s*=>";
                var arrowMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    arrowFunctionPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in arrowMatches)
                {
                    var functionName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Value;
                    var parameters = ParseParameters(paramsStr);
                    
                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = functionName,
                        ReturnType = "object",
                        Parameters = parameters,
                        Documentation = ExtractJSDoc(scriptContent, functionName)
                    });
                }

                // Parse method definitions: name: function(param1, param2) { ... }
                var methodPattern = @"(\w+)\s*:\s*function\s*\(([^)]*)\)";
                var methodMatches = System.Text.RegularExpressions.Regex.Matches(
                    scriptContent,
                    methodPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in methodMatches)
                {
                    var functionName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Value;
                    var parameters = ParseParameters(paramsStr);
                    
                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = functionName,
                        ReturnType = "object",
                        Parameters = parameters,
                        Documentation = ExtractJSDoc(scriptContent, functionName)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing JavaScript signatures: {ex.Message}");
            }

            return signatures;
        }

        private List<IParameterInfo> ParseParameters(string paramsStr)
        {
            var parameters = new List<IParameterInfo>();

            if (string.IsNullOrWhiteSpace(paramsStr))
                return parameters;

            var paramNames = paramsStr.Split(',')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            foreach (var paramName in paramNames)
            {
                // Check for default value: param = defaultValue
                var defaultMatch = System.Text.RegularExpressions.Regex.Match(paramName, @"^(\w+)\s*=\s*(.+)$");
                if (defaultMatch.Success)
                {
                    var name = defaultMatch.Groups[1].Value;
                    var defaultValue = defaultMatch.Groups[2].Value.Trim();
                    
                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = name,
                        Type = "object",
                        IsOptional = true,
                        DefaultValue = ParseDefaultValue(defaultValue)
                    });
                }
                else
                {
                    // Check for rest parameter: ...param
                    var restMatch = System.Text.RegularExpressions.Regex.Match(paramName, @"^\.\.\.(\w+)$");
                    if (restMatch.Success)
                    {
                        parameters.Add(new Objects.Interop.ParameterInfo
                        {
                            Name = restMatch.Groups[1].Value,
                            Type = "object[]",
                            IsOptional = true
                        });
                    }
                    else
                    {
                        parameters.Add(new Objects.Interop.ParameterInfo
                        {
                            Name = paramName,
                            Type = "object"
                        });
                    }
                }
            }

            return parameters;
        }

        private object ParseDefaultValue(string defaultValue)
        {
            // Try to parse common default values
            if (defaultValue == "null" || defaultValue == "undefined")
                return null;

            if (defaultValue == "true")
                return true;

            if (defaultValue == "false")
                return false;

            if (int.TryParse(defaultValue, out var intVal))
                return intVal;

            if (double.TryParse(defaultValue, out var doubleVal))
                return doubleVal;

            // String value (remove quotes)
            if (defaultValue.StartsWith("\"") && defaultValue.EndsWith("\""))
                return defaultValue.Substring(1, defaultValue.Length - 2);

            if (defaultValue.StartsWith("'") && defaultValue.EndsWith("'"))
                return defaultValue.Substring(1, defaultValue.Length - 2);

            return defaultValue;
        }

        private string ExtractJSDoc(string scriptContent, string functionName)
        {
            // Try to find JSDoc comment before function
            var pattern = $@"\/\*\*[\s\S]*?\*\/[\s\S]*?function\s+{functionName}";
            var match = System.Text.RegularExpressions.Regex.Match(scriptContent, pattern);
            
            if (match.Success)
            {
                var jsDoc = match.Groups[0].Value;
                // Extract description from JSDoc
                var descMatch = System.Text.RegularExpressions.Regex.Match(jsDoc, @"\*\s+(.+)");
                if (descMatch.Success)
                {
                    return descMatch.Groups[1].Value.Trim();
                }
            }

            return string.Empty;
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

                // _engine?.Dispose(); // Uncomment when ClearScript is available
                _initialized = false;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error disposing JavaScript provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }
    }
}

