namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

/// <summary>
/// Represents the status of a cross-chain bridge order
/// </summary>
public enum BridgeOrderStatus
{
    /// <summary>
    /// Order completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Order is pending
    /// </summary>
    Pending,
    
    /// <summary>
    /// Order was canceled
    /// </summary>
    Canceled,
    
    /// <summary>
    /// Order expired
    /// </summary>
    Expired,
    
    /// <summary>
    /// Insufficient funds to complete order
    /// </summary>
    InsufficientFunds,
    
    /// <summary>
    /// Sufficient funds available
    /// </summary>
    SufficientFunds,
    
    /// <summary>
    /// Insufficient funds for transaction fee
    /// </summary>
    InsufficientFundsForFee,
    
    /// <summary>
    /// Order not found
    /// </summary>
    NotFound
}

