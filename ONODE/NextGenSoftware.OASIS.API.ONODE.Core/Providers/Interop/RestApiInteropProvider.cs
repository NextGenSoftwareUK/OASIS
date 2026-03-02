using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
    /// REST API interop provider for remote libraries
    /// Supports HTTP/HTTPS endpoints for library functions
    /// </summary>
    public class RestApiInteropProvider : ILibraryInteropProvider
    {
        private readonly Dictionary<string, RestLibraryInfo> _loadedLibraries;
        private readonly HttpClient _httpClient;
        private readonly object _lockObject = new object();
        private bool _initialized = false;

        public InteropProviderType ProviderType => InteropProviderType.RestApi;

        public string[] SupportedExtensions => new[]
        {
            ".json", ".api", ".rest"
        };

        public RestApiInteropProvider()
        {
            _loadedLibraries = new Dictionary<string, RestLibraryInfo>();
            _httpClient = new HttpClient();
        }

        public Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool> { Result = true };
            _initialized = true;
            return Task.FromResult(result);
        }

        public Task<OASISResult<ILoadedLibrary>> LoadLibraryAsync(string libraryPath, Dictionary<string, object> options = null)
        {
            var result = new OASISResult<ILoadedLibrary>();

            try
            {
                // libraryPath is the API base URL
                var baseUrl = libraryPath;
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
                {
                    OASISErrorHandling.HandleError(ref result, "Invalid API URL.");
                    return Task.FromResult(result);
                }

                var libraryId = Guid.NewGuid().ToString();
                var libraryName = uri.Host;

                // Load API metadata if available
                var metadata = LoadApiMetadata(baseUrl).Result;

                lock (_lockObject)
                {
                    _loadedLibraries[libraryId] = new RestLibraryInfo
                    {
                        LibraryId = libraryId,
                        BaseUrl = baseUrl,
                        Metadata = metadata
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

                result.Message = $"REST API library loaded: {baseUrl}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading REST API library: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, $"Error unloading REST API library: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        public async Task<OASISResult<T>> InvokeAsync<T>(string libraryId, string functionName, params object[] parameters)
        {
            var result = new OASISResult<T>();

            try
            {
                lock (_lockObject)
                {
                    if (!_loadedLibraries.TryGetValue(libraryId, out var library))
                    {
                        OASISErrorHandling.HandleError(ref result, "Library not loaded.");
                        return result;
                    }

                    // Build API endpoint URL
                    var endpoint = $"{library.BaseUrl.TrimEnd('/')}/{functionName}";

                    // Make HTTP POST request with parameters
                    var jsonContent = JsonSerializer.Serialize(parameters ?? new object[0]);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = _httpClient.PostAsync(endpoint, httpContent).Result;
                    var responseContent = response.Content.ReadAsStringAsync().Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var deserialized = JsonSerializer.Deserialize<T>(responseContent);
                        result.Result = deserialized;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"API call failed: {response.StatusCode} - {responseContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error invoking REST API function: {ex.Message}", ex);
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

                    // Try to get API documentation/endpoints
                    if (library.Metadata != null && library.Metadata.ContainsKey("Endpoints"))
                    {
                        var endpoints = library.Metadata["Endpoints"] as IEnumerable<string>;
                        result.Result = endpoints ?? new List<string>();
                    }
                    else
                    {
                        // Try to fetch from /api/docs or /swagger endpoint
                        var docsUrl = $"{library.BaseUrl.TrimEnd('/')}/api/docs";
                        var docsResponse = _httpClient.GetAsync(docsUrl).Result;
                        if (docsResponse.IsSuccessStatusCode)
                        {
                            var docsContent = docsResponse.Content.ReadAsStringAsync().Result;
                            // Parse OpenAPI/Swagger spec to extract endpoints
                            result.Result = ParseApiEndpoints(docsContent);
                        }
                        else
                        {
                            result.Result = new List<string>();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting REST API functions: {ex.Message}", ex);
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
                        LibraryName = library.BaseUrl,
                        Language = "REST API",
                        Framework = "HTTP/HTTPS",
                        CustomProperties = library.Metadata ?? new Dictionary<string, object>()
                    };

                    result.Result = metadata;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting REST API metadata: {ex.Message}", ex);
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

                    // Parse OpenAPI/Swagger spec to get function signatures
                    var signatures = new List<IFunctionSignature>();
                    
                    // Try to fetch API spec
                    var specUrl = $"{library.BaseUrl.TrimEnd('/')}/swagger.json";
                    var specResponse = _httpClient.GetAsync(specUrl).Result;
                    if (specResponse.IsSuccessStatusCode)
                    {
                        var specContent = specResponse.Content.ReadAsStringAsync().Result;
                        signatures = ParseOpenApiSpec(specContent);
                    }

                    result.Result = signatures;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting REST API function signatures: {ex.Message}", ex);
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

                _httpClient?.Dispose();
                _initialized = false;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error disposing REST API provider: {ex.Message}", ex);
            }

            return Task.FromResult(result);
        }

        private async Task<Dictionary<string, object>> LoadApiMetadata(string baseUrl)
        {
            var metadata = new Dictionary<string, object>();

            try
            {
                // Try to fetch API info endpoint
                var infoUrl = $"{baseUrl.TrimEnd('/')}/api/info";
                var response = await _httpClient.GetAsync(infoUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var info = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                    if (info != null)
                    {
                        foreach (var kvp in info)
                        {
                            metadata[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch
            {
                // Metadata endpoint not available, continue
            }

            return metadata;
        }

        private List<string> ParseApiEndpoints(string docsContent)
        {
            var endpoints = new List<string>();

            try
            {
                // Parse OpenAPI/Swagger JSON to extract paths
                var jsonDoc = JsonDocument.Parse(docsContent);
                if (jsonDoc.RootElement.TryGetProperty("paths", out var paths))
                {
                    foreach (var path in paths.EnumerateObject())
                    {
                        endpoints.Add(path.Name.TrimStart('/'));
                    }
                }
            }
            catch
            {
                // Parsing failed, return empty list
            }

            return endpoints;
        }

        private List<IFunctionSignature> ParseOpenApiSpec(string specContent)
        {
            var signatures = new List<IFunctionSignature>();

            try
            {
                var jsonDoc = JsonDocument.Parse(specContent);
                if (jsonDoc.RootElement.TryGetProperty("paths", out var paths))
                {
                    foreach (var path in paths.EnumerateObject())
                    {
                        var endpoint = path.Name.TrimStart('/');
                        
                        // Get POST method (or first available method)
                        if (path.Value.TryGetProperty("post", out var postMethod))
                        {
                            var parameters = new List<IParameterInfo>();
                            
                            if (postMethod.TryGetProperty("parameters", out var paramsArray))
                            {
                                foreach (var param in paramsArray.EnumerateArray())
                                {
                                    parameters.Add(new ParameterInfo
                                    {
                                        Name = param.TryGetProperty("name", out var name) ? name.GetString() : "param",
                                        Type = MapOpenApiTypeToCSharp(param),
                                        IsOptional = param.TryGetProperty("required", out var required) && !required.GetBoolean()
                                    });
                                }
                            }

                            var returnType = "object";
                            if (postMethod.TryGetProperty("responses", out var responses))
                            {
                                if (responses.TryGetProperty("200", out var successResponse))
                                {
                                    returnType = MapOpenApiTypeToCSharp(successResponse);
                                }
                            }

                            signatures.Add(new FunctionSignature
                            {
                                FunctionName = endpoint,
                                ReturnType = returnType,
                                Parameters = parameters,
                                Documentation = postMethod.TryGetProperty("description", out var desc) ? desc.GetString() : string.Empty
                            });
                        }
                    }
                }
            }
            catch
            {
                // Parsing failed
            }

            return signatures;
        }

        private string MapOpenApiTypeToCSharp(JsonElement element)
        {
            if (element.TryGetProperty("type", out var type))
            {
                return type.GetString() switch
                {
                    "string" => "string",
                    "integer" => "int",
                    "number" => "double",
                    "boolean" => "bool",
                    "array" => "object[]",
                    "object" => "object",
                    _ => "object"
                };
            }

            if (element.TryGetProperty("schema", out var schema))
            {
                return MapOpenApiTypeToCSharp(schema);
            }

            return "object";
        }

        private class RestLibraryInfo
        {
            public string LibraryId { get; set; }
            public string BaseUrl { get; set; }
            public Dictionary<string, object> Metadata { get; set; }
        }
    }
}

