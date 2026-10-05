using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;

/// <summary>
/// Response containing atomic swap status information for UI display
/// Matches the BridgeStatus interface expected by zypherpunk-wallet-ui
/// </summary>
public class AtomicSwapStatusResponse
{
    /// <summary>
    /// Unique identifier for this swap/bridge operation
    /// </summary>
    public string BridgeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Source chain
    /// </summary>
    public string FromChain { get; set; } = string.Empty;
    
    /// <summary>
    /// Destination chain
    /// </summary>
    public string ToChain { get; set; } = string.Empty;
    
    /// <summary>
    /// Amount being swapped
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Current status: pending, locked, minting, completed, failed
    /// </summary>
    public string Status { get; set; } = "pending";
    
    /// <summary>
    /// Lock transaction hash (Zcash locking tx)
    /// </summary>
    public string? LockTxHash { get; set; }
    
    /// <summary>
    /// Mint transaction hash (Starknet mint tx)
    /// </summary>
    public string? MintTxHash { get; set; }
    
    /// <summary>
    /// Viewing key hash for Zcash auditability
    /// </summary>
    public string? ViewingKeyHash { get; set; }
    
    /// <summary>
    /// When the swap was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the swap was completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Response containing atomic swap history
/// </summary>
public class AtomicSwapHistoryResponse
{
    /// <summary>
    /// List of swap operations
    /// </summary>
    public List<AtomicSwapStatusResponse> Bridges { get; set; } = new List<AtomicSwapStatusResponse>();
    
    /// <summary>
    /// Total number of swaps
    /// </summary>
    public int Total { get; set; }
}

