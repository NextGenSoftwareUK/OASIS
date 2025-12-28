using Application.DTOs.RiskManagement;
using Domain.Entities;

namespace Application.Contracts;

/// <summary>
/// Service for generating and managing risk recommendations
/// </summary>
public interface IRiskRecommendationService
{
    /// <summary>
    /// Get risk recommendations for a symbol
    /// </summary>
    Task<Result<List<RiskRecommendationDto>>> GetRecommendationsAsync(
        string symbol,
        CancellationToken token = default);

    /// <summary>
    /// Get risk recommendations for a specific position
    /// </summary>
    Task<Result<List<RiskRecommendationDto>>> GetRecommendationsForPositionAsync(
        string symbol,
        Position position,
        CancellationToken token = default);

    /// <summary>
    /// Get return-to-baseline recommendations for a symbol
    /// </summary>
    Task<Result<List<RiskRecommendationDto>>> GetReturnToBaselineRecommendationsAsync(
        string symbol,
        CancellationToken token = default);

    /// <summary>
    /// Acknowledge a risk recommendation
    /// </summary>
    Task<Result<bool>> AcknowledgeRecommendationAsync(
        Guid recommendationId,
        string? acknowledgedBy = null,
        CancellationToken token = default);

    /// <summary>
    /// Generate and save recommendations for a symbol
    /// </summary>
    Task<Result<List<RiskRecommendationDto>>> GenerateRecommendationsAsync(
        string symbol,
        Position? position = null,
        CancellationToken token = default);
}

