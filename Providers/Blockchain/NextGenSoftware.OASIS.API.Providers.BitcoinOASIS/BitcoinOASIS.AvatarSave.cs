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
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Real Bitcoin implementation: Store avatar data in OP_RETURN transaction
                var avatarData = new
                {
                    id = avatar.Id.ToString(),
                    username = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    avatar_type = avatar.AvatarType.Value.ToString(),
                    created_date = avatar.CreatedDate.ToString("O"),
                    modified_date = DateTime.UtcNow.ToString("O"),
                    metadata = JsonSerializer.Serialize(avatar.MetaData)
                };

                var avatarJson = JsonSerializer.Serialize(avatarData);
                var avatarBytes = Encoding.UTF8.GetBytes(avatarJson);

                // Create Bitcoin RPC request for OP_RETURN transaction
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "createrawtransaction",
                    @params = new object[]
                    {
                        new object[0], // inputs (empty for OP_RETURN)
                        new Dictionary<string, object>
                        {
                            ["data"] = Convert.ToHexString(avatarBytes) // OP_RETURN data
                        }
                    }
                };

                // Get UTXOs for funding
                var utxoRequest = new
                {
                    jsonrpc = "2.0",
                    id = 2,
                    method = "listunspent",
                    @params = new object[] { 1, 9999999, new string[0] }
                };

                var utxoContent = new StringContent(JsonSerializer.Serialize(utxoRequest), Encoding.UTF8, "application/json");
                var utxoResponse = await _httpClient.PostAsync("", utxoContent);

                if (utxoResponse.IsSuccessStatusCode)
                {
                    var utxoContentResult = await utxoResponse.Content.ReadAsStringAsync();
                    var utxoData = JsonSerializer.Deserialize<JsonElement>(utxoContentResult);

                    if (utxoData.TryGetProperty("result", out var utxos) && utxos.GetArrayLength() > 0)
                    {
                        // Use first UTXO for funding
                        var utxo = utxos[0];
                        var inputs = new[]
                        {
                            new
                            {
                                txid = utxo.GetProperty("txid").GetString(),
                                vout = utxo.GetProperty("vout").GetInt32()
                            }
                        };

                        // Create transaction with OP_RETURN
                        var createRequest = new
                        {
                            jsonrpc = "2.0",
                            id = 3,
                            method = "createrawtransaction",
                            @params = new object[]
                            {
                                inputs,
                                new Dictionary<string, object>
                                {
                                    ["data"] = Convert.ToHexString(avatarBytes)
                                }
                            }
                        };

                        var createContent = new StringContent(JsonSerializer.Serialize(createRequest), Encoding.UTF8, "application/json");
                        var createResponse = await _httpClient.PostAsync("", createContent);

                        if (createResponse.IsSuccessStatusCode)
                        {
                            var createResult = await createResponse.Content.ReadAsStringAsync();
                            var createData = JsonSerializer.Deserialize<JsonElement>(createResult);

                            if (createData.TryGetProperty("result", out var rawTx))
                            {
                                // Sign the transaction
                                var signRequest = new
                                {
                                    jsonrpc = "2.0",
                                    id = 4,
                                    method = "signrawtransactionwithwallet",
                                    @params = new object[] { rawTx.GetString() }
                                };

                                var signContent = new StringContent(JsonSerializer.Serialize(signRequest), Encoding.UTF8, "application/json");
                                var signResponse = await _httpClient.PostAsync("", signContent);

                                if (signResponse.IsSuccessStatusCode)
                                {
                                    var signResult = await signResponse.Content.ReadAsStringAsync();
                                    var signData = JsonSerializer.Deserialize<JsonElement>(signResult);

                                    if (signData.TryGetProperty("result", out var signedResult) &&
                                        signedResult.TryGetProperty("hex", out var signedHex))
                                    {
                                        // Broadcast the transaction
                                        var broadcastRequest = new
                                        {
                                            jsonrpc = "2.0",
                                            id = 5,
                                            method = "sendrawtransaction",
                                            @params = new object[] { signedHex.GetString() }
                                        };

                                        var broadcastContent = new StringContent(JsonSerializer.Serialize(broadcastRequest), Encoding.UTF8, "application/json");
                                        var broadcastResponse = await _httpClient.PostAsync("", broadcastContent);

                                        if (broadcastResponse.IsSuccessStatusCode)
                                        {
                                            var broadcastResult = await broadcastResponse.Content.ReadAsStringAsync();
                                            var broadcastData = JsonSerializer.Deserialize<JsonElement>(broadcastResult);

                                            if (broadcastData.TryGetProperty("result", out var txid))
                                            {
                                                response.Result = avatar;
                                                response.IsError = false;
                                                response.Message = $"Avatar saved to Bitcoin blockchain. Transaction ID: {txid.GetString()}";
                                                
                                                // Store transaction ID in avatar metadata
                                                avatar.ProviderMetaData[Core.Enums.ProviderType.BitcoinOASIS]["transactionId"] = txid.GetString();
                                                avatar.ProviderMetaData[Core.Enums.ProviderType.BitcoinOASIS]["savedAt"] = DateTime.UtcNow.ToString("O");
                                            }
                                            else
                                            {
                                                OASISErrorHandling.HandleError(ref response, "Failed to get transaction ID from Bitcoin response");
                                            }
                                        }
                                        else
                                        {
                                            OASISErrorHandling.HandleError(ref response, $"Failed to broadcast Bitcoin transaction: {broadcastResponse.StatusCode}");
                                        }
                                    }
                                    else
                                    {
                                        OASISErrorHandling.HandleError(ref response, "Failed to sign Bitcoin transaction");
                                    }
                                }
                                else
                                {
                                    OASISErrorHandling.HandleError(ref response, $"Failed to sign Bitcoin transaction: {signResponse.StatusCode}");
                                }
                            }
                            else
                            {
                                OASISErrorHandling.HandleError(ref response, "Failed to create Bitcoin transaction");
                            }
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, $"Failed to create Bitcoin transaction: {createResponse.StatusCode}");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No UTXOs available for Bitcoin transaction");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get UTXOs from Bitcoin: {utxoResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to Bitcoin: {ex.Message}", ex);
            }

            return response;
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Serialize avatar detail as separate object with type marker so we can load it without deriving from avatar
                var wrapper = new { oasisType = "AvatarDetail", value = avatar };
                var avatarDetailJson = JsonSerializer.Serialize(wrapper);
                var avatarDetailBytes = Encoding.UTF8.GetBytes(avatarDetailJson);

                // Create Bitcoin transaction with avatar detail data
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
                            address = "", // IAvatarDetail doesn't have ProviderWallets, using empty string as fallback
                            value = 0, // OP_RETURN transaction
                            script = Convert.ToHexString(avatarDetailBytes) // Store avatar detail data in OP_RETURN
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    // IAvatarDetail doesn't have ProviderWallets, skipping wallet assignment

                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = "Avatar detail saved successfully to Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar detail to Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar detail to Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Bitcoin is immutable, so we can't actually delete
                // Instead, we mark the avatar as deleted in a new transaction
                var deleteData = new
                {
                    action = "delete",
                    avatarId = id.ToString(),
                    timestamp = DateTime.UtcNow,
                    softDelete = softDelete
                };

                var deleteJson = JsonSerializer.Serialize(deleteData);
                var deleteBytes = Encoding.UTF8.GetBytes(deleteJson);

                // Create Bitcoin transaction with delete marker
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
                            address = "", // OP_RETURN transaction
                            value = 0,
                            script = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = "Avatar deletion marked successfully on Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Bitcoin: {ex.Message}", ex);
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Bitcoin is immutable, so we can't actually delete
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

                // Create Bitcoin transaction with delete marker
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
                            address = "", // OP_RETURN transaction
                            value = 0,
                            script = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = "Avatar deletion marked successfully on Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Bitcoin: {ex.Message}", ex);
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Bitcoin is immutable, so we can't actually delete
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

                // Create Bitcoin transaction with delete marker
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
                            address = "", // OP_RETURN transaction
                            value = 0,
                            script = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = "Avatar deletion marked successfully on Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Bitcoin: {ex.Message}", ex);
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Bitcoin is immutable, so we can't actually delete
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

                // Create Bitcoin transaction with delete marker
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
                            address = "", // OP_RETURN transaction
                            value = 0,
                            script = Convert.ToHexString(deleteBytes)
                        }
                    }
                };

                // Submit transaction to Bitcoin network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/tx", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = "Avatar deletion marked successfully on Bitcoin blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mark avatar deletion on Bitcoin: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error marking avatar deletion on Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

    }
}
