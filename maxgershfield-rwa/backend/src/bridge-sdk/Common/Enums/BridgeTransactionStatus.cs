namespace Common.Enums;

/// <summary>
/// Enum representing the possible statuses of a blockchain transaction.
/// This is used to track the current state of a transaction in the bridge system.
/// </summary>
public enum BridgeTransactionStatus
{
    Pending,
    SufficientFunds,
    InsufficientFunds,
    InsufficientFundsForFee,
    Expired,
    Completed,
    Canceled,
    NotFound
}