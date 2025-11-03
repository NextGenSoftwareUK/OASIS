using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Represents ownership of a specific asset with current status and value.
/// Used for portfolio queries and collateral optimization.
/// </summary>
public class AssetOwnership
{
    /// <summary>
    /// Asset identifier
    /// </summary>
    public string AssetId { get; set; }

    /// <summary>
    /// Owner identifier
    /// </summary>
    public string OwnerId { get; set; }

    /// <summary>
    /// Asset type (US Treasuries, Corporate Bonds, Real Estate, etc.)
    /// </summary>
    public string AssetType { get; set; }

    /// <summary>
    /// Asset name/description
    /// </summary>
    public string AssetName { get; set; }

    /// <summary>
    /// Current market value (real-time from price oracles)
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Chain where asset is located
    /// </summary>
    public ProviderType Chain { get; set; }

    /// <summary>
    /// Whether asset is encumbered
    /// </summary>
    public bool IsEncumbered { get; set; }

    /// <summary>
    /// If encumbered, the encumbrance details
    /// </summary>
    public EncumbranceStatus EncumbranceDetails { get; set; }

    /// <summary>
    /// Liquidity score (0-100, higher = easier to liquidate)
    /// </summary>
    public int Liquidity { get; set; }

    /// <summary>
    /// Volatility score (0-100, higher = more volatile)
    /// </summary>
    public int Volatility { get; set; }

    /// <summary>
    /// When this asset was acquired
    /// </summary>
    public DateTimeOffset AcquisitionDate { get; set; }

    /// <summary>
    /// Cost basis (purchase price)
    /// </summary>
    public decimal CostBasis { get; set; }

    /// <summary>
    /// Unrealized gain/loss
    /// </summary>
    public decimal UnrealizedPnL => Value - CostBasis;

    /// <summary>
    /// Asset metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Last time this record was updated
    /// </summary>
    public DateTimeOffset LastUpdate { get; set; }
}





