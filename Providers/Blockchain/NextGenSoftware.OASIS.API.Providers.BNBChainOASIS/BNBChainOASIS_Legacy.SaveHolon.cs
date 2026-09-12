using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Numerics;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.BNBChainOASIS
{
    public partial class BNBChainOASIS_Legacy
    {
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid parentId, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadHolonsForParentAsync(parentId, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Save holon using smart contract with ALL fields
                var holonData = new
                {
                    holonId = holon.Id.ToString(),
                    name = holon.Name ?? "",
                    description = holon.Description ?? "",
                    holonType = holon.HolonType.ToString(),
                    parentHolonId = holon.ParentHolonId.ToString(),
                    parentOmniverseId = holon.ParentOmniverseId.ToString(),
                    parentMultiverseId = holon.ParentMultiverseId.ToString(),
                    parentUniverseId = holon.ParentUniverseId.ToString(),
                    parentDimensionId = holon.ParentDimensionId.ToString(),
                    dimensionLevel = holon.DimensionLevel.ToString(),
                    subDimensionLevel = holon.SubDimensionLevel.ToString(),
                    nodes = JsonSerializer.Serialize(holon.Nodes ?? new List<INode>()),
                    metadata = JsonSerializer.Serialize(holon.MetaData ?? new Dictionary<string, object>())
                };

                // Call smart contract function to create/update holon
                var createHolonFunction = _contract.GetFunction("createHolon");
                var gasEstimate = createHolonFunction.EstimateGasAsync(
                    holonData.holonId,
                    holonData.name,
                    holonData.description,
                    holonData.holonType,
                    holonData.parentHolonId,
                    holonData.parentOmniverseId,
                    holonData.parentMultiverseId,
                    holonData.parentUniverseId,
                    holonData.parentDimensionId,
                    holonData.dimensionLevel,
                    holonData.subDimensionLevel,
                    holonData.nodes,
                    holonData.metadata
                ).Result;

                var transactionReceipt = createHolonFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    holonData.holonId,
                    holonData.name,
                    holonData.description,
                    holonData.holonType,
                    holonData.parentHolonId,
                    holonData.parentOmniverseId,
                    holonData.parentMultiverseId,
                    holonData.parentUniverseId,
                    holonData.parentDimensionId,
                    holonData.dimensionLevel,
                    holonData.subDimensionLevel,
                    holonData.nodes,
                    holonData.metadata
                ).Result;

                if (transactionReceipt.Status.Value == 1)
                {
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = $"Holon saved to BNB Chain successfully. Transaction hash: {transactionReceipt.TransactionHash}";

                    // Store transaction hash in holon metadata
                    holon.ProviderMetaData[Core.Enums.ProviderType.BNBChainOASIS]["transactionHash"] = transactionReceipt.TransactionHash;
                    holon.ProviderMetaData[Core.Enums.ProviderType.BNBChainOASIS]["savedAt"] = DateTime.UtcNow.ToString("O");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction failed on BNB Chain");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to BNB Chain: {ex.Message}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Delete avatar using smart contract
                var deleteRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_sendTransaction",
                    @params = new object[]
                    {
                        new
                        {
                            from = _account?.Address ?? "0x0000000000000000000000000000000000000000",
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("deleteAvatar") + EncodeParameter(id.ToString()),
                            gas = "0x" + (500000).ToString("x")
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(deleteRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && !string.IsNullOrEmpty(resultData.GetString()))
                    {
                        result.Result = true;
                        result.IsError = false;
                        result.Message = $"Avatar {id} deleted from BNB Chain successfully. Transaction: {resultData.GetString()}";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to delete avatar from BNB Chain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Load all holons using smart contract
                var loadRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_call",
                    @params = new object[]
                    {
                        new
                        {
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("getAllHolons")
                        },
                        "latest"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && resultData.GetString() != "0x")
                    {
                        var holons = ParseBNBChainToHolons(resultData.GetString());
                        result.Result = holons;
                        result.IsError = false;
                        result.Message = $"Loaded {holons.Count()} holons from BNB Chain successfully";
                    }
                    else
                    {
                        result.Result = new List<IHolon>();
                        result.IsError = false;
                        result.Message = "No holons found on BNB Chain";
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                var savedHolons = new List<IHolon>();
                var errors = new List<string>();

                // Real BNB Chain implementation: Save multiple holons using smart contract
                foreach (var holon in holons)
                {
                    try
                    {
                        var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, continueOnErrorRecursive);
                        if (saveResult.IsError)
                        {
                            errors.Add($"Failed to save holon {holon.Id}: {saveResult.Message}");
                            if (!continueOnError)
                            {
                                OASISErrorHandling.HandleError(ref result, $"Failed to save holon {holon.Id}: {saveResult.Message}");
                                return result;
                            }
                        }
                        else
                        {
                            savedHolons.Add(saveResult.Result);
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Error saving holon {holon.Id}: {ex.Message}";
                        errors.Add(errorMsg);
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, errorMsg, ex);
                            return result;
                        }
                    }
                }

                result.Result = savedHolons;
                result.IsError = false;
                result.Message = $"Saved {savedHolons.Count} holons to BNB Chain successfully";
                if (errors.Count > 0)
                {
                    result.Message += $". {errors.Count} errors occurred: {string.Join("; ", errors)}";
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Load all avatar details using smart contract
                var loadRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_call",
                    @params = new object[]
                    {
                        new
                        {
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("getAllAvatarDetails")
                        },
                        "latest"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && resultData.GetString() != "0x")
                    {
                        var avatarDetails = ParseBNBChainToAvatarDetails(resultData.GetString());
                        result.Result = avatarDetails;
                        result.IsError = false;
                        result.Message = $"Loaded {avatarDetails.Count()} avatar details from BNB Chain successfully";
                    }
                    else
                    {
                        result.Result = new List<IAvatarDetail>();
                        result.IsError = false;
                        result.Message = "No avatar details found on BNB Chain";
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar details from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from BNB Chain: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool continueOnErrorRecursive = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, continueOnErrorRecursive, version).Result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0)
        {
            return LoadAvatarByEmailAsync(email, version).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Search using smart contract
                var searchData = new
                {
                    avatarId = searchParams.AvatarId.ToString(),
                    searchOnlyForCurrentAvatar = searchParams.SearchOnlyForCurrentAvatar,
                    searchGroups = JsonSerializer.Serialize(searchParams.SearchGroups ?? new List<ISearchGroupBase>())
                };

                var searchJson = JsonSerializer.Serialize(searchData);
                var searchRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_call",
                    @params = new object[]
                    {
                        new
                        {
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("search") + EncodeParameter(searchJson)
                        },
                        "latest"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(searchRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && resultData.GetString() != "0x")
                    {
                        var holons = ParseBNBChainToHolons(resultData.GetString());
                        var searchResults = new SearchResults
                        {
                            SearchResultHolons = holons.ToList(),
                            NumberOfResults = holons.Count(),
                            NumberOfDuplicates = 0
                        };

                        result.Result = searchResults;
                        result.IsError = false;
                        result.Message = $"Search completed successfully. Found {holons.Count()} results";
                    }
                    else
                    {
                        var emptyResults = new SearchResults
                        {
                            SearchResultHolons = new List<IHolon>(),
                            NumberOfResults = 0,
                            NumberOfDuplicates = 0
                        };

                        result.Result = emptyResults;
                        result.IsError = false;
                        result.Message = "No results found";
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to search on BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error searching on BNB Chain: {ex.Message}");
            }
            return result;
        }
    }
}
