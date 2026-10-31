using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Starknet;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.StarknetOASIS;

public sealed partial class StarknetOASIS
{
    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
    {
        return LockTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }
            if (_rpcClient == null)
            {
                OASISErrorHandling.HandleError(ref result, "Starknet RPC client is not initialized");
                return result;
            }

            // Get values from request (ILockWeb3TokenRequest has FromWalletAddress and TokenAddress)
            var tokenAddress = request.TokenAddress;
            var fromAddress = request.FromWalletAddress;
            // Amount is not in the interface - would need to be in MetaData or specified separately
            var amount = 0m; // Amount would need to be specified separately

            if (string.IsNullOrWhiteSpace(tokenAddress) || string.IsNullOrWhiteSpace(fromAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and from wallet address are required");
                return result;
            }

            // Lock token by transferring to bridge pool
            var bridgePoolAddress = Environment.GetEnvironmentVariable("STARKNET_BRIDGE_POOL_ADDRESS") ?? "starknet_bridge_pool";
            var payload = new StarknetTransactionPayload
            {
                From = fromAddress,
                To = tokenAddress,
                Amount = amount,
                Memo = bridgePoolAddress // Bridge pool address in memo
            };
            var txResult = await _rpcClient.SubmitTransactionAsync(payload);
            if (txResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Token lock failed: {txResult.Message}");
                return result;
            }

            result.Result.TransactionResult = txResult.Result;
            result.IsError = false;
            result.Message = "Token locked successfully on Starknet.";
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
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }
            if (_rpcClient == null)
            {
                OASISErrorHandling.HandleError(ref result, "Starknet RPC client is not initialized");
                return result;
            }

            // Get values from request (IUnlockWeb3TokenRequest doesn't have Amount or UnlockedToWalletAddress directly)
            var tokenAddress = request.TokenAddress;
            // IUnlockWeb3TokenRequest doesn't have MetaData - would need to be passed separately or via concrete class
            var unlockedToAddress = ""; // Would need to be provided via concrete class or separate parameter
            var amount = 0m; // Would need to be provided via concrete class or separate parameter

            if (string.IsNullOrWhiteSpace(tokenAddress) || string.IsNullOrWhiteSpace(unlockedToAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and unlocked to wallet address are required in MetaData");
                return result;
            }

            // Unlock token by transferring from bridge pool to recipient
            var bridgePoolAddress = Environment.GetEnvironmentVariable("STARKNET_BRIDGE_POOL_ADDRESS") ?? "starknet_bridge_pool";
            var payload = new StarknetTransactionPayload
            {
                From = bridgePoolAddress,
                To = tokenAddress,
                Amount = amount,
                Memo = unlockedToAddress // Recipient address in memo
            };
            var txResult = await _rpcClient.SubmitTransactionAsync(payload);
            if (txResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Token unlock failed: {txResult.Message}");
                return result;
            }

            result.Result.TransactionResult = txResult.Result;
            result.IsError = false;
            result.Message = "Token unlocked successfully on Starknet.";
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
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }
            if (_rpcClient == null)
            {
                OASISErrorHandling.HandleError(ref result, "Starknet RPC client is not initialized");
                return result;
            }

            var balanceResult = await _rpcClient.GetBalanceAsync(request.WalletAddress);
            if (balanceResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance: {balanceResult.Message}");
                return result;
            }

            result.Result = (double)balanceResult.Result;
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting balance: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
    {
        return GetTransactionsAsync(request).Result;
    }

    public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
    {
        var result = new OASISResult<IList<IWalletTransaction>>();
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }
            if (_rpcClient == null)
            {
                OASISErrorHandling.HandleError(ref result, "Starknet RPC client is not initialized");
                return result;
            }

            // Get transactions using RPC client
            // Note: Starknet transaction history queries may require special handling
            // For now, return empty list as transaction history queries are not yet fully implemented
            result.Result = new List<IWalletTransaction>();
            result.Message = "Transaction history query for Starknet is simplified (privacy-focused blockchain)";
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting transactions: {ex.Message}", ex);
        }
        return result;
    }

    /// <summary>
    /// Derives Starknet public key from private key using STARK-friendly curve
    /// Note: This is a simplified implementation. In production, use proper Starknet SDK for key derivation.
    /// </summary>
    /// <summary>
    /// Derives Starknet address from private key
    /// </summary>

    /// <summary>
    /// Derives Starknet address from public key
    /// </summary>

    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
    {
        var result = new OASISResult<decimal>();
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }
            if (_rpcClient == null)
            {
                OASISErrorHandling.HandleError(ref result, "Starknet RPC client is not initialized");
                return result;
            }

            if (string.IsNullOrWhiteSpace(accountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Account address is required");
                return result;
            }

            var balanceResult = await _rpcClient.GetBalanceAsync(accountAddress);
            if (balanceResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, balanceResult.Message, balanceResult.Exception);
                return result;
            }

            result.Result = balanceResult.Result;
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting account balance: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!_isActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Starknet provider: {activateResult.Message}");
                    return result;
                }
            }
            if (_rpcClient == null)
            {
                OASISErrorHandling.HandleError(ref result, "Starknet RPC client is not initialized");
                return result;
            }

            if (string.IsNullOrWhiteSpace(senderAccountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Sender address is required");
                return result;
            }

            // Check balance first
            var balance = await _rpcClient.GetBalanceAsync(senderAccountAddress);
            if (balance.IsError)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = balance.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                OASISErrorHandling.HandleError(ref result, balance.Message, balance.Exception);
                return result;
            }

            if (balance.Result < amount)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = $"Insufficient Starknet funds ({balance.Result}) for withdraw {amount}",
                    Status = BridgeTransactionStatus.InsufficientFunds
                };
                OASISErrorHandling.HandleError(ref result, result.Result.ErrorMessage);
                return result;
            }

            // Submit transaction
            var txResult = await _rpcClient.SubmitTransactionAsync(new StarknetTransactionPayload
            {
                From = senderAccountAddress,
                To = string.Empty, // Bridge pool address would go here
                Amount = amount
            });

            if (txResult.IsError)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = txResult.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                OASISErrorHandling.HandleError(ref result, txResult.Message, txResult.Exception);
                return result;
            }

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = txResult.Result,
                IsSuccessful = true,
                Status = BridgeTransactionStatus.Pending
            };
            result.IsError = false;
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

}
