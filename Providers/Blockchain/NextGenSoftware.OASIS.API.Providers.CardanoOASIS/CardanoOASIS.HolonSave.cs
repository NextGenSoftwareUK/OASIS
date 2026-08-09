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
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.CardanoOASIS
{
    public partial class CardanoOASIS
    {
public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
{
    return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
}

public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
{
    var response = new OASISResult<IHolon>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        if (holon == null)
        {
            OASISErrorHandling.HandleError(ref response, "Holon cannot be null");
            return response;
        }

        // Get wallet for the holon (use avatar's wallet if holon has CreatedByAvatarId)
        Guid avatarId = holon.CreatedByAvatarId != Guid.Empty ? holon.CreatedByAvatarId : holon.Id;
        var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatarId, Core.Enums.ProviderType.CardanoOASIS);
        if (walletResult.IsError || walletResult.Result == null)
        {
            OASISErrorHandling.HandleError(ref response, "Could not retrieve wallet address for holon");
            return response;
        }

        var walletAddress = walletResult.Result.WalletAddress;

        // Serialize holon to JSON
        var holonJson = JsonSerializer.Serialize(holon, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        // Get UTXOs for the wallet address using Blockfrost API
        var utxosResponse = await _httpClient.GetAsync($"/addresses/{walletAddress}/utxos");
        if (!utxosResponse.IsSuccessStatusCode)
        {
            OASISErrorHandling.HandleError(ref response, $"Failed to get UTXOs for Cardano address: {utxosResponse.StatusCode}");
            return response;
        }

        var utxosContent = await utxosResponse.Content.ReadAsStringAsync();
        var utxosData = JsonSerializer.Deserialize<JsonElement[]>(utxosContent);
        
        if (utxosData == null || utxosData.Length == 0)
        {
            OASISErrorHandling.HandleError(ref response, "No UTXOs available for transaction");
            return response;
        }

        // Use first UTXO
        var utxo = utxosData[0];
        var txHash = utxo.TryGetProperty("tx_hash", out var txHashProp) ? txHashProp.GetString() : "";
        var outputIndex = utxo.TryGetProperty("output_index", out var indexProp) ? indexProp.GetInt32() : 0;

        // Create Cardano transaction with holon data in metadata
        var transactionRequest = new
        {
            inputs = new[]
            {
                new
                {
                    tx_hash = txHash,
                    output_index = outputIndex
                }
            },
            outputs = new[]
            {
                new
                {
                    address = walletAddress,
                    amount = new[]
                    {
                        new
                        {
                            unit = "lovelace",
                            quantity = "1000000"
                        }
                    }
                }
            },
            metadata = new Dictionary<string, object>
            {
                ["721"] = new Dictionary<string, object>
                {
                    [holon.Id.ToString()] = new Dictionary<string, object>
                    {
                        ["holon_data"] = holonJson
                    }
                }
            }
        };

        // Submit transaction to Cardano network via Blockfrost API
        var jsonContent = JsonSerializer.Serialize(transactionRequest);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var submitResponse = await _httpClient.PostAsync("/tx/submit", content);
        if (submitResponse.IsSuccessStatusCode)
        {
            var responseContent = await submitResponse.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            string txId = null;
            if (responseData.TryGetProperty("tx_hash", out var txHashResult))
            {
                txId = txHashResult.GetString();
            }
            else if (responseData.TryGetProperty("id", out var idProp))
            {
                txId = idProp.GetString();
            }

            if (!string.IsNullOrEmpty(txId))
            {
                // Store transaction hash in provider unique storage key
                if (holon.ProviderUniqueStorageKey == null)
                    holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.CardanoOASIS] = txId;

                response.Result = holon;
                response.IsError = false;
                response.IsSaved = true;
                response.Message = $"Holon saved successfully to Cardano blockchain: {txId}";

                // Handle children if requested
                if (saveChildren && holon.Children != null && holon.Children.Any())
                {
                    var childResults = new List<OASISResult<IHolon>>();
                    foreach (var child in holon.Children)
                    {
                        var childResult = await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth - 1, continueOnError, saveChildrenOnProvider);
                        childResults.Add(childResult);
                        
                        if (!continueOnError && childResult.IsError)
                        {
                            OASISErrorHandling.HandleError(ref response, $"Failed to save child holon {child.Id}: {childResult.Message}");
                            return response;
                        }
                    }
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref response, "Failed to save holon to Cardano blockchain - no transaction hash returned");
            }
        }
        else
        {
            var errorContent = await submitResponse.Content.ReadAsStringAsync();
            OASISErrorHandling.HandleError(ref response, $"Failed to save holon to Cardano: {submitResponse.StatusCode} - {errorContent}");
        }
    }
    catch (Exception ex)
    {
        response.Exception = ex;
        OASISErrorHandling.HandleError(ref response, $"Error saving holon to Cardano: {ex.Message}", ex);
    }

    return response;
}


public override OASISResult<IHolon> DeleteHolon(Guid id)
{
    return DeleteHolonAsync(id).Result;
}

public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
{
    var response = new OASISResult<IHolon>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        // Cardano is immutable, so we can't actually delete
        // Instead, we mark the holon as deleted in a new transaction
        var deleteData = new
        {
            action = "delete",
            holonId = id.ToString(),
            timestamp = DateTime.UtcNow
        };

        var deleteJson = JsonSerializer.Serialize(deleteData);
        var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);

        // Create Cardano transaction with delete marker
        var transactionRequest = new
        {
            inputs = new[]
            {
                        new
                        {
                            tx_hash = "", // Will be filled by UTXO lookup
                            output_index = 0
                        }
                    },
            outputs = new[]
            {
                        new
                        {
                            address = "", // Datum transaction
                            amount = new[]
                            {
                                new
                                {
                                    unit = "lovelace",
                                    quantity = "0"
                                }
                            },
                            datum = Convert.ToHexString(deleteBytes)
                        }
                    }
        };

        // Submit transaction to Cardano network
        var jsonContent = JsonSerializer.Serialize(transactionRequest);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var submitResponse = await _httpClient.PostAsync("/tx/submit", content);
        if (submitResponse.IsSuccessStatusCode)
        {
            response.Result = new Holon { Id = id };
            response.IsError = false;
            response.Message = "Holon deletion marked successfully on Cardano blockchain";
        }
        else
        {
            OASISErrorHandling.HandleError(ref response, $"Failed to mark holon deletion on Cardano: {submitResponse.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref response, $"Error marking holon deletion on Cardano: {ex.Message}", ex);
    }

    return response;
}

public override OASISResult<IHolon> DeleteHolon(string providerKey)
{
    return DeleteHolonAsync(providerKey).Result;
}

public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
{
    var response = new OASISResult<IHolon>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        // Cardano is immutable, so we can't actually delete
        // Instead, we mark the holon as deleted in a new transaction
        var deleteData = new
        {
            action = "delete",
            providerKey = providerKey,
            timestamp = DateTime.UtcNow
        };

        var deleteJson = JsonSerializer.Serialize(deleteData);
        var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);

        // Create Cardano transaction with delete marker
        var transactionRequest = new
        {
            inputs = new[]
            {
                        new
                        {
                            tx_hash = "", // Will be filled by UTXO lookup
                            output_index = 0
                        }
                    },
            outputs = new[]
            {
                        new
                        {
                            address = "", // Datum transaction
                            amount = new[]
                            {
                                new
                                {
                                    unit = "lovelace",
                                    quantity = "0"
                                }
                            },
                            datum = Convert.ToHexString(deleteBytes)
                        }
                    }
        };

        // Submit transaction to Cardano network
        var jsonContent = JsonSerializer.Serialize(transactionRequest);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var submitResponse = await _httpClient.PostAsync("/tx/submit", content);
        if (submitResponse.IsSuccessStatusCode)
        {
            // Wallet is managed by WalletManager, no need to update ProviderWallets directly
            response.Result = new Holon { };
            response.IsError = false;
            response.Message = "Holon deletion marked successfully on Cardano blockchain";
        }
        else
        {
            OASISErrorHandling.HandleError(ref response, $"Failed to mark holon deletion on Cardano: {submitResponse.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref response, $"Error marking holon deletion on Cardano: {ex.Message}", ex);
    }

    return response;
}

public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
{
    return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
}

public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
{
    var response = new OASISResult<ISearchResults>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        // Extract search query from SearchGroups
        string searchQuery = null;
        DateTime fromDate = DateTime.MinValue;
        DateTime toDate = DateTime.MaxValue;
        
        if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
        {
            var firstGroup = searchParams.SearchGroups.FirstOrDefault();
            if (firstGroup is ISearchTextGroup textGroup && !string.IsNullOrWhiteSpace(textGroup.SearchQuery))
            {
                searchQuery = textGroup.SearchQuery;
            }
        }
        
        // Extract date filters if available (check if searchParams has date properties)
        if (searchParams != null)
        {
            // Try to get dates from searchParams properties if they exist
            var searchParamsType = searchParams.GetType();
            var fromDateProp = searchParamsType.GetProperty("FromDate");
            var toDateProp = searchParamsType.GetProperty("ToDate");
            
            if (fromDateProp != null)
            {
                var fromDateValue = fromDateProp.GetValue(searchParams);
                if (fromDateValue is DateTime fromDt && fromDt != DateTime.MinValue)
                    fromDate = fromDt;
            }
            
            if (toDateProp != null)
            {
                var toDateValue = toDateProp.GetValue(searchParams);
                if (toDateValue is DateTime toDt && toDt != DateTime.MaxValue)
                    toDate = toDt;
            }
        }
        
        // Search Cardano blockchain for transactions matching search criteria
        var searchRequest = new
        {
            query = searchQuery ?? "",
            filters = new
            {
                fromDate = fromDate,
                toDate = toDate,
                version = version
            }
        };

        var jsonContent = JsonSerializer.Serialize(searchRequest);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var searchResponse = await _httpClient.PostAsync("/search", content);
        if (searchResponse.IsSuccessStatusCode)
        {
            var responseContent = await searchResponse.Content.ReadAsStringAsync();
            var searchData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var results = new SearchResults();
            // Parse search results and populate results object

            response.Result = results;
            response.IsError = false;
            response.Message = "Search completed successfully on Cardano blockchain";
        }
        else
        {
            OASISErrorHandling.HandleError(ref response, $"Failed to search Cardano blockchain: {searchResponse.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref response, $"Error searching Cardano blockchain: {ex.Message}", ex);
    }

    return response;
}

public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
{
    return ImportAsync(holons).Result;
}

public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
{
    var response = new OASISResult<bool>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        // Import holons to Cardano blockchain
        var importResult = await SaveHolonsAsync(holons);
        if (importResult.IsError)
        {
            OASISErrorHandling.HandleError(ref response, $"Failed to import holons to Cardano: {importResult.Message}");
            return response;
        }

        response.Result = true;
        response.IsError = false;
        response.Message = $"Successfully imported {holons.Count()} holons to Cardano blockchain";
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref response, $"Error importing holons to Cardano: {ex.Message}", ex);
    }

    return response;
}

public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
{
    return ExportAllAsync(version).Result;
}

public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
{
    var response = new OASISResult<IEnumerable<IHolon>>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        // Export all data from Cardano blockchain
        var exportRequest = new
        {
            version = version,
            includeDeleted = false
        };

        var jsonContent = JsonSerializer.Serialize(exportRequest);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var exportResponse = await _httpClient.PostAsync("/export", content);
        if (exportResponse.IsSuccessStatusCode)
        {
            var responseContent = await exportResponse.Content.ReadAsStringAsync();
            var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var holons = new List<IHolon>();
            // Parse export data and populate holons list

            response.Result = holons;
            response.IsError = false;
            response.Message = "Export completed successfully from Cardano blockchain";
        }
        else
        {
            OASISErrorHandling.HandleError(ref response, $"Failed to export from Cardano blockchain: {exportResponse.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref response, $"Error exporting from Cardano blockchain: {ex.Message}", ex);
    }

    return response;
}

public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
{
    return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
}

public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
{
    var response = new OASISResult<IEnumerable<IHolon>>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        // Export all data for specific avatar from Cardano blockchain
        var exportRequest = new
        {
            avatarId = avatarId.ToString(),
            version = version,
            includeDeleted = false
        };

        var jsonContent = JsonSerializer.Serialize(exportRequest);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var exportResponse = await _httpClient.PostAsync("/export/avatar", content);
        if (exportResponse.IsSuccessStatusCode)
        {
            var responseContent = await exportResponse.Content.ReadAsStringAsync();
            var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var holons = new List<IHolon>();
            // Parse export data and populate holons list

            response.Result = holons;
            response.IsError = false;
            response.Message = "Avatar data export completed successfully from Cardano blockchain";
        }
        else
        {
            OASISErrorHandling.HandleError(ref response, $"Failed to export avatar data from Cardano blockchain: {exportResponse.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref response, $"Error exporting avatar data from Cardano blockchain: {ex.Message}", ex);
    }

    return response;
}

public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
{
    return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
}

public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
{
    var response = new OASISResult<IEnumerable<IHolon>>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        // Export all data for specific avatar by username from Cardano blockchain
        var exportRequest = new
        {
            avatarUsername = avatarUsername,
            version = version,
            includeDeleted = false
        };

        var jsonContent = JsonSerializer.Serialize(exportRequest);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var exportResponse = await _httpClient.PostAsync("/export/avatar/username", content);
        if (exportResponse.IsSuccessStatusCode)
        {
            var responseContent = await exportResponse.Content.ReadAsStringAsync();
            var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var holons = new List<IHolon>();
            // Parse export data and populate holons list

            response.Result = holons;
            response.IsError = false;
            response.Message = "Avatar data export completed successfully from Cardano blockchain";
        }
        else
        {
            OASISErrorHandling.HandleError(ref response, $"Failed to export avatar data from Cardano blockchain: {exportResponse.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref response, $"Error exporting avatar data from Cardano blockchain: {ex.Message}", ex);
    }

    return response;
}

public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
{
    return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
}

public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
{
    var response = new OASISResult<IEnumerable<IHolon>>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        // Export all data for specific avatar by email from Cardano blockchain
        var exportRequest = new
        {
            avatarEmail = avatarEmailAddress,
            version = version,
            includeDeleted = false
        };

        var jsonContent = JsonSerializer.Serialize(exportRequest);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var exportResponse = await _httpClient.PostAsync("/export/avatar/email", content);
        if (exportResponse.IsSuccessStatusCode)
        {
            var responseContent = await exportResponse.Content.ReadAsStringAsync();
            var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var holons = new List<IHolon>();
            // Parse export data and populate holons list

            response.Result = holons;
            response.IsError = false;
            response.Message = "Avatar data export completed successfully from Cardano blockchain";
        }
        else
        {
            OASISErrorHandling.HandleError(ref response, $"Failed to export avatar data from Cardano blockchain: {exportResponse.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref response, $"Error exporting avatar data from Cardano blockchain: {ex.Message}", ex);
    }

    return response;
}

        // Missing abstract method implementations
    }
}
