namespace RadixBridge.DTOs.Transaction;

/// <summary>
/// Represents the response for a transaction status request.
/// </summary>
public class TransactionStatusResponse
{
    /// <summary>
    /// The current status of the transaction intent.
    /// </summary>
    [JsonProperty("intent_status")]
    public string IntentStatus { get; set; } = string.Empty;

    /// <summary>
    /// A description of the status.
    /// </summary>
    [JsonProperty("status_description")]
    public string StatusDescription { get; set; } = string.Empty;

    /// <summary>
    /// The epoch from which the transaction is invalid.
    /// </summary>
    [JsonProperty("invalid_from_epoch")]
    public ulong InvalidFromEpoch { get; set; }

    /// <summary>
    /// List of known payloads associated with the transaction intent.
    /// </summary>
    [JsonProperty("known_payloads")]
    public List<Payload> KnownPayloads { get; set; } = new();
}