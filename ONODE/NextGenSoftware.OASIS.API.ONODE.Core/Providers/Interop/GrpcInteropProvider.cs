using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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
    /// gRPC interop provider for remote libraries
    /// High-performance RPC protocol
    /// </summary>
    public class GrpcInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, GrpcLibraryInfo> _loadedLibraries;
        private readonly object _lockObject = new object();
        private bool _initialized = false;
        private bool _grpcAvailable = false;
        private HttpClient _httpClient;

        public InteropProviderType ProviderType => InteropProviderType.Grpc;

        public string[] SupportedExtensions => new[]
        {
            ".proto", ".grpc"
        };

        public GrpcInteropProvider()
        {
            _loadedLibraries = new Dictionary<string, GrpcLibraryInfo>();
            _httpClient = new HttpClient();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                // Check if gRPC libraries are available via reflection
                _grpcAvailable = CheckGrpcAvailability();
                _initialized = true;
                result.Result = true;
                
                if (_grpcAvailable)
                {
                    result.Message = "gRPC interop provider initialized. Both signature extraction and code execution are available.";
                }
                else
                {
                    result.Message = "gRPC interop provider initialized. Function signatures will be extracted from .proto files. gRPC client libraries not found - code execution disabled. Install Grpc.Net.Client NuGet package for full support.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing gRPC provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private bool CheckGrpcAvailability()
        {
            try
            {
                // Try to load gRPC types via reflection
                var grpcAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name.Contains("Grpc.Net.Client") || 
                                        a.GetName().Name.Contains("Grpc.Core"));
                
                return grpcAssembly != null;
            }
            catch
            {
                return false;
            }
        }

        public Task<OASISResult<ILoadedLibrary>> LoadLibraryAsync(string libraryPath, Dictionary<string, object> options = null)
        {
            var result = new OASISResult<ILoadedLibrary>();

            try
            {
                // libraryPath is the gRPC endpoint URL or .proto file path
                var endpoint = libraryPath;
                if (endpoint.EndsWith(".proto", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse .proto file to extract service definitions
                    endpoint = options?.ContainsKey("Endpoint") == true 
                        ? options["Endpoint"].ToString() 
                        : "localhost:50051";
                }

                var libraryId = Guid.NewGuid().ToString();
                var libraryName = System.IO.Path.GetFileName(libraryPath);

                lock (_lockObject)
                {
                    _loadedLibraries[libraryId] = new GrpcLibraryInfo
                    {
                        LibraryId = libraryId,
                        Endpoint = endpoint,
                        ProtoFilePath = libraryPath.EndsWith(".proto") ? libraryPath : null
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

                result.Message = "gRPC library loaded. Function signatures will be extracted from .proto file.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading gRPC library: {ex.Message}", ex);
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
                    if (!_loadedLibraries.ContainsKey(libraryId))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not found.");
                        return Task.FromResult(result);
                    }

                    _loadedLibraries.Remove(libraryId);
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unloading gRPC library: {ex.Message}", ex);
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
                    if (!_loadedLibraries.TryGetValue(libraryId, out var library))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    if (!_grpcAvailable)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            "gRPC client libraries are not available. Cannot execute gRPC calls. " +
                            "Install Grpc.Net.Client NuGet package for code execution. " +
                            "Signature extraction works without runtime, but code execution requires it.");
                        return Task.FromResult(result);
                    }

                    return ExecuteGrpcFunctionAsync<T>(library, functionName, parameters);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking gRPC function: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private async Task<OASISResult<T>> ExecuteGrpcFunctionAsync<T>(GrpcLibraryInfo library, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                // Try to use gRPC via reflection if available
                var grpcResult = await TryInvokeGrpcViaReflection<T>(library, functionName, parameters);
                if (!grpcResult.IsError)
                {
                    return grpcResult;
                }

                var fallbackResult = await TryGrpcWebFallback<T>(library, functionName, parameters);
                if (!fallbackResult.IsError)
                {
                    return fallbackResult;
                }

                OASISErrorHandling.HandleError(ref result, 
                    "gRPC execution failed. Ensure gRPC client code is generated from .proto files using Grpc.Tools, " +
                    "or that the gRPC service supports gRPC-Web/HTTP JSON transcoding. " +
                    "Alternatively, use the REST API provider for HTTP/JSON endpoints.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing gRPC function '{functionName}': {ex.Message}", ex);
            }

            return result;
        }

        private async Task<OASISResult<T>> TryInvokeGrpcViaReflection<T>(GrpcLibraryInfo library, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                var grpcAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name.Contains("Grpc.Net.Client") || 
                                        a.GetName().Name.Contains("Grpc.Core"));

                if (grpcAssembly == null)
                {
                    OASISErrorHandling.HandleError(ref result, "gRPC assemblies not found.");
                    return result;
                }

                var endpoint = library.Endpoint;
                if (string.IsNullOrEmpty(endpoint))
                {
                    endpoint = "localhost:50051";
                }

                // Try to find generated client types in loaded assemblies
                var clientTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => t.Name.EndsWith("Client") && 
                               t.GetMethods().Any(m => m.Name == functionName))
                    .ToList();

                if (clientTypes.Any())
                {
                    foreach (var clientType in clientTypes)
                    {
                        try
                        {
                            var method = clientType.GetMethod(functionName, 
                                BindingFlags.Public | 
                                BindingFlags.Instance);
                            
                            if (method != null)
                            {
                                var channelType = grpcAssembly.GetType("Grpc.Net.Client.GrpcChannel") ??
                                                 grpcAssembly.GetType("Grpc.Core.Channel");
                                
                                if (channelType != null)
                                {
                                    var createMethod = channelType.GetMethod("ForAddress", 
                                        new[] { typeof(string) }) ??
                                        channelType.GetMethod("Create", 
                                        new[] { typeof(string) });
                                    
                                    if (createMethod != null)
                                    {
                                        var channel = createMethod.Invoke(null, new object[] { endpoint });
                                        var client = Activator.CreateInstance(clientType, channel);
                                        
                                        object request;
                                        if (parameters != null && parameters.Length > 0)
                                        {
                                            request = parameters[0];
                                        }
                                        else
                                        {
                                            var requestType = method.GetParameters().FirstOrDefault()?.ParameterType;
                                            if (requestType != null)
                                            {
                                                request = Activator.CreateInstance(requestType);
                                            }
                                            else
                                            {
                                                request = new { };
                                            }
                                        }
                                        
                                        var invokeResult = method.Invoke(client, new[] { request });
                                        
                                        if (invokeResult is Task taskResult)
                                        {
                                            await taskResult;
                                            var resultProperty = taskResult.GetType().GetProperty("Result");
                                            if (resultProperty != null)
                                            {
                                                var taskValue = resultProperty.GetValue(taskResult);
                                                result.Result = (T)Convert.ChangeType(taskValue, typeof(T));
                                                result.Message = $"gRPC function '{functionName}' executed successfully.";
                                                return result;
                                            }
                                        }
                                        else
                                        {
                                            result.Result = (T)Convert.ChangeType(invokeResult, typeof(T));
                                            result.Message = $"gRPC function '{functionName}' executed successfully.";
                                            return result;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }

                // Fallback: Try gRPC-Web/HTTP JSON transcoding
                var fallbackResult = await TryGrpcWebFallback<T>(library, functionName, parameters);
                if (!fallbackResult.IsError)
                {
                    return fallbackResult;
                }

                OASISErrorHandling.HandleError(ref result, 
                    "gRPC execution requires generated client code from .proto files. " +
                    "Use Grpc.Tools to generate C# client code, or ensure the client assembly is loaded. " +
                    "Alternatively, use the REST API provider for HTTP/JSON endpoints.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking gRPC via reflection: {ex.Message}", ex);
            }

            return result;
        }

        private async Task<OASISResult<T>> TryGrpcWebFallback<T>(GrpcLibraryInfo library, string functionName, object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                var endpoint = library.Endpoint;
                if (string.IsNullOrEmpty(endpoint))
                {
                    endpoint = "localhost:50051";
                }

                var baseUrl = endpoint.StartsWith("http") ? endpoint : $"https://{endpoint}";
                var grpcWebUrl = $"{baseUrl}/{functionName}";

                var requestBody = parameters != null && parameters.Length > 0
                    ? JsonSerializer.Serialize(parameters[0])
                    : "{}";

                var httpContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
                httpContent.Headers.Add("Content-Type", "application/grpc-web+json");

                var response = await _httpClient.PostAsync(grpcWebUrl, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var deserialized = JsonSerializer.Deserialize<T>(responseContent);
                    result.Result = deserialized;
                    result.Message = $"gRPC-Web function '{functionName}' executed successfully.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"gRPC-Web call failed: {response.StatusCode} - {responseContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, 
                    $"gRPC-Web fallback failed: {ex.Message}. " +
                    "Ensure the gRPC service supports gRPC-Web or use generated client code.");
            }

            return result;
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
                    if (!_loadedLibraries.TryGetValue(libraryId, out var library))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Parse .proto file to extract RPC methods
                    var signatures = ParseProtoFile(library.ProtoFilePath ?? library.Endpoint);
                    var methodNames = signatures.Select(s => s.FunctionName).ToList();

                    result.Result = methodNames;
                    result.Message = methodNames.Count > 0
                        ? $"Found {methodNames.Count} RPC methods in gRPC service."
                        : "No RPC methods found.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting gRPC functions: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public bool IsLibraryLoaded(string libraryId)
        {
            lock (_lockObject)
            {
                return _loadedLibraries.ContainsKey(libraryId);
            }
        }

        public Task<OASISResult<ILibraryMetadata>> GetLibraryMetadataAsync(string libraryId)
        {
            var result = new OASISResult<ILibraryMetadata>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedLibraries.TryGetValue(libraryId, out var library))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    var metadata = new LibraryMetadata
                    {
                        LibraryName = library.Endpoint,
                        Language = "gRPC",
                        Framework = "Protocol Buffers",
                        CustomProperties = new Dictionary<string, object>
                        {
                            { "Endpoint", library.Endpoint }
                        }
                    };

                    result.Result = metadata;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting gRPC metadata: {ex.Message}", ex);
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
                    if (!_loadedLibraries.TryGetValue(libraryId, out var library))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return Task.FromResult(result);
                    }

                    // Parse .proto file to extract RPC method signatures
                    var signatures = ParseProtoFile(library.ProtoFilePath ?? library.Endpoint);

                    result.Result = signatures;
                    result.Message = signatures.Count > 0
                        ? $"Found {signatures.Count} RPC method signatures in gRPC service."
                        : "No RPC method signatures found.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting gRPC function signatures: {ex.Message}", ex);
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
                    _loadedLibraries.Clear();
                }

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error disposing gRPC provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private List<IFunctionSignature> ParseProtoFile(string filePath)
        {
            var signatures = new List<IFunctionSignature>();

            try
            {
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                    return signatures;

                var protoContent = System.IO.File.ReadAllText(filePath);

                // Parse .proto file to extract RPC methods
                // rpc MethodName(RequestType) returns (ResponseType);
                var rpcPattern = @"rpc\s+(\w+)\s*\(([^)]+)\)\s+returns\s*\(([^)]+)\)";
                var rpcMatches = System.Text.RegularExpressions.Regex.Matches(
                    protoContent,
                    rpcPattern,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                foreach (System.Text.RegularExpressions.Match match in rpcMatches)
                {
                    var methodName = match.Groups[1].Value;
                    var requestType = match.Groups[2].Value.Trim();
                    var responseType = match.Groups[3].Value.Trim();

                    signatures.Add(new Objects.Interop.FunctionSignature
                    {
                        FunctionName = methodName,
                        ReturnType = responseType,
                        Parameters = new List<IParameterInfo>
                        {
                            new Objects.Interop.ParameterInfo
                            {
                                Name = "request",
                                Type = requestType
                            }
                        },
                        Documentation = $"gRPC RPC method: {methodName}"
                    });
                }
            }
            catch
            {
                // Parsing failed
            }

            return signatures;
        }

        private class GrpcLibraryInfo
        {
            public string LibraryId { get; set; }
            public string Endpoint { get; set; }
            public string ProtoFilePath { get; set; }
        }
    }
}

