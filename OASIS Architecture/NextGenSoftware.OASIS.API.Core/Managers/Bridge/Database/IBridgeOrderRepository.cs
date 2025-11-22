using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.Database;

/// <summary>
/// Repository interface for bridge order database operations
/// </summary>
public interface IBridgeOrderRepository
{
    /// <summary>
    /// Creates a new bridge order in the database
    /// </summary>
    Task<OASISResult<Guid>> CreateOrderAsync(
        CreateBridgeOrderRequest request,
        CancellationToken token = default);

    /// <summary>
    /// Retrieves a bridge order by its ID
    /// </summary>
    Task<OASISResult<BridgeOrder>> GetOrderByIdAsync(
        Guid orderId,
        CancellationToken token = default);

    /// <summary>
    /// Retrieves all orders for a specific user
    /// </summary>
    Task<OASISResult<IEnumerable<BridgeOrder>>> GetOrdersByUserIdAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken token = default);

    /// <summary>
    /// Updates the status of a bridge order
    /// </summary>
    Task<OASISResult<bool>> UpdateOrderStatusAsync(
        Guid orderId,
        string status,
        string errorMessage = null,
        CancellationToken token = default);

    /// <summary>
    /// Updates withdrawal transaction details
    /// </summary>
    Task<OASISResult<bool>> UpdateWithdrawTransactionAsync(
        Guid orderId,
        string transactionId,
        DateTime? completedAt = null,
        CancellationToken token = default);

    /// <summary>
    /// Updates deposit transaction details
    /// </summary>
    Task<OASISResult<bool>> UpdateDepositTransactionAsync(
        Guid orderId,
        string transactionId,
        DateTime? completedAt = null,
        CancellationToken token = default);

    /// <summary>
    /// Marks an order as completed
    /// </summary>
    Task<OASISResult<bool>> CompleteOrderAsync(
        Guid orderId,
        CancellationToken token = default);

    /// <summary>
    /// Marks an order as failed
    /// </summary>
    Task<OASISResult<bool>> FailOrderAsync(
        Guid orderId,
        string reason,
        CancellationToken token = default);

    /// <summary>
    /// Marks an order as rolled back
    /// </summary>
    Task<OASISResult<bool>> RollbackOrderAsync(
        Guid orderId,
        string reason,
        CancellationToken token = default);

    /// <summary>
    /// Records a balance check for an order
    /// </summary>
    Task<OASISResult<Guid>> RecordBalanceCheckAsync(
        Guid orderId,
        string chainType,
        string chain,
        string address,
        decimal? balanceBefore,
        decimal? balanceAfter,
        bool isSufficient,
        CancellationToken token = default);

    /// <summary>
    /// Gets all active (pending, non-expired) orders
    /// </summary>
    Task<OASISResult<IEnumerable<BridgeOrder>>> GetActiveOrdersAsync(
        CancellationToken token = default);

    /// <summary>
    /// Gets orders that have expired and need cleanup
    /// </summary>
    Task<OASISResult<IEnumerable<BridgeOrder>>> GetExpiredOrdersAsync(
        CancellationToken token = default);

    /// <summary>
    /// Logs a transaction event
    /// </summary>
    Task<OASISResult<Guid>> LogTransactionEventAsync(
        Guid orderId,
        string logLevel,
        string eventType,
        string message,
        string transactionId = null,
        string chain = null,
        CancellationToken token = default);

    /// <summary>
    /// Records an exchange rate in history
    /// </summary>
    Task<OASISResult<Guid>> RecordExchangeRateAsync(
        string fromToken,
        string toToken,
        decimal rate,
        string source,
        int validForMinutes = 5,
        CancellationToken token = default);

    /// <summary>
    /// Gets the most recent exchange rate for a token pair
    /// </summary>
    Task<OASISResult<decimal>> GetLatestExchangeRateAsync(
        string fromToken,
        string toToken,
        CancellationToken token = default);

    /// <summary>
    /// Gets order statistics for a user
    /// </summary>
    Task<OASISResult<UserBridgeOrderStats>> GetUserStatsAsync(
        Guid userId,
        CancellationToken token = default);
}

/// <summary>
/// Represents a bridge order entity from the database
/// </summary>
public class BridgeOrder
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string FromChain { get; set; }
    public string ToChain { get; set; }
    public decimal FromAmount { get; set; }
    public decimal ToAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public string WithdrawTransactionId { get; set; }
    public string DepositTransactionId { get; set; }
    public string Status { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }
    public bool IsRolledBack { get; set; }
    public string ErrorMessage { get; set; }
    public string FailureReason { get; set; }
    public string RollbackReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? WithdrawCompletedAt { get; set; }
    public DateTime? DepositCompletedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? RolledBackAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// User bridge order statistics
/// </summary>
public class UserBridgeOrderStats
{
    public Guid UserId { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int FailedOrders { get; set; }
    public int PendingOrders { get; set; }
    public decimal TotalVolume { get; set; }
    public DateTime? FirstOrderAt { get; set; }
    public DateTime? LastOrderAt { get; set; }
    
    public decimal SuccessRate => TotalOrders > 0 
        ? (decimal)CompletedOrders / TotalOrders * 100 
        : 0;
}





