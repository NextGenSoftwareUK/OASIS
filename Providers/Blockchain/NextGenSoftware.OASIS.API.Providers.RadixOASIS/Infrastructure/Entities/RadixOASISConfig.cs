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
}

