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
    public string FromNetwork { get; set; }
    
    /// <summary>
    /// The source token
    /// </summary>
    public string FromToken { get; set; }
    
    /// <summary>
    /// Current balance in the source account
    /// </summary>
    public decimal CurrentBalance { get; set; }
    
    /// <summary>
    /// Required amount for the order
    /// </summary>
    public decimal RequiredAmount { get; set; }
    
    /// <summary>
    /// Order status
    /// </summary>
    public string Status { get; set; }
    
    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; set; }
    
    /// <summary>
    /// Transaction hash (if completed)
    /// </summary>
    public string? TransactionHash { get; set; }

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

