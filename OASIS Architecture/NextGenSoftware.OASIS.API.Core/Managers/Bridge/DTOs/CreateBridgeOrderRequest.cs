using System;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;

/// <summary>
/// Request to create a cross-chain bridge order (e.g., SOL to XRD swap)
/// </summary>
public class CreateBridgeOrderRequest
{
    /// <summary>
    /// The source token symbol (e.g., "SOL", "XRD")
    /// </summary>
    public string FromToken { get; set; } = string.Empty;
    
    /// <summary>
    /// The destination token symbol
    /// </summary>
    public string ToToken { get; set; } = string.Empty;
    
    /// <summary>
    /// The source blockchain network
    /// </summary>
    public string FromNetwork { get; set; } = string.Empty;
    
    /// <summary>
    /// The destination blockchain network
    /// </summary>
    public string ToNetwork { get; set; } = string.Empty;
    
    /// <summary>
    /// Amount to bridge/swap
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Destination address on the target chain
    /// </summary>
    public string DestinationAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID initiating the order
    /// </summary>
    public Guid UserId { get; set; }
}

