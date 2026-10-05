using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.CorporateAction.Requests;

/// <summary>
/// Request DTO for creating a corporate action
/// </summary>
public sealed record CreateCorporateActionRequestDto
{
    [Required]
    [MaxLength(20)]
    public string Symbol { get; init; } = string.Empty;

    [Required]
    public CorporateActionType Type { get; init; }

    [Required]
    public DateTime ExDate { get; init; }

    [Required]
    public DateTime RecordDate { get; init; }

    [Required]
    public DateTime EffectiveDate { get; init; }

    public decimal? SplitRatio { get; init; }

    public decimal? DividendAmount { get; init; }

    public string? DividendCurrency { get; init; }

    public string? AcquiringSymbol { get; init; }

    public decimal? ExchangeRatio { get; init; }

    [Required]
    [MaxLength(100)]
    public string DataSource { get; init; } = string.Empty;
}

