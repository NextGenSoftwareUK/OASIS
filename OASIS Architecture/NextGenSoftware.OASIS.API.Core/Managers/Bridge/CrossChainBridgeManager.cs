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
private const string Eth = "ETH";
private const string Xrd = "XRD";
private const string Zec = "ZEC";
private const string Aztec = "AZTEC";
private const string Miden = "MIDEN";
private const string Starknet = "STARKNET";
    
    private readonly Dictionary<string, IOASISBridge> _bridgeMap;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IBridgeOrderRepository _repository;
    private readonly ViewingKeyAuditService _viewingKeyAuditService;
    private readonly ProofVerificationService _proofVerificationService;
    private readonly MpcExecutionService _mpcExecutionService;
    private IOASISBridge ResolveBridge(string tokenSymbol)
    {
        if (string.IsNullOrWhiteSpace(tokenSymbol))
            return null;

        var key = tokenSymbol.ToUpperInvariant();
        return _bridgeMap.TryGetValue(key, out var bridge) ? bridge : null;
    }

    private static bool IsPrivateBridgePair(string fromToken, string toToken)
    {
        if (string.IsNullOrWhiteSpace(fromToken) || string.IsNullOrWhiteSpace(toToken))
            return false;

        var from = fromToken.ToUpperInvariant();
        var to = toToken.ToUpperInvariant();

        // Zcash ↔ Aztec bridge
        if ((from == Zec && to == Aztec) || (from == Aztec && to == Zec))
            return true;

        // Zcash ↔ Miden bridge (Track 4)
        if ((from == Zec && to == Miden) || (from == Miden && to == Zec))
            return true;

        // Zcash ↔ Starknet bridge (Starknet Track)
        if ((from == Zec && to == Starknet) || (from == Starknet && to == Zec))
            return true;

        return false;
    }

    public CrossChainBridgeManager(
        Dictionary<string, IOASISBridge> bridges,
        IExchangeRateService exchangeRateService = null,
        IBridgeOrderRepository repository = null,
        ViewingKeyAuditService viewingKeyAuditService = null,
        ProofVerificationService proofVerificationService = null,
        MpcExecutionService mpcExecutionService = null)
    {
        if (bridges == null || bridges.Count == 0)
        {
            throw new ArgumentException("At least one bridge implementation is required.", nameof(bridges));
        }

        _bridgeMap = bridges.ToDictionary(
            kv => kv.Key.ToUpperInvariant(),
            kv => kv.Value);

        _exchangeRateService = exchangeRateService ?? new CoinGeckoExchangeRateService();
        _repository = repository;
        _viewingKeyAuditService = viewingKeyAuditService ?? new ViewingKeyAuditService();
        _proofVerificationService = proofVerificationService ?? new ProofVerificationService();
        _mpcExecutionService = mpcExecutionService ?? new MpcExecutionService();
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

            var fromToken = request.FromToken?.ToUpperInvariant();
            var toToken = request.ToToken?.ToUpperInvariant();

            if (IsPrivateBridgePair(fromToken, toToken))
            {
                return await HandlePrivateBridgeOrderAsync(request, token);
            }

            var withdrawBridge = ResolveBridge(fromToken);
            var depositBridge = ResolveBridge(toToken);

            if (withdrawBridge == null || depositBridge == null)
            {
                result.IsError = true;
                result.Message = $"Bridge implementation missing for {request.FromToken} or {request.ToToken}";
                return result;
            }

            // Get exchange rate
            var exchangeRateResult = await GetExchangeRateAsync(request.FromToken, request.ToToken, token);
            if (exchangeRateResult.IsError)
            {
                result.IsError = true;
                result.Message = $"Failed to get exchange rate: {exchangeRateResult.Message}";
                return result;
            }

            decimal convertedAmount = exchangeRateResult.Result * request.Amount;

            // Check if this is a supported swap pair (SOL ↔ XRD, SOL ↔ ETH, ETH ↔ SOL)
            var isSupportedPair = (fromToken == Sol && toToken == Xrd) ||
                                  (fromToken == Xrd && toToken == Sol) ||
                                  (fromToken == Sol && toToken == Eth) ||
                                  (fromToken == Eth && toToken == Sol);
            
            if (isSupportedPair)
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
                
                await ApplyViewingKeyAuditAsync(request, swapResult.Result?.TransactionId);
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

    private async Task<OASISResult<CreateBridgeOrderResponse>> HandlePrivateBridgeOrderAsync(
        CreateBridgeOrderRequest request,
        CancellationToken token)
    {
        var result = new OASISResult<CreateBridgeOrderResponse>();
        var withdrawBridge = ResolveBridge(request.FromToken);
        var depositBridge = ResolveBridge(request.ToToken);

        if (withdrawBridge == null || depositBridge == null)
        {
            result.IsError = true;
            result.Message = $"Private bridge not configured for {request.FromToken}->{request.ToToken}";
            return result;
        }

        if (request.RequireProofVerification)
        {
            var proofCheck = await _proofVerificationService.VerifyProofAsync(request.ProofPayload, request.ProofType, token);
            if (proofCheck.IsError || !proofCheck.Result)
            {
                result.IsError = true;
                result.Message = $"Proof verification failed: {proofCheck.Message}";
                return result;
            }
        }

        if (request.EnableMpc)
        {
            var mpcResult = await _mpcExecutionService.StartMpcSessionAsync(request.FromToken, request.ToToken, request.Amount, token);
            if (mpcResult.IsError)
            {
                result.IsError = true;
                result.Message = $"MPC orchestration failed: {mpcResult.Message}";
                return result;
            }

            request.MpcSessionId = mpcResult.Result;
        }

        var withdrawResult = await withdrawBridge.WithdrawAsync(request.Amount, request.FromAddress, string.Empty);
        if (withdrawResult.IsError)
        {
            result.IsError = true;
            result.Message = $"{request.FromToken} withdrawal failed: {withdrawResult.Message}";
            return result;
        }

        var depositResult = await depositBridge.DepositAsync(request.Amount, request.DestinationAddress);
        if (depositResult.IsError)
        {
            result.IsError = true;
            result.Message = $"{request.ToToken} deposit failed: {depositResult.Message}";
            return result;
        }

        var proofVerification = await _proofVerificationService.VerifyBridgeCommitmentAsync(
            withdrawResult.Result?.TransactionId,
            depositResult.Result?.TransactionId,
            request.ProofPayload,
            token);

        if (proofVerification.IsError || !proofVerification.Result)
        {
            result.IsError = true;
            result.Message = $"Cross-chain proof verification failed: {proofVerification.Message}";
            return result;
        }

        await ApplyViewingKeyAuditAsync(request, withdrawResult.Result?.TransactionId);

        result.Result = new CreateBridgeOrderResponse(Guid.NewGuid(),
            $"Private bridge order completed. Deposit Tx: {depositResult.Result?.TransactionId}");
        result.IsError = false;
        return result;
    }

    private async Task ApplyViewingKeyAuditAsync(CreateBridgeOrderRequest request, string transactionId)
    {
        if (_viewingKeyAuditService == null || !request.EnableViewingKeyAudit ||
            string.IsNullOrWhiteSpace(request.ViewingKey))
        {
            return;
        }

        await _viewingKeyAuditService.RecordViewingKeyAsync(new ViewingKeyAuditEntry
        {
            TransactionId = transactionId,
            ViewingKey = request.ViewingKey,
            SourceChain = request.FromChain,
            DestinationChain = request.ToChain,
            DestinationAddress = request.DestinationAddress,
            UserId = request.UserId,
            Timestamp = DateTime.UtcNow,
            Notes = request.PrivacyMetadata
        });
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
            var fromBridge = ResolveBridge(order.FromChain);
            var toBridge = ResolveBridge(order.ToChain);

            if (fromBridge == null || toBridge == null)
            {
                result.IsError = true;
                result.Message = $"Bridge implementation not configured for {order.FromChain}->{order.ToChain}";
                return result;
            }

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
            var upperFrom = (fromToken ?? string.Empty).ToUpperInvariant();
            var upperTo = (toToken ?? string.Empty).ToUpperInvariant();

            // Use the exchange rate service to get real-time rates
            var rateResult = await _exchangeRateService.GetExchangeRateAsync(upperFrom, upperTo, token);
            
            if (!rateResult.IsError)
            {
                result.Result = rateResult.Result;
                result.IsError = false;
                result.Message = rateResult.Message;
                return result;
            }

            if (IsSolBridgePair(upperFrom, upperTo))
            {
                var bridgeResult = await ComputeSolBridgeRateAsync(upperFrom, upperTo, token);
                if (!bridgeResult.IsError)
                {
                    return bridgeResult;
                }

                result.IsError = true;
                result.Message = $"{rateResult.Message}; fallback: {bridgeResult.Message}";
                return result;
            }

            result.IsError = true;
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

    private static bool IsSolBridgePair(string fromToken, string toToken)
    {
        if (string.IsNullOrWhiteSpace(fromToken) || string.IsNullOrWhiteSpace(toToken))
            return false;

        if ((fromToken == Zec && toToken == Aztec) || (fromToken == Aztec && toToken == Zec))
            return true;

        return false;
    }

    private async Task<OASISResult<decimal>> ComputeSolBridgeRateAsync(
        string fromToken,
        string toToken,
        CancellationToken token)
    {
        var result = new OASISResult<decimal>();
        try
        {
            var fromToSol = await _exchangeRateService.GetExchangeRateAsync(fromToken, Sol, token);
            var toToSol = await _exchangeRateService.GetExchangeRateAsync(toToken, Sol, token);

            if (fromToSol.IsError)
            {
                result.IsError = true;
                result.Message = $"Failed to derive {fromToken}/SOL rate: {fromToSol.Message}";
                return result;
            }

            if (toToSol.IsError)
            {
                result.IsError = true;
                result.Message = $"Failed to derive {toToken}/SOL rate: {toToSol.Message}";
                return result;
            }

            if (toToSol.Result == 0)
            {
                result.IsError = true;
                result.Message = $"{toToken}/SOL rate returned zero";
                return result;
            }

            result.Result = fromToSol.Result / toToSol.Result;
            result.IsError = false;
            result.Message = $"Derived {fromToken}/{toToken} via SOL";
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error deriving exchange rate via SOL: {ex.Message}", ex);
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

    public async Task<OASISResult<bool>> RecordViewingKeyAsync(ViewingKeyAuditEntry entry, CancellationToken token = default)
    {
        var result = new OASISResult<bool>();
        try
        {
            await _viewingKeyAuditService.RecordViewingKeyAsync(entry, token);
            result.Result = true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to record viewing key: {ex.Message}", ex);
        }

        return result;
    }

    public async Task<OASISResult<bool>> VerifyProofAsync(string proofPayload, string proofType, CancellationToken token = default)
    {
        var result = new OASISResult<bool>();
        try
        {
            var verification = await _proofVerificationService.VerifyProofAsync(proofPayload, proofType, token);
            if (verification.IsError || !verification.Result)
            {
                result.IsError = true;
                result.Message = verification.Message;
                return result;
            }

            result.Result = true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to verify proof: {ex.Message}", ex);
        }

        return result;
    }
}

