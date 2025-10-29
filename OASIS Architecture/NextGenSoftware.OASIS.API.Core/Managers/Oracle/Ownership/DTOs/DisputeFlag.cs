using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Flags an ownership dispute for human review when consensus is low.
/// </summary>
public class DisputeFlag
{
    public string FlagId { get; set; }
    public string AssetId { get; set; }
    public string Reason { get; set; }
    public List<OwnershipRecord> ConflictingRecords { get; set; } = new();
    public int LowestConsensusLevel { get; set; }
    public DateTimeOffset FlaggedAt { get; set; }
    public bool IsResolved { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string Resolution { get; set; }
}

