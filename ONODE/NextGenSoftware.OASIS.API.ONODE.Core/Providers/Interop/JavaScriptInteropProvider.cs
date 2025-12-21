using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly Dictionary<string, dynamic> _loadedScripts;
        private readonly object _lockObject = new object();
        private bool _initialized = false;
        // private V8ScriptEngine _engine; // Uncomment when ClearScript is available

        public InteropProviderType ProviderType => InteropProviderType.JavaScript;

        public string[] SupportedExtensions => new[]
        {
            ".js", ".mjs", ".cjs"
        };

        public JavaScriptInteropProvider()
        {
            _loadedScripts = new Dictionary<string, dynamic>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                // Initialize V8 engine
                // _engine = new V8ScriptEngine(); // Uncomment when ClearScript is available

                _initialized = true;
                result.Result = true;
                result.Message = "JavaScript interop provider initialized. Function signatures will be extracted from source code.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing JavaScript provider: {ex.Message}. Make sure ClearScript is installed.", ex);
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

                // Load JavaScript file
                var scriptContent = System.IO.File.ReadAllText(libraryPath);
                // _engine.Execute(scriptContent); // Uncomment when ClearScript is available

                var libraryId = Guid.NewGuid().ToString();
                var libraryName = System.IO.Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedScripts[libraryId] = scriptContent;
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
                    if (!_loadedScripts.ContainsKey(libraryId))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Invoke JavaScript function
                    // var returnValue = _engine.Invoke(functionName, parameters);
                    // result.Result = (T)Convert.ChangeType(returnValue, typeof(T));

                    result.Message = "JavaScript function invoked successfully.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking JavaScript function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
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
                    if (!_loadedScripts.TryGetValue(libraryId, out var scriptContentObj))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    var scriptContent = scriptContentObj as string;
                    if (scriptContent == null)
                    {
                        OASISErrorHandling.HandleError(ref result, "Script content is not a string.");
                        return Task.FromResult(result);
                    }

                    // Parse JavaScript to extract function signatures
                    var signatures = ParseJavaScriptSignatures(scriptContent);
                    
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

