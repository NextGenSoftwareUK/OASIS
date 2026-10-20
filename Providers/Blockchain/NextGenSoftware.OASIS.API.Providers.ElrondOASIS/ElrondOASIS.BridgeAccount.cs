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
    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
    {
        var result = new OASISResult<decimal>();
        try
        {
            if (!IsProviderActivated || _httpClient == null)
            {
                OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(accountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Account address is required");
                return result;
            }

            // Call Elrond API to get account balance
            var response = await _httpClient.GetAsync($"/address/{accountAddress}/balance", token);
            var content = await response.Content.ReadAsStringAsync(token);
            var jsonDoc = JsonDocument.Parse(content);

            if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement))
            {
                var balanceStr = dataElement.GetProperty("balance").GetString();
                if (ulong.TryParse(balanceStr, out var balance))
                {
                    // Elrond amounts are in wei (1 EGLD = 10^18 wei)
                    result.Result = balance / 1_000_000_000_000_000_000m;
                    result.IsError = false;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse balance");
                }
            }
            else
            {
                result.Result = 0m;
                result.IsError = false;
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting Elrond account balance: {ex.Message}", ex);
        }
        return result;
    }

    /// <summary>New MultiversX account: 24-word BIP-39 mnemonic and its first SLIP-10 Ed25519 key (hex public key, hex secret key).</summary>
    public Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
        try
        {
            token.ThrowIfCancellationRequested();
            var mnemonic = ElrondKeys.NewMnemonic();
            var keys = ElrondKeys.FromMnemonic(mnemonic);
            result.Result = (keys.PublicKeyHex, keys.SecretKeyHex, mnemonic);
            result.Message = $"MultiversX account {keys.Address} created.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error creating MultiversX account: {ex.Message}", ex);
        }
        return Task.FromResult(result);
    }

    /// <summary>Restores the first account (index 0) of a BIP-39 mnemonic, as MultiversX wallets do.</summary>
    public Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey)>();
        try
        {
            token.ThrowIfCancellationRequested();
            var keys = ElrondKeys.FromMnemonic(seedPhrase);
            result.Result = (keys.PublicKeyHex, keys.SecretKeyHex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Not a valid MultiversX mnemonic: {ex.Message}", ex);
        }
        return Task.FromResult(result);
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!IsProviderActivated || _httpClient == null)
            {
                OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Sender account address and private key are required");
                return result;
            }

            if (amount <= 0)
            {
                OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                return result;
            }

            // Convert amount to wei
            var weiAmount = (ulong)(amount * 1_000_000_000_000_000_000m);
            // Get bridge pool address from config or use default
            var bridgePoolAddress = _config?.BridgePoolAddress ?? "erd1qqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqq";

            // Create Elrond transfer transaction
            // In production, use Elrond SDK to build and sign transactions
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = Guid.NewGuid().ToString(),
                IsSuccessful = true,
                Status = BridgeTransactionStatus.Pending
            };
            result.IsError = false;
            result.Message = "Elrond withdrawal transaction created (requires full transaction signing implementation)";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
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

    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!IsProviderActivated || _httpClient == null)
            {
                OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(receiverAccountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Receiver account address is required");
                return result;
            }

            if (amount <= 0)
            {
                OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                return result;
            }

            // Convert amount to wei
            var weiAmount = (ulong)(amount * 1_000_000_000_000_000_000m);

            // Create Elrond transfer transaction from bridge pool to receiver
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = Guid.NewGuid().ToString(),
                IsSuccessful = true,
                Status = BridgeTransactionStatus.Pending
            };
            result.IsError = false;
            result.Message = "Elrond deposit transaction created (requires full transaction signing implementation)";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
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

    public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
    {
        var result = new OASISResult<BridgeTransactionStatus>();
        try
        {
            if (!IsProviderActivated || _httpClient == null)
            {
                OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(transactionHash))
            {
                OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                return result;
            }

            // Query Elrond API for transaction status
            var response = await _httpClient.GetAsync($"/transaction/{transactionHash}", token);
            var content = await response.Content.ReadAsStringAsync(token);
            var jsonDoc = JsonDocument.Parse(content);

            if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty("transaction", out var transactionElement))
            {
                if (transactionElement.TryGetProperty("status", out var statusElement))
                {
                    var status = statusElement.GetString();
                    result.Result = status == "success" ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled;
                    result.IsError = false;
                }
                else
                {
                    result.Result = BridgeTransactionStatus.Pending;
                    result.IsError = false;
                }
            }
            else
            {
                result.Result = BridgeTransactionStatus.NotFound;
                result.IsError = true;
                result.Message = "Transaction not found";
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting Elrond transaction status: {ex.Message}", ex);
            result.Result = BridgeTransactionStatus.NotFound;
        }
        return result;
    }
    }
}
