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
                ValidateBridgeAccounts(sender, recipient);
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
                    throw transfer.Exception ?? new InvalidOperationException(transfer.Message ?? "Hedera bridge transfer did not establish consensus success.");
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

        private void ValidateBridgeAccounts(string sender, string recipient)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(_bridgePoolAccountId ?? "", @"\A[0-9]+\.[0-9]+\.[1-9][0-9]*\z"))
                throw new InvalidOperationException("An explicit nonzero numeric Hedera bridge pool account is required.");
            foreach (var account in new[] { sender, recipient })
                if (!System.Text.RegularExpressions.Regex.IsMatch(account ?? "", @"\A[0-9]+\.[0-9]+\.[1-9][0-9]*\z"))
                    throw new ArgumentException("Bridge transfers require explicit nonzero numeric sender and recipient accounts.");
            if (sender == recipient) throw new ArgumentException("Bridge sender and recipient must differ.");
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

        internal static BridgeTransactionStatus MapReceiptStatus(string status) => status switch
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
            return await ExecuteAvatarNftEscrowAsync("nftEscrowLock", request?.LockedByAvatarId ?? Guid.Empty,
                request?.Web3NFTId ?? Guid.Empty, request?.NFTTokenAddress);
        }

        public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
        {
            return UnlockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
        {
            return await ExecuteAvatarNftEscrowAsync("nftEscrowUnlock", request?.UnlockedByAvatarId ?? Guid.Empty,
                request?.Web3NFTId ?? Guid.Empty, request?.NFTTokenAddress);
        }

        private async Task<OASISResult<IWeb3NFTTransactionResponse>> ExecuteAvatarNftEscrowAsync(string action,
            Guid avatarId, Guid web3NftId, string address)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(_nftEscrowContractId ?? "", @"\A[0-9]+\.[0-9]+\.[1-9][0-9]*\z")
                    || !System.Text.RegularExpressions.Regex.IsMatch(_nftEscrowRuntimeCodeHash ?? "", @"\A0x[0-9a-fA-F]{64}\z")
                    || _nftEscrowGasLimit <= 0 || _nftEscrowGasLimit > 9007199254740991L || _nftEscrowMaxFeeTinybars <= 0)
                    throw new InvalidOperationException("An explicit Hedera NFT escrow contract, verified runtime code hash, positive gas limit and fee ceiling are required.");
                if (avatarId == Guid.Empty) throw new ArgumentException("An explicit requesting avatar identity is required for NFT escrow.");
                var asset = ParseHederaNftAddress(address);
                var walletResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(avatarId, Core.Enums.ProviderType.HashgraphOASIS, showPrivateKeys: true);
                if (walletResult.IsError || walletResult.Result == null || string.IsNullOrWhiteSpace(walletResult.Result.PrivateKey))
                    throw new InvalidOperationException("Could not resolve the requesting avatar's real Hedera signing wallet.");
                var wallet = walletResult.Result;
                var mutationId = HederaSdkClient.CreateTransactionId(wallet.WalletAddress);
                var committed = await HederaSdkClient.InvokeAsync(action, new
                {
                    network = _hederaNetwork, nodes = _consensusNodes, sender = wallet.WalletAddress,
                    privateKey = wallet.PrivateKey, publicKey = wallet.PublicKey, keyType = "der",
                    tokenId = asset.TokenId, serial = asset.Serial, contractId = _nftEscrowContractId,
                    runtimeCodeHash = _nftEscrowRuntimeCodeHash,
                    gas = _nftEscrowGasLimit, maxFeeTinybars = _nftEscrowMaxFeeTinybars.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    transactionId = mutationId, approvalTransactionId = action == "nftEscrowLock" ? HederaSdkClient.CreateTransactionId(wallet.WalletAddress) : null
                });
                if (committed.GetProperty("status").GetString() != "SUCCESS"
                    || committed.GetProperty("transactionId").GetString() != mutationId)
                    throw new InvalidOperationException("Escrow SDK response does not establish the requested mutation's verified success.");
                result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                {
                    TransactionResult = mutationId,
                    Web3NFT = new Web3NFT { Id = web3NftId, NFTTokenAddress = address, CollectionPublicKey = asset.TokenId }
                };
                result.Message = "Hedera NFT escrow mutation confirmed by consensus receipt and custody readback.";
                if (committed.TryGetProperty("approvalTransactionId", out var approval) && approval.ValueKind == JsonValueKind.String)
                    result.Message += " Approval transaction: " + approval.GetString() + ".";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error processing Hashgraph NFT escrow: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
        {
            return await ExecuteNftBridgeTransferAsync(nftTokenAddress, tokenId, senderAccountAddress,
                _bridgePoolAccountId, () => senderPrivateKey, "NFT bridge withdrawal");
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
        {
            // The source hash is not a proof or release authorization. Settlement must be authorized by the host.
            return await ExecuteNftBridgeTransferAsync(nftTokenAddress, tokenId, _bridgePoolAccountId,
                receiverAccountAddress, _bridgePoolSigningKey, "NFT bridge deposit");
        }

        private async Task<OASISResult<BridgeTransactionResponse>> ExecuteNftBridgeTransferAsync(string tokenAddress,
            string serial, string sender, string recipient, Func<string> signingKey, string memo)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                ValidateBridgeAccounts(sender, recipient);
                var asset = ParseHederaNftAddress((tokenAddress ?? "").Contains('/') ? tokenAddress : tokenAddress + "/" + serial);
                if (serial != asset.Serial) throw new ArgumentException("NFT bridge serial must agree with the asset address.");
                var key = signingKey?.Invoke();
                if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("A real sender DER signing key is required for the Hedera NFT bridge transfer.");
                var transfer = await SendNFTAsync(new SendWeb3NFTRequest
                {
                    TokenAddress = asset.TokenId, TokenId = asset.Serial,
                    FromNFTTokenAddress = asset.TokenId + "/" + asset.Serial,
                    FromWalletAddress = sender, ToWalletAddress = recipient, Amount = 1,
                    OwnerPrivateKey = key, MemoText = memo
                });
                if (transfer.IsError || string.IsNullOrWhiteSpace(transfer.Result?.TransactionResult))
                    throw transfer.Exception ?? new InvalidOperationException(transfer.Message ?? "HTS NFT bridge transfer did not establish consensus success.");
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = transfer.Result.TransactionResult, IsSuccessful = true,
                    Status = BridgeTransactionStatus.Completed
                };
                result.Message = "HTS NFT bridge transfer confirmed by consensus receipt; source settlement and release authorization are not established.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error processing Hashgraph NFT bridge transfer: {ex.Message}", ex);
            }
            return result;
        }

    }
}
