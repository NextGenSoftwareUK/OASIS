using System;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;

/// <summary>
/// Request to create a cross-chain NFT bridge order (e.g., NFT from Solana to Ethereum)
/// </summary>
public class CreateNFTBridgeOrderRequest
{
    /// <summary>
    /// The source blockchain network/provider
    /// </summary>
    public string FromChain { get; set; } = string.Empty;

    /// <summary>
    /// The destination blockchain network/provider
    /// </summary>
    public string ToChain { get; set; } = string.Empty;

    /// <summary>
    /// The NFT token address on the source chain
    /// </summary>
    public string NFTTokenAddress { get; set; } = string.Empty;

    /// <summary>
    /// The unique token ID of the NFT
    /// </summary>
    public string TokenId { get; set; } = string.Empty;

    /// <summary>
    /// Source address on the origin chain
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Destination address on the target chain
    /// </summary>
    public string DestinationAddress { get; set; } = string.Empty;

    /// <summary>
    /// How long until the order expires (in minutes)
    /// </summary>
    public int ExpiresInMinutes { get; set; } = 30;

    /// <summary>
    /// User ID initiating the order
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Optional viewing key for auditability
    /// </summary>
    public string ViewingKey { get; set; } = string.Empty;

    /// <summary>
    /// Enables automatic viewing key audit logging
    /// </summary>
    public bool EnableViewingKeyAudit { get; set; }

    /// <summary>
    /// Serialized proof payload for privacy-preserving bridges
    /// </summary>
    public string ProofPayload { get; set; } = string.Empty;

    /// <summary>
    /// Type of proof being submitted
    /// </summary>
    public string ProofType { get; set; } = string.Empty;

    /// <summary>
    /// Forces proof verification step
    /// </summary>
    public bool RequireProofVerification { get; set; }
}

