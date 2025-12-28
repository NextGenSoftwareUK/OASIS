namespace RadixBridge.DTOs.GetAccountBalance;

/// <summary>
/// Represents the response for a single fungible resource balance of an account.
/// </summary>
public class AccountFungibleResourceBalanceDto
{
    /// <summary>
    /// The version of the ledger state.
    /// </summary>
    [JsonProperty("state_version")]
    public long StateVersion { get; set; }

    /// <summary>
    /// Summary information about the ledger header.
    /// </summary>
    [JsonProperty("ledger_header_summary")]
    public LedgerHeaderSummaryDto LedgerHeaderSummary { get; set; } = new();

    /// <summary>
    /// The account's address in Bech32m format.
    /// </summary>
    [JsonProperty("account_address")]
    public string AccountAddress { get; set; } = string.Empty;

    /// <summary>
    /// The fungible resource balance, including resource address and amount.
    /// </summary>
    [JsonProperty("fungible_resource_balance")]
    public FungibleResourceBalanceDto FungibleResourceBalance { get; set; } = new();
}