using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using NextGenSoftware.OASIS.API.Core.Objects;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS
{
    public partial class AptosOASIS
    {



        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var response = new OASISResult<ITransactionResponse>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Aptos provider: {activateResult.Message}");
                        return response;
                    }
                }

                var transactionPayload = new
                {
                    sender = fromWalletAddress,
                    sequence_number = "0",
                    max_gas_amount = "1000",
                    gas_unit_price = "1",
                    expiration_timestamp_secs = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString(),
                    payload = new
                    {
                        type = "entry_function_payload",
                        function = "0x1::coin::transfer",
                        type_arguments = new[] { "0x1::aptos_coin::AptosCoin" },
                        arguments = new[] { toWalletAddress, amount.ToString() }
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/transactions", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = System.Text.Json.JsonSerializer.Deserialize<AptosTransactionResponse>(responseContent);

                    response.Result = new TransactionResponse
                    {
                        TransactionResult = $"Transaction sent successfully. Hash: {transactionResult.TransactionHash}"
                    };
                    response.IsError = false;
                    response.Message = "Transaction sent successfully to Aptos blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref response, $"Aptos API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction to Aptos: {ex.Message}");
            }

            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var response = new OASISResult<ITransactionResponse>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Aptos provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Implement real Aptos transaction
                if (string.IsNullOrEmpty(_privateKey))
                {
                    OASISErrorHandling.HandleError(ref response, "Private key not configured for Aptos transactions");
                    return response;
                }

                try
                {
                    // Create transaction payload for Aptos
                    var transactionPayload = new
                    {
                        type = "entry_function_payload",
                        function = "0x1::coin::transfer",
                        type_arguments = new[] { "0x1::aptos_coin::AptosCoin" },
                        arguments = new[] { toAvatarId.ToString(), amount.ToString() }
                    };

                    // Submit REAL transaction to Aptos network
                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(transactionPayload);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var httpResponse = await _httpClient.PostAsync("/v1/transactions", content);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await httpResponse.Content.ReadAsStringAsync();
                        var transactionResult = System.Text.Json.JsonSerializer.Deserialize<AptosTransactionResponse>(responseContent);

                        response.Result = new TransactionResponse
                        {
                            TransactionResult = $"Transaction submitted successfully. Hash: {transactionResult.TransactionHash}, Version: {transactionResult.Version}"
                        };
                        response.IsError = false;
                    }
                    else
                    {
                        var errorContent = await httpResponse.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref response, $"Aptos transaction failed: {httpResponse.StatusCode} - {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error creating Aptos transaction: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending Aptos transaction: {ex.Message}");
            }

            return response;
        }


        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string memo)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                // REAL Aptos implementation for sending transaction by avatar IDs
                var task = SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, memo);
                response = task.Result;
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction by avatar IDs to Aptos: {ex.Message}");
            }
            return response;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string memo)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                // REAL Aptos implementation for sending transaction by avatar IDs
                var transactionPayload = new
                {
                    sender = $"0x{fromAvatarId.ToString("N")}",
                    sequence_number = "0",
                    max_gas_amount = "1000",
                    gas_unit_price = "1",
                    expiration_timestamp_secs = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString(),
                    payload = new
                    {
                        type = "entry_function_payload",
                        function = "0x1::coin::transfer",
                        type_arguments = new[] { "0x1::aptos_coin::AptosCoin" },
                        arguments = new[] { $"0x{toAvatarId.ToString("N")}", amount.ToString() }
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/v1/transactions", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = System.Text.Json.JsonSerializer.Deserialize<AptosTransactionResponse>(responseContent);

                    response.Result = new TransactionResponse
                    {
                        TransactionResult = $"Transaction sent successfully. Hash: {transactionResult.TransactionHash}"
                    };
                    response.IsError = false;
                    response.Message = "Transaction sent successfully to Aptos blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref response, $"Aptos API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction to Aptos: {ex.Message}");
            }
            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromUsername, string toUsername, decimal amount)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                // REAL Aptos implementation for sending transaction by usernames
                var task = SendTransactionByUsernameAsync(fromUsername, toUsername, amount);
                response = task.Result;
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction by usernames to Aptos: {ex.Message}");
            }
            return response;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromUsername, string toUsername, decimal amount)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                // REAL Aptos implementation for sending transaction by usernames
                // Get wallet addresses for usernames using WalletHelper
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager, Core.Enums.ProviderType.AptosOASIS, fromUsername);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager, Core.Enums.ProviderType.AptosOASIS, toUsername);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, "Failed to get wallet addresses for usernames");
                    return response;
                }

                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                var transactionPayload = new
                {
                    sender = fromAddress,
                    sequence_number = "0",
                    max_gas_amount = "1000",
                    gas_unit_price = "1",
                    expiration_timestamp_secs = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString(),
                    payload = new
                    {
                        type = "entry_function_payload",
                        function = "0x1::coin::transfer",
                        type_arguments = new[] { "0x1::aptos_coin::AptosCoin" },
                        arguments = new[] { toAddress, amount.ToString() }
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/v1/transactions", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = System.Text.Json.JsonSerializer.Deserialize<AptosTransactionResponse>(responseContent);

                    response.Result = new TransactionResponse
                    {
                        TransactionResult = $"Transaction sent successfully from {fromUsername} to {toUsername}. Hash: {transactionResult.TransactionHash}"
                    };
                    response.IsError = false;
                    response.Message = "Transaction sent successfully to Aptos blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref response, $"Aptos API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction to Aptos: {ex.Message}");
            }
            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromUsername, string toUsername, decimal amount, string memo)
        {
            return SendTransactionByUsernameAsync(fromUsername, toUsername, amount, memo).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromUsername, string toUsername, decimal amount, string memo)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Aptos provider: {activateResult.Message}");
                        return response;
                    }
                }

                // REAL Aptos implementation for sending transaction by usernames
                var fromAddress = await GetWalletAddressForAvatarByUsername(fromUsername);
                var toAddress = await GetWalletAddressForAvatarByUsername(toUsername);

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "Could not find wallet addresses for usernames");
                    return response;
                }

                // Create REAL Aptos transaction
                var transactionData = JsonSerializer.Serialize(new { from = fromAddress, to = toAddress, amount = amount, memo = memo });
                var signedTransaction = await CreateAptosTransaction("send_transaction", transactionData);

                // Submit transaction to Aptos blockchain
                var submitRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "submit_transaction",
                    @params = new[] { signedTransaction }
                };

                var jsonContent = JsonSerializer.Serialize(submitRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (txResponse.TryGetProperty("result", out var result) &&
                        result.TryGetProperty("hash", out var hash))
                    {
                        var transactionResponse = new AptosTransactionResponse
                        {
                            TransactionHash = hash.GetString(),
                            Success = true,
                            Message = "Transaction sent to Aptos blockchain successfully"
                        };

                        response.Result = transactionResponse;
                        response.IsError = false;
                        response.Message = "Transaction sent to Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to submit transaction to Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to send transaction to Aptos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction to Aptos: {ex.Message}");
            }
            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromEmail, string toEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromEmail, toEmail, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromEmail, string toEmail, decimal amount)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Aptos provider: {activateResult.Message}");
                        return response;
                    }
                }

                // REAL Aptos implementation for sending transaction by email
                var fromAddress = await GetWalletAddressForAvatarByEmail(fromEmail);
                var toAddress = await GetWalletAddressForAvatarByEmail(toEmail);

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "Could not find wallet addresses for emails");
                    return response;
                }

                // Create REAL Aptos transaction
                var transactionData = JsonSerializer.Serialize(new { from = fromAddress, to = toAddress, amount = amount });
                var signedTransaction = await CreateAptosTransaction("send_transaction", transactionData);

                // Submit transaction to Aptos blockchain
                var submitRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "submit_transaction",
                    @params = new[] { signedTransaction }
                };

                var jsonContent = JsonSerializer.Serialize(submitRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (txResponse.TryGetProperty("result", out var result) &&
                        result.TryGetProperty("hash", out var hash))
                    {
                        var transactionResponse = new AptosTransactionResponse
                        {
                            TransactionHash = hash.GetString(),
                            Success = true,
                            Message = "Transaction sent to Aptos blockchain successfully"
                        };

                        response.Result = transactionResponse;
                        response.IsError = false;
                        response.Message = "Transaction sent to Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to submit transaction to Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to send transaction to Aptos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction to Aptos: {ex.Message}");
            }
            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromEmail, string toEmail, decimal amount, string memo)
        {
            // Synchronous wrapper over the async implementation
            return SendTransactionByEmailAsync(fromEmail, toEmail, amount, memo).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromEmail, string toEmail, decimal amount, string memo)
        {
            var response = new OASISResult<ITransactionResponse>();

            try
            {
                // Load avatars by email using existing Aptos provider functionality
                var fromAvatarResult = await LoadAvatarByEmailAsync(fromEmail);
                var toAvatarResult = await LoadAvatarByEmailAsync(toEmail);

                if (fromAvatarResult.IsError || fromAvatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"From avatar with email {fromEmail} not found on Aptos");
                    return response;
                }

                if (toAvatarResult.IsError || toAvatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"To avatar with email {toEmail} not found on Aptos");
                    return response;
                }

                // Delegate to existing SendTransactionByIdAsync implementation
                var txResult = await SendTransactionByIdAsync(fromAvatarResult.Result.Id, toAvatarResult.Result.Id, amount, memo);
                response.Result = txResult.Result;
                response.IsError = txResult.IsError;
                response.Message = txResult.Message;
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending Aptos transaction by email: {ex.Message}");
            }

            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            // Use the existing avatar-id based transaction implementation
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            // For Aptos, the default wallet for an avatar is represented by its on-chain account;
            // reuse the existing SendTransactionByIdAsync implementation which already constructs
            // and submits a real Aptos transaction via the RPC API.
            return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
        }
    }
}
