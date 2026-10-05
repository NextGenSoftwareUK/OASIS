namespace RadixBridge;

/// <summary>
/// Options for configuring a technical account bridge with Radix.
/// This class inherits from <see cref="TechnicalAccountBridgeOptions"/> and adds additional properties specific to Radix.
/// </summary>
public sealed class RadixTechnicalAccountBridgeOptions : TechnicalAccountBridgeOptions
{
    /// <summary>
    /// Gets or sets the network ID for the Radix bridge.
    /// This ID specifies which Radix network (Mainnet, StokeNet, etc.) the bridge should interact with.
    /// </summary>
    /// <remarks>
    /// By default, this is set to 0x02, which corresponds to a specific Radix network.
    /// It can be changed to interact with different Radix networks (e.g., Mainnet or Testnet).
    /// </remarks>
    public required byte NetworkId { get; set; } = 0x02;

    /// <summary>
    /// Gets or sets the account address used for interacting with the Radix network.
    /// This address will be used to send or receive transactions within the Radix blockchain.
    /// </summary>
    /// <remarks>
    /// The <see cref="AccountAddress"/> is crucial for the bridge to interact with the Radix blockchain.
    /// The address provided here will be used for any deposit, withdrawal, or other account-based operations.
    /// </remarks>
    public required string AccountAddress { get; set; }
}