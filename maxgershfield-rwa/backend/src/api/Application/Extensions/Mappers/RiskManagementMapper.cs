using Application.DTOs.RiskManagement;
using Application.DTOs.RiskManagement.Responses;
using Domain.Enums;

namespace Application.Extensions.Mappers;

/// <summary>
/// Extension methods for mapping RiskManagement DTOs to API response DTOs
/// </summary>
public static class RiskManagementMapper
{
    /// <summary>
    /// Maps RiskAssessmentDto to RiskAssessmentResponseDto
    /// </summary>
    public static RiskAssessmentResponseDto ToResponseDto(this RiskAssessmentDto dto)
        => new()
        {
            Symbol = dto.Symbol,
            Level = dto.Level.ToString(),
            RiskScore = dto.RiskScore,
            CurrentLeverage = dto.CurrentLeverage,
            RecommendedLeverage = dto.RecommendedLeverage,
            Factors = dto.Factors.Select(f => f.ToResponseDto()).ToList(),
            ActiveRiskWindow = dto.ActiveRiskWindow?.ToSummaryDto(),
            Recommendations = dto.Recommendations.Select(r => r.ToSummaryDto()).ToList(),
            AssessedAt = dto.AssessedAt
        };

    /// <summary>
    /// Maps RiskFactorDto to RiskFactorResponseDto
    /// </summary>
    public static RiskFactorResponseDto ToResponseDto(this RiskFactorDto dto)
        => new()
        {
            Type = dto.Type.ToString(),
            Description = dto.Description,
            Impact = dto.Impact,
            EffectiveDate = dto.EffectiveDate
        };

    /// <summary>
    /// Maps RiskWindowDto to RiskWindowSummaryDto
    /// </summary>
    public static RiskWindowSummaryDto ToSummaryDto(this RiskWindowDto dto)
        => new()
        {
            Id = dto.Id,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Level = dto.Level.ToString()
        };

    /// <summary>
    /// Maps RiskWindowDto to RiskWindowDetailDto
    /// </summary>
    public static RiskWindowDetailDto ToDetailDto(this RiskWindowDto dto)
        => new()
        {
            Id = dto.Id,
            Level = dto.Level.ToString(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Factors = dto.Factors.Select(f => f.ToResponseDto()).ToList(),
            CreatedAt = dto.CreatedAt
        };

    /// <summary>
    /// Maps RiskWindowDto to RiskWindowResponseDto
    /// </summary>
    public static RiskWindowResponseDto ToWindowResponseDto(this RiskWindowDto? dto, string symbol)
        => new()
        {
            Symbol = symbol,
            HasActiveWindow = dto != null,
            RiskWindow = dto?.ToDetailDto()
        };

    /// <summary>
    /// Maps RiskRecommendationDto to RiskRecommendationSummaryDto
    /// </summary>
    public static RiskRecommendationSummaryDto ToSummaryDto(this RiskRecommendationDto dto)
        => new()
        {
            Id = dto.Id,
            Action = dto.Action.ToString(),
            TargetLeverage = dto.TargetLeverage,
            ReductionPercentage = dto.ReductionPercentage,
            Reason = dto.Reason,
            Priority = dto.Priority.ToString()
        };

    /// <summary>
    /// Maps RiskRecommendationDto to RiskRecommendationResponseDto
    /// </summary>
    public static RiskRecommendationResponseDto ToResponseDto(this RiskRecommendationDto dto)
        => new()
        {
            Id = dto.Id,
            Symbol = dto.Symbol,
            PositionId = dto.PositionId,
            Action = dto.Action.ToString(),
            CurrentLeverage = dto.CurrentLeverage,
            TargetLeverage = dto.TargetLeverage,
            ReductionPercentage = dto.ReductionPercentage,
            IncreasePercentage = dto.IncreasePercentage,
            Reason = dto.Reason,
            Priority = dto.Priority.ToString(),
            RecommendedBy = dto.RecommendedBy,
            ValidUntil = dto.ValidUntil,
            Acknowledged = dto.Acknowledged,
            AcknowledgedAt = dto.AcknowledgedAt,
            RelatedFactors = dto.RelatedFactors.Select(f => f.ToResponseDto()).ToList()
        };

    /// <summary>
    /// Maps ActiveRiskWindowItemDto from RiskWindowDto
    /// </summary>
    public static ActiveRiskWindowItemDto ToActiveWindowItemDto(this RiskWindowDto dto)
        => new()
        {
            Symbol = dto.Symbol,
            Level = dto.Level.ToString(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Factors = dto.Factors.Select(f => f.ToResponseDto()).ToList()
        };
}

