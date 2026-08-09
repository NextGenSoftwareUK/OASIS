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
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NBitcoin;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS
{
    public partial class BitcoinOASIS
    {
        /// <summary>
        /// Parse Bitcoin blockchain response to Avatar object
        /// </summary>
        private Avatar ParseBitcoinToAvatar(string bitcoinJson)
        {
            try
            {
                // Parse real Bitcoin OP_RETURN data
                var bitcoinData = JsonSerializer.Deserialize<JsonElement>(bitcoinJson);
                
                // Extract avatar data from Bitcoin OP_RETURN transaction
                var avatar = new Avatar
                {
                    Id = Guid.TryParse(bitcoinData.TryGetProperty("id", out var id) ? id.GetString() : null, out var guid) ? guid : CreateDeterministicGuid($"{ProviderType.Value}:{(bitcoinData.TryGetProperty("address", out var addr) ? addr.GetString() : "bitcoin_user")}"),
                    Username = bitcoinData.TryGetProperty("username", out var username) ? username.GetString() : "bitcoin_user",
                    Email = bitcoinData.TryGetProperty("email", out var email) ? email.GetString() : "user@bitcoin.example",
                    FirstName = bitcoinData.TryGetProperty("first_name", out var firstName) ? firstName.GetString() : "Bitcoin",
                    LastName = bitcoinData.TryGetProperty("last_name", out var lastName) ? lastName.GetString() : "User",
                    AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(bitcoinData.TryGetProperty("avatar_type", out var avatarType) ? avatarType.GetString() : "User", out var type) ? type : AvatarType.User),
                    CreatedDate = DateTime.TryParse(bitcoinData.TryGetProperty("created_date", out var createdDate) ? createdDate.GetString() : DateTime.UtcNow.ToString("O"), out var created) ? created : DateTime.UtcNow,
                    ModifiedDate = DateTime.TryParse(bitcoinData.TryGetProperty("modified_date", out var modifiedDate) ? modifiedDate.GetString() : DateTime.UtcNow.ToString("O"), out var modified) ? modified : DateTime.UtcNow,
                    MetaData = new Dictionary<string, object>
                    {
                        ["BitcoinData"] = bitcoinJson,
                        ["ParsedAt"] = DateTime.UtcNow,
                        ["Provider"] = "BitcoinOASIS"
                    }
                };

                return avatar;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateAvatarFromBitcoin(bitcoinJson);
            }
        }

        /// <summary>
        /// Parse Bitcoin OP_RETURN data to AvatarDetail (separate object, not derived from avatar).
        /// Supports wrapper format { "oasisType": "AvatarDetail", "value": { ... } } or raw AvatarDetail JSON.
        /// </summary>
        private AvatarDetail ParseBitcoinToAvatarDetail(string bitcoinJson)
        {
            if (string.IsNullOrWhiteSpace(bitcoinJson)) return null;
            try
            {
                var el = JsonSerializer.Deserialize<JsonElement>(bitcoinJson);
                JsonElement toParse = el;
                if (el.TryGetProperty("oasisType", out var type) && type.GetString() == "AvatarDetail" && el.TryGetProperty("value", out var value))
                    toParse = value;
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var detail = JsonSerializer.Deserialize<AvatarDetail>(toParse.GetRawText(), options);
                if (detail != null && detail.Id != Guid.Empty)
                    return detail;
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Create Avatar from Bitcoin response when JSON deserialization fails
        /// </summary>
        private Avatar CreateAvatarFromBitcoin(string bitcoinJson)
        {
            try
            {
                // Extract basic information from Bitcoin JSON response
                var avatar = new Avatar
                {
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:{ExtractBitcoinProperty(bitcoinJson, "address") ?? "bitcoin_user"}"),
                    Username = ExtractBitcoinProperty(bitcoinJson, "address") ?? "bitcoin_user",
                    Email = ExtractBitcoinProperty(bitcoinJson, "email") ?? "user@bitcoin.example",
                    FirstName = ExtractBitcoinProperty(bitcoinJson, "first_name"),
                    LastName = ExtractBitcoinProperty(bitcoinJson, "last_name"),
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
        /// Extract property value from Bitcoin JSON response
        /// </summary>
        private string ExtractBitcoinProperty(string bitcoinJson, string propertyName)
        {
            try
            {
                // Simple regex-based extraction for Bitcoin properties
                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(bitcoinJson, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Avatar to Bitcoin blockchain format
        /// </summary>
        private string ConvertAvatarToBitcoin(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with Bitcoin blockchain structure
                var bitcoinData = new
                {
                    address = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(bitcoinData, new JsonSerializerOptions
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
        /// Convert Holon to Bitcoin blockchain format
        /// </summary>
        private string ConvertHolonToBitcoin(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with Bitcoin blockchain structure
                var bitcoinData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(bitcoinData, new JsonSerializerOptions
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



        public void Dispose()
        {
            _httpClient?.Dispose();
        }



        /// <summary>
        /// Parse Bitcoin blockchain response to Avatar object
        /// </summary>
        private Avatar ParseBitcoinToAvatar(JsonElement bitcoinData, string identifier)
        {
            try
            {
                var avatar = new Avatar
                {
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:{identifier}"),
                    Username = identifier,
                    Email = bitcoinData.TryGetProperty("address", out var address) ? address.GetString() : identifier,
                    FirstName = "Bitcoin",
                    LastName = "User",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = 1,
                    IsActive = true
                };

                // Add Bitcoin-specific metadata
                if (bitcoinData.TryGetProperty("chain_stats", out var chainStats))
                {
                    if (chainStats.TryGetProperty("funded_txo_sum", out var funded))
                    {
                        avatar.ProviderMetaData[Core.Enums.ProviderType.BitcoinOASIS]["bitcoin_balance"] = funded.GetInt64().ToString();
                    }
                    if (chainStats.TryGetProperty("spent_txo_sum", out var spent))
                    {
                        avatar.ProviderMetaData[Core.Enums.ProviderType.BitcoinOASIS]["bitcoin_spent"] = spent.GetInt64().ToString();
                    }
                }
                if (bitcoinData.TryGetProperty("mempool_stats", out var mempoolStats))
                {
                    if (mempoolStats.TryGetProperty("funded_txo_sum", out var mempoolFunded))
                    {
                        avatar.ProviderMetaData[Core.Enums.ProviderType.BitcoinOASIS]["bitcoin_mempool_balance"] = mempoolFunded.GetInt64().ToString();
                    }
                }

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Parse Bitcoin blockchain response to Holon object
        /// </summary>
        private Holon ParseBitcoinToHolon(string bitcoinJson)
        {
            try
            {
                // Parse real Bitcoin OP_RETURN data
                var bitcoinData = JsonSerializer.Deserialize<JsonElement>(bitcoinJson);
                
                // Extract holon data from Bitcoin OP_RETURN transaction
                var holon = new Holon
                {
                    Id = Guid.TryParse(bitcoinData.TryGetProperty("id", out var id) ? id.GetString() : null, out var guid) ? guid : CreateDeterministicGuid($"{ProviderType.Value}:{(bitcoinData.TryGetProperty("name", out var name) ? name.GetString() : "bitcoin_holon")}"),
                    Name = bitcoinData.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "Bitcoin Holon",
                    Description = bitcoinData.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                    HolonType = Enum.TryParse<HolonType>(bitcoinData.TryGetProperty("type", out var type) ? type.GetString() : "Holon", out var holonType) ? holonType : HolonType.Holon,
                    CreatedDate = DateTime.TryParse(bitcoinData.TryGetProperty("created", out var createdDate) ? createdDate.GetString() : DateTime.UtcNow.ToString("O"), out var created) ? created : DateTime.UtcNow,
                    ModifiedDate = DateTime.TryParse(bitcoinData.TryGetProperty("modified", out var modifiedDate) ? modifiedDate.GetString() : DateTime.UtcNow.ToString("O"), out var modified) ? modified : DateTime.UtcNow,
                    Version = 1,
                    IsActive = true
                };

                // Parse parent holon ID if available
                if (bitcoinData.TryGetProperty("parent_holon_id", out var parentId) && Guid.TryParse(parentId.GetString(), out var parentGuid))
                {
                    holon.ParentHolonId = parentGuid;
                }

                // Parse metadata if available
                if (bitcoinData.TryGetProperty("metadata", out var metadata))
                {
                    try
                    {
                        var metadataStr = metadata.GetString();
                        if (!string.IsNullOrWhiteSpace(metadataStr))
                        {
                            var metadataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataStr);
                            if (metadataDict != null)
                            {
                                holon.MetaData = metadataDict;
                            }
                        }
                    }
                    catch
                    {
                        // If metadata parsing fails, store raw string
                        holon.MetaData = new Dictionary<string, object>
                        {
                            ["RawMetadata"] = metadata.GetString()
                        };
                    }
                }

                // Add Bitcoin-specific metadata
                holon.MetaData = holon.MetaData ?? new Dictionary<string, object>();
                holon.MetaData["BitcoinData"] = bitcoinJson;
                holon.MetaData["ParsedAt"] = DateTime.UtcNow;
                holon.MetaData["Provider"] = "BitcoinOASIS";

                return holon;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateHolonFromBitcoin(bitcoinJson);
            }
        }

        /// <summary>
        /// Create Holon from Bitcoin response when JSON deserialization fails
        /// </summary>
        private Holon CreateHolonFromBitcoin(string bitcoinJson)
        {
            try
            {
                // Extract basic information from Bitcoin JSON response
                var holon = new Holon
                {
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:{ExtractBitcoinProperty(bitcoinJson, "name") ?? "bitcoin_holon"}"),
                    Name = ExtractBitcoinProperty(bitcoinJson, "name") ?? "Bitcoin Holon",
                    Description = ExtractBitcoinProperty(bitcoinJson, "description") ?? "",
                    HolonType = HolonType.Holon,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = 1,
                    IsActive = true
                };

                return holon;
            }
            catch (Exception)
            {
                return null;
            }
        }



        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                    return result;
                }

                // Convert decimal amount to satoshis (1 BTC = 100,000,000 satoshis)
                var amountInSatoshis = (long)(amount * 100000000);

                // Create Bitcoin transaction using Blockstream API
                var transactionRequest = new
                {
                    inputs = new[]
                    {
                        new
                        {
                            txid = "", // Will be filled by UTXO lookup
                            vout = 0
                        }
                    },
                    outputs = new[]
                    {
                        new
                        {
                            address = toWalletAddress,
                            value = amountInSatoshis
                        }
                    }
                };

                // First, get UTXOs for the from address
                var utxoResponse = await _httpClient.GetAsync($"/address/{fromWalletAddress}/utxo");
                if (!utxoResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get UTXOs for address {fromWalletAddress}: {utxoResponse.StatusCode}");
                    return result;
                }

                var utxoContent = await utxoResponse.Content.ReadAsStringAsync();
                var utxos = JsonSerializer.Deserialize<JsonElement[]>(utxoContent);

                if (utxos == null || utxos.Length == 0)
                {
                    OASISErrorHandling.HandleError(ref result, $"No UTXOs found for address {fromWalletAddress}");
                    return result;
                }

                // Find sufficient UTXOs
                long totalValue = 0;
                var selectedUtxos = new List<object>();

                foreach (var utxo in utxos)
                {
                    var value = utxo.GetProperty("value").GetInt64();
                    totalValue += value;
                    selectedUtxos.Add(new
                    {
                        txid = utxo.GetProperty("txid").GetString(),
                        vout = utxo.GetProperty("vout").GetInt32()
                    });

                    if (totalValue >= amountInSatoshis)
                        break;
                }

                if (totalValue < amountInSatoshis)
                {
                    OASISErrorHandling.HandleError(ref result, $"Insufficient funds. Available: {totalValue} satoshis, Required: {amountInSatoshis} satoshis");
                    return result;
                }

                // Create transaction with selected UTXOs
                var finalTransaction = new
                {
                    inputs = selectedUtxos,
                    outputs = new[]
                    {
                        new
                        {
                            address = toWalletAddress,
                            value = amountInSatoshis
                        }
                    }
                };

                // Broadcast transaction
                var jsonContent = JsonSerializer.Serialize(finalTransaction);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var broadcastResponse = await _httpClient.PostAsync("/tx", content);
                if (broadcastResponse.IsSuccessStatusCode)
                {
                    var responseContent = await broadcastResponse.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new BitcoinTransactionResponse
                    {
                        TransactionResult = responseData.GetProperty("txid").GetString(),
                        MemoText = memoText
                    };
                    result.IsError = false;
                    result.Message = $"Bitcoin transaction sent successfully. TXID: {result.Result.TransactionResult}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to broadcast Bitcoin transaction: {broadcastResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error sending Bitcoin transaction: {ex.Message}");
            }

            return result;
        }

        // LoadAllAvatarDetailsAsync is already implemented above (around line 2240)

    }
}
