using Application.Contracts;
using Application.DTOs.RiskManagement;
using BuildingBlocks.Extensions.ResultPattern;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ImplementationContract;

/// <summary>
/// Service for generating and managing risk recommendations
/// </summary>
public sealed class RiskRecommendationService(
    DataContext dbContext,
    IRiskWindowService riskWindowService,
    IRiskAssessmentService riskAssessmentService,
    ILogger<RiskRecommendationService> logger) : IRiskRecommendationService
{
    private const decimal LeverageBuffer = 1.1m; // 10% buffer before recommending deleveraging
    private const decimal BaselineLeverageThreshold = 0.9m; // 90% of normal leverage
    private const decimal GradualDeleverageTargetRatio = 0.8m; // 80% of recommended leverage for gradual

    /// <summary>
    /// Get risk recommendations for a symbol
    /// </summary>
    public async Task<Result<List<RiskRecommendationDto>>> GetRecommendationsAsync(
        string symbol,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetRecommendationsAsync), startTime);

        try
        {
            var recommendations = await dbContext.Set<RiskRecommendation>()
                .Where(rr => rr.Symbol == symbol &&
                            (rr.ValidUntil == null || rr.ValidUntil >= DateTime.UtcNow) &&
                            !rr.Acknowledged)
                .OrderByDescending(rr => rr.Priority)
                .ThenBy(rr => rr.RecommendedBy)
                .ToListAsync(token);

            var dtos = recommendations.Select(r => MapToDto(r, new List<RiskFactorDto>())).ToList();

            logger.OperationCompleted(nameof(GetRecommendationsAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<List<RiskRecommendationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting recommendations for {Symbol}", symbol);
            return Result<List<RiskRecommendationDto>>.Failure(
                ResultPatternError.InternalServerError($"Error getting recommendations: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get risk recommendations for a specific position
    /// </summary>
    public async Task<Result<List<RiskRecommendationDto>>> GetRecommendationsForPositionAsync(
        string symbol,
        Position position,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetRecommendationsForPositionAsync), startTime);

        try
        {
            // Get position-specific recommendations
            var recommendations = await dbContext.Set<RiskRecommendation>()
                .Where(rr => rr.Symbol == symbol &&
                            (rr.PositionId == null || rr.PositionId == position.Id) &&
                            (rr.ValidUntil == null || rr.ValidUntil >= DateTime.UtcNow) &&
                            !rr.Acknowledged)
                .OrderByDescending(rr => rr.Priority)
                .ThenBy(rr => rr.RecommendedBy)
                .ToListAsync(token);

            // If no recommendations exist, generate them
            if (recommendations.Count == 0)
            {
                var generateResult = await GenerateRecommendationsAsync(symbol, position, token);
                if (generateResult.IsSuccess && generateResult.Value != null)
                {
                    return Result<List<RiskRecommendationDto>>.Success(generateResult.Value);
                }
            }

            var dtos = recommendations.Select(r => MapToDto(r, new List<RiskFactorDto>())).ToList();

            logger.OperationCompleted(nameof(GetRecommendationsForPositionAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<List<RiskRecommendationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting recommendations for position {PositionId}", position.Id);
            return Result<List<RiskRecommendationDto>>.Failure(
                ResultPatternError.InternalServerError($"Error getting recommendations: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get return-to-baseline recommendations for a symbol
    /// </summary>
    public async Task<Result<List<RiskRecommendationDto>>> GetReturnToBaselineRecommendationsAsync(
        string symbol,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetReturnToBaselineRecommendationsAsync), startTime);

        try
        {
            // Check if we're past a risk window
            var recentRiskWindowResult = await riskWindowService.GetRecentRiskWindowAsync(
                symbol, 
                daysBack: 7, 
                token);

            if (!recentRiskWindowResult.IsSuccess || recentRiskWindowResult.Value == null)
            {
                logger.OperationCompleted(nameof(GetReturnToBaselineRecommendationsAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - startTime);
                return Result<List<RiskRecommendationDto>>.Success(new List<RiskRecommendationDto>());
            }

            var recentRiskWindow = recentRiskWindowResult.Value;
            var now = DateTime.UtcNow;

            // Only generate if risk window has ended
            if (now <= recentRiskWindow.EndDate)
            {
                logger.OperationCompleted(nameof(GetReturnToBaselineRecommendationsAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - startTime);
                return Result<List<RiskRecommendationDto>>.Success(new List<RiskRecommendationDto>());
            }

            // Get current recommendations to check leverage
            var recommendations = await dbContext.Set<RiskRecommendation>()
                .Where(rr => rr.Symbol == symbol &&
                            rr.Action == RiskAction.ReturnToBaseline &&
                            (rr.ValidUntil == null || rr.ValidUntil >= now) &&
                            !rr.Acknowledged)
                .ToListAsync(token);

            var dtos = recommendations.Select(r => MapToDto(r, new List<RiskFactorDto>())).ToList();

            logger.OperationCompleted(nameof(GetReturnToBaselineRecommendationsAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<List<RiskRecommendationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting return-to-baseline recommendations for {Symbol}", symbol);
            return Result<List<RiskRecommendationDto>>.Failure(
                ResultPatternError.InternalServerError($"Error getting return-to-baseline recommendations: {ex.Message}"));
        }
    }

    /// <summary>
    /// Acknowledge a risk recommendation
    /// </summary>
    public async Task<Result<bool>> AcknowledgeRecommendationAsync(
        Guid recommendationId,
        string? acknowledgedBy = null,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(AcknowledgeRecommendationAsync), startTime);

        try
        {
            var recommendation = await dbContext.Set<RiskRecommendation>()
                .FirstOrDefaultAsync(rr => rr.Id == recommendationId, token);

            if (recommendation == null)
            {
                return Result<bool>.Failure(
                    ResultPatternError.NotFound($"Recommendation with ID {recommendationId} not found"));
            }

            recommendation.Acknowledged = true;
            recommendation.AcknowledgedAt = DateTime.UtcNow;
            recommendation.AcknowledgedBy = acknowledgedBy;

            await dbContext.SaveChangesAsync(token);

            logger.OperationCompleted(nameof(AcknowledgeRecommendationAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error acknowledging recommendation {RecommendationId}", recommendationId);
            return Result<bool>.Failure(
                ResultPatternError.InternalServerError($"Error acknowledging recommendation: {ex.Message}"));
        }
    }

    /// <summary>
    /// Generate and save recommendations for a symbol
    /// </summary>
    public async Task<Result<List<RiskRecommendationDto>>> GenerateRecommendationsAsync(
        string symbol,
        Position? position = null,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GenerateRecommendationsAsync), startTime);

        try
        {
            var recommendations = new List<RiskRecommendation>();

            // 1. Get risk assessment
            var assessmentResult = await riskAssessmentService.AssessRiskAsync(symbol, position, token);
            if (!assessmentResult.IsSuccess || assessmentResult.Value == null)
            {
                return Result<List<RiskRecommendationDto>>.Failure(
                    ResultPatternError.InternalServerError("Failed to assess risk"));
            }

            var assessment = assessmentResult.Value;
            decimal currentLeverage = position?.Leverage ?? assessment.CurrentLeverage;
            decimal recommendedLeverage = assessment.RecommendedLeverage;

            // 2. Check for deleveraging recommendations
            if (currentLeverage > recommendedLeverage * LeverageBuffer)
            {
                var deleveragingRecommendation = await GenerateDeleveragingRecommendationAsync(
                    symbol, 
                    position, 
                    currentLeverage, 
                    recommendedLeverage, 
                    assessment.ActiveRiskWindow,
                    token);
                if (deleveragingRecommendation != null)
                {
                    recommendations.Add(deleveragingRecommendation);
                }
            }

            // 3. Check for return-to-baseline recommendations
            var returnToBaselineRecommendation = await GenerateReturnToBaselineRecommendationAsync(
                symbol, 
                position, 
                currentLeverage, 
                token);
            if (returnToBaselineRecommendation != null)
            {
                recommendations.Add(returnToBaselineRecommendation);
            }

            // 4. Save recommendations to database
            if (recommendations.Count > 0)
            {
                dbContext.Set<RiskRecommendation>().AddRange(recommendations);
                await dbContext.SaveChangesAsync(token);
            }

            // 5. Map to DTOs
            var factors = assessment.Factors;
            var dtos = recommendations.Select(r => MapToDto(r, factors)).ToList();

            logger.OperationCompleted(nameof(GenerateRecommendationsAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<List<RiskRecommendationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating recommendations for {Symbol}", symbol);
            return Result<List<RiskRecommendationDto>>.Failure(
                ResultPatternError.InternalServerError($"Error generating recommendations: {ex.Message}"));
        }
    }

    #region Private Helper Methods

    private async Task<RiskRecommendation?> GenerateDeleveragingRecommendationAsync(
        string symbol,
        Position? position,
        decimal currentLeverage,
        decimal recommendedLeverage,
        RiskWindowDto? activeRiskWindow,
        CancellationToken token)
    {
        // Check if there's an active or upcoming risk window
        var now = DateTime.UtcNow;
        var upcomingWindowsResult = await riskWindowService.GetUpcomingRiskWindowsAsync(
            new List<string> { symbol }, 
            daysAhead: 7, 
            token);

        RiskWindowDto? relevantWindow = activeRiskWindow;
        if (relevantWindow == null && upcomingWindowsResult.IsSuccess && upcomingWindowsResult.Value != null)
        {
            relevantWindow = upcomingWindowsResult.Value.FirstOrDefault();
        }

        if (relevantWindow == null)
        {
            return null; // No risk window, no need to deleverage
        }

        var daysUntilWindow = (relevantWindow.StartDate - now).Days;
        RiskAction action;
        decimal targetLeverage;
        Priority priority;

        if (daysUntilWindow <= 3)
        {
            // Immediate deleveraging
            action = RiskAction.Deleverage;
            targetLeverage = recommendedLeverage;
            priority = relevantWindow.Level == RiskLevel.Critical ? Priority.Critical : Priority.High;
        }
        else if (daysUntilWindow <= 7)
        {
            // Gradual deleveraging
            action = RiskAction.GradualDeleverage;
            targetLeverage = recommendedLeverage * GradualDeleverageTargetRatio;
            priority = Priority.Medium;
        }
        else
        {
            return null; // Too far in the future
        }

        decimal reductionPercentage = ((currentLeverage - targetLeverage) / currentLeverage) * 100m;
        string reason = $"Risk window identified: {string.Join(", ", relevantWindow.Factors.Select(f => f.Description))}";

        return new RiskRecommendation
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            PositionId = position?.Id,
            Action = action,
            CurrentLeverage = currentLeverage,
            TargetLeverage = targetLeverage,
            ReductionPercentage = reductionPercentage,
            Reason = reason,
            Priority = priority,
            RecommendedBy = now,
            ValidUntil = relevantWindow.EndDate,
            Acknowledged = false
        };
    }

    private async Task<RiskRecommendation?> GenerateReturnToBaselineRecommendationAsync(
        string symbol,
        Position? position,
        decimal currentLeverage,
        CancellationToken token)
    {
        // Check if we're past a risk window
        var recentRiskWindowResult = await riskWindowService.GetRecentRiskWindowAsync(
            symbol, 
            daysBack: 7, 
            token);

        if (!recentRiskWindowResult.IsSuccess || recentRiskWindowResult.Value == null)
        {
            return null;
        }

        var recentRiskWindow = recentRiskWindowResult.Value;
        var now = DateTime.UtcNow;

        // Only generate if risk window has ended
        if (now <= recentRiskWindow.EndDate)
        {
            return null;
        }

        // Check if leverage is below normal (90% threshold)
        const decimal normalLeverage = 5.0m; // Base leverage for Low risk
        if (currentLeverage >= normalLeverage * BaselineLeverageThreshold)
        {
            return null; // Already at or above baseline threshold
        }

        decimal increasePercentage = ((normalLeverage - currentLeverage) / currentLeverage) * 100m;

        return new RiskRecommendation
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            PositionId = position?.Id,
            Action = RiskAction.ReturnToBaseline,
            CurrentLeverage = currentLeverage,
            TargetLeverage = normalLeverage,
            IncreasePercentage = increasePercentage,
            Reason = "Risk window has passed, returning to baseline leverage",
            Priority = Priority.Low,
            RecommendedBy = now,
            ValidUntil = null, // No expiration for return-to-baseline
            Acknowledged = false
        };
    }

    private RiskRecommendationDto MapToDto(RiskRecommendation recommendation, List<RiskFactorDto> relatedFactors)
    {
        return new RiskRecommendationDto
        {
            Id = recommendation.Id,
            Symbol = recommendation.Symbol,
            PositionId = recommendation.PositionId,
            Action = recommendation.Action,
            CurrentLeverage = recommendation.CurrentLeverage,
            TargetLeverage = recommendation.TargetLeverage,
            ReductionPercentage = recommendation.ReductionPercentage,
            IncreasePercentage = recommendation.IncreasePercentage,
            Reason = recommendation.Reason,
            Priority = recommendation.Priority,
            RecommendedBy = recommendation.RecommendedBy,
            ValidUntil = recommendation.ValidUntil,
            Acknowledged = recommendation.Acknowledged,
            AcknowledgedAt = recommendation.AcknowledgedAt,
            AcknowledgedBy = recommendation.AcknowledgedBy,
            RelatedFactors = relatedFactors
        };
    }

    #endregion
}

