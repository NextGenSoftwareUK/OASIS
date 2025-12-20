using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Providers.Interop
{
    /// <summary>
    /// JavaScript/Node.js interop provider using ClearScript or Edge.js
    /// Requires ClearScript NuGet package: Microsoft.ClearScript.V8
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
                result.Message = "JavaScript interop provider initialized. Note: Requires Microsoft.ClearScript.V8 NuGet package.";
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

                result.Result = new LoadedLibrary
                {
                    LibraryId = libraryId,
                    LibraryPath = libraryPath,
                    LibraryName = libraryName,
                    ProviderType = ProviderType,
                    LoadedAt = DateTime.UtcNow,
                    Metadata = options ?? new Dictionary<string, object>()
                };

                result.Message = "JavaScript library loaded. Note: Full functionality requires Microsoft.ClearScript.V8 NuGet package.";
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

                    result.Message = "JavaScript invocation. Note: Full functionality requires Microsoft.ClearScript.V8 NuGet package.";
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

                    // Parse JavaScript to extract function names
                    // This would require a JavaScript parser or AST library
                    result.Result = new List<string>();
                    result.Message = "JavaScript function discovery. Note: Full functionality requires Microsoft.ClearScript.V8 NuGet package.";
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
                Message = "JavaScript metadata extraction. Note: Full functionality requires Microsoft.ClearScript.V8 NuGet package."
            };
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

