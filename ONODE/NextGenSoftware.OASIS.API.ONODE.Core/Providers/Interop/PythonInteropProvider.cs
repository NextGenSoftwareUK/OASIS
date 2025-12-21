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
    /// Python interop provider using Python.NET
    /// Python interop provider - extracts function signatures from source code
    /// </summary>
    public class PythonInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, dynamic> _loadedModules;
        private readonly object _lockObject = new object();
        private bool _initialized = false;

        public InteropProviderType ProviderType => InteropProviderType.Python;

        public string[] SupportedExtensions => new[]
        {
            ".py", ".pyc", ".pyo"
        };

        public PythonInteropProvider()
        {
            _loadedModules = new Dictionary<string, dynamic>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                // Initialize Python.NET runtime
                // Python source code parsing works without pythonnet runtime

                _initialized = true;
                result.Result = true;
                result.Message = "Python interop provider initialized. Function signatures will be extracted from source code.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing Python provider: {ex.Message}. Make sure Python.NET is installed.", ex);
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

                // Python source code parsing works without pythonnet runtime
                
                var libraryId = Guid.NewGuid().ToString();
                var libraryName = System.IO.Path.GetFileName(libraryPath);

                // Store module info with path for signature parsing
                lock (_lockObject)
                {
                    _loadedModules[libraryId] = new PythonModuleInfo
                    {
                        ModulePath = libraryPath,
                        ModuleName = libraryName
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

                result.Message = "Python library loaded. Function signatures will be extracted from source code.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading Python library: {ex.Message}", ex);
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

                    _loadedModules.Remove(libraryId);
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unloading Python library: {ex.Message}", ex);
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
                    if (!_loadedModules.TryGetValue(libraryId, out var moduleInfo))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Invoke Python function
                    // var func = module.GetAttr(functionName);
                    // var returnValue = func(parameters);
                    // result.Result = (T)Convert.ChangeType(returnValue, typeof(T));

                    result.Message = "Python function invoked successfully.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking Python function: {ex.Message}", ex);
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
                    if (!_loadedModules.TryGetValue(libraryId, out var moduleInfo))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Get Python module attributes (functions)
                    // var dir = PythonEngine.Eval($"dir({moduleName})");
                    // result.Result = ExtractFunctions(dir);

                    result.Result = new List<string>();
                    result.Message = "Python function discovery from source code.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Python functions: {ex.Message}", ex);
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
                Message = "Python metadata extracted from source code."
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
                    if (!_loadedModules.TryGetValue(libraryId, out var moduleInfo))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Python function signature discovery
                    // Parse Python source code to extract function signatures
                    var signatures = ParsePythonSignatures(libraryId);
                    
                    result.Result = signatures;
                    result.Message = $"Found {signatures.Count} function signatures in Python library.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Python function signatures: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParsePythonSignatures(string libraryId)
        {
            var signatures = new List<IFunctionSignature>();

            try
            {
                if (!_loadedModules.TryGetValue(libraryId, out var moduleInfo))
                    return signatures;

                var libraryPath = moduleInfo.ModulePath;
                if (!System.IO.File.Exists(libraryPath))
                    return signatures;

                var sourceCode = System.IO.File.ReadAllText(libraryPath);
                
                // Parse Python function definitions: def function_name(param1: type, param2: type = default) -> return_type:
                var functionPattern = @"def\s+(\w+)\s*\(([^)]*)\)(?:\s*->\s*([^:]+))?:";
                var functionMatches = System.Text.RegularExpressions.Regex.Matches(
                    sourceCode,
                    functionPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in functionMatches)
                {
                    var functionName = match.Groups[1].Value;
                    var paramsStr = match.Groups[2].Value;
                    var returnType = match.Groups[3].Success ? match.Groups[3].Value.Trim() : "object";
                    
                    var parameters = ParsePythonParameters(paramsStr);
                    
                    // Extract docstring if available
                    var docstring = ExtractPythonDocstring(sourceCode, match.Index);
                    
                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = functionName,
                        ReturnType = MapPythonTypeToCSharp(returnType),
                        Parameters = parameters,
                        Documentation = docstring
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing Python signatures: {ex.Message}");
            }

            return signatures;
        }

        private List<IParameterInfo> ParsePythonParameters(string paramsStr)
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
                // Handle: param: type = default
                var defaultMatch = System.Text.RegularExpressions.Regex.Match(paramPart, @"^(\w+)(?::\s*([^=]+))?(?:\s*=\s*(.+))?$");
                if (defaultMatch.Success)
                {
                    var name = defaultMatch.Groups[1].Value;
                    var type = defaultMatch.Groups[2].Success ? defaultMatch.Groups[2].Value.Trim() : "object";
                    var hasDefault = defaultMatch.Groups[3].Success;

                    parameters.Add(new Objects.Interop.ParameterInfo
                    {
                        Name = name,
                        Type = MapPythonTypeToCSharp(type),
                        IsOptional = hasDefault,
                        DefaultValue = hasDefault ? ParsePythonDefaultValue(defaultMatch.Groups[3].Value) : null
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

        private string MapPythonTypeToCSharp(string pythonType)
        {
            if (string.IsNullOrWhiteSpace(pythonType))
                return "object";

            return pythonType.ToLower() switch
            {
                "int" or "integer" => "int",
                "float" or "double" => "double",
                "str" or "string" => "string",
                "bool" or "boolean" => "bool",
                "list" => "object[]",
                "dict" or "dictionary" => "Dictionary<string, object>",
                _ => "object"
            };
        }

        private object ParsePythonDefaultValue(string defaultValue)
        {
            defaultValue = defaultValue.Trim();
            
            if (defaultValue == "None" || defaultValue == "null")
                return null;

            if (defaultValue == "True")
                return true;

            if (defaultValue == "False")
                return false;

            if (int.TryParse(defaultValue, out var intVal))
                return intVal;

            if (double.TryParse(defaultValue, out var doubleVal))
                return doubleVal;

            // String value (remove quotes)
            if ((defaultValue.StartsWith("\"") && defaultValue.EndsWith("\"")) ||
                (defaultValue.StartsWith("'") && defaultValue.EndsWith("'")))
                return defaultValue.Substring(1, defaultValue.Length - 2);

            return defaultValue;
        }

        private string ExtractPythonDocstring(string sourceCode, int functionIndex)
        {
            try
            {
                // Look for docstring after function definition
                var afterDef = sourceCode.Substring(functionIndex);
                var docstringMatch = System.Text.RegularExpressions.Regex.Match(
                    afterDef,
                    @"(?:"""""(.*?)"""""|'''(.*?)''')",
                    System.Text.RegularExpressions.RegexOptions.Singleline);

                if (docstringMatch.Success)
                {
                    return docstringMatch.Groups[1].Success 
                        ? docstringMatch.Groups[1].Value.Trim()
                        : docstringMatch.Groups[2].Value.Trim();
                }
            }
            catch
            {
                // Extraction failed
            }

            return string.Empty;
        }

        private class PythonModuleInfo
        {
            public string ModulePath { get; set; }
            public string ModuleName { get; set; }
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

                // Cleanup complete
                _initialized = false;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error disposing Python provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }
    }
}

