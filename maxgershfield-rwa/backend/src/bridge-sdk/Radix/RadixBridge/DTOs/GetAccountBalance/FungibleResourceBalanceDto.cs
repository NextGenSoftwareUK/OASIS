namespace RadixBridge.DTOs.GetAccountBalance;

/// <summary>
/// Represents the fungible resource balance for an account.
/// </summary>
public class FungibleResourceBalanceDto
{
    /// <summary>
    /// The address of the fungible resource.
    /// </summary>
    [JsonProperty("fungible_resource_address")]
    public string FungibleResourceAddress { get; set; } = string.Empty;

    /// <summary>
    /// The amount of the fungible resource.
    /// </summary>
    [JsonProperty("amount")]
    public string Amount { get; set; } = string.Empty;
}