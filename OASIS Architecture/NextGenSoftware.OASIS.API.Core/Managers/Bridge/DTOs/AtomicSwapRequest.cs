using System;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;

/// <summary>
/// Request to create a Starknet ↔ Zcash atomic swap
/// </summary>
public class AtomicSwapRequest
{
    /// <summary>
    /// Source chain (Zcash or Starknet)
    /// </summary>
    public string FromChain { get; set; } = string.Empty;
    
    /// <summary>
    /// Destination chain (Zcash or Starknet)
    /// </summary>
    public string ToChain { get; set; } = string.Empty;
    
    /// <summary>
    /// Amount to swap
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Source address on origin chain
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Destination address on target chain
    /// </summary>
    public string ToAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Use partial notes for Zcash (optional)
    /// </summary>
    public bool UsePartialNotes { get; set; }
    
    /// <summary>
    /// Generate viewing key for auditability (optional)
    /// </summary>
    public bool GenerateViewingKey { get; set; }
    
    /// <summary>
    /// User ID initiating the swap
    /// </summary>
    public Guid UserId { get; set; }
}

