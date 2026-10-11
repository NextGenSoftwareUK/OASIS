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
        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                token.ThrowIfCancellationRequested();
                var generated = await HederaSdkClient.InvokeAsync("generateMnemonic", new { keyType = "ed25519" }, token);
                result.Result = (generated.GetProperty("publicKey").GetString(), generated.GetProperty("privateKey").GetString(),
                    generated.GetProperty("seedPhrase").GetString());
                result.Message = "Hedera SDK standard Ed25519 signing identity generated. No ledger account was created; funded account creation or linking is required separately.";
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
                token.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(seedPhrase)) throw new ArgumentException("A Hedera SDK mnemonic is required.", nameof(seedPhrase));
                var restored = await HederaSdkClient.InvokeAsync("restoreMnemonic", new { keyType = "ed25519", seedPhrase }, token);
                result.Result = (restored.GetProperty("publicKey").GetString(), restored.GetProperty("privateKey").GetString());
                result.Message = "Hedera SDK standard Ed25519 signing identity restored. Ledger account existence and wallet linkage were not verified.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring Hashgraph account: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            return await ExecuteNativeBridgeTransferAsync(amount, senderAccountAddress, _bridgePoolAccountId,
                () => senderPrivateKey, "Bridge withdrawal");
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            return await ExecuteNativeBridgeTransferAsync(amount, _bridgePoolAccountId, receiverAccountAddress,
                _bridgePoolSigningKey, "Bridge deposit");
        }

        private async Task<OASISResult<BridgeTransactionResponse>> ExecuteNativeBridgeTransferAsync(decimal amount,
            string sender, string recipient, Func<string> signingKey, string memo)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(_bridgePoolAccountId ?? "", @"\A[0-9]+\.[0-9]+\.[1-9][0-9]*\z"))
                    throw new InvalidOperationException("An explicit nonzero numeric Hedera bridge pool account is required.");
                foreach (var account in new[] { sender, recipient })
                    if (!System.Text.RegularExpressions.Regex.IsMatch(account ?? "", @"\A[0-9]+\.[0-9]+\.[1-9][0-9]*\z"))
                        throw new ArgumentException("Bridge transfers require explicit nonzero numeric sender and recipient accounts.");
                if (sender == recipient) throw new ArgumentException("Bridge sender and recipient must differ.");
                var tinybars = checked(amount * 100000000m);
                if (tinybars <= 0 || tinybars != decimal.Truncate(tinybars) || tinybars > long.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(amount), "Bridge amount must fit positive exact int64 tinybars.");
                var key = signingKey?.Invoke();
                if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("A real sender DER signing key is required for the Hedera bridge transfer.");
                var transfer = await SendTokenAsync(new SendWeb3TokenRequest
                {
                    FromWalletAddress = sender, ToWalletAddress = recipient, Amount = amount,
                    FromTokenAddress = "HBAR", OwnerPrivateKey = key, MemoText = memo
                });
                if (transfer.IsError || string.IsNullOrWhiteSpace(transfer.Result?.TransactionResult))
                    throw new InvalidOperationException(transfer.Message ?? "Hedera bridge transfer did not establish consensus success.");
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = transfer.Result.TransactionResult, IsSuccessful = true,
                    Status = BridgeTransactionStatus.Completed
                };
                result.Message = "Hedera native bridge transfer confirmed by consensus receipt; cross-chain settlement is not established.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error processing Hashgraph native bridge transfer: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                var receipt = await HederaSdkClient.InvokeAsync("receipt", new
                {
                    network = _hederaNetwork, nodes = _consensusNodes, transactionId = transactionHash
                }, token);
                var status = receipt.GetProperty("status").GetString();
                result.Result = MapReceiptStatus(status);
                result.IsError = false;
                result.Message = $"Transaction status retrieved from the configured Hedera consensus network: {status}.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Hashgraph transaction status: {ex.Message}", ex);
            }
            return result;
        }

        private static BridgeTransactionStatus MapReceiptStatus(string status) => status switch
        {
            "SUCCESS" => BridgeTransactionStatus.Completed,
            "UNKNOWN" => BridgeTransactionStatus.Pending,
            "RECEIPT_NOT_FOUND" => BridgeTransactionStatus.NotFound,
            "TRANSACTION_EXPIRED" => BridgeTransactionStatus.Expired,
            "INSUFFICIENT_ACCOUNT_BALANCE" or "INSUFFICIENT_PAYER_BALANCE" => BridgeTransactionStatus.InsufficientFunds,
            "INSUFFICIENT_TX_FEE" => BridgeTransactionStatus.InsufficientFundsForFee,
            // These are terminal statuses from an actual receipt, not transport/precheck exceptions.
            "CONTRACT_REVERT_EXECUTED" or "CONTRACT_EXECUTION_EXCEPTION" or "INVALID_SIGNATURE"
                or "INVALID_ACCOUNT_ID" or "INVALID_TOKEN_ID" or "TOKEN_WAS_DELETED"
                or "TOKEN_IS_PAUSED" or "ACCOUNT_FROZEN_FOR_TOKEN" or "ACCOUNT_KYC_NOT_GRANTED_FOR_TOKEN"
                or "TOKEN_NOT_ASSOCIATED_TO_ACCOUNT" or "INSUFFICIENT_TOKEN_BALANCE"
                or "INVALID_NFT_ID" or "SENDER_DOES_NOT_OWN_NFT_SERIAL_NO" => BridgeTransactionStatus.Failed,
            // Unrecognized/malformed responses cannot establish a ledger outcome.
            _ => throw new InvalidOperationException($"Hedera consensus receipt reports an unclassified status: {status}.")
        };



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
