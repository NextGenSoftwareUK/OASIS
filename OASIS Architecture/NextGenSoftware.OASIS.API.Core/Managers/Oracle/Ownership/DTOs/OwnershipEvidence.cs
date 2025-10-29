using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Complete evidence package proving ownership at a specific time.
/// </summary>
public class OwnershipEvidence
{
    public string AssetId { get; set; }
    public string Owner { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public OwnershipRecord OwnershipRecord { get; set; }
    public List<OwnershipEvent> OwnershipHistory { get; set; } = new();
    public BlockchainProof Proof { get; set; }
    public int ConsensusLevel { get; set; }
    public bool IsCourtAdmissible { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
}

