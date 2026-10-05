using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Records whether an asset was available at a specific time.
/// Critical for: Dispute resolution, "could we have used it then?"
/// </summary>
public class AvailabilityRecord
{
    public string AssetId { get; set; }
    public DateTimeOffset CheckTime { get; set; }
    public bool WasAvailable { get; set; }
    public decimal AvailableValue { get; set; }
    public List<Encumbrance> ActiveEncumbrances { get; set; } = new();
    public string Reason { get; set; }
    public int ConsensusLevel { get; set; }
}






