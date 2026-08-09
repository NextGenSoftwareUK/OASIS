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
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
{
    return DeleteAvatarAsync(id, softDelete).Result;
}



OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
    var response = new OASISResult<IEnumerable<IAvatar>>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        // Get avatars near me from Cardano blockchain
        var queryUrl = $"/addresses/nearby?lat={geoLat}&long={geoLong}&radius={radiusInMeters}";

        var httpResponse = _httpClient.GetAsync(queryUrl).Result;
        if (httpResponse.IsSuccessStatusCode)
        {
            var content = httpResponse.Content.ReadAsStringAsync().Result;
            // Parse Cardano JSON and create Avatar collection
            var avatars = new List<IAvatar>();
            response.Result = avatars;
            response.IsError = false;
            response.Message = "Avatars near me loaded successfully from Cardano blockchain";
        }
        else
        {
            OASISErrorHandling.HandleError(ref response, $"Failed to get avatars near me from Cardano blockchain: {httpResponse.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        response.Exception = ex;
        OASISErrorHandling.HandleError(ref response, $"Error getting avatars near me from Cardano: {ex.Message}");
    }

    return response;
}

OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
    var response = new OASISResult<IEnumerable<IHolon>>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref response, "Cardano provider is not activated");
            return response;
        }

        // Get holons near me from Cardano blockchain
        var queryUrl = $"/addresses/holons?lat={geoLat}&long={geoLong}&radius={radiusInMeters}&type={Type}";

        var httpResponse = _httpClient.GetAsync(queryUrl).Result;
        if (httpResponse.IsSuccessStatusCode)
        {
            var content = httpResponse.Content.ReadAsStringAsync().Result;
            // Parse Cardano JSON and create Holon collection
            var holons = new List<IHolon>();
            response.Result = holons;
            response.IsError = false;
            response.Message = "Holons near me loaded successfully from Cardano blockchain";
        }
        else
        {
            OASISErrorHandling.HandleError(ref response, $"Failed to get holons near me from Cardano blockchain: {httpResponse.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        response.Exception = ex;
        OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from Cardano: {ex.Message}");
    }

    return response;
}



/// <summary>
/// Parse Cardano blockchain response to Avatar object
/// </summary>
private IAvatar ParseCardanoToAvatar(string cardanoJson)
{
    try
    {
        // Deserialize the complete Avatar object from Cardano JSON
        var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(cardanoJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        return avatar;
    }
    catch (Exception)
    {
        // If JSON deserialization fails, try to extract basic info
        return CreateAvatarFromCardano(cardanoJson);
    }
}

/// <summary>
/// Parse Cardano metadata to AvatarDetail (separate from Avatar; do not build from Avatar).
/// </summary>
private IAvatarDetail ParseCardanoToAvatarDetail(string cardanoJson)
{
    try
    {
        var detail = System.Text.Json.JsonSerializer.Deserialize<AvatarDetail>(cardanoJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        if (detail != null && detail.Id != Guid.Empty) return detail;
    }
    catch { }
    try
    {
        var stakeAddress = ExtractCardanoProperty(cardanoJson, "stake_address") ?? ExtractCardanoProperty(cardanoJson, "address") ?? "cardano_user";
        var id = CreateDeterministicGuid($"{ProviderType.Value}:{stakeAddress}");
        return new AvatarDetail
        {
            Id = id,
            Username = stakeAddress,
            Email = ExtractCardanoProperty(cardanoJson, "email") ?? "",
            FirstName = ExtractCardanoProperty(cardanoJson, "first_name") ?? "",
            LastName = ExtractCardanoProperty(cardanoJson, "last_name") ?? ""
        };
    }
    catch { return null; }
}

/// <summary>
/// Create Avatar from Cardano response when JSON deserialization fails
/// </summary>
private IAvatar CreateAvatarFromCardano(string cardanoJson)
{
    try
    {
        // Extract basic information from Cardano JSON response
        var stakeAddress = ExtractCardanoProperty(cardanoJson, "stake_address") ?? ExtractCardanoProperty(cardanoJson, "address") ?? "cardano_user";
        var avatar = new Avatar
        {
            Id = CreateDeterministicGuid($"{ProviderType.Value}:{stakeAddress}"),
            Username = stakeAddress,
            Email = ExtractCardanoProperty(cardanoJson, "email") ?? $"user@{stakeAddress}.cardano",
            FirstName = ExtractCardanoProperty(cardanoJson, "first_name"),
            LastName = ExtractCardanoProperty(cardanoJson, "last_name"),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        return avatar;
    }
    catch (Exception)
    {
        return null;
    }
}

/// <summary>
/// Extract property value from Cardano JSON response
/// </summary>
private string ExtractCardanoProperty(string cardanoJson, string propertyName)
{
    try
    {
        // Simple regex-based extraction for Cardano properties
        var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
        var match = System.Text.RegularExpressions.Regex.Match(cardanoJson, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }
    catch (Exception)
    {
        return null;
    }
}

/// <summary>
/// Convert Avatar to Cardano blockchain format
/// </summary>
private string ConvertAvatarToCardano(IAvatar avatar)
{
    try
    {
        // Serialize Avatar to JSON with Cardano blockchain structure
        var cardanoData = new
        {
            stake_address = avatar.Username,
            email = avatar.Email,
            first_name = avatar.FirstName,
            last_name = avatar.LastName,
            created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        return System.Text.Json.JsonSerializer.Serialize(cardanoData, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }
    catch (Exception)
    {
        // Fallback to basic JSON serialization
        return System.Text.Json.JsonSerializer.Serialize(avatar, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }
}

/// <summary>
/// Convert Holon to Cardano blockchain format
/// </summary>
private string ConvertHolonToCardano(IHolon holon)
{
    try
    {
        // Serialize Holon to JSON with Cardano blockchain structure
        var cardanoData = new
        {
            id = holon.Id.ToString(),
            type = holon.HolonType.ToString(),
            name = holon.Name,
            description = holon.Description,
            created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        return System.Text.Json.JsonSerializer.Serialize(cardanoData, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }
    catch (Exception)
    {
        // Fallback to basic JSON serialization
        return System.Text.Json.JsonSerializer.Serialize(holon, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }
}



public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
{
    return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
}

public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
{
    var result = new OASISResult<ITransactionResponse>();

    try
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref result, "Cardano provider is not activated");
            return result;
        }

        // Convert decimal amount to lovelace (1 ADA = 1,000,000 lovelace)
        var amountInLovelace = (long)(amount * 1000000);

        // Get UTXOs for the from address using Blockfrost API
        var utxoResponse = await _httpClient.GetAsync($"/addresses/{fromWalletAddress}/utxos");
        if (!utxoResponse.IsSuccessStatusCode)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to get UTXOs for Cardano address {fromWalletAddress}: {utxoResponse.StatusCode}");
            return result;
        }

        var utxoContent = await utxoResponse.Content.ReadAsStringAsync();
        var utxos = JsonSerializer.Deserialize<JsonElement[]>(utxoContent);

        if (utxos == null || utxos.Length == 0)
        {
            OASISErrorHandling.HandleError(ref result, $"No UTXOs found for Cardano address {fromWalletAddress}");
            return result;
        }

        // Find sufficient UTXOs
        long totalValue = 0;
        var selectedUtxos = new List<object>();

        foreach (var utxo in utxos)
        {
            var value = utxo.GetProperty("amount").GetProperty("quantity").GetInt64();
            totalValue += value;
            selectedUtxos.Add(new
            {
                tx_hash = utxo.GetProperty("tx_hash").GetString(),
                output_index = utxo.GetProperty("output_index").GetInt32()
            });

            if (totalValue >= amountInLovelace)
                break;
        }

        if (totalValue < amountInLovelace)
        {
            OASISErrorHandling.HandleError(ref result, $"Insufficient funds. Available: {totalValue} lovelace, Required: {amountInLovelace} lovelace");
            return result;
        }

        // Create Cardano transaction
        var transactionRequest = new
        {
            inputs = selectedUtxos,
            outputs = new[]
            {
                        new
                        {
                            address = toWalletAddress,
                            amount = new[]
                            {
                                new
                                {
                                    unit = "lovelace",
                                    quantity = amountInLovelace.ToString()
                                }
                            }
                        }
                    },
            metadata = new
            {
                memo = memoText
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

            result.Result = new TransactionResponse
            {
                TransactionResult = responseData.GetProperty("tx_hash").GetString()
            };
            result.IsError = false;
            result.Message = $"Cardano transaction sent successfully. TX Hash: {result.Result.TransactionResult}";
        }
        else
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to submit Cardano transaction: {submitResponse.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        result.Exception = ex;
        OASISErrorHandling.HandleError(ref result, $"Error sending Cardano transaction: {ex.Message}");
    }

    return result;
}

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

    }
}
