namespace RadixBridge.DTOs.GetConstructionMetadata;

/// <summary>
/// Represents the "ledger_clock" object in the JSON response.
/// </summary>
public class LedgerClock
{
    /// <summary>
    /// Unix timestamp in milliseconds.
    /// </summary>
    [JsonProperty("unix_timestamp_ms")]
    public long UnixTimestampMs { get; set; }

    /// <summary>
    /// ISO-8601 formatted date and time string.
    /// </summary>
    [JsonProperty("date_time")]
    public string DateTime { get; set; } = string.Empty;
}