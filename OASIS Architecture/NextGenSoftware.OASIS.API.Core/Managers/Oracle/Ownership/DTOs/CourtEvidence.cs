using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Court-admissible evidence package for legal proceedings.
/// Includes all blockchain proofs, oracle signatures, and timestamps.
/// </summary>
public class CourtEvidence
{
    public string EvidenceId { get; set; }
    public string AssetId { get; set; }
    public string ClaimantId { get; set; }
    public DateTimeOffset ClaimTimestamp { get; set; }
    
    public BlockchainProof BlockchainProof { get; set; }
    public List<OracleAttestation> OracleAttestations { get; set; } = new();
    public List<string> SupportingDocuments { get; set; } = new();
    
    public bool IsCourtAdmissible { get; set; } = true;
    public bool IsTamperProof { get; set; } = true;
    public DateTimeOffset GeneratedAt { get; set; }
    
    /// <summary>
    /// Legal summary suitable for court filing
    /// </summary>
    public string LegalSummary { get; set; }
}

/// <summary>
/// Oracle node attestation/signature
/// </summary>
public class OracleAttestation
{
    public string OracleNodeId { get; set; }
    public string Signature { get; set; }
    public DateTimeOffset SignedAt { get; set; }
    public string Statement { get; set; }
}

