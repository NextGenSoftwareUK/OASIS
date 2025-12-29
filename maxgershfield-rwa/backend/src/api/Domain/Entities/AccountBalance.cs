namespace Domain.Entities;

/// <summary>
/// Represents the balance information for a specific virtual account in the system.
/// This entity links the virtual account to its associated network token and stores the balance.
/// </summary>
public sealed class AccountBalance : BaseEntity
{
    /// <summary>
    /// The current balance of the account, stored as a decimal value.
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// The unique identifier of the associated virtual account.
    /// This field establishes a relationship between the balance and the virtual account.
    /// </summary>
    public Guid VirtualAccountId { get; set; }

    /// <summary>
    /// The associated virtual account for this balance.
    /// This property creates a navigation property to the <see cref="VirtualAccount"/> entity.
    /// </summary>
    public VirtualAccount VirtualAccount { get; set; } = default!;

    /// <summary>
    /// The unique identifier of the associated network token.
    /// This field links the balance to a specific token in a particular network.
    /// </summary>
    public Guid NetworkTokenId { get; set; }

    /// <summary>
    /// The associated network token for this balance.
    /// This property creates a navigation property to the <see cref="NetworkToken"/> entity.
    /// </summary>
    public NetworkToken NetworkToken { get; set; } = default!;
}