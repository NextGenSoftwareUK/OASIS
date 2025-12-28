namespace Common.Options;

/// <summary>
/// Abstract class representing the bridge configuration.
/// Used to store the keys and URI required to connect to the technical account of the bridge.
/// </summary>

public abstract class TechnicalAccountBridgeOptions
{
    /// <summary>
    /// The public key of the technical account (Pool Account).
    /// This key is used for authentication and performing operations via the bridge.
    /// </summary>
    public required string PublicKey { get; init; }

    /// <summary>
    /// The private key of the technical account (Pool Account).
    /// Used to sign transactions and access funds through the bridge.
    /// </summary>
    public required string PrivateKey { get; init; }

    /// <summary>
    /// The URI for the bridge's RPC API.
    /// This URI is used to communicate with the bridge server and perform operations via its API.
    /// </summary>
    public required string HostUri { get; init; }
}
