namespace Domain.Entities;

/// <summary>
/// Represents a wallet that is linked to a user account on a specific blockchain network.
/// The wallet is identified by its public key and is associated with a user and a network.
/// </summary>
public sealed class WalletLinkedAccount : BaseEntity
{
    /// <summary>
    /// The ID of the user who owns the linked wallet.
    /// This establishes the relationship between the wallet and the user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user associated with the linked wallet.
    /// This represents the full user entity and provides a navigation property to the associated user.
    /// </summary>
    public User User { get; set; } = default!;

    /// <summary>
    /// The ID of the blockchain network to which the wallet is linked.
    /// This defines which blockchain network the wallet is associated with (e.g., Ethereum, Solana).
    /// </summary>
    public Guid NetworkId { get; set; }

    /// <summary>
    /// The network associated with the linked wallet.
    /// This provides a navigation property to the network entity, detailing the specifics of the blockchain network.
    /// </summary>
    public Network Network { get; set; } = default!;

    /// <summary>
    /// The public key of the linked wallet.
    /// This key is used to identify the wallet on the blockchain and is essential for transactions.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// The date and time when the wallet was linked to the user's account.
    /// This marks the moment the wallet became associated with the user.
    /// </summary>
    public DateTimeOffset LinkedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<RwaTokenOwnershipTransfer> RwaTokenOwnershipTransferBuyers { get; set; } = [];
    public ICollection<RwaToken> RwaTokens { get; set; } = [];
}