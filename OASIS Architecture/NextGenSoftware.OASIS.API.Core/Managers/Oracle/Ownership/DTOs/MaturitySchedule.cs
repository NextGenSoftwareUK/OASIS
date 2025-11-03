using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Represents when pledged collateral will become available.
/// Critical for: Planning collateral usage, answering "when will I have $X?"
/// </summary>
public class MaturitySchedule
{
    /// <summary>
    /// Time when collateral matures/becomes available
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// List of assets maturing at this time
    /// </summary>
    public List<Encumbrance> Assets { get; set; } = new();

    /// <summary>
    /// Total value becoming available
    /// </summary>
    public decimal TotalValueFreeing { get; set; }

    /// <summary>
    /// Asset types becoming available
    /// </summary>
    public List<string> AssetTypes { get; set; } = new();

    /// <summary>
    /// Counterparties releasing collateral
    /// </summary>
    public List<string> Counterparties { get; set; } = new();

    /// <summary>
    /// Chains where releases will occur
    /// </summary>
    public List<string> Chains { get; set; } = new();

    /// <summary>
    /// Time until this maturity (from now)
    /// </summary>
    public TimeSpan TimeUntil => Time - DateTimeOffset.Now;

    /// <summary>
    /// Whether auto-release is configured
    /// </summary>
    public bool HasAutoRelease { get; set; }
}





