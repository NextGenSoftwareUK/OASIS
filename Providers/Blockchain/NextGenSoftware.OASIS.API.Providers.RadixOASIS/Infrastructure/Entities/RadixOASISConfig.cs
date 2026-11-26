namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;

/// <summary>
/// Configuration for RadixOASIS provider
/// </summary>
public class RadixOASISConfig
{
    /// <summary>
    /// Radix network URI (e.g., https://stokenet.radixdlt.com)
    /// </summary>
    public string HostUri { get; set; } = string.Empty;
    
    /// <summary>
    /// Network ID (1 = MainNet, 2 = StokNet)
    /// </summary>
    public byte NetworkId { get; set; }
    
    /// <summary>
    /// Technical account address for bridge operations
    /// </summary>
    public string AccountAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Private key (hex encoded) for the technical account
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Component used by the token and lock operations when a request names no token address. Core storage does not use it.
    /// </summary>
    public string? OasisBlueprintAddress { get; set; }

    /// <summary>Node Core API root used for storage transactions, for example <c>http://127.0.0.1:3333/core/</c>.</summary>
    public string? CoreApiUri { get; set; }

    /// <summary>Node Engine State API root used for storage reads, for example <c>http://127.0.0.1:3336/engine-state/</c>.</summary>
    public string? EngineStateApiUri { get; set; }

    /// <summary>Logical network name the node expects (mainnet, stokenet, localnet).</summary>
    public string? NetworkName { get; set; }

    /// <summary>
    /// <c>OasisKeyValueStore</c> component (contracts/src/oasis_key_value_store.rs) that holds this provider's records.
    /// It must be owned by the signature badge of <see cref="PrivateKey"/>, whose account pays the fees.
    /// </summary>
    public string? KeyValueStoreComponentAddress { get; set; }

    /// <summary>
    /// 32-byte record encryption key (hex). Ledger state is public, so records are compressed and encrypted before
    /// they are written. Required for storage.
    /// </summary>
    public string? StorageEncryptionKey { get; set; }
}
