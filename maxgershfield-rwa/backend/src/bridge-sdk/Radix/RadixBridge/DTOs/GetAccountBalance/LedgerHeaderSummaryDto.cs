namespace RadixBridge.DTOs.GetAccountBalance;

/// <summary>
/// Represents the summary information about the ledger header.
/// </summary>
public class LedgerHeaderSummaryDto
{
    /// <summary>
    /// Information about the current epoch and round.
    /// </summary>
    [JsonProperty("epoch_round")]
    public EpochRoundDto EpochRound { get; set; } = new();

    /// <summary>
    /// Hashes related to the ledger's state, transactions, and receipts.
    /// </summary>
    [JsonProperty("ledger_hashes")]
    public LedgerHashesDto LedgerHashes { get; set; } = new();

    /// <summary>
    /// Timestamp details provided by the proposer.
    /// </summary>
    [JsonProperty("proposer_timestamp")]
    public ProposerTimestampDto ProposerTimestamp { get; set; } = new();
}