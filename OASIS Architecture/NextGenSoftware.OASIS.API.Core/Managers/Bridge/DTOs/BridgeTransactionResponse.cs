using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;

/// <summary>
/// Represents the response from a bridge transaction operation.
/// </summary>
public class BridgeTransactionResponse
{
    /// <summary>
    /// The transaction hash/ID
    /// </summary>
    public string TransactionId { get; set; }
    
    /// <summary>
    /// Optional duplicate transaction ID (for certain blockchain implementations)
    /// </summary>
    public string? DuplicateTransactionId { get; set; }
    
    /// <summary>
    /// Whether the transaction was successful
    /// </summary>
    public bool IsSuccessful { get; set; }
    
    /// <summary>
    /// Error message if transaction failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// The status of the transaction
    /// </summary>
    public BridgeTransactionStatus Status { get; set; }

    public BridgeTransactionResponse()
    {
        TransactionId = string.Empty;
    }

    public BridgeTransactionResponse(string transactionId, string? duplicateTransactionId, bool isSuccessful, 
        string? errorMessage, BridgeTransactionStatus status)
    {
        TransactionId = transactionId;
        DuplicateTransactionId = duplicateTransactionId;
        IsSuccessful = isSuccessful;
        ErrorMessage = errorMessage;
        Status = status;
    }
}

