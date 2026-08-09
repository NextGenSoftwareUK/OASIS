using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
    public partial class HashgraphOASIS
    {
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon from Hedera File Service or Smart Contract
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Query smart contract for holon data
                    var contractQuery = new
                    {
                        contractId = _contractAddress,
                        functionName = "getHolon",
                        parameters = new[] { id.ToString() }
                    };

                    var jsonContent = JsonSerializer.Serialize(contractQuery);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", content);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (contractResult.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.String)
                        {
                            var holonJson = resultProp.GetString();
                            var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (holon != null)
                            {
                                result.Result = holon;
                                result.IsError = false;
                                result.Message = "Holon loaded from Hashgraph smart contract successfully";
                                return result;
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Hashgraph smart contract: {contractResponse.StatusCode}");
                }
                else
                {
                    // Query Hedera File Service for holon data
                    // Use deterministic file ID based on holon ID
                    var fileId = $"0.0.{HashUtility.GetNumericHash(id.ToString())}";
                    var fileResponse = await _httpClient.GetAsync($"/api/v1/files/{fileId}");

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileContent);
                        
                        if (fileResult.TryGetProperty("contents", out var contentsProp))
                        {
                            var contentsBase64 = contentsProp.GetString();
                            var contentsBytes = Convert.FromBase64String(contentsBase64);
                            var holonJson = Encoding.UTF8.GetString(contentsBytes);
                            var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            
                            if (holon != null)
                            {
                                result.Result = holon;
                                result.IsError = false;
                                result.Message = "Holon loaded from Hedera File Service successfully";
                                return result;
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Hedera File Service: {fileResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon from Hedera File Service or Smart Contract by provider key
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Query smart contract for holon data by provider key
                    var contractQuery = new
                    {
                        contractId = _contractAddress,
                        functionName = "getHolonByProviderKey",
                        parameters = new[] { providerKey }
                    };

                    var jsonContent = JsonSerializer.Serialize(contractQuery);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", content);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (contractResult.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.String)
                        {
                            var holonJson = resultProp.GetString();
                            var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (holon != null)
                            {
                                result.Result = holon;
                                result.IsError = false;
                                result.Message = "Holon loaded from Hashgraph smart contract successfully";
                                return result;
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Hashgraph smart contract: {contractResponse.StatusCode}");
                }
                else
                {
                    // Query Hedera File Service for holon data by provider key
                    // Search for files with matching memo
                    var fileResponse = await _httpClient.GetAsync($"/api/v1/files?memo={Uri.EscapeDataString($"OASIS Holon: {providerKey}")}");

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileContent);
                        
                        if (fileResult.TryGetProperty("files", out var filesProp) && filesProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var file in filesProp.EnumerateArray())
                            {
                                if (file.TryGetProperty("file_id", out var fileIdProp))
                                {
                                    var fileId = fileIdProp.GetString();
                                    var fileContentsResponse = await _httpClient.GetAsync($"/api/v1/files/{fileId}/contents");
                                    
                                    if (fileContentsResponse.IsSuccessStatusCode)
                                    {
                                        var contentsBase64 = await fileContentsResponse.Content.ReadAsStringAsync();
                                        var contentsBytes = Convert.FromBase64String(contentsBase64);
                                        var holonJson = Encoding.UTF8.GetString(contentsBytes);
                                        var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                        
                                        if (holon != null && holon.ProviderUniqueStorageKey != null && holon.ProviderUniqueStorageKey.ContainsValue(providerKey))
                                        {
                                            result.Result = holon;
                                            result.IsError = false;
                                            result.Message = "Holon loaded from Hedera File Service successfully";
                                            return result;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Hedera File Service: {fileResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons for parent from Hedera File Service or Smart Contract
                var holons = new List<IHolon>();
                
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Query smart contract for holons by parent ID
                    var contractQuery = new
                    {
                        contractId = _contractAddress,
                        functionName = "getHolonsForParent",
                        parameters = new[] { id.ToString(), type.ToString() }
                    };

                    var jsonContent = JsonSerializer.Serialize(contractQuery);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", content);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (contractResult.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var holonJson in resultProp.EnumerateArray())
                            {
                                var holon = JsonSerializer.Deserialize<Holon>(holonJson.GetString(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (holon != null)
                                {
                                    holons.Add(holon);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Query Hedera File Service for holons by parent ID
                    // Search for files with parent ID in memo
                    var fileResponse = await _httpClient.GetAsync($"/api/v1/files?memo={Uri.EscapeDataString($"OASIS Holon Parent: {id}")}");

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileContent);
                        
                        if (fileResult.TryGetProperty("files", out var filesProp) && filesProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var file in filesProp.EnumerateArray())
                            {
                                if (file.TryGetProperty("file_id", out var fileIdProp))
                                {
                                    var fileId = fileIdProp.GetString();
                                    var fileContentsResponse = await _httpClient.GetAsync($"/api/v1/files/{fileId}/contents");
                                    
                                    if (fileContentsResponse.IsSuccessStatusCode)
                                    {
                                        var contentsBase64 = await fileContentsResponse.Content.ReadAsStringAsync();
                                        var contentsBytes = Convert.FromBase64String(contentsBase64);
                                        var holonJson = Encoding.UTF8.GetString(contentsBytes);
                                        var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                        
                                        if (holon != null && holon.ParentHolonId == id && (type == HolonType.All || holon.HolonType == type))
                                        {
                                            holons.Add(holon);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons for parent from Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons for parent from Hedera File Service or Smart Contract by provider key
                var holons = new List<IHolon>();
                
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Query smart contract for holons by parent provider key
                    var contractQuery = new
                    {
                        contractId = _contractAddress,
                        functionName = "getHolonsForParentByProviderKey",
                        parameters = new[] { providerKey, type.ToString() }
                    };

                    var jsonContent = JsonSerializer.Serialize(contractQuery);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", content);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (contractResult.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var holonJson in resultProp.EnumerateArray())
                            {
                                var holon = JsonSerializer.Deserialize<Holon>(holonJson.GetString(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (holon != null)
                                {
                                    holons.Add(holon);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Query Hedera File Service for holons by parent provider key
                    var fileResponse = await _httpClient.GetAsync($"/api/v1/files?memo={Uri.EscapeDataString($"OASIS Holon Parent: {providerKey}")}");

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileContent);
                        
                        if (fileResult.TryGetProperty("files", out var filesProp) && filesProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var file in filesProp.EnumerateArray())
                            {
                                if (file.TryGetProperty("file_id", out var fileIdProp))
                                {
                                    var fileId = fileIdProp.GetString();
                                    var fileContentsResponse = await _httpClient.GetAsync($"/api/v1/files/{fileId}/contents");
                                    
                                    if (fileContentsResponse.IsSuccessStatusCode)
                                    {
                                        var contentsBase64 = await fileContentsResponse.Content.ReadAsStringAsync();
                                        var contentsBytes = Convert.FromBase64String(contentsBase64);
                                        var holonJson = Encoding.UTF8.GetString(contentsBytes);
                                        var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                        
                                        if (holon != null && holon.ProviderUniqueStorageKey != null && holon.ProviderUniqueStorageKey.ContainsValue(providerKey) && (type == HolonType.All || holon.HolonType == type))
                                        {
                                            holons.Add(holon);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons for parent from Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons by metadata from Hashgraph blockchain
                var loadRequest = new
                {
                    metaKey = metaKey,
                    metaValue = metaValue,
                    holonType = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    loadChildrenFromProvider = loadChildrenFromProvider,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/metadata", content);
                if (loadResponse.IsSuccessStatusCode)
                {
                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse load data and populate holons list
                    if (loadData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons loaded by metadata successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons by metadata from Hashgraph blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons by multiple metadata key-value pairs from Hashgraph blockchain
                var loadRequest = new
                {
                    metaKeyValuePairs = metaKeyValuePairs,
                    metaKeyValuePairMatchMode = metaKeyValuePairMatchMode.ToString(),
                    holonType = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    loadChildrenFromProvider = loadChildrenFromProvider,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/metadata/multiple", content);
                if (loadResponse.IsSuccessStatusCode)
                {
                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse load data and populate holons list
                    if (loadData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons loaded by multiple metadata successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons by multiple metadata from Hashgraph blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by multiple metadata from Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load all holons from Hashgraph blockchain
                var loadRequest = new
                {
                    holonType = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    loadChildrenFromProvider = loadChildrenFromProvider,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/all", content);
                if (loadResponse.IsSuccessStatusCode)
                {
                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse load data and populate holons list
                    if (loadData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "All holons loaded successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load all holons from Hashgraph blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

    }
}
