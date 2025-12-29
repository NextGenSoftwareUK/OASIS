namespace RadixBridge.DTOs.GetAccountBalance;

/// <summary>
/// Represents information about the current epoch and round.
/// </summary>
public class EpochRoundDto
{
    /// <summary>
    /// The current epoch number.
    /// </summary>
    [JsonProperty("epoch")]
    public long Epoch { get; set; }

    /// <summary>
    /// The current round number within the epoch.
    /// </summary>
    [JsonProperty("round")]
    public long Round { get; set; }
}