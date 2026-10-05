namespace Domain.Entities;

/// <summary>
/// Represents a blockchain network in the system, such as Solana or Ethereum.
/// This entity stores the name, description, and type of the network,
/// along with collections of associated virtual accounts, network tokens, and wallet-linked accounts.
/// </summary>
public sealed class Network : BaseEntity
{
    /// <summary>
    /// The name of the network (e.g., "Solana", "Ethereum").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// An optional description of the network, providing more context or details.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The type of the network, which can define its properties and behavior.
    /// This could indicate whether the network is public, private, or any other type.
    /// </summary>
    public NetworkType NetworkType { get; set; }

    /// <summary>
    /// The collection of virtual accounts associated with this network.
    /// This represents all the virtual accounts that exist within this particular blockchain network.
    /// </summary>
    public ICollection<VirtualAccount> VirtualAccounts { get; } = new List<VirtualAccount>();

    /// <summary>
    /// The collection of network tokens associated with this network.
    /// These tokens are native to the network and can be used for various purposes (e.g., transaction fees).
    /// </summary>
    public ICollection<NetworkToken> NetworkTokens { get; } = new List<NetworkToken>();

    /// <summary>
    /// The collection of wallet-linked accounts associated with this network.
    /// These accounts are linked to the network for transactions or other network-related operations.
    /// </summary>
    public ICollection<WalletLinkedAccount> WalletLinkedAccounts { get; } = new List<WalletLinkedAccount>();
}