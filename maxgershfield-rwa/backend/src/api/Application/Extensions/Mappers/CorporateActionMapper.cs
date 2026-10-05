using Application.DTOs.CorporateAction.Requests;
using Application.DTOs.CorporateAction.Responses;
using Domain.Entities;
using Domain.Enums;

namespace Application.Extensions.Mappers;

/// <summary>
/// Extension methods for mapping CorporateAction entities to DTOs
/// </summary>
public static class CorporateActionMapper
{
    /// <summary>
    /// Maps a CorporateAction entity to CorporateActionResponseDto
    /// </summary>
    public static CorporateActionResponseDto ToResponseDto(this CorporateAction entity)
        => new(
            entity.Id,
            entity.Symbol,
            entity.Type.ToString(),
            entity.ExDate,
            entity.RecordDate,
            entity.EffectiveDate,
            entity.SplitRatio,
            entity.DividendAmount,
            entity.DividendCurrency,
            entity.AcquiringSymbol,
            entity.ExchangeRatio,
            entity.DataSource,
            entity.IsVerified,
            entity.CreatedAt,
            entity.UpdatedAt);

    /// <summary>
    /// Maps a CorporateAction entity to UpcomingCorporateActionResponseDto
    /// </summary>
    public static UpcomingCorporateActionResponseDto ToUpcomingResponseDto(this CorporateAction entity)
    {
        var today = DateTime.UtcNow.Date;
        var daysUntil = (entity.EffectiveDate.Date - today).Days;

        return new(
            entity.Id,
            entity.Type.ToString(),
            entity.ExDate,
            entity.EffectiveDate,
            entity.SplitRatio,
            entity.DividendAmount,
            daysUntil);
    }

    /// <summary>
    /// Maps CreateCorporateActionRequestDto to CorporateAction entity
    /// </summary>
    public static CorporateAction ToEntity(this CreateCorporateActionRequestDto request)
        => new()
        {
            Symbol = request.Symbol,
            Type = request.Type,
            ExDate = request.ExDate,
            RecordDate = request.RecordDate,
            EffectiveDate = request.EffectiveDate,
            SplitRatio = request.SplitRatio,
            DividendAmount = request.DividendAmount,
            DividendCurrency = request.DividendCurrency,
            AcquiringSymbol = request.AcquiringSymbol,
            ExchangeRatio = request.ExchangeRatio,
            DataSource = request.DataSource,
            IsVerified = false // Manual entries are not verified by default
        };

    /// <summary>
    /// Maps a CorporateAction entity to CreateCorporateActionResponseDto
    /// </summary>
    public static CreateCorporateActionResponseDto ToCreateResponseDto(this CorporateAction entity)
        => new(
            entity.Id,
            entity.Symbol,
            entity.Type.ToString(),
            entity.CreatedAt);
}

