namespace Common.DTOs;

/// <summary>
/// Represents the response returned after initiating a blockchain transaction.
/// This includes the transaction ID, associated data, success status, and any potential error messages.
/// This DTO can be used for various blockchain operations, such as withdrawals, deposits, or other transactions.
/// </summary>
public record TransactionResponse(
    string? TransactionId,
    string? Data,
    bool Success,
    string? ErrorMessage,
    BridgeTransactionStatus Status
);