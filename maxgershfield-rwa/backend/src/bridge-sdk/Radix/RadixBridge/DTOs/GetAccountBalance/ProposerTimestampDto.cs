namespace RadixBridge.DTOs.GetAccountBalance;

/// <summary>
/// Represents timestamp details from the proposer.
/// </summary>
public class ProposerTimestampDto
{
    /// <summary>
    /// The timestamp in milliseconds since Unix epoch.
    /// </summary>
    [JsonProperty("unix_timestamp_ms")]
    public long UnixTimestampMs { get; set; }

    /// <summary>
    /// The timestamp as a formatted string.
    /// </summary>
    [JsonProperty("date_time")]
    public string DateTime { get; set; } = string.Empty;
}