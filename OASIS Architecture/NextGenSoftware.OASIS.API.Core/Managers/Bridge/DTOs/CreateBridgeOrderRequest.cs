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
    /// The source blockchain network (alias for FromNetwork)
    /// </summary>
    public string FromChain 
    { 
        get => FromNetwork; 
        set => FromNetwork = value; 
    }
    
    /// <summary>
    /// The destination blockchain network (alias for ToNetwork)
    /// </summary>
    public string ToChain 
    { 
        get => ToNetwork; 
        set => ToNetwork = value; 
    }
    
    /// <summary>
    /// The source blockchain network
    /// </summary>
    public string FromNetwork { get; set; } = string.Empty;
    
    /// <summary>
    /// The destination blockchain network
    /// </summary>
    public string ToNetwork { get; set; } = string.Empty;
    
    /// <summary>
    /// Amount to bridge/swap (alias for Amount)
    /// </summary>
    public decimal FromAmount 
    { 
        get => Amount; 
        set => Amount = value; 
    }
    
    /// <summary>
    /// Amount to receive (calculated from exchange rate)
    /// </summary>
    public decimal ToAmount { get; set; }
    
    /// <summary>
    /// Amount to bridge/swap
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Exchange rate between from and to tokens
    /// </summary>
    public decimal ExchangeRate { get; set; }
    
    /// <summary>
    /// Source address on the origin chain
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Destination address on the target chain (alias for DestinationAddress)
    /// </summary>
    public string ToAddress 
    { 
        get => DestinationAddress; 
        set => DestinationAddress = value; 
    }
    
    /// <summary>
    /// Destination address on the target chain
    /// </summary>
    public string DestinationAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// How long until the order expires (in minutes)
    /// </summary>
    public int ExpiresInMinutes { get; set; } = 30;
    
    /// <summary>
    /// User ID initiating the order (alias for UserId)
    /// </summary>
    public Guid CreatedBy 
    { 
        get => UserId; 
        set => UserId = value; 
    }
    
    /// <summary>
    /// User ID initiating the order
    /// </summary>
    public Guid UserId { get; set; }
}

