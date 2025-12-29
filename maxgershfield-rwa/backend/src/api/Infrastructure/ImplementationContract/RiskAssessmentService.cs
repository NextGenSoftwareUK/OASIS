using Application.Contracts;
using Application.DTOs.RiskManagement;
using BuildingBlocks.Extensions.ResultPattern;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ImplementationContract;

/// <summary>
/// Service for assessing risk levels for symbols and positions
/// </summary>
public sealed class RiskAssessmentService(
    DataContext dbContext,
    IRiskWindowService riskWindowService,
    IRiskRecommendationService riskRecommendationService,
    ILogger<RiskAssessmentService> logger) : IRiskAssessmentService
{
    private const decimal BaseLeverage = 5.0m; // Configurable base leverage

    /// <summary>
    /// Assess risk for a symbol (optionally with position data)
    /// </summary>
    public async Task<Result<RiskAssessmentDto>> AssessRiskAsync(
        string symbol,
        Position? position = null,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(AssessRiskAsync), startTime);

        try
        {
            // 1. Identify current risk window
            var riskWindowResult = await riskWindowService.IdentifyRiskWindowAsync(
                symbol, 
                DateTime.UtcNow, 
                token);

            RiskWindowDto? activeRiskWindow = null;
            RiskLevel riskLevel = RiskLevel.Low;
            List<RiskFactorDto> factors = new();

            if (riskWindowResult.IsSuccess && riskWindowResult.Value != null)
            {
                activeRiskWindow = riskWindowResult.Value;
                riskLevel = activeRiskWindow.Level;
                factors = activeRiskWindow.Factors;
            }

            // 2. Get current leverage (from position or default)
            decimal currentLeverage = position?.Leverage ?? BaseLeverage;

            // 3. Calculate recommended leverage based on risk level
            decimal recommendedLeverage = GetRecommendedLeverage(riskLevel);

            // 4. Calculate risk score (0-100 scale)
            decimal riskScore = CalculateRiskScore(riskLevel, factors, currentLeverage, recommendedLeverage);

            // 5. Get recommendations
            List<RiskRecommendationDto> recommendations = new();
            if (position != null)
            {
                var recommendationsResult = await riskRecommendationService.GetRecommendationsForPositionAsync(
                    symbol, 
                    position, 
                    token);
                if (recommendationsResult.IsSuccess && recommendationsResult.Value != null)
                {
                    recommendations = recommendationsResult.Value;
                }
            }
            else
            {
                var recommendationsResult = await riskRecommendationService.GetRecommendationsAsync(
                    symbol, 
                    token);
                if (recommendationsResult.IsSuccess && recommendationsResult.Value != null)
                {
                    recommendations = recommendationsResult.Value;
                }
            }

            var assessment = new RiskAssessmentDto
            {
                Symbol = symbol,
                Level = riskLevel,
                CurrentLeverage = currentLeverage,
                RecommendedLeverage = recommendedLeverage,
                RiskScore = riskScore,
                Factors = factors,
                ActiveRiskWindow = activeRiskWindow,
                Recommendations = recommendations,
                AssessedAt = DateTime.UtcNow
            };

            logger.OperationCompleted(nameof(AssessRiskAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<RiskAssessmentDto>.Success(assessment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assessing risk for {Symbol}", symbol);
            return Result<RiskAssessmentDto>.Failure(
                ResultPatternError.InternalServerError($"Error assessing risk: {ex.Message}"));
        }
    }

    /// <summary>
    /// Assess risk for multiple symbols in batch
    /// </summary>
    public async Task<Result<List<RiskAssessmentDto>>> AssessBatchRiskAsync(
        List<string> symbols,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(AssessBatchRiskAsync), startTime);

        var assessments = new List<RiskAssessmentDto>();

        foreach (var symbol in symbols)
        {
            var assessmentResult = await AssessRiskAsync(symbol, null, token);
            if (assessmentResult.IsSuccess && assessmentResult.Value != null)
            {
                assessments.Add(assessmentResult.Value);
            }
        }

        logger.OperationCompleted(nameof(AssessBatchRiskAsync), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow - startTime);

        return Result<List<RiskAssessmentDto>>.Success(assessments);
    }

    /// <summary>
    /// Assess risk for a specific position
    /// </summary>
    public async Task<Result<RiskAssessmentDto>> AssessPositionRiskAsync(
        string symbol,
        Position position,
        CancellationToken token = default)
    {
        return await AssessRiskAsync(symbol, position, token);
    }

    #region Private Helper Methods

    private decimal GetRecommendedLeverage(RiskLevel riskLevel)
    {
        return riskLevel switch
        {
            RiskLevel.Low => BaseLeverage, // 5x
            RiskLevel.Medium => BaseLeverage * 0.7m, // 3.5x
            RiskLevel.High => BaseLeverage * 0.5m, // 2.5x
            RiskLevel.Critical => BaseLeverage * 0.3m, // 1.5x
            _ => BaseLeverage
        };
    }

    private decimal CalculateRiskScore(
        RiskLevel riskLevel, 
        List<RiskFactorDto> factors, 
        decimal currentLeverage, 
        decimal recommendedLeverage)
    {
        // Base score from risk level (0-60 points)
        decimal baseScore = riskLevel switch
        {
            RiskLevel.Low => 0m,
            RiskLevel.Medium => 20m,
            RiskLevel.High => 40m,
            RiskLevel.Critical => 60m,
            _ => 0m
        };

        // Factor impact score (0-20 points)
        decimal factorScore = factors.Sum(f => f.Impact * 20m);
        factorScore = Math.Min(20m, factorScore);

        // Leverage deviation score (0-20 points)
        decimal leverageDeviation = 0m;
        if (currentLeverage > recommendedLeverage)
        {
            decimal deviationRatio = (currentLeverage - recommendedLeverage) / recommendedLeverage;
            leverageDeviation = Math.Min(20m, deviationRatio * 40m); // Up to 20 points for 50%+ deviation
        }

        decimal totalScore = baseScore + factorScore + leverageDeviation;
        return Math.Min(100m, totalScore);
    }

    #endregion
}

