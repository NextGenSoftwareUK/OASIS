namespace RadixBridge.DTOs.Transaction;

/// <summary>
/// Represents the response for a transaction submission to the network.
/// </summary>
public class TransactionSubmitResponse
{
    /// <summary>
    /// Indicates if the transaction is a duplicate.
    /// </summary>
    [JsonProperty("duplicate")]
    public bool Duplicate { get; set; }
}