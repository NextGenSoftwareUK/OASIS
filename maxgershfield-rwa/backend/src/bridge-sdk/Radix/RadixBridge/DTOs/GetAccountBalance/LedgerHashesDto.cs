namespace RadixBridge.DTOs.GetAccountBalance;

/// <summary>
/// Represents the ledger hashes for state, transaction, and receipt trees.
/// </summary>
public class LedgerHashesDto
{
    /// <summary>
    /// The hash of the state tree.
    /// </summary>
    [JsonProperty("state_tree_hash")]
    public string StateTreeHash { get; set; } = string.Empty;

    /// <summary>
    /// The hash of the transaction tree.
    /// </summary>
    [JsonProperty("transaction_tree_hash")]
    public string TransactionTreeHash { get; set; } = string.Empty;

    /// <summary>
    /// The hash of the receipt tree.
    /// </summary>
    [JsonProperty("receipt_tree_hash")]
    public string ReceiptTreeHash { get; set; } = string.Empty;
}