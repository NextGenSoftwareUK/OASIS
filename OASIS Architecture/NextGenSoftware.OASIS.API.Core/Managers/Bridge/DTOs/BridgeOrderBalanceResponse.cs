using System;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;

/// <summary>
/// Response containing bridge order balance and status information
/// </summary>
public class BridgeOrderBalanceResponse
{
    /// <summary>
    /// The order ID
    /// </summary>
    public Guid OrderId { get; set; }
    
    /// <summary>
    /// The source network
    /// </summary>
    public string FromNetwork { get; set; } = string.Empty;
    
    /// <summary>
    /// Source chain (alias for FromNetwork)
    /// </summary>
    public string FromChain { get => FromNetwork; set => FromNetwork = value; }
    
    /// <summary>
    /// The destination network
    /// </summary>
    public string ToNetwork { get; set; } = string.Empty;
    
    /// <summary>
    /// Destination chain (alias for ToNetwork)
    /// </summary>
    public string ToChain { get => ToNetwork; set => ToNetwork = value; }
    
    /// <summary>
    /// The source token
    /// </summary>
    public string FromToken { get; set; } = string.Empty;
    
    /// <summary>
    /// The destination token
    /// </summary>
    public string ToToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Current balance in the source account
    /// </summary>
    public decimal CurrentBalance { get; set; }
    
    /// <summary>
    /// Current balance in "from" account (alias)
    /// </summary>
    public decimal CurrentFromBalance { get => CurrentBalance; set => CurrentBalance = value; }
    
    /// <summary>
    /// Current balance in "to" account
    /// </summary>
    public decimal CurrentToBalance { get; set; }
    
    /// <summary>
    /// Required amount for the order
    /// </summary>
    public decimal RequiredAmount { get; set; }
    
    /// <summary>
    /// Amount to send from source chain
    /// </summary>
    public decimal FromAmount { get; set; }
    
    /// <summary>
    /// Amount to receive on destination chain
    /// </summary>
    public decimal ToAmount { get; set; }
    
    /// <summary>
    /// Exchange rate applied
    /// </summary>
    public decimal ExchangeRate { get; set; }
    
    /// <summary>
    /// Source address
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Destination address
    /// </summary>
    public string ToAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Order status
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Order status enum
    /// </summary>
    public string OrderStatus { get => Status; set => Status = value; }
    
    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Transaction hash (if completed)
    /// </summary>
    public string? TransactionHash { get; set; }
    
    /// <summary>
    /// Withdraw transaction ID
    /// </summary>
    public string? WithdrawTransactionId { get; set; }
    
    /// <summary>
    /// Deposit transaction ID
    /// </summary>
    public string? DepositTransactionId { get; set; }
    
    /// <summary>
    /// Withdraw status
    /// </summary>
    public string? WithdrawStatus { get; set; }
    
    /// <summary>
    /// Deposit status
    /// </summary>
    public string? DepositStatus { get; set; }
    
    /// <summary>
    /// Whether the order is completed
    /// </summary>
    public bool IsCompleted { get; set; }
    
    /// <summary>
    /// Whether the order failed
    /// </summary>
    public bool IsFailed { get; set; }
    
    /// <summary>
    /// Whether the order was rolled back
    /// </summary>
    public bool IsRolledBack { get; set; }
    
    /// <summary>
    /// When the order was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the order was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// When the order was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// When the order failed
    /// </summary>
    public DateTime? FailedAt { get; set; }
    
    /// <summary>
    /// When the order expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Whether there is sufficient balance
    /// </summary>
    public bool HasSufficientBalance { get; set; }

    public BridgeOrderBalanceResponse()
    {
    }

    public BridgeOrderBalanceResponse(Guid orderId, string fromNetwork, string fromToken, 
        decimal currentBalance, decimal requiredAmount, string status, string message, string? transactionHash)
    {
        OrderId = orderId;
        FromNetwork = fromNetwork;
        FromToken = fromToken;
        CurrentBalance = currentBalance;
        RequiredAmount = requiredAmount;
        Status = status;
        Message = message;
        TransactionHash = transactionHash;
    }
}

