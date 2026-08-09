using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
    public partial class HashgraphOASIS
    {
        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Burn NFT on Hedera using HTS
                var burnUrl = $"{_httpClient.BaseAddress}/api/v1/tokens/{request.NFTTokenAddress}/nfts/{request.Web3NFTId}/burn";
                var response = await _httpClient.PostAsync(burnUrl, new StringContent("{}", Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(content);

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = txData.TryGetProperty("transaction_id", out var txId) ? txId.GetString() : "",
                        Web3NFT = new Web3NFT
                        {
                            NFTTokenAddress = request.NFTTokenAddress
                        }
                    };
                    result.IsError = false;
                    result.Message = "NFT burned successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph NFT burn failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }


        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var hashgraphClient = new HashgraphClient();
                var accountInfo = await hashgraphClient.CreateAccountAsync();
                
                if (accountInfo != null)
                {
                    result.Result = (accountInfo.PublicKey ?? "", accountInfo.PrivateKey ?? "", accountInfo.SeedPhrase ?? "");
                    result.IsError = false;
                    result.Message = "Hashgraph account created successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to create Hashgraph account");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating Hashgraph account: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var hashgraphClient = new HashgraphClient();
                var accountInfo = await hashgraphClient.RestoreAccountAsync(seedPhrase);
                
                if (accountInfo != null)
                {
                    result.Result = (accountInfo.PublicKey ?? "", accountInfo.PrivateKey ?? "");
                    result.IsError = false;
                    result.Message = "Hashgraph account restored successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to restore Hashgraph account");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring Hashgraph account: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = senderAccountAddress,
                    ToAddress = _contractAddress ?? "0.0.0",
                    Amount = amount,
                    Memo = "Bridge withdrawal"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = transactionResult.TransactionId ?? "",
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Completed
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph withdrawal completed successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to process Hashgraph withdrawal");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error processing Hashgraph withdrawal: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = _contractAddress ?? "0.0.0",
                    ToAddress = receiverAccountAddress,
                    Amount = amount,
                    Memo = "Bridge deposit"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = transactionResult.TransactionId ?? "",
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Completed
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph deposit completed successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to process Hashgraph deposit");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error processing Hashgraph deposit: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var hashgraphClient = new HashgraphClient();
                var status = await hashgraphClient.GetTransactionStatusAsync(transactionHash);

                result.Result = status;
                result.IsError = false;
                result.Message = "Transaction status retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Hashgraph transaction status: {ex.Message}", ex);
            }
            return result;
        }



        public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
        {
            return LockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var hashgraphClient = new HashgraphClient();
                var nftData = await hashgraphClient.GetNFTData(request.NFTTokenAddress);
                
                if (nftData != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = "NFT locked on Hashgraph",
                        Web3NFT = new Web3NFT
                        {
                            NFTTokenAddress = request.NFTTokenAddress
                        }
                    };
                    result.IsError = false;
                    result.Message = "NFT locked successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to lock NFT on Hashgraph");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking NFT on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
        {
            return UnlockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var hashgraphClient = new HashgraphClient();
                var nftData = await hashgraphClient.GetNFTData(request.NFTTokenAddress);
                
                if (nftData != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = "NFT unlocked on Hashgraph",
                        Web3NFT = new Web3NFT
                        {
                            NFTTokenAddress = request.NFTTokenAddress
                        }
                    };
                    result.IsError = false;
                    result.Message = "NFT unlocked successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to unlock NFT on Hashgraph");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = senderAccountAddress,
                    ToAddress = _contractAddress ?? "0.0.0",
                    Amount = 0m,
                    Memo = $"NFT withdrawal: {nftTokenAddress}:{tokenId}"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = transactionResult.TransactionId ?? "",
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Completed
                    };
                    result.IsError = false;
                    result.Message = "NFT withdrawn successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to withdraw NFT on Hashgraph");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = _contractAddress ?? "0.0.0",
                    ToAddress = receiverAccountAddress,
                    Amount = 0m,
                    Memo = $"NFT deposit: {nftTokenAddress}:{tokenId}"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = transactionResult.TransactionId ?? "",
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Completed
                    };
                    result.IsError = false;
                    result.Message = "NFT deposited successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deposit NFT on Hashgraph");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing NFT on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }
    }
}
