namespace Domain.Entities;

/// <summary>
/// Represents a network token in the system, typically a native token within a blockchain network.
/// This entity stores the symbol, description, and the associated network, along with account balances in that token.
/// </summary>
public sealed class NetworkToken : BaseEntity
{
    /// <summary>
    /// The symbol of the token (e.g., "SOL" for Solana, "ETH" for Ethereum).
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// An optional description of the token, providing additional context or details about the token's usage or purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The unique identifier of the associated network for this token.
    /// This field establishes a relationship with the <see cref="Network"/> entity.
    /// </summary>
    public Guid NetworkId { get; set; }

    /// <summary>
    /// The network to which this token belongs.
    /// This property creates a navigation property to the <see cref="Network"/> entity.
    /// </summary>
    public Network Network { get; set; } = default!;

    /// <summary>
    /// The collection of account balances associated with this network token.
    /// This represents the balance of this token in different accounts within the system.
    /// </summary>
    public ICollection<AccountBalance> AccountBalances { get; } = new List<AccountBalance>();
}