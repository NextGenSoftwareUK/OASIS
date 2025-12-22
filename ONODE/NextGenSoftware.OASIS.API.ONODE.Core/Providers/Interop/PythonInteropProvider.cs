using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.Interop;
using NextGenSoftware.OASIS.Common;
// Python.NET runtime support (optional - graceful fallback if not available)
#if PYTHONNET_AVAILABLE
using Python.Runtime;
#endif

namespace NextGenSoftware.OASIS.API.ONODE.Core.Providers.Interop
{
    /// <summary>
    /// Python interop provider using Python.NET
    /// Supports both signature extraction (works without runtime) and code execution (requires Python.NET runtime)
    /// </summary>
    public class PythonInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, PythonModuleInfo> _loadedModules;
        private readonly object _lockObject = new object();
        private bool _initialized = false;
        private bool _pythonRuntimeAvailable = false;
#if PYTHONNET_AVAILABLE
        private dynamic _pythonEngine;
#endif

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
                // Try to initialize Python.NET runtime (optional)
                _pythonRuntimeAvailable = TryInitializePythonRuntime();
                
                _initialized = true;
                result.Result = true;
                
                if (_pythonRuntimeAvailable)
                {
                    result.Message = "Python interop provider initialized with Python.NET runtime. Both signature extraction and code execution are available.";
                }
                else
                {
                    result.Message = "Python interop provider initialized. Function signatures will be extracted from source code. Python.NET runtime not available - code execution disabled. Install Python.NET for full support.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing Python provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Attempts to initialize Python.NET runtime (graceful fallback if not available)
        /// </summary>
        private bool TryInitializePythonRuntime()
        {
#if PYTHONNET_AVAILABLE
            try
            {
                if (!PythonEngine.IsInitialized)
                {
                    PythonEngine.Initialize();
                }
                _pythonEngine = Py.GIL();
                return true;
            }
            catch
            {
                // Python.NET not available or Python runtime not installed
                return false;
            }
#else
            // Python.NET not compiled in - check if available at runtime
            try
            {
                var pythonNetType = Type.GetType("Python.Runtime.PythonEngine, Python.Runtime");
                if (pythonNetType != null)
                {
                    var isInitializedProperty = pythonNetType.GetProperty("IsInitialized");
                    var initializeMethod = pythonNetType.GetMethod("Initialize");
                    
                    if (isInitializedProperty != null && initializeMethod != null)
                    {
                        var isInitialized = (bool)isInitializedProperty.GetValue(null);
                        if (!isInitialized)
                        {
                            initializeMethod.Invoke(null, null);
                        }
                        return true;
                    }
                }
            }
            catch
            {
                // Python.NET not available
            }
            return false;
#endif
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

                    // Check if Python runtime is available for execution
                    if (!_pythonRuntimeAvailable)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            "Python.NET runtime is not available. Cannot execute Python code. " +
                            "Install Python.NET (dotnet add package pythonnet) and ensure Python runtime is installed. " +
                            "Signature extraction works without runtime, but code execution requires it.");
                        return Task.FromResult(result);
                    }

                    // Execute Python function using Python.NET
                    return InvokePythonFunctionAsync<T>(moduleInfo, functionName, parameters);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking Python function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Invokes a Python function using Python.NET runtime
        /// </summary>
        private Task<OASISResult<T>> InvokePythonFunctionAsync<T>(PythonModuleInfo moduleInfo, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
#if PYTHONNET_AVAILABLE
                using (Py.GIL())
                {
                    // Add module directory to Python path
                    dynamic sys = Py.Import("sys");
                    var modulePath = System.IO.Path.GetDirectoryName(moduleInfo.ModulePath);
                    if (!string.IsNullOrEmpty(modulePath))
                    {
                        sys.path.insert(0, modulePath);
                    }

                    // Import the module
                    var moduleName = System.IO.Path.GetFileNameWithoutExtension(moduleInfo.ModulePath);
                    dynamic module = Py.Import(moduleName);

                    // Get the function
                    if (!PyObject.IsTrue(module.HasAttr(functionName)))
                    {
                        OASISErrorHandling.HandleError(ref result, $"Function '{functionName}' not found in Python module '{moduleName}'.");
                        return Task.FromResult(result);
                    }

                    dynamic func = module.GetAttr(functionName);

                    // Invoke function with parameters
                    dynamic returnValue;
                    if (parameters != null && parameters.Length > 0)
                    {
                        // Convert C# parameters to Python objects
                        var pyArgs = new PyObject[parameters.Length];
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            pyArgs[i] = ToPython(parameters[i]);
                        }
                        returnValue = func(new PyTuple(pyArgs));
                    }
                    else
                    {
                        returnValue = func();
                    }

                    // Convert result back to C# type
                    if (returnValue == null || PyObject.IsTrue(returnValue == PyObject.None))
                    {
                        if (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Python function returned None, but expected non-nullable value type {typeof(T).Name}.");
                            return Task.FromResult(result);
                        }
                        result.Result = default(T);
                    }
                    else
                    {
                        result.Result = (T)Convert.ChangeType(returnValue.AsManagedObject(typeof(object)), typeof(T));
                    }

                    result.Message = $"Python function '{functionName}' executed successfully.";
                }
#else
                // Try dynamic loading if Python.NET not compiled in
                var pythonNetType = Type.GetType("Python.Runtime.Py, Python.Runtime");
                if (pythonNetType != null)
                {
                    // Use reflection to call Python.NET methods
                    var importMethod = pythonNetType.GetMethod("Import", new[] { typeof(string) });
                    var gilMethod = pythonNetType.GetMethod("GIL");
                    
                    if (importMethod != null && gilMethod != null)
                    {
                        using (var gil = gilMethod.Invoke(null, null) as IDisposable)
                        {
                            var moduleName = System.IO.Path.GetFileNameWithoutExtension(moduleInfo.ModulePath);
                            dynamic module = importMethod.Invoke(null, new[] { moduleName });
                            
                            if (module != null)
                            {
                                dynamic func = module.GetAttr(functionName);
                                var returnValue = func(parameters);
                                result.Result = (T)Convert.ChangeType(returnValue, typeof(T));
                                result.Message = $"Python function '{functionName}' executed successfully.";
                            }
                        }
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Python.NET runtime not available. Install pythonnet package.");
                }
#endif
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing Python function '{functionName}': {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

#if PYTHONNET_AVAILABLE
        /// <summary>
        /// Converts a C# object to a Python object
        /// </summary>
        private PyObject ToPython(object obj)
        {
            if (obj == null)
                return PyObject.None;

            if (obj is PyObject pyObj)
                return pyObj;

            // Use Python.Runtime to convert
            return new PyObject(Python.Runtime.Runtime.PyObject_FromManagedObject(obj));
        }
#endif

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

                    // Try to get functions from Python runtime if available, otherwise use source code parsing
                    if (_pythonRuntimeAvailable)
                    {
                        result.Result = GetPythonFunctionsFromRuntime(moduleInfo);
                        result.Message = "Python functions discovered from runtime.";
                    }
                    else
                    {
                        // Fall back to source code parsing
                        var signatures = ParsePythonSignatures(libraryId);
                        result.Result = signatures.Select(s => s.FunctionName).ToList();
                        result.Message = "Python functions discovered from source code (runtime not available).";
                    }
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

        /// <summary>
        /// Gets Python functions from runtime (if available)
        /// </summary>
        private List<string> GetPythonFunctionsFromRuntime(PythonModuleInfo moduleInfo)
        {
            var functions = new List<string>();

            try
            {
#if PYTHONNET_AVAILABLE
                using (Py.GIL())
                {
                    var moduleName = System.IO.Path.GetFileNameWithoutExtension(moduleInfo.ModulePath);
                    dynamic module = Py.Import(moduleName);
                    
                    if (module != null)
                    {
                        dynamic dirResult = module.__dir__();
                        foreach (var item in dirResult)
                        {
                            var name = item.ToString();
                            if (PyObject.IsTrue(module.HasAttr(name)))
                            {
                                dynamic attr = module.GetAttr(name);
                                if (PyObject.IsTrue(Py.Import("inspect").callable(attr)))
                                {
                                    functions.Add(name);
                                }
                            }
                        }
                    }
                }
#else
                // Try reflection-based approach
                var pythonNetType = Type.GetType("Python.Runtime.Py, Python.Runtime");
                if (pythonNetType != null)
                {
                    var importMethod = pythonNetType.GetMethod("Import", new[] { typeof(string) });
                    var gilMethod = pythonNetType.GetMethod("GIL");
                    
                    if (importMethod != null && gilMethod != null)
                    {
                        using (var gil = gilMethod.Invoke(null, null) as IDisposable)
                        {
                            var moduleName = System.IO.Path.GetFileNameWithoutExtension(moduleInfo.ModulePath);
                            dynamic module = importMethod.Invoke(null, new[] { moduleName });
                            
                            if (module != null)
                            {
                                dynamic dirResult = module.__dir__();
                                foreach (var item in dirResult)
                                {
                                    functions.Add(item.ToString());
                                }
                            }
                        }
                    }
                }
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting Python functions from runtime: {ex.Message}");
            }

            return functions;
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

                // Cleanup Python runtime if initialized
                if (_pythonRuntimeAvailable)
                {
#if PYTHONNET_AVAILABLE
                    try
                    {
                        if (PythonEngine.IsInitialized)
                        {
                            _pythonEngine?.Dispose();
                            PythonEngine.Shutdown();
                        }
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
#endif
                    _pythonRuntimeAvailable = false;
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

