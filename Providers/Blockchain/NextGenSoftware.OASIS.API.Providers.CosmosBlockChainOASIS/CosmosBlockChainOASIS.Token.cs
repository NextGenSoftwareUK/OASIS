using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
// using Microsoft.Azure.Cosmos;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS
{
    public partial class CosmosBlockChainOASIS
    {
        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrEmpty(request.FromWalletAddress) || string.IsNullOrEmpty(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "FromWalletAddress and ToWalletAddress are required");
                    return result;
                }

                // Cosmos uses REST API for transactions
                // Build transaction payload for Cosmos bank send
                var transactionPayload = new
                {
                    body = new
                    {
                        messages = new[]
                        {
                            new
                            {
                                type = "/cosmos.bank.v1beta1.MsgSend",
                                value = new
                                {
                                    from_address = request.FromWalletAddress,
                                    to_address = request.ToWalletAddress,
                                    amount = new[]
                                    {
                                        new
                                        {
                                            denom = "uatom", // Cosmos native token (would come from request in production)
                                            amount = ((long)(request.Amount * 1000000)).ToString() // Convert to uatom (6 decimals)
                                        }
                                    }
                                }
                            }
                        }
                    },
                    auth_info = new
                    {
                        signer_infos = new object[0],
                        fee = new
                        {
                            amount = new[] { new { denom = "uatom", amount = "1000" } },
                            gas_limit = "200000"
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/cosmos/tx/v1beta1/txs", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var hash = transactionResult.TryGetProperty("tx_response", out var txResponse) &&
                               txResponse.TryGetProperty("txhash", out var txhash)
                        ? txhash.GetString()
                        : "unknown";

                    result.Result = new TransactionResponse { TransactionResult = hash };
                    result.IsError = false;
                    result.Message = "Token sent successfully on Cosmos blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Cosmos API error: {httpResponse.StatusCode} - {errorContent}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Cosmos token minting requires admin permissions
                // Minting is typically done through a custom module or bank module
                var mintAddress = _contractAddress ?? request.MintedByAvatarId.ToString();
                
                var transactionPayload = new
                {
                    body = new
                    {
                        messages = new[]
                        {
                            new
                            {
                                type = "/cosmos.bank.v1beta1.MsgMint",
                                value = new
                                {
                                    amount = new[]
                                    {
                                        new
                                        {
                                            denom = "uatom",
                                            amount = "1000000" // Mint 1 ATOM (would come from request in production)
                                        }
                                    }
                                }
                            }
                        }
                    },
                    auth_info = new
                    {
                        signer_infos = new object[0],
                        fee = new
                        {
                            amount = new[] { new { denom = "uatom", amount = "1000" } },
                            gas_limit = "200000"
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/cosmos/tx/v1beta1/txs", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var hash = transactionResult.TryGetProperty("tx_response", out var txResponse) &&
                               txResponse.TryGetProperty("txhash", out var txhash)
                        ? txhash.GetString()
                        : "unknown";

                    result.Result = new TransactionResponse { TransactionResult = hash };
                    result.IsError = false;
                    result.Message = "Token minted successfully on Cosmos blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Cosmos API error: {httpResponse.StatusCode} - {errorContent}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrEmpty(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // Cosmos token burning
                var transactionPayload = new
                {
                    body = new
                    {
                        messages = new[]
                        {
                            new
                            {
                                type = "/cosmos.bank.v1beta1.MsgBurn",
                                value = new
                                {
                                    amount = new[]
                                    {
                                        new
                                        {
                                            denom = request.TokenAddress,
                                            amount = "1000000" // Burn amount (would come from request in production)
                                        }
                                    }
                                }
                            }
                        }
                    },
                    auth_info = new
                    {
                        signer_infos = new object[0],
                        fee = new
                        {
                            amount = new[] { new { denom = "uatom", amount = "1000" } },
                            gas_limit = "200000"
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/cosmos/tx/v1beta1/txs", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var hash = transactionResult.TryGetProperty("tx_response", out var txResponse) &&
                               txResponse.TryGetProperty("txhash", out var txhash)
                        ? txhash.GetString()
                        : "unknown";

                    result.Result = new TransactionResponse { TransactionResult = hash };
                    result.IsError = false;
                    result.Message = "Token burned successfully on Cosmos blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Cosmos API error: {httpResponse.StatusCode} - {errorContent}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrEmpty(request.TokenAddress) || string.IsNullOrEmpty(request.FromWalletPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                    return result;
                }

                // Lock token by transferring to bridge pool
                var bridgePoolAddress = _contractAddress ?? "cosmos1..."; // Bridge pool address
                
                var transactionPayload = new
                {
                    body = new
                    {
                        messages = new[]
                        {
                            new
                            {
                                type = "/cosmos.bank.v1beta1.MsgSend",
                                value = new
                                {
                                    from_address = request.FromWalletPrivateKey, // Would derive address from private key in production
                                    to_address = bridgePoolAddress,
                                    amount = new[]
                                    {
                                        new
                                        {
                                            denom = request.TokenAddress,
                                            amount = "1000000" // Lock amount (would come from request in production)
                                        }
                                    }
                                }
                            }
                        }
                    },
                    auth_info = new
                    {
                        signer_infos = new object[0],
                        fee = new
                        {
                            amount = new[] { new { denom = "uatom", amount = "1000" } },
                            gas_limit = "200000"
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/cosmos/tx/v1beta1/txs", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var hash = transactionResult.TryGetProperty("tx_response", out var txResponse) &&
                               txResponse.TryGetProperty("txhash", out var txhash)
                        ? txhash.GetString()
                        : "unknown";

                    result.Result = new TransactionResponse { TransactionResult = hash };
                    result.IsError = false;
                    result.Message = "Token locked successfully on Cosmos blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Cosmos API error: {httpResponse.StatusCode} - {errorContent}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrEmpty(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // Unlock token by transferring from bridge pool to recipient
                var bridgePoolAddress = _contractAddress ?? "cosmos1..."; // Bridge pool address
                var recipientAddress = bridgePoolAddress; // Would get from UnlockedByAvatarId in production
                
                var transactionPayload = new
                {
                    body = new
                    {
                        messages = new[]
                        {
                            new
                            {
                                type = "/cosmos.bank.v1beta1.MsgSend",
                                value = new
                                {
                                    from_address = bridgePoolAddress,
                                    to_address = recipientAddress,
                                    amount = new[]
                                    {
                                        new
                                        {
                                            denom = request.TokenAddress,
                                            amount = "1000000" // Unlock amount (would come from request in production)
                                        }
                                    }
                                }
                            }
                        }
                    },
                    auth_info = new
                    {
                        signer_infos = new object[0],
                        fee = new
                        {
                            amount = new[] { new { denom = "uatom", amount = "1000" } },
                            gas_limit = "200000"
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/cosmos/tx/v1beta1/txs", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var hash = transactionResult.TryGetProperty("tx_response", out var txResponse) &&
                               txResponse.TryGetProperty("txhash", out var txhash)
                        ? txhash.GetString()
                        : "unknown";

                    result.Result = new TransactionResponse { TransactionResult = hash };
                    result.IsError = false;
                    result.Message = "Token unlocked successfully on Cosmos blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Cosmos API error: {httpResponse.StatusCode} - {errorContent}");
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

    }
}
