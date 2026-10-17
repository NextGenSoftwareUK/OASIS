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
    // NFT-specific lock/unlock methods
    public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
    {
        return LockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                return result;
            }

            var bridgePoolAddress = _contractAddress ?? "erd1qqqqqqqqqqqqqpgqhe8t5jewej70zupmh44jurgn29psua5l2jps3ntjj3";
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = string.Empty,
                ToWalletAddress = bridgePoolAddress,
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error locking NFT: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
    {
        return UnlockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                return result;
            }

            var bridgePoolAddress = _contractAddress ?? "erd1qqqqqqqqqqqqqpgqhe8t5jewej70zupmh44jurgn29psua5l2jps3ntjj3";
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = bridgePoolAddress,
                ToWalletAddress = string.Empty,
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to unlock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
    {
        return BurnNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                return result;
            }
            if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Burn request and NFT token address are required");
                return result;
            }
            // Elrond/MultiversX burn: send NFT to zero address
            var burnRequest = new SendWeb3NFTRequest
            {
                FromWalletAddress = string.Empty,
                ToWalletAddress = "erd1qqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqq",
                TokenAddress = request.NFTTokenAddress ?? "",
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1
            };
            var sendResult = await SendNFTAsync(burnRequest);
            if (!sendResult.IsError && sendResult.Result != null)
            {
                result.IsError = false;
                result.Result.TransactionResult = sendResult.Result.TransactionResult;
                result.Message = "NFT burn submitted on Elrond.";
            }
            else
                OASISErrorHandling.HandleError(ref result, sendResult.Message ?? "Failed to burn NFT on Elrond", sendResult.Exception);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error burning NFT on Elrond: {ex.Message}", ex);
        }
        return result;
    }

    // NFT Bridge Methods
    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                return result;
            }

            var lockRequest = new LockWeb3NFTRequest
            {
                NFTTokenAddress = nftTokenAddress,
                Web3NFTId = Guid.TryParse(tokenId, out var guid) ? guid : Guid.NewGuid(),
                LockedByAvatarId = Guid.Empty
            };

            var lockResult = await LockNFTAsync(lockRequest);
            if (lockResult.IsError || lockResult.Result == null)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = lockResult.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {lockResult.Message}");
                return result;
            }

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = lockResult.Result.TransactionResult ?? string.Empty,
                IsSuccessful = !lockResult.IsError,
                Status = BridgeTransactionStatus.Pending
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
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
                OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(receiverAccountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address and receiver address are required");
                return result;
            }

            var mintRequest = new MintWeb3NFTRequest
            {
                SendToAddressAfterMinting = receiverAccountAddress,
            };

            var mintResult = await MintNFTAsync(mintRequest);
            if (mintResult.IsError || mintResult.Result == null)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = mintResult.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                OASISErrorHandling.HandleError(ref result, $"Failed to deposit/mint NFT: {mintResult.Message}");
                return result;
            }

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = mintResult.Result.TransactionResult ?? string.Empty,
                IsSuccessful = !mintResult.IsError,
                Status = BridgeTransactionStatus.Pending
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error depositing NFT: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }


    OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.SendToken(ISendWeb3TokenRequest request) => ((IOASISBlockchainStorageProvider)this).SendTokenAsync(request).Result;
    async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.SendTokenAsync(ISendWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (request == null) { OASISErrorHandling.HandleError(ref result, "Request is required"); return result; }
            var tx = await SendTransactionAsync(request.FromWalletAddress ?? "", request.ToWalletAddress ?? "", request.Amount, request.MemoText ?? "");
            result.Result = tx.Result; result.IsError = tx.IsError; result.Message = tx.Message; result.Exception = tx.Exception;
        }
        catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
        return result;
    }
    OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.MintToken(IMintWeb3TokenRequest request) => ((IOASISBlockchainStorageProvider)this).MintTokenAsync(request).Result;
    async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.MintTokenAsync(IMintWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Symbol))
            { OASISErrorHandling.HandleError(ref result, "MintTokenAsync: request and Symbol (ESDT token identifier) are required."); return result; }
            if (!IsProviderActivated)
            { OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated."); return result; }

            // MultiversX ESDT local mint: ESDTLocalMint@{hex(tokenId)}@{hex(amount)}
            var tokenIdHex = Convert.ToHexString(Encoding.UTF8.GetBytes(request.Symbol));
            var amountBigInt = new System.Numerics.BigInteger(request.Amount * 1_000_000_000_000_000_000m);
            var amountHex = amountBigInt.ToString("X");
            var walletAddress = await GetWalletAddressAsync();
            var txData = new
            {
                nonce = await GetAccountNonceAsync(),
                value = "0",
                receiver = walletAddress,
                sender = walletAddress,
                gasPrice = 1000000000,
                gasLimit = 60000000,
                data = $"ESDTLocalMint@{tokenIdHex}@{amountHex}",
                chainID = "1",
                version = 1
            };
            var response = await _httpClient.PostAsync("/transaction/send",
                new StringContent(JsonSerializer.Serialize(txData), Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var txResult = JsonSerializer.Deserialize<ElrondTransactionResult>(content);
                result.Result = new ElrondTransactionResponse { TransactionResult = txResult?.txHash ?? "" };
                result.IsError = false;
                result.Message = $"ESDT MintToken submitted. TX: {txResult?.txHash}";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"MintTokenAsync failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error in MintTokenAsync: {ex.Message}", ex); }
        return result;
    }
    OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.BurnToken(IBurnWeb3TokenRequest request) => ((IOASISBlockchainStorageProvider)this).BurnTokenAsync(request).Result;
    async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.BurnTokenAsync(IBurnWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
            { OASISErrorHandling.HandleError(ref result, "BurnTokenAsync: request and TokenAddress are required."); return result; }
            if (!IsProviderActivated)
            { OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated."); return result; }

            // MultiversX ESDT NFT burn: ESDTNFTBurn@{hex(tokenId)}@{hex(nonce=1)}@{hex(amount=1)}
            var tokenIdHex = Convert.ToHexString(Encoding.UTF8.GetBytes(request.TokenAddress));
            var walletAddress = await GetWalletAddressAsync();
            var txData = new
            {
                nonce = await GetAccountNonceAsync(),
                value = "0",
                receiver = walletAddress,
                sender = walletAddress,
                gasPrice = 1000000000,
                gasLimit = 60000000,
                data = $"ESDTNFTBurn@{tokenIdHex}@01@01",
                chainID = "1",
                version = 1
            };
            var response = await _httpClient.PostAsync("/transaction/send",
                new StringContent(JsonSerializer.Serialize(txData), Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var txResult = JsonSerializer.Deserialize<ElrondTransactionResult>(content);
                result.Result = new ElrondTransactionResponse { TransactionResult = txResult?.txHash ?? "" };
                result.IsError = false;
                result.Message = $"ESDT BurnToken submitted. TX: {txResult?.txHash}";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"BurnTokenAsync failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error in BurnTokenAsync: {ex.Message}", ex); }
        return result;
    }
    OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.LockToken(ILockWeb3TokenRequest request) => ((IOASISBlockchainStorageProvider)this).LockTokenAsync(request).Result;
    async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.LockTokenAsync(ILockWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || string.IsNullOrWhiteSpace(request.FromWalletAddress))
            { OASISErrorHandling.HandleError(ref result, "LockTokenAsync: request, TokenAddress and FromWalletAddress are required."); return result; }
            if (!IsProviderActivated)
            { OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated."); return result; }

            // MultiversX bridge lock: ESDTNFTTransfer to bridge contract with @lock argument
            // Bridge SC: erd1qqqqqqqqqqqqqpgqmuk0q2saj0mgutxm4teywre6dl8wqf58xamqdrukln
            const string bridgeAddress = "erd1qqqqqqqqqqqqqpgqmuk0q2saj0mgutxm4teywre6dl8wqf58xamqdrukln";
            var tokenIdHex = Convert.ToHexString(Encoding.UTF8.GetBytes(request.TokenAddress));
            var bridgeAddrHex = Convert.ToHexString(Encoding.UTF8.GetBytes(bridgeAddress));
            var txData = new
            {
                nonce = await GetAccountNonceAsync(),
                value = "0",
                receiver = request.FromWalletAddress,
                sender = request.FromWalletAddress,
                gasPrice = 1000000000,
                gasLimit = 60000000,
                data = $"ESDTNFTTransfer@{tokenIdHex}@01@01@{bridgeAddrHex}@6C6F636B",
                chainID = "1",
                version = 1
            };
            var response = await _httpClient.PostAsync("/transaction/send",
                new StringContent(JsonSerializer.Serialize(txData), Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var txResult = JsonSerializer.Deserialize<ElrondTransactionResult>(content);
                result.Result = new ElrondTransactionResponse { TransactionResult = txResult?.txHash ?? "" };
                result.IsError = false;
                result.Message = $"ESDT LockToken submitted. TX: {txResult?.txHash}";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"LockTokenAsync failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error in LockTokenAsync: {ex.Message}", ex); }
        return result;
    }
    OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.UnlockToken(IUnlockWeb3TokenRequest request) => ((IOASISBlockchainStorageProvider)this).UnlockTokenAsync(request).Result;
    async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.UnlockTokenAsync(IUnlockWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
            { OASISErrorHandling.HandleError(ref result, "UnlockTokenAsync: request and TokenAddress are required."); return result; }
            if (!IsProviderActivated)
            { OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated."); return result; }

            // MultiversX bridge unlock: call bridge SC unlock function with token identifier
            const string bridgeAddress = "erd1qqqqqqqqqqqqqpgqmuk0q2saj0mgutxm4teywre6dl8wqf58xamqdrukln";
            var tokenIdHex = Convert.ToHexString(Encoding.UTF8.GetBytes(request.TokenAddress));
            var walletAddress = await GetWalletAddressAsync();
            var txData = new
            {
                nonce = await GetAccountNonceAsync(),
                value = "0",
                receiver = bridgeAddress,
                sender = walletAddress,
                gasPrice = 1000000000,
                gasLimit = 60000000,
                data = $"unlock@{tokenIdHex}",
                chainID = "1",
                version = 1
            };
            var response = await _httpClient.PostAsync("/transaction/send",
                new StringContent(JsonSerializer.Serialize(txData), Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var txResult = JsonSerializer.Deserialize<ElrondTransactionResult>(content);
                result.Result = new ElrondTransactionResponse { TransactionResult = txResult?.txHash ?? "" };
                result.IsError = false;
                result.Message = $"ESDT UnlockToken submitted. TX: {txResult?.txHash}";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"UnlockTokenAsync failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error in UnlockTokenAsync: {ex.Message}", ex); }
        return result;
    }
    OASISResult<double> IOASISBlockchainStorageProvider.GetBalance(IGetWeb3WalletBalanceRequest request) => ((IOASISBlockchainStorageProvider)this).GetBalanceAsync(request).Result;
    async Task<OASISResult<double>> IOASISBlockchainStorageProvider.GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
    {
        var result = new OASISResult<double>();
        if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress)) { OASISErrorHandling.HandleError(ref result, "Wallet address required"); return result; }
        var balanceResult = await GetAccountBalanceAsync(request.WalletAddress);
        if (!balanceResult.IsError && balanceResult.Result >= 0) { result.Result = (double)balanceResult.Result; result.IsError = false; result.Message = balanceResult.Message; }
        else { result.IsError = true; result.Message = balanceResult.Message; result.Exception = balanceResult.Exception; }
        return result;
    }
    OASISResult<IList<IWalletTransaction>> IOASISBlockchainStorageProvider.GetTransactions(IGetWeb3TransactionsRequest request) => ((IOASISBlockchainStorageProvider)this).GetTransactionsAsync(request).Result;
    Task<OASISResult<IList<IWalletTransaction>>> IOASISBlockchainStorageProvider.GetTransactionsAsync(IGetWeb3TransactionsRequest request)
    {
        var result = new OASISResult<IList<IWalletTransaction>>();
        result.Result = new List<IWalletTransaction>();
        if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress)) { OASISErrorHandling.HandleError(ref result, "Wallet address required"); return Task.FromResult(result); }
        OASISErrorHandling.HandleError(ref result, "GetTransactions for Elrond requires gateway transaction history endpoint.");
        return Task.FromResult(result);
    }
    OASISResult<IKeyPairAndWallet> IOASISBlockchainStorageProvider.GenerateKeyPair() => ((IOASISBlockchainStorageProvider)this).GenerateKeyPairAsync().Result;
    async Task<OASISResult<IKeyPairAndWallet>> IOASISBlockchainStorageProvider.GenerateKeyPairAsync()
    {
        var result = new OASISResult<IKeyPairAndWallet>();
        try
        {
            var accountResult = await CreateAccountAsync(CancellationToken.None);
            if (!accountResult.IsError && accountResult.Result.PublicKey != null)
            {
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    keyPair.PublicKey = accountResult.Result.PublicKey;
                    keyPair.PrivateKey = accountResult.Result.PrivateKey;
                    keyPair.WalletAddressLegacy = "";
                }
                result.Result = keyPair; result.IsError = false; result.Message = accountResult.Message;
            }
            else OASISErrorHandling.HandleError(ref result, accountResult.Message ?? "Failed to create account");
        }
        catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
        return result;
    }



    }
}
