namespace RadixBridge.DTOs.Transaction;

/// <summary>
/// Represents the payload associated with the transaction intent.
/// </summary>
public class Payload
{
    /// <summary>
    /// The hash of the payload.
    /// </summary>
    [JsonProperty("payload_hash")]
    public string PayloadHash { get; set; } = string.Empty;

    /// <summary>
    /// The Bech32m-encoded version of the payload hash.
    /// </summary>
    [JsonProperty("payload_hash_bech32m")]
    public string PayloadHashBech32M { get; set; } = string.Empty;

    /// <summary>
    /// The version of the state associated with this payload.
    /// </summary>
    [JsonProperty("state_version")]
    public int StateVersion { get; set; }

    /// <summary>
    /// The status of the payload.
    /// </summary>
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// An error message if there was an issue with the payload.
    /// </summary>
    [JsonProperty("error_message")]
    public string ErrorMessage { get; set; } = string.Empty;
}