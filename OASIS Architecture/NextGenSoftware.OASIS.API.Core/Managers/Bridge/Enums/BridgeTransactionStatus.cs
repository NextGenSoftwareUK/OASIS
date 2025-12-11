namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

/// <summary>
/// Represents the status of a bridge transaction.
/// </summary>
public enum BridgeTransactionStatus
{
    /// <summary>
    /// Transaction completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Transaction is pending confirmation
    /// </summary>
    Pending,
    
    /// <summary>
    /// Transaction was canceled
    /// </summary>
    Canceled,
    
    /// <summary>
    /// Transaction expired before completion
    /// </summary>
    Expired,
    
    /// <summary>
    /// Insufficient funds to complete transaction
    /// </summary>
    InsufficientFunds,
    
    /// <summary>
    /// Sufficient funds available for transaction
    /// </summary>
    SufficientFunds,
    
    /// <summary>
    /// Insufficient funds to cover transaction fee
    /// </summary>
    InsufficientFundsForFee,
    
    /// <summary>
    /// Transaction not found
    /// </summary>
    NotFound
}

