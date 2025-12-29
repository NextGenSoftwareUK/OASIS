using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.Interop;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Providers.Interop
{
    /// <summary>
    /// .NET interop provider for .NET assemblies
    /// Direct reflection-based access to .NET libraries
    /// </summary>
    public class DotNetInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, Assembly> _loadedAssemblies;
        private readonly object _lockObject = new object();

        public InteropProviderType ProviderType => InteropProviderType.DotNet;

        public string[] SupportedExtensions => new[]
        {
            ".dll", ".exe"
        };

        public DotNetInteropProvider()
        {
            _loadedAssemblies = new Dictionary<string, Assembly>();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool> { Result = true };
            return Task.FromResult(result);
        }

        public Task<OASISResult<ILoadedLibrary>> LoadLibraryAsync(string libraryPath, Dictionary<string, object> options = null)
        {
            var result = new OASISResult<ILoadedLibrary>();

            try
            {
                if (!File.Exists(libraryPath))
                {
                    OASISErrorHandling.HandleError(ref result, "Assembly file not found.");
                    return Task.FromResult(result);
                }

                var assembly = Assembly.LoadFrom(libraryPath);
                var libraryId = Guid.NewGuid().ToString();
                var libraryName = Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedAssemblies[libraryId] = assembly;
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

                result.Message = ".NET assembly loaded successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading .NET assembly: {ex.Message}", ex);
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
                    if (!_loadedAssemblies.ContainsKey(libraryId))
                    {
                        OASISErrorHandling.HandleError(ref result, "Assembly not loaded.");
                        return Task.FromResult(result);
                    }

                    _loadedAssemblies.Remove(libraryId);
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unloading .NET assembly: {ex.Message}", ex);
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
                    if (!_loadedAssemblies.TryGetValue(libraryId, out var assembly))
                    {
                        OASISErrorHandling.HandleError(ref result, "Assembly not loaded.");
                        return Task.FromResult(result);
                    }

                    // Parse function name: ClassName.MethodName or Namespace.ClassName.MethodName
                    var parts = functionName.Split('.');
                    if (parts.Length < 2)
                    {
                        OASISErrorHandling.HandleError(ref result, "Function name must be in format: ClassName.MethodName");
                        return Task.FromResult(result);
                    }

                    var methodName = parts[parts.Length - 1];
                    var className = string.Join(".", parts.Take(parts.Length - 1));

                    var type = assembly.GetType(className);
                    if (type == null)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Type '{className}' not found in assembly.");
                        return Task.FromResult(result);
                    }

                    var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                    if (method == null)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Method '{methodName}' not found in type '{className}'.");
                        return Task.FromResult(result);
                    }

                    object instance = null;
                    if (!method.IsStatic)
                    {
                        instance = Activator.CreateInstance(type);
                    }

                    var returnValue = method.Invoke(instance, parameters);
                    
                    if (returnValue is T typedValue)
                    {
                        result.Result = typedValue;
                    }
                    else if (returnValue != null)
                    {
                        result.Result = (T)Convert.ChangeType(returnValue, typeof(T));
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking .NET method: {ex.Message}", ex);
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
                    if (!_loadedAssemblies.TryGetValue(libraryId, out var assembly))
                    {
                        OASISErrorHandling.HandleError(ref result, "Assembly not loaded.");
                        return Task.FromResult(result);
                    }

                    var functions = new List<string>();
                    foreach (var type in assembly.GetTypes())
                    {
                        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                        {
                            if (!method.IsSpecialName) // Exclude properties, events, etc.
                            {
                                functions.Add($"{type.FullName}.{method.Name}");
                            }
                        }
                    }

                    result.Result = functions;
                    result.Message = $"Found {functions.Count} methods in .NET assembly.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting .NET functions: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public bool IsLibraryLoaded(string libraryId)
        {
            lock (_lockObject)
            {
                return _loadedAssemblies.ContainsKey(libraryId);
            }
        }

        public Task<OASISResult<ILibraryMetadata>> GetLibraryMetadataAsync(string libraryId)
        {
            var result = new OASISResult<ILibraryMetadata>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedAssemblies.TryGetValue(libraryId, out var assembly))
                    {
                        OASISErrorHandling.HandleError(ref result, "Assembly not loaded.");
                        return Task.FromResult(result);
                    }

                    var metadata = new LibraryMetadata
                    {
                        LibraryName = assembly.GetName().Name,
                        Language = "C#",
                        Framework = ".NET",
                        Version = assembly.GetName().Version?.ToString(),
                        CustomProperties = new Dictionary<string, object>
                        {
                            { "FullName", assembly.FullName },
                            { "Location", assembly.Location }
                        }
                    };

                    result.Result = metadata;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting .NET metadata: {ex.Message}", ex);
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
                    if (!_loadedAssemblies.TryGetValue(libraryId, out var assembly))
                    {
                        OASISErrorHandling.HandleError(ref result, "Assembly not loaded.");
                        return Task.FromResult(result);
                    }

                    var signatures = new List<IFunctionSignature>();

                    foreach (var type in assembly.GetTypes())
                    {
                        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                        {
                            if (method.IsSpecialName)
                                continue;

                            var parameters = method.GetParameters().Select(p => new Objects.Interop.ParameterInfo
                            {
                                Name = p.Name,
                                Type = p.ParameterType.Name,
                                IsOptional = p.IsOptional,
                                DefaultValue = p.HasDefaultValue ? p.DefaultValue : null
                            }).ToList();

                            signatures.Add(new Objects.Interop.FunctionSignature
                            {
                                FunctionName = $"{type.FullName}.{method.Name}",
                                ReturnType = method.ReturnType.Name,
                                Parameters = parameters.Cast<IParameterInfo>().ToList(),
                                Documentation = GetXmlDocumentation(method)
                            });
                        }
                    }

                    result.Result = signatures;
                    result.Message = $"Found {signatures.Count} method signatures in .NET assembly.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting .NET function signatures: {ex.Message}", ex);
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
                    _loadedAssemblies.Clear();
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error disposing .NET provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private string GetXmlDocumentation(MethodInfo method)
        {
            // XML documentation extraction would go here
            // For now, return empty string
            return string.Empty;
        }
    }
}

