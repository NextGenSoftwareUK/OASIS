using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Request to create a new encumbrance (pledge, lien, lock).
/// </summary>
public class CreateEncumbranceRequest
{
    public string AssetId { get; set; }
    public EncumbranceType Type { get; set; }
    public string Owner { get; set; }
    public string Counterparty { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset MaturityTime { get; set; }
    public int Priority { get; set; } = 1;
    public ProviderType Chain { get; set; }
    public bool AutoRelease { get; set; } = true;
    public decimal InterestRate { get; set; }
    public decimal Haircut { get; set; }
    public Dictionary<string, string> Terms { get; set; } = new();
}

