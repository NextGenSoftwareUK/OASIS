using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Result of verifying an ownership claim using multi-oracle consensus.
/// </summary>
public class OwnershipVerification
{
    public bool IsValid { get; set; }
    public int ConsensusLevel { get; set; }
    public List<string> Evidence { get; set; } = new();
    public string Reason { get; set; }
    public DateTimeOffset VerifiedAt { get; set; }
}





