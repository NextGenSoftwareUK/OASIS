namespace RadixBridge.DTOs.GetConstructionMetadata;

/// <summary>
/// Represents the response for the current epoch and ledger clock from the API.
/// </summary>
public class CurrentEpochResponse
{
    /// <summary>
    /// The current epoch number.
    /// Текущий номер эпохи.
    /// </summary>
    [JsonProperty("current_epoch")]
    public ulong CurrentEpoch { get; set; }

    /// <summary>
    /// The ledger clock object containing timestamp and date-time details.
    /// </summary>
    [JsonProperty("ledger_clock")]
    public LedgerClock LedgerClock { get; set; } = new();
}