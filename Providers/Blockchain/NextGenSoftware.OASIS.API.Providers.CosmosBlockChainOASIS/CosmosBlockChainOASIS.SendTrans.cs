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
        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
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

                // Get wallet addresses for usernames using WalletHelper
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager, Core.Enums.ProviderType.CosmosBlockChainOASIS, fromAvatarUsername);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager, Core.Enums.ProviderType.CosmosBlockChainOASIS, toAvatarUsername);
                
                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for usernames");
                    return result;
                }
                
                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                // Submit transaction to Cosmos network via Cosmos SDK API
                var txUrl = "/cosmos/tx/v1beta1/txs";
                var txRequest = new
                {
                    tx = new
                    {
                        body = new
                        {
                            messages = new[]
                            {
                                new
                                {
                                    type = "/cosmos.bank.v1beta1.MsgSend",
                                    from_address = fromAddress,
                                    to_address = toAddress,
                                    amount = new[]
                                    {
                                        new
                                        {
                                            denom = token.ToLowerInvariant(),
                                            amount = amount.ToString()
                                        }
                                    }
                                }
                            },
                            memo = $"OASIS transaction from {fromAvatarUsername} to {toAvatarUsername}"
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
                    },
                    mode = "BROADCAST_MODE_SYNC"
                };

                var jsonContent = JsonSerializer.Serialize(txRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(txUrl, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txHash = txResponse.TryGetProperty("tx_response", out var txResp) &&
                                 txResp.TryGetProperty("txhash", out var hash)
                        ? hash.GetString()
                        : "";

                    result.Result = new TransactionResponse
                    {
                        TransactionResult = txHash ?? ""
                    };
                    result.IsError = false;
                    result.Message = "Cosmos transaction sent successfully";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Failed to send Cosmos transaction: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByUsernameAsync(token): {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, "ATOM");
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
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

                // Get wallet addresses for emails using WalletHelper
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, Core.Enums.ProviderType.CosmosBlockChainOASIS, fromAvatarEmail);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, Core.Enums.ProviderType.CosmosBlockChainOASIS, toAvatarEmail);
                
                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for emails");
                    return result;
                }
                
                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                // Submit transaction to Cosmos network via Cosmos SDK API
                var txUrl = "/cosmos/tx/v1beta1/txs";
                var txRequest = new
                {
                    tx = new
                    {
                        body = new
                        {
                            messages = new[]
                            {
                                new
                                {
                                    type = "/cosmos.bank.v1beta1.MsgSend",
                                    from_address = fromAddress,
                                    to_address = toAddress,
                                    amount = new[]
                                    {
                                        new
                                        {
                                            denom = token.ToLowerInvariant(),
                                            amount = amount.ToString()
                                        }
                                    }
                                }
                            },
                            memo = $"OASIS transaction from {fromAvatarEmail} to {toAvatarEmail}"
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
                    },
                    mode = "BROADCAST_MODE_SYNC"
                };

                var jsonContent = JsonSerializer.Serialize(txRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(txUrl, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txHash = txResponse.TryGetProperty("tx_response", out var txResp) &&
                                 txResp.TryGetProperty("txhash", out var hash)
                        ? hash.GetString()
                        : "";

                    result.Result = new TransactionResponse
                    {
                        TransactionResult = txHash ?? ""
                    };
                    result.IsError = false;
                    result.Message = "Cosmos transaction sent successfully";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Failed to send Cosmos transaction: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByEmailAsync(token): {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }


        public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
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

                // Get wallet addresses for avatars using WalletHelper
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.CosmosBlockChainOASIS, fromAvatarId);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.CosmosBlockChainOASIS, toAvatarId);
                
                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for avatars");
                    return result;
                }
                
                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                // Submit transaction to Cosmos network via Cosmos SDK API
                var txUrl = "/cosmos/tx/v1beta1/txs";
                var txRequest = new
                {
                    tx = new
                    {
                        body = new
                        {
                            messages = new[]
                            {
                                new
                                {
                                    type = "/cosmos.bank.v1beta1.MsgSend",
                                    from_address = fromAddress,
                                    to_address = toAddress,
                                    amount = new[]
                                    {
                                        new
                                        {
                                            denom = "uatom",
                                            amount = amount.ToString()
                                        }
                                    }
                                }
                            },
                            memo = $"OASIS default wallet transaction from {fromAvatarId} to {toAvatarId}"
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
                    },
                    mode = "BROADCAST_MODE_SYNC"
                };

                var jsonContent = JsonSerializer.Serialize(txRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(txUrl, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txHash = txResponse.TryGetProperty("tx_response", out var txResp) &&
                                 txResp.TryGetProperty("txhash", out var hash)
                        ? hash.GetString()
                        : "";

                    result.Result = new TransactionResponse
                    {
                        TransactionResult = txHash ?? ""
                    };
                    result.IsError = false;
                    result.Message = "Cosmos default wallet transaction sent successfully";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Failed to send Cosmos default wallet transaction: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByDefaultWalletAsync: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation)
        {
            return SendNFTAsync(transation).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transation)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Cosmos Blockchain provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Create Cosmos NFT transfer transaction using REST API
                var txRequest = new
                {
                    tx = new
                    {
                        body = new
                        {
                            messages = new[]
                            {
                                new
                                {
                                    type = "cosmos.nft.v1beta1.MsgTransfer",
                                    value = new
                                    {
                                        class_id = transation.TokenAddress,
                                        id = transation.TokenId,
                                        sender = transation.FromWalletAddress,
                                        receiver = transation.ToWalletAddress
                                    }
                                }
                            },
                            memo = "OASIS NFT transfer",
                            timeout_height = "0"
                        },
                        auth_info = new
                        {
                            signer_infos = new[]
                            {
                                new
                                {
                                    public_key = new
                                    {
                                        type = "cosmos.crypto.secp256k1.PubKey",
                                        value = "..." // This would be the actual public key
                                    },
                                    mode_info = new
                                    {
                                        single = new
                                        {
                                            mode = "SIGN_MODE_DIRECT"
                                        }
                                    },
                                    sequence = "0"
                                }
                            },
                            fee = new
                            {
                                amount = new[]
                                {
                                    new
                                    {
                                        denom = "uatom",
                                        amount = "5000" // Gas fee
                                    }
                                },
                                gas_limit = "200000"
                            }
                        },
                        signatures = new[] { "..." } // This would be the actual signature
                    },
                    mode = "BROADCAST_MODE_SYNC"
                };

                var jsonContent = JsonSerializer.Serialize(txRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/cosmos/tx/v1beta1/txs", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var nftTransactionResponse = new Web3NFTTransactionResponse();
                    if (txResponse.TryGetProperty("tx_response", out var txResp))
                    {
                        if (txResp.TryGetProperty("txhash", out var hash))
                            nftTransactionResponse.TransactionResult = hash.GetString();
                    }

                    response.Result = nftTransactionResponse;
                    response.IsError = false;
                    response.Message = "NFT transfer sent to Cosmos blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to send NFT to Cosmos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending NFT to Cosmos: {ex.Message}");
            }
            return response;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
        {
            return MintNFTAsync(transation).Result;
        }

    }
}
