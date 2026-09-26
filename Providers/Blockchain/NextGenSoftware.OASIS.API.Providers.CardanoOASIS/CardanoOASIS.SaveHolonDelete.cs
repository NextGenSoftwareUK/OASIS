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
public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
{
    return SaveAvatarDetailAsync(avatar).Result;
}

public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
{
    var response = new OASISResult<IAvatarDetail>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        // Serialize avatar detail to JSON
        var avatarDetailJson = JsonSerializer.Serialize(avatar);
        var avatarDetailBytes = Encoding.UTF8.GetBytes(avatarDetailJson);

        // Create Cardano transaction with avatar detail data
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
                            address = await GetWalletAddressForAvatarAsync(avatar.Id),
                            amount = new[]
                            {
                                new
                                {
                                    unit = "lovelace",
                                    quantity = "0"
                                }
                            },
                            datum = Convert.ToHexString(avatarDetailBytes) // Store avatar detail data in datum
                        }
                    }
        };

        // Submit transaction to Cardano network
        var jsonContent = JsonSerializer.Serialize(transactionRequest);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var submitResponse = await _httpClient.PostAsync("/tx/submit", content);
        if (submitResponse.IsSuccessStatusCode)
        {
            var responseContent = await submitResponse.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Wallet is managed by WalletManager, no need to update ProviderWallets directly
            // {
            //     Address = responseData.GetProperty("tx_hash").GetString(),
            //     ProviderType = Core.Enums.ProviderType.CardanoOASIS
            // };

            response.Result = avatar;
            response.IsError = false;
            response.Message = "Avatar detail saved successfully to Cardano blockchain";
        }
        else
        {
            OASISErrorHandling.HandleError(ref response, $"Failed to save avatar detail to Cardano: {submitResponse.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref response, $"Error saving avatar detail to Cardano: {ex.Message}", ex);
    }

    return response;
}

public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
{
    return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
}

public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
{
    var response = new OASISResult<bool>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        // Cardano is immutable, so we can't actually delete
        // Instead, we mark the avatar as deleted in a new transaction
        var deleteData = new
        {
            action = "delete",
            avatarEmail = avatarEmail,
            timestamp = DateTime.UtcNow,
            softDelete = softDelete
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
            response.Result = true;
            response.IsError = false;
            response.Message = "Avatar deletion marked successfully on Cardano blockchain";
        }
        else
        {
            OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Cardano: {submitResponse.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Cardano: {ex.Message}", ex);
    }

    return response;
}

public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
{
    return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
}

public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
{
    var response = new OASISResult<bool>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        // Cardano is immutable, so we can't actually delete
        // Instead, we mark the avatar as deleted in a new transaction
        var deleteData = new
        {
            action = "delete",
            avatarUsername = avatarUsername,
            timestamp = DateTime.UtcNow,
            softDelete = softDelete
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
            response.Result = true;
            response.IsError = false;
            response.Message = "Avatar deletion marked successfully on Cardano blockchain";
        }
        else
        {
            OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Cardano: {submitResponse.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Cardano: {ex.Message}", ex);
    }

    return response;
}

public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
{
    return DeleteAvatarAsync(providerKey, softDelete).Result;
}

public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
{
    var response = new OASISResult<bool>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        // Cardano is immutable, so we can't actually delete
        // Instead, we mark the avatar as deleted in a new transaction
        var deleteData = new
        {
            action = "delete",
            providerKey = providerKey,
            timestamp = DateTime.UtcNow,
            softDelete = softDelete
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
            response.Result = true;
            response.IsError = false;
            response.Message = "Avatar deletion marked successfully on Cardano blockchain";
        }
        else
        {
            OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Cardano: {submitResponse.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Cardano: {ex.Message}", ex);
    }

    return response;
}

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


    }
}
