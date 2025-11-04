using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Database;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge;

/// <summary>
/// Manager for cross-chain bridge operations between different blockchain networks
/// Orchestrates atomic swaps between chains like SOL ↔ XRD
/// </summary>
public class CrossChainBridgeManager : ICrossChainBridgeManager
{
    private const string Sol = "SOL";
    private const string Xrd = "XRD";
    
    private readonly IOASISBridge _solanaBridge;
    private readonly IOASISBridge _radixBridge;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IBridgeOrderRepository _repository;

    public CrossChainBridgeManager(
        IOASISBridge solanaBridge, 
        IOASISBridge radixBridge, 
        IExchangeRateService exchangeRateService = null,
        IBridgeOrderRepository repository = null)
    {
        _solanaBridge = solanaBridge ?? throw new ArgumentNullException(nameof(solanaBridge));
        _radixBridge = radixBridge ?? throw new ArgumentNullException(nameof(radixBridge));
        _exchangeRateService = exchangeRateService ?? new CoinGeckoExchangeRateService();
        _repository = repository; // Optional: can be null for stateless mode
    }

    /// <summary>
    /// Creates a cross-chain bridge order (e.g., SOL to XRD swap)
    /// Performs atomic operations with automatic rollback on failure
    /// </summary>
    public async Task<OASISResult<CreateBridgeOrderResponse>> CreateBridgeOrderAsync(
        CreateBridgeOrderRequest request,
        CancellationToken token = default)
    {
        var result = new OASISResult<CreateBridgeOrderResponse>();
        
        try
        {
            // Validate request
            var validationResult = ValidateRequest(request);
            if (validationResult.IsError)
            {
                result.IsError = true;
                result.Message = validationResult.Message;
                return result;
            }

            // Determine which bridge to use for withdrawal and deposit
            IOASISBridge withdrawBridge = request.FromToken == Xrd ? _radixBridge : _solanaBridge;
            IOASISBridge depositBridge = request.FromToken == Xrd ? _solanaBridge : _radixBridge;

            // Get exchange rate
            var exchangeRateResult = await GetExchangeRateAsync(request.FromToken, request.ToToken, token);
            if (exchangeRateResult.IsError)
            {
                result.IsError = true;
                result.Message = $"Failed to get exchange rate: {exchangeRateResult.Message}";
                return result;
            }

            decimal convertedAmount = exchangeRateResult.Result * request.Amount;

            // Check if this is a valid SOL ↔ XRD swap
            if (request is { FromToken: Xrd, ToToken: Sol } or { FromToken: Sol, ToToken: Xrd })
            {
                // Get source account address (this would come from user's virtual account in a real implementation)
                string sourceAccountAddress = request.UserId.ToString(); // Placeholder
                string sourcePrivateKey = string.Empty; // This would be securely retrieved

                // Check balance
                var balanceResult = await withdrawBridge.GetAccountBalanceAsync(sourceAccountAddress, token);
                if (balanceResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to check balance: {balanceResult.Message}";
                    return result;
                }

                if (balanceResult.Result < request.Amount)
                {
                    result.Result = new CreateBridgeOrderResponse(
                        Guid.NewGuid(),
                        "Insufficient funds for bridge operation"
                    );
                    result.IsError = false;
                    return result;
                }

                // Execute atomic swap with rollback capability
                var swapResult = await ExecuteAtomicSwapAsync(
                    withdrawBridge,
                    depositBridge,
                    request.Amount,
                    convertedAmount,
                    sourceAccountAddress,
                    sourcePrivateKey,
                    request.DestinationAddress,
                    token);

                if (swapResult.IsError)
                {
                    result.IsError = true;
                    result.Message = swapResult.Message;
                    return result;
                }

                var orderId = Guid.NewGuid();
                result.Result = new CreateBridgeOrderResponse(
                    orderId,
                    $"Bridge order created successfully. Transaction: {swapResult.Result.TransactionId}"
                );
                result.IsError = false;
                
                return result;
            }

            result.IsError = true;
            result.Message = "Unsupported token pair for bridge operation";
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error creating bridge order: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Executes an atomic swap between two chains with automatic rollback on failure
    /// </summary>
    private async Task<OASISResult<BridgeTransactionResponse>> ExecuteAtomicSwapAsync(
        IOASISBridge withdrawBridge,
        IOASISBridge depositBridge,
        decimal withdrawAmount,
        decimal depositAmount,
        string sourceAddress,
        string sourcePrivateKey,
        string destinationAddress,
        CancellationToken token)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        BridgeTransactionResponse? withdrawTx = null;
        BridgeTransactionResponse? depositTx = null;

        try
        {
            // Step 1: Withdraw from source chain
            var withdrawResult = await withdrawBridge.WithdrawAsync(
                withdrawAmount,
                sourceAddress,
                sourcePrivateKey);

            if (withdrawResult.IsError)
            {
                result.IsError = true;
                result.Message = $"Withdrawal failed: {withdrawResult.Message}";
                return result;
            }

            withdrawTx = withdrawResult.Result;

            // Step 2: Deposit to destination chain
            var depositResult = await depositBridge.DepositAsync(
                depositAmount,
                destinationAddress);

            if (depositResult.IsError)
            {
                // Rollback: Return funds to source account
                var rollbackResult = await withdrawBridge.DepositAsync(withdrawAmount, sourceAddress);
                
                if (rollbackResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"CRITICAL: Deposit failed AND rollback failed. " +
                                   $"Deposit error: {depositResult.Message}. " +
                                   $"Rollback error: {rollbackResult.Message}";
                    return result;
                }

                result.IsError = true;
                result.Message = $"Deposit failed, funds returned to source account. Error: {depositResult.Message}";
                return result;
            }

            depositTx = depositResult.Result;

            // Step 3: Verify deposit transaction status
            var statusResult = await depositBridge.GetTransactionStatusAsync(depositTx.TransactionId, token);
            
            if (statusResult.IsError || statusResult.Result == BridgeTransactionStatus.Canceled)
            {
                // Attempt rollback
                var rollbackResult = await withdrawBridge.DepositAsync(withdrawAmount, sourceAddress);
                
                result.IsError = true;
                result.Message = rollbackResult.IsError
                    ? $"Transaction verification failed AND rollback failed"
                    : "Transaction verification failed, funds returned to source account";
                return result;
            }

            result.Result = new BridgeTransactionResponse(
                depositTx.TransactionId,
                withdrawTx.TransactionId,
                true,
                "Atomic swap completed successfully",
                BridgeTransactionStatus.Completed
            );
            result.IsError = false;
            
            return result;
        }
        catch (Exception ex)
        {
            // Attempt emergency rollback if withdrawal succeeded
            if (withdrawTx != null && withdrawTx.IsSuccessful)
            {
                try
                {
                    await withdrawBridge.DepositAsync(withdrawAmount, sourceAddress);
                }
                catch
                {
                    // Log critical error - manual intervention may be required
                }
            }

            OASISErrorHandling.HandleError(ref result,
                $"Critical error during atomic swap: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Checks the status and balance of a bridge order
    /// Retrieves order details from database and verifies current balances on both chains
    /// </summary>
    public async Task<OASISResult<BridgeOrderBalanceResponse>> CheckOrderBalanceAsync(
        Guid orderId,
        CancellationToken token = default)
    {
        var result = new OASISResult<BridgeOrderBalanceResponse>();
        
        try
        {
            // Check if repository is available
            if (_repository == null)
            {
                result.IsError = true;
                result.Message = "Bridge order repository not configured - database integration required";
                return result;
            }

            // Get the order from database
            var orderResult = await _repository.GetOrderByIdAsync(orderId, token);
            if (orderResult.IsError || orderResult.Result == null)
            {
                result.IsError = true;
                result.Message = $"Order not found: {orderResult.Message}";
                return result;
            }

            var order = orderResult.Result;

            // Determine which bridge handles which chain
            IOASISBridge fromBridge = order.FromChain.ToUpper() switch
            {
                "SOL" or "SOLANA" => _solanaBridge,
                "XRD" or "RADIX" => _radixBridge,
                _ => throw new NotSupportedException($"Chain {order.FromChain} not supported")
            };

            IOASISBridge toBridge = order.ToChain.ToUpper() switch
            {
                "SOL" or "SOLANA" => _solanaBridge,
                "XRD" or "RADIX" => _radixBridge,
                _ => throw new NotSupportedException($"Chain {order.ToChain} not supported")
            };

            // Check current balances on both chains
            var fromBalanceResult = await fromBridge.GetAccountBalanceAsync(order.FromAddress, token);
            var toBalanceResult = await toBridge.GetAccountBalanceAsync(order.ToAddress, token);

            // Record balance checks in database
            if (!fromBalanceResult.IsError)
            {
                await _repository.RecordBalanceCheckAsync(
                    orderId,
                    "Source",
                    order.FromChain,
                    order.FromAddress,
                    null, // balanceBefore (not tracked in this context)
                    fromBalanceResult.Result,
                    fromBalanceResult.Result >= order.FromAmount,
                    token);
            }

            if (!toBalanceResult.IsError)
            {
                await _repository.RecordBalanceCheckAsync(
                    orderId,
                    "Destination",
                    order.ToChain,
                    order.ToAddress,
                    null, // balanceBefore (not tracked in this context)
                    toBalanceResult.Result,
                    true, // Destination doesn't need sufficient balance
                    token);
            }

            // Check transaction statuses if we have transaction IDs
            BridgeTransactionStatus? withdrawStatus = null;
            if (!string.IsNullOrEmpty(order.WithdrawTransactionId))
            {
                var withdrawStatusResult = await fromBridge.GetTransactionStatusAsync(
                    order.WithdrawTransactionId, token);
                if (!withdrawStatusResult.IsError)
                {
                    withdrawStatus = withdrawStatusResult.Result;
                }
            }

            BridgeTransactionStatus? depositStatus = null;
            if (!string.IsNullOrEmpty(order.DepositTransactionId))
            {
                var depositStatusResult = await toBridge.GetTransactionStatusAsync(
                    order.DepositTransactionId, token);
                if (!depositStatusResult.IsError)
                {
                    depositStatus = depositStatusResult.Result;
                }
            }

            // Build response
            result.Result = new BridgeOrderBalanceResponse
            {
                OrderId = order.OrderId,
                FromChain = order.FromChain,
                ToChain = order.ToChain,
                FromAddress = order.FromAddress,
                ToAddress = order.ToAddress,
                FromAmount = order.FromAmount,
                ToAmount = order.ToAmount,
                ExchangeRate = order.ExchangeRate,
                CurrentFromBalance = fromBalanceResult.IsError ? 0 : fromBalanceResult.Result,
                CurrentToBalance = toBalanceResult.IsError ? 0 : toBalanceResult.Result,
                WithdrawTransactionId = order.WithdrawTransactionId,
                DepositTransactionId = order.DepositTransactionId,
                WithdrawStatus = withdrawStatus?.ToString(),
                DepositStatus = depositStatus?.ToString(),
                OrderStatus = order.Status,
                IsCompleted = order.IsCompleted,
                IsFailed = order.IsFailed,
                IsRolledBack = order.IsRolledBack,
                ErrorMessage = order.ErrorMessage,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                CompletedAt = order.CompletedAt,
                FailedAt = order.FailedAt,
                ExpiresAt = order.ExpiresAt ?? DateTime.UtcNow.AddMinutes(30),
                HasSufficientBalance = fromBalanceResult.IsError ? false : fromBalanceResult.Result >= order.FromAmount
            };

            result.IsError = false;
            result.Message = $"Order balance check completed. Status: {order.Status}";
            
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error checking order balance: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Gets the current exchange rate between two tokens
    /// </summary>
    public async Task<OASISResult<decimal>> GetExchangeRateAsync(
        string fromToken,
        string toToken,
        CancellationToken token = default)
    {
        var result = new OASISResult<decimal>();
        
        try
        {
            // Use the exchange rate service to get real-time rates
            var rateResult = await _exchangeRateService.GetExchangeRateAsync(fromToken, toToken, token);
            
            if (rateResult.IsError)
            {
                result.IsError = true;
                result.Message = rateResult.Message;
                return result;
            }

            result.Result = rateResult.Result;
            result.IsError = false;
            result.Message = rateResult.Message;
            
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error getting exchange rate: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Validates a bridge order request
    /// </summary>
    private OASISResult<bool> ValidateRequest(CreateBridgeOrderRequest request)
    {
        var result = new OASISResult<bool>();

        if (request.Amount <= 0)
        {
            result.IsError = true;
            result.Message = "Amount must be greater than zero";
            return result;
        }

        if (string.IsNullOrWhiteSpace(request.FromToken) || string.IsNullOrWhiteSpace(request.ToToken))
        {
            result.IsError = true;
            result.Message = "From and To tokens must be specified";
            return result;
        }

        if (string.IsNullOrWhiteSpace(request.DestinationAddress))
        {
            result.IsError = true;
            result.Message = "Destination address must be specified";
            return result;
        }

        if (request.FromToken == request.ToToken)
        {
            result.IsError = true;
            result.Message = "Cannot swap between the same token";
            return result;
        }

        // Validate address formats
        if (request.ToToken == Sol && !IsValidSolanaAddress(request.DestinationAddress))
        {
            result.IsError = true;
            result.Message = "Invalid Solana address format";
            return result;
        }

        if (request.ToToken == Xrd && !IsValidRadixAddress(request.DestinationAddress))
        {
            result.IsError = true;
            result.Message = "Invalid Radix address format";
            return result;
        }

        result.Result = true;
        result.IsError = false;
        return result;
    }

    /// <summary>
    /// Validates Solana address format (44 characters, alphanumeric)
    /// </summary>
    private bool IsValidSolanaAddress(string address)
        => !string.IsNullOrWhiteSpace(address) && 
           address.Length == 44 && 
           address.All(char.IsLetterOrDigit);

    /// <summary>
    /// Validates Radix address format (starts with account_tdx_)
    /// </summary>
    private bool IsValidRadixAddress(string address)
        => !string.IsNullOrWhiteSpace(address) && 
           address.StartsWith("account_tdx_");
}

