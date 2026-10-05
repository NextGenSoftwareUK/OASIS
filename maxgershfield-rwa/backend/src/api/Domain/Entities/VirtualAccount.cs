namespace Domain.Entities;

/// <summary>
/// Represents a virtual account associated with a user in a blockchain network.
/// This account includes details like the private and public keys, address, and seed phrase for key generation.
/// </summary>
public sealed class VirtualAccount : BaseEntity
{
    /// <summary>
    /// The private key associated with the virtual account.
    /// This key is used for signing transactions and accessing the account's funds.
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// The public key associated with the virtual account.
    /// This key is used for identifying the account on the network and receiving funds.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// The address of the virtual account on the blockchain.
    /// This address is used to receive and send funds to the account.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// The seed phrase used for generating the private and public keys.
    /// This phrase is crucial for recovering the account and generating the keys.
    /// </summary>
    public string SeedPhrase { get; set; } = string.Empty;

    /// <summary>
    /// The type of network the virtual account is associated with (e.g., Solana, Ethereum).
    /// This defines which blockchain network the account belongs to.
    /// </summary>
    public NetworkType NetworkType { get; set; }

    /// <summary>
    /// The ID of the user associated with this virtual account.
    /// This establishes a relationship between the account and the user who owns it.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user associated with the virtual account.
    /// This represents the full user entity and provides a navigation property to the associated user.
    /// </summary>
    public User User { get; set; } = default!;

    /// <summary>
    /// The ID of the network associated with the virtual account.
    /// This links the account to a specific blockchain network.
    /// </summary>
    public Guid NetworkId { get; set; }

    /// <summary>
    /// The network associated with the virtual account.
    /// This provides a navigation property to the network entity, detailing the specifics of the blockchain network.
    /// </summary>
    public Network Network { get; set; } = default!;

    /// <summary>
    /// A collection of account balances for the virtual account.
    /// This represents the amount of each token held by the account on the network.
    /// </summary>
    public ICollection<AccountBalance> Balances { get; } = [];

    public ICollection<RwaToken> RwaTokens { get; set; } = [];

    public ICollection<RwaTokenPriceHistory> RwaTokenPriceHistories { get; set; } = [];
    public ICollection<RwaTokenOwnershipTransfer> RwaTokenOwnershipTransferSellers { get; set; } = [];
}