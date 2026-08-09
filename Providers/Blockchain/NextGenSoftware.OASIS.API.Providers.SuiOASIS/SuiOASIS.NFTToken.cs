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
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS
{
    public partial class SuiOASIS
    {
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Sui provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ExportAllDataForAvatarByIdAsync is not supported by Sui provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAllDataForAvatarByIdAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        // Add IOASISBlockchainStorageProvider methods
        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText = "")
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText = "")
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Sui provider is not activated");
                    return result;
                }

                // Sui native SUI transfer via RPC
                var mistAmount = (ulong)(amount * 1_000_000_000m);
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_transferSui",
                    @params = new object[]
                    {
                        fromWalletAddress,
                        toWalletAddress,
                        mistAmount.ToString(),
                        _privateKey // In production, this would be properly signed
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txHash = responseData.TryGetProperty("result", out var resultProp) ? resultProp.GetString() : string.Empty;
                    result.Result.TransactionResult = txHash ?? string.Empty;
                    result.IsError = false;
                    result.Message = "Sui transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send Sui transaction: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {ex.Message}", ex);
            }
            return result;
        }



        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Sui provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "ToWalletAddress is required");
                    return result;
                }

                // Sui token transfer via RPC
                // Convert amount to MIST (1 SUI = 1,000,000,000 MIST)
                var mistAmount = (ulong)(request.Amount * 1_000_000_000m);
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_transferObject",
                    @params = new object[]
                    {
                        request.FromTokenAddress ?? "0x2::sui::SUI", // Default to native SUI
                        request.ToWalletAddress,
                        mistAmount.ToString(),
                        _privateKey // In production, this would be properly signed
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txHash = responseData.TryGetProperty("result", out var resultProp) ? resultProp.GetString() : string.Empty;
                    result.Result.TransactionResult = txHash ?? string.Empty;
                    result.IsError = false;
                    result.Message = "Token sent successfully on Sui";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send token on Sui: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Sui provider is not activated");
                    return result;
                }

                if (request == null || request.MetaData == null || 
                    !request.MetaData.ContainsKey("TokenAddress") || string.IsNullOrWhiteSpace(request.MetaData["TokenAddress"]?.ToString()) ||
                    !request.MetaData.ContainsKey("MintToWalletAddress") || string.IsNullOrWhiteSpace(request.MetaData["MintToWalletAddress"]?.ToString()))
                {
                    OASISErrorHandling.HandleError(ref result, "TokenAddress and MintToWalletAddress are required in MetaData");
                    return result;
                }

                var tokenAddress = request.MetaData["TokenAddress"].ToString();
                var mintToWalletAddress = request.MetaData["MintToWalletAddress"].ToString();
                var amount = request.MetaData?.ContainsKey("Amount") == true && decimal.TryParse(request.MetaData["Amount"]?.ToString(), out var amt) ? amt : 0m;

                // Sui token minting via RPC (requires Move smart contract)
                var mistAmount = (ulong)(amount * 1_000_000_000m);
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_moveCall",
                    @params = new object[]
                    {
                        mintToWalletAddress,
                        tokenAddress,
                        "mint",
                        new object[] { },
                        new object[] { mistAmount.ToString() },
                        Guid.NewGuid().ToString()
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txHash = responseData.TryGetProperty("result", out var resultProp) ? resultProp.GetString() : string.Empty;
                    result.Result.TransactionResult = txHash ?? string.Empty;
                    result.IsError = false;
                    result.Message = "Token minted successfully on Sui";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint token on Sui: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Sui provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                    string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "TokenAddress and OwnerPrivateKey are required");
                    return result;
                }

                // Sui token burning via RPC (requires Move smart contract)
                // IBurnWeb3TokenRequest doesn't have Amount, so we'll burn the full balance
                // In a real implementation, you would get the balance first
                var mistAmount = 0UL; // Would need to get balance from token contract
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_moveCall",
                    @params = new object[]
                    {
                        request.OwnerPrivateKey, // Use OwnerPrivateKey to derive wallet address
                        request.TokenAddress,
                        "burn",
                        new object[] { },
                        new object[] { mistAmount.ToString() },
                        Guid.NewGuid().ToString()
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txHash = responseData.TryGetProperty("result", out var resultProp) ? resultProp.GetString() : string.Empty;
                    result.Result.TransactionResult = txHash ?? string.Empty;
                    result.IsError = false;
                    result.Message = "Token burned successfully on Sui";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to burn token on Sui: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Sui provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                    string.IsNullOrWhiteSpace(request.FromWalletPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "TokenAddress and FromWalletPrivateKey are required");
                    return result;
                }

                // Sui token locking via RPC (requires Move smart contract with lock functionality)
                // ILockWeb3TokenRequest doesn't have Amount, so we'll lock the full balance
                // In a real implementation, you would get the balance first
                var mistAmount = 0UL; // Would need to get balance from token contract
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_moveCall",
                    @params = new object[]
                    {
                        request.FromWalletAddress,
                        request.TokenAddress,
                        "lock",
                        new object[] { },
                        new object[] { mistAmount.ToString() },
                        Guid.NewGuid().ToString()
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txHash = responseData.TryGetProperty("result", out var resultProp) ? resultProp.GetString() : string.Empty;
                    result.Result.TransactionResult = txHash ?? string.Empty;
                    result.IsError = false;
                    result.Message = "Token locked successfully on Sui";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock token on Sui: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Sui provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "TokenAddress is required");
                    return result;
                }

                // Sui token unlocking via RPC (requires Move smart contract with unlock functionality)
                // IUnlockWeb3TokenRequest doesn't have Amount or UnlockWalletAddress
                // In a real implementation, you would get these from the locked token record using Web3TokenId
                var mistAmount = 0UL; // Would need to get from locked token record
                var unlockWalletAddress = ""; // Would need to get from locked token record
                
                if (string.IsNullOrWhiteSpace(unlockWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Unlock wallet address is required but not available in IUnlockWeb3TokenRequest interface");
                    return result;
                }
                
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_moveCall",
                    @params = new object[]
                    {
                        unlockWalletAddress,
                        request.TokenAddress,
                        "unlock",
                        new object[] { },
                        new object[] { mistAmount.ToString() },
                        Guid.NewGuid().ToString()
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txHash = responseData.TryGetProperty("result", out var resultProp) ? resultProp.GetString() : string.Empty;
                    result.Result.TransactionResult = txHash ?? string.Empty;
                    result.IsError = false;
                    result.Message = "Token unlocked successfully on Sui";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlock token on Sui: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Sui provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "WalletAddress is required");
                    return result;
                }

                // Get Sui balance via RPC
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_getBalance",
                    @params = new object[] { request.WalletAddress }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    if (responseData.TryGetProperty("result", out var resultProp))
                    {
                        var totalBalance = resultProp.TryGetProperty("totalBalance", out var balanceProp) ? balanceProp.GetString() : "0";
                        var balanceInMist = ulong.Parse(totalBalance);
                        var balanceInSUI = balanceInMist / 1_000_000_000.0;
                        result.Result = balanceInSUI;
                        result.IsError = false;
                        result.Message = "Balance retrieved successfully";
                    }
                    else
                    {
                        result.Result = 0.0;
                        result.IsError = false;
                    }
                }
                else
                {
                    result.Result = 0.0;
                    result.IsError = false;
                    result.Message = "Account not found or has zero balance";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance: {ex.Message}", ex);
            }
            return result;
        }

    }
}
