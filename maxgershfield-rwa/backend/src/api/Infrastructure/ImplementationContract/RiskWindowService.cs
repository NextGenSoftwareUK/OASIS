using Application.Contracts;
using Application.DTOs.RiskManagement;
using BuildingBlocks.Extensions.ResultPattern;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ImplementationContract;

/// <summary>
/// Service for identifying and managing risk windows based on corporate actions, volatility, and liquidity
/// </summary>
public sealed class RiskWindowService(
    DataContext dbContext,
    ICorporateActionService corporateActionService,
    IVolatilityService volatilityService,
    ILiquidityService liquidityService,
    ILogger<RiskWindowService> logger) : IRiskWindowService
{
    private const int CorporateActionWindowDaysBefore = 3;
    private const int CorporateActionWindowDaysAfter = 3;
    private const int CorporateActionHighRiskDays = 7;
    private const int CorporateActionCriticalRiskDays = 3;
    private const decimal VolatilityHighThreshold = 0.4m; // 40%
    private const decimal VolatilityCriticalThreshold = 0.6m; // 60%
    private const decimal LiquidityLowThreshold = 0.3m; // 30%

    /// <summary>
    /// Identify risk window for a symbol at a specific date
    /// </summary>
    public async Task<Result<RiskWindowDto>> IdentifyRiskWindowAsync(
        string symbol,
        DateTime date,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(IdentifyRiskWindowAsync), startTime);

        try
        {
            var factors = new List<Domain.Entities.RiskFactor>();
            var riskLevel = RiskLevel.Low;
            DateTime? windowStart = null;
            DateTime? windowEnd = null;

            // 1. Check for corporate actions
            var corporateActionFactors = await IdentifyCorporateActionRiskAsync(symbol, date, token);
            factors.AddRange(corporateActionFactors.Factors);
            if (corporateActionFactors.RiskLevel > riskLevel)
            {
                riskLevel = corporateActionFactors.RiskLevel;
            }
            if (corporateActionFactors.StartDate.HasValue && 
                (windowStart == null || corporateActionFactors.StartDate < windowStart))
            {
                windowStart = corporateActionFactors.StartDate;
            }
            if (corporateActionFactors.EndDate.HasValue && 
                (windowEnd == null || corporateActionFactors.EndDate > windowEnd))
            {
                windowEnd = corporateActionFactors.EndDate;
            }

            // 2. Check for high volatility
            var volatilityFactor = await IdentifyVolatilityRiskAsync(symbol, date, token);
            if (volatilityFactor != null)
            {
                factors.Add(volatilityFactor);
                var volatilityRiskLevel = volatilityFactor.Impact >= 0.9m ? RiskLevel.Critical : RiskLevel.High;
                if (volatilityRiskLevel > riskLevel)
                {
                    riskLevel = volatilityRiskLevel;
                }
                if (windowStart == null)
                {
                    windowStart = date;
                }
                if (windowEnd == null || date.AddDays(7) > windowEnd)
                {
                    windowEnd = date.AddDays(7);
                }
            }

            // 3. Check for low liquidity
            var liquidityFactor = await IdentifyLiquidityRiskAsync(symbol, date, token);
            if (liquidityFactor != null)
            {
                factors.Add(liquidityFactor);
                if (RiskLevel.High > riskLevel)
                {
                    riskLevel = RiskLevel.High;
                }
                if (windowStart == null)
                {
                    windowStart = date;
                }
                if (windowEnd == null || date.AddDays(7) > windowEnd)
                {
                    windowEnd = date.AddDays(7);
                }
            }

            // If no risk factors found, return low risk window
            if (factors.Count == 0)
            {
                var lowRiskWindow = new RiskWindowDto
                {
                    Id = Guid.NewGuid(),
                    Symbol = symbol,
                    Level = RiskLevel.Low,
                    StartDate = date,
                    EndDate = date,
                    Factors = new List<RiskFactorDto>(),
                    CreatedAt = DateTime.UtcNow
                };

                logger.OperationCompleted(nameof(IdentifyRiskWindowAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - startTime);
                return Result<RiskWindowDto>.Success(lowRiskWindow);
            }

            // Create risk window entity
            var riskWindow = new RiskWindow
            {
                Id = Guid.NewGuid(),
                Symbol = symbol,
                Level = riskLevel,
                StartDate = windowStart ?? date,
                EndDate = windowEnd ?? date.AddDays(7)
            };

            // Add factors to risk window
            foreach (var factor in factors)
            {
                factor.RiskWindowId = riskWindow.Id;
                factor.Id = Guid.NewGuid();
            }

            riskWindow.Factors = factors;

            // Save to database
            dbContext.Set<RiskWindow>().Add(riskWindow);
            await dbContext.SaveChangesAsync(token);

            // Map to DTO
            var riskWindowDto = MapToDto(riskWindow);

            logger.OperationCompleted(nameof(IdentifyRiskWindowAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<RiskWindowDto>.Success(riskWindowDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error identifying risk window for {Symbol} at {Date}", symbol, date);
            return Result<RiskWindowDto>.Failure(
                ResultPatternError.InternalServerError($"Error identifying risk window: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get all active risk windows for a list of symbols
    /// </summary>
    public async Task<Result<List<RiskWindowDto>>> GetActiveRiskWindowsAsync(
        List<string> symbols,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetActiveRiskWindowsAsync), startTime);

        try
        {
            var now = DateTime.UtcNow;
            var riskWindows = await dbContext.Set<RiskWindow>()
                .Include(rw => rw.Factors)
                .Where(rw => symbols.Contains(rw.Symbol) &&
                            rw.StartDate <= now &&
                            rw.EndDate >= now)
                .OrderByDescending(rw => rw.Level)
                .ThenBy(rw => rw.StartDate)
                .ToListAsync(token);

            var dtos = riskWindows.Select(MapToDto).ToList();

            logger.OperationCompleted(nameof(GetActiveRiskWindowsAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<List<RiskWindowDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting active risk windows");
            return Result<List<RiskWindowDto>>.Failure(
                ResultPatternError.InternalServerError($"Error getting active risk windows: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get upcoming risk windows for a list of symbols
    /// </summary>
    public async Task<Result<List<RiskWindowDto>>> GetUpcomingRiskWindowsAsync(
        List<string> symbols,
        int daysAhead = 7,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetUpcomingRiskWindowsAsync), startTime);

        try
        {
            var now = DateTime.UtcNow;
            var futureDate = now.AddDays(daysAhead);

            var riskWindows = await dbContext.Set<RiskWindow>()
                .Include(rw => rw.Factors)
                .Where(rw => symbols.Contains(rw.Symbol) &&
                            rw.StartDate > now &&
                            rw.StartDate <= futureDate)
                .OrderBy(rw => rw.StartDate)
                .ThenByDescending(rw => rw.Level)
                .ToListAsync(token);

            var dtos = riskWindows.Select(MapToDto).ToList();

            logger.OperationCompleted(nameof(GetUpcomingRiskWindowsAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<List<RiskWindowDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting upcoming risk windows");
            return Result<List<RiskWindowDto>>.Failure(
                ResultPatternError.InternalServerError($"Error getting upcoming risk windows: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get recent risk window for a symbol
    /// </summary>
    public async Task<Result<RiskWindowDto?>> GetRecentRiskWindowAsync(
        string symbol,
        int daysBack = 7,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetRecentRiskWindowAsync), startTime);

        try
        {
            var now = DateTime.UtcNow;
            var pastDate = now.AddDays(-daysBack);

            var riskWindow = await dbContext.Set<RiskWindow>()
                .Include(rw => rw.Factors)
                .Where(rw => rw.Symbol == symbol &&
                            rw.EndDate >= pastDate &&
                            rw.EndDate <= now)
                .OrderByDescending(rw => rw.EndDate)
                .FirstOrDefaultAsync(token);

            if (riskWindow == null)
            {
                logger.OperationCompleted(nameof(GetRecentRiskWindowAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - startTime);
                return Result<RiskWindowDto?>.Success(null);
            }

            var dto = MapToDto(riskWindow);

            logger.OperationCompleted(nameof(GetRecentRiskWindowAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<RiskWindowDto?>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting recent risk window for {Symbol}", symbol);
            return Result<RiskWindowDto?>.Failure(
                ResultPatternError.InternalServerError($"Error getting recent risk window: {ex.Message}"));
        }
    }

    /// <summary>
    /// Save or update a risk window in the database
    /// </summary>
    public async Task<Result<RiskWindowDto>> SaveRiskWindowAsync(
        RiskWindow riskWindow,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(SaveRiskWindowAsync), startTime);

        try
        {
            var existing = await dbContext.Set<RiskWindow>()
                .FirstOrDefaultAsync(rw => rw.Id == riskWindow.Id, token);

            if (existing != null)
            {
                // Update existing
                existing.Symbol = riskWindow.Symbol;
                existing.Level = riskWindow.Level;
                existing.StartDate = riskWindow.StartDate;
                existing.EndDate = riskWindow.EndDate;
                dbContext.Set<RiskWindow>().Update(existing);
            }
            else
            {
                // Add new
                dbContext.Set<RiskWindow>().Add(riskWindow);
            }

            await dbContext.SaveChangesAsync(token);

            // Reload with factors
            var saved = await dbContext.Set<RiskWindow>()
                .Include(rw => rw.Factors)
                .FirstAsync(rw => rw.Id == riskWindow.Id, token);

            var dto = MapToDto(saved);

            logger.OperationCompleted(nameof(SaveRiskWindowAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<RiskWindowDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving risk window");
            return Result<RiskWindowDto>.Failure(
                ResultPatternError.InternalServerError($"Error saving risk window: {ex.Message}"));
        }
    }

    #region Private Helper Methods

    private async Task<(List<Domain.Entities.RiskFactor> Factors, RiskLevel RiskLevel, DateTime? StartDate, DateTime? EndDate)> 
        IdentifyCorporateActionRiskAsync(string symbol, DateTime date, CancellationToken token)
    {
        var factors = new List<Domain.Entities.RiskFactor>();
        var riskLevel = RiskLevel.Low;
        DateTime? windowStart = null;
        DateTime? windowEnd = null;

        var upcomingActions = await corporateActionService.GetUpcomingCorporateActionsAsync(
            symbol, 
            daysAhead: CorporateActionHighRiskDays, 
            token);

        foreach (var action in upcomingActions)
        {
            var daysUntilExDate = (action.ExDate - date).Days;
            var daysUntilEffective = (action.EffectiveDate - date).Days;

            RiskLevel actionRiskLevel;
            decimal impact;

            if (daysUntilExDate <= CorporateActionCriticalRiskDays || 
                daysUntilEffective <= CorporateActionCriticalRiskDays)
            {
                actionRiskLevel = RiskLevel.Critical;
                impact = 1.0m;
            }
            else if (daysUntilExDate <= CorporateActionHighRiskDays || 
                     daysUntilEffective <= CorporateActionHighRiskDays)
            {
                actionRiskLevel = RiskLevel.High;
                impact = 0.8m;
            }
            else
            {
                continue; // Too far in the future
            }

            if (actionRiskLevel > riskLevel)
            {
                riskLevel = actionRiskLevel;
            }

            var factorStartDate = action.ExDate.AddDays(-CorporateActionWindowDaysBefore);
            var factorEndDate = action.EffectiveDate.AddDays(CorporateActionWindowDaysAfter);

            if (windowStart == null || factorStartDate < windowStart)
            {
                windowStart = factorStartDate;
            }
            if (windowEnd == null || factorEndDate > windowEnd)
            {
                windowEnd = factorEndDate;
            }

            factors.Add(new Domain.Entities.RiskFactor
            {
                Type = RiskFactorType.CorporateAction,
                Description = $"{action.Type} effective {action.EffectiveDate:yyyy-MM-dd}",
                Impact = impact,
                EffectiveDate = action.EffectiveDate,
                Details = System.Text.Json.JsonSerializer.Serialize(new { 
                    ActionId = action.Id,
                    ActionType = action.Type.ToString(),
                    ExDate = action.ExDate,
                    EffectiveDate = action.EffectiveDate
                })
            });
        }

        return (factors, riskLevel, windowStart, windowEnd);
    }

    private async Task<Domain.Entities.RiskFactor?> IdentifyVolatilityRiskAsync(
        string symbol, 
        DateTime date, 
        CancellationToken token)
    {
        var volatilityResult = await volatilityService.GetVolatilityAsync(symbol, days: 30, token);
        
        if (!volatilityResult.IsSuccess || volatilityResult.Value == null)
        {
            return null;
        }

        var volatility = volatilityResult.Value;

        if (volatility > VolatilityCriticalThreshold)
        {
            return new Domain.Entities.RiskFactor
            {
                Type = RiskFactorType.HighVolatility,
                Description = $"High volatility: {volatility:P}",
                Impact = Math.Min(0.9m, volatility / 0.5m), // Cap at 0.9
                EffectiveDate = date
            };
        }
        else if (volatility > VolatilityHighThreshold)
        {
            return new Domain.Entities.RiskFactor
            {
                Type = RiskFactorType.HighVolatility,
                Description = $"High volatility: {volatility:P}",
                Impact = Math.Min(0.9m, volatility / 0.5m),
                EffectiveDate = date
            };
        }

        return null;
    }

    private async Task<Domain.Entities.RiskFactor?> IdentifyLiquidityRiskAsync(
        string symbol, 
        DateTime date, 
        CancellationToken token)
    {
        var liquidityResult = await liquidityService.GetLiquidityScoreAsync(symbol, token);
        
        if (!liquidityResult.IsSuccess || liquidityResult.Value == null)
        {
            return null;
        }

        var liquidityScore = liquidityResult.Value;

        if (liquidityScore < LiquidityLowThreshold)
        {
            return new Domain.Entities.RiskFactor
            {
                Type = RiskFactorType.LowLiquidity,
                Description = $"Low liquidity: {liquidityScore:P}",
                Impact = 1.0m - liquidityScore,
                EffectiveDate = date
            };
        }

        return null;
    }

    private RiskWindowDto MapToDto(RiskWindow riskWindow)
    {
        return new RiskWindowDto
        {
            Id = riskWindow.Id,
            Symbol = riskWindow.Symbol,
            Level = riskWindow.Level,
            StartDate = riskWindow.StartDate,
            EndDate = riskWindow.EndDate,
            Factors = riskWindow.Factors.Select(f => new RiskFactorDto
            {
                Type = f.Type,
                Description = f.Description,
                Impact = f.Impact,
                EffectiveDate = f.EffectiveDate,
                Details = f.Details
            }).ToList(),
            CreatedAt = riskWindow.CreatedAt
        };
    }

    #endregion
}

