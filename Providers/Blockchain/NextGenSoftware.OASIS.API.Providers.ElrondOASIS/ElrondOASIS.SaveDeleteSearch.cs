using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS
{
    public partial class ElrondOASIS
    {
        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
        {
            // Elrond currently does not generate native code from STAR metadata.
            return true;
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
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Convert decimal amount to EGLD (1 EGLD = 10^18 wei)
                var amountInWei = (long)(amount * 1000000000000000000);

                // Get account info for balance check
                var accountResponse = await _httpClient.GetAsync($"/accounts/{fromWalletAddress}");
                if (!accountResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get account info for Elrond address {fromWalletAddress}: {accountResponse.StatusCode}");
                    return result;
                }

                var accountContent = await accountResponse.Content.ReadAsStringAsync();
                var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);

                var balance = accountData.GetProperty("balance").GetString();
                var balanceValue = long.Parse(balance);

                if (balanceValue < amountInWei)
                {
                    OASISErrorHandling.HandleError(ref result, $"Insufficient balance. Available: {balanceValue} wei, Required: {amountInWei} wei");
                    return result;
                }

                var nonce = accountData.GetProperty("nonce").GetInt64();

                // Create Elrond transaction
                var transactionRequest = new
                {
                    nonce = nonce,
                    value = amountInWei.ToString(),
                    receiver = toWalletAddress,
                    sender = fromWalletAddress,
                    gasPrice = 1000000000, // 1 GWei
                    gasLimit = 50000,
                    data = memoText,
                    chainID = "1", // Mainnet
                    version = 1
                };

                // Submit transaction to Elrond network
                var jsonContent = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/transactions", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new ElrondTransactionResponse
                    {
                        TransactionResult = responseData.GetProperty("txHash").GetString(),
                        MemoText = memoText
                    };
                    result.IsError = false;
                    result.Message = $"Elrond transaction sent successfully. TX Hash: {result.Result.TransactionResult}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to submit Elrond transaction: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error sending Elrond transaction: {ex.Message}");
            }

            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ElrondOASIS, fromAvatarId, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ElrondOASIS, toAvatarId, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create Elrond transaction
                var transaction = new
                {
                    nonce = 0,
                    value = amount.ToString(),
                    receiver = toWalletResult.Result,
                    sender = fromWalletResult.Result,
                    gasPrice = 1000000000,
                    gasLimit = 50000,
                    data = "",
                    chainID = _chainId
                };

                var json = JsonSerializer.Serialize(transaction);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                    var transactionResponse = new ElrondTransactionResponse
                    {
                        TransactionHash = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "transaction-completed",
                        Success = true
                    };

                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = "Transaction sent successfully via Elrond";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send transaction via Elrond: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Elrond: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ElrondOASIS, fromAvatarId, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ElrondOASIS, toAvatarId, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create Elrond token transaction
                var transaction = new
                {
                    nonce = 0,
                    value = "0",
                    receiver = toWalletResult.Result,
                    sender = fromWalletResult.Result,
                    gasPrice = 1000000000,
                    gasLimit = 50000,
                    data = $"ESDTTransfer@{token}@{amount}",
                    chainID = _chainId
                };

                var json = JsonSerializer.Serialize(transaction);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                    var transactionResponse = new ElrondTransactionResponse
                    {
                        TransactionHash = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "token-transaction-completed",
                        Success = true
                    };

                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = $"Token transaction sent successfully via Elrond for {token}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send token transaction via Elrond: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via Elrond: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by username
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ElrondOASIS, fromAvatarUsername, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ElrondOASIS, toAvatarUsername, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create Elrond transaction
                var transaction = new
                {
                    nonce = 0,
                    value = amount.ToString(),
                    receiver = toWalletResult.Result,
                    sender = fromWalletResult.Result,
                    gasPrice = 1000000000,
                    gasLimit = 50000,
                    data = "",
                    chainID = _chainId
                };

                var json = JsonSerializer.Serialize(transaction);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                    var transactionResponse = new ElrondTransactionResponse
                    {
                        TransactionHash = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "transaction-completed",
                        Success = true
                    };

                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = "Transaction sent successfully via Elrond";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send transaction via Elrond: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Elrond: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by username
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ElrondOASIS, fromAvatarUsername, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ElrondOASIS, toAvatarUsername, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create Elrond token transaction
                var transaction = new
                {
                    nonce = 0,
                    value = "0",
                    receiver = toWalletResult.Result,
                    sender = fromWalletResult.Result,
                    gasPrice = 1000000000,
                    gasLimit = 50000,
                    data = $"ESDTTransfer@{token}@{amount}",
                    chainID = _chainId
                };

                var json = JsonSerializer.Serialize(transaction);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                    var transactionResponse = new ElrondTransactionResponse
                    {
                        TransactionHash = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "token-transaction-completed",
                        Success = true
                    };

                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = $"Token transaction sent successfully via Elrond for {token}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send token transaction via Elrond: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via Elrond: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by email
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.ElrondOASIS, fromAvatarEmail, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.ElrondOASIS, toAvatarEmail, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create Elrond transaction
                var transaction = new
                {
                    nonce = 0,
                    value = amount.ToString(),
                    receiver = toWalletResult.Result,
                    sender = fromWalletResult.Result,
                    gasPrice = 1000000000,
                    gasLimit = 50000,
                    data = "",
                    chainID = _chainId
                };

                var json = JsonSerializer.Serialize(transaction);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                    var transactionResponse = new ElrondTransactionResponse
                    {
                        TransactionHash = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "transaction-completed",
                        Success = true
                    };

                    result.Result = transactionResponse;
                    result.IsError = false;
                    result.Message = "Transaction sent successfully via Elrond";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send transaction via Elrond: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Elrond: {ex.Message}", ex);
            }
            return result;
        }

    }
}
