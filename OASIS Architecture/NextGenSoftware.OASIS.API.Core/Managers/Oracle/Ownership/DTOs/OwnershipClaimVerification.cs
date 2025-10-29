using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Result of verifying a single ownership claim.
/// </summary>
public class OwnershipClaimVerification
{
    public DisputeClaim Claim { get; set; }
    public bool IsValid { get; set; }
    public int ConsensusLevel { get; set; }
    public List<string> Evidence { get; set; } = new();
    public string RejectionReason { get; set; }
    public DateTimeOffset VerifiedAt { get; set; }
}

