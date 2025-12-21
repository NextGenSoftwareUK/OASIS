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
    /// gRPC interop provider for remote libraries
    /// High-performance RPC protocol
    /// </summary>
    public class GrpcInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, GrpcLibraryInfo> _loadedLibraries;
        private readonly object _lockObject = new object();

        public InteropProviderType ProviderType => InteropProviderType.Grpc;

        public string[] SupportedExtensions => new[]
        {
            ".proto", ".grpc"
        };

        public GrpcInteropProvider()
        {
            _loadedLibraries = new Dictionary<string, GrpcLibraryInfo>();
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

                    // gRPC invocation requires gRPC client library
                    // For now, return placeholder
                    result.Message = "gRPC function invocation. Requires gRPC client library for execution.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking gRPC function: {ex.Message}", ex);
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

