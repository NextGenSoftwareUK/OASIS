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
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
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

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                    return result;
                }

                // Serialize holon to JSON
                string holonInfo = JsonSerializer.Serialize(holon);
                int holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                string holonId = holon.Id.ToString();

                // Use Hedera File Service or Smart Contract Service to store holon data via HTTP API
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Smart contract storage - use Hedera Smart Contract Service via REST API
                    var contractData = new
                    {
                        contractId = _contractAddress,
                        functionParameters = new
                        {
                            functionName = "createHolon",
                            parameters = new[]
                            {
                                new { type = "string", value = holonId },
                                new { type = "string", value = holonInfo }
                            }
                        }
                    };

                    var contractJson = JsonSerializer.Serialize(contractData);
                    var contractContent = new StringContent(contractJson, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", contractContent);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var contractResponseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(contractResponseContent);
                        
                        if (contractResult.TryGetProperty("transactionId", out var txId))
                        {
                            holon.ProviderUniqueStorageKey[ProviderType.Value] = txId.GetString();
                            
                            // Save child holons recursively if requested
                            if (saveChildren && holon.Children != null && holon.Children.Any())
                            {
                                foreach (var child in holon.Children)
                                {
                                    await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth > 0 ? maxChildDepth - 1 : 0, continueOnError, saveChildrenOnProvider);
                                }
                            }
                            
                            result.Result = holon;
                            result.IsError = false;
                            result.IsSaved = true;
                            result.Message = "Holon saved to Hashgraph smart contract successfully";
                            return result;
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holon to Hashgraph smart contract: {await contractResponse.Content.ReadAsStringAsync()}");
                    return result;
                }
                else
                {
                    // Use Hedera File Service via REST API
                    var fileData = new
                    {
                        contents = Encoding.UTF8.GetBytes(holonInfo),
                        fileMemo = $"OASIS Holon: {holonId}"
                    };

                    var fileJson = JsonSerializer.Serialize(new
                    {
                        contents = Convert.ToBase64String(fileData.contents),
                        fileMemo = fileData.fileMemo
                    });
                    var fileContent = new StringContent(fileJson, Encoding.UTF8, "application/json");
                    var fileResponse = await _httpClient.PostAsync("/api/v1/files", fileContent);

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileResponseContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileResponseContent);
                        
                        if (fileResult.TryGetProperty("fileId", out var fileId))
                        {
                            holon.ProviderUniqueStorageKey[ProviderType.Value] = fileId.GetString();
                            
                            // Save child holons recursively if requested
                            if (saveChildren && holon.Children != null && holon.Children.Any())
                            {
                                foreach (var child in holon.Children)
                                {
                                    await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth > 0 ? maxChildDepth - 1 : 0, continueOnError, saveChildrenOnProvider);
                                }
                            }
                            
                            result.Result = holon;
                            result.IsError = false;
                            result.IsSaved = true;
                            result.Message = "Holon saved to Hedera File Service successfully";
                            return result;
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holon to Hedera File Service: {await fileResponse.Content.ReadAsStringAsync()}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to Hashgraph: {ex.Message}", ex);
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
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

                // Save multiple holons to Hashgraph blockchain
                var saveRequest = new
                {
                    holons = holons.Select(h => new
                    {
                        id = h.Id.ToString(),
                        name = h.Name,
                        description = h.Description,
                        data = JsonSerializer.Serialize(h),
                        version = h.Version,
                        parentId = h.ParentHolonId.ToString(),
                        holonType = h.HolonType.ToString()
                    }).ToArray(),
                    saveChildren = saveChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    saveChildrenOnProvider = saveChildrenOnProvider
                };

                var jsonContent = JsonSerializer.Serialize(saveRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var saveResponse = await _httpClient.PostAsync("/api/v1/holons/batch", content);
                if (saveResponse.IsSuccessStatusCode)
                {
                    var responseContent = await saveResponse.Content.ReadAsStringAsync();
                    var saveData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var savedHolons = new List<IHolon>();
                    if (saveData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            savedHolons.Add(holon);
                        }
                    }

                    result.Result = savedHolons;
                    result.IsError = false;
                    result.Message = $"Successfully saved {savedHolons.Count} holons to Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holons to Hashgraph blockchain: {saveResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
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

                // Delete holon from Hashgraph blockchain
                var deleteRequest = new
                {
                    id = id.ToString(),
                    deleted = true,
                    deletedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var jsonContent = JsonSerializer.Serialize(deleteRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var deleteResponse = await _httpClient.PostAsync($"/api/v1/holons/{id}/delete", content);
                if (deleteResponse.IsSuccessStatusCode)
                {
                    var responseContent = await deleteResponse.Content.ReadAsStringAsync();
                    var deleteData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (deleteData.TryGetProperty("holon", out var holonElement))
                    {
                        var deletedHolon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                        result.Result = deletedHolon;
                        result.IsError = false;
                        result.Message = "Holon deleted successfully from Hashgraph blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Invalid response format from Hashgraph blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon from Hashgraph blockchain: {deleteResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
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

                // Delete holon by provider key from Hashgraph blockchain
                var deleteRequest = new
                {
                    providerKey = providerKey,
                    deleted = true,
                    deletedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var jsonContent = JsonSerializer.Serialize(deleteRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var deleteResponse = await _httpClient.PostAsync($"/api/v1/holons/provider/{providerKey}/delete", content);
                if (deleteResponse.IsSuccessStatusCode)
                {
                    var responseContent = await deleteResponse.Content.ReadAsStringAsync();
                    var deleteData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (deleteData.TryGetProperty("holon", out var holonElement))
                    {
                        var deletedHolon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                        result.Result = deletedHolon;
                        result.IsError = false;
                        result.Message = "Holon deleted successfully from Hashgraph blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Invalid response format from Hashgraph blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon from Hashgraph blockchain: {deleteResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

    }
}
