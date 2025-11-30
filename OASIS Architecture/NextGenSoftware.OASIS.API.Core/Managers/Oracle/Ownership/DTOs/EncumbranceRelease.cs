using System;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Confirmation of encumbrance release.
/// </summary>
public class EncumbranceRelease
{
    public string EncumbranceId { get; set; }
    public string AssetId { get; set; }
    public DateTimeOffset ReleaseTime { get; set; }
    public string ReleaseTransactionHash { get; set; }
    public string Reason { get; set; }
    public bool WasAutomatic { get; set; }
}





