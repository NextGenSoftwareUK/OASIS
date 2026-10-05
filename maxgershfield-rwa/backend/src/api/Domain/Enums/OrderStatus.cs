namespace Domain.Enums;

/// <summary>
/// Defines the various statuses an order can have in the system.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// The order is awaiting processing.
    /// </summary>
    Pending,

    /// <summary>
    /// The order has sufficient funds to be processed.
    /// </summary>
    SufficientFunds,

    /// <summary>
    /// The order does not have enough funds to be processed.
    /// </summary>
    InsufficientFunds,

    /// <summary>
    /// The order has expired and can no longer be processed.
    /// </summary>
    Expired,

    /// <summary>
    /// The order has been successfully completed.
    /// </summary>
    Completed,

    /// <summary>
    /// The order has been canceled and will not be processed.
    /// </summary>
    Canceled,

    /// <summary>
    /// The order was not found in the system.
    /// </summary>
    NotFound,

    /// <summary>
    /// The order has insufficient funds to cover the transaction fee.
    /// </summary>
    InsufficientFundsForFee
}