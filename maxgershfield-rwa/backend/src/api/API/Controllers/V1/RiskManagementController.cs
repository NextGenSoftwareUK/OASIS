using API.Controllers.Base;
using Application.Contracts;
using Application.DTOs.RiskManagement;
using Application.DTOs.RiskManagement.Responses;
using Application.Extensions.Mappers;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers.V1;

/// <summary>
/// Controller for risk management endpoints
/// </summary>
[Route("api/oracle/rwa/risk")]
public class RiskManagementController(
    IRiskAssessmentService riskAssessmentService,
    IRiskWindowService riskWindowService,
    IRiskRecommendationService riskRecommendationService,
    DataContext dbContext) : V1BaseController
{
    /// <summary>
    /// Get current risk assessment for a symbol
    /// </summary>
    /// <param name="symbol">Stock ticker symbol (e.g., "AAPL")</param>
    /// <param name="positionId">Optional position ID for position-specific assessment</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Risk assessment for the symbol</returns>
    [HttpGet("{symbol}/assessment")]
    public async Task<IActionResult> GetRiskAssessment(
        string symbol,
        [FromQuery] string? positionId = null,
        CancellationToken token = default)
    {
        Position? position = null;
        if (!string.IsNullOrEmpty(positionId))
        {
            // In a real implementation, you would fetch the position from the perp DEX
            // For now, we'll pass null and let the service use default leverage
            // TODO: Integrate with position service to fetch actual position data
        }

        var result = await riskAssessmentService.AssessRiskAsync(symbol, position, token);
        
        if (!result.IsSuccess || result.Value == null)
        {
            return result.ToActionResult();
        }

        var response = result.Value.ToResponseDto();
        return result.ToActionResult().ToActionResult(); // Convert Result<RiskAssessmentDto> -> Result<RiskAssessmentResponseDto>
    }

    /// <summary>
    /// Get active or upcoming risk window for a symbol
    /// </summary>
    /// <param name="symbol">Stock ticker symbol</param>
    /// <param name="date">Optional date to check for risk window (default: now)</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Risk window information</returns>
    [HttpGet("{symbol}/window")]
    public async Task<IActionResult> GetRiskWindow(
        string symbol,
        [FromQuery] DateTime? date = null,
        CancellationToken token = default)
    {
        var checkDate = date ?? DateTime.UtcNow;
        var result = await riskWindowService.IdentifyRiskWindowAsync(symbol, checkDate, token);
        
        if (!result.IsSuccess)
        {
            // Return empty response if no risk window found (not an error)
            var emptyResponse = new RiskWindowResponseDto 
            { 
                Symbol = symbol, 
                HasActiveWindow = false 
            };
            return Ok(emptyResponse);
        }

        var response = result.Value?.ToWindowResponseDto(symbol) ?? 
            new RiskWindowResponseDto 
            { 
                Symbol = symbol, 
                HasActiveWindow = false 
            };
        
        return Ok(response);
    }

    /// <summary>
    /// Get risk recommendations for a symbol with filtering and pagination
    /// </summary>
    /// <param name="symbol">Stock ticker symbol</param>
    /// <param name="positionId">Optional position ID filter</param>
    /// <param name="acknowledged">Optional filter by acknowledged status</param>
    /// <param name="action">Optional filter by action type</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Paginated list of recommendations</returns>
    [HttpGet("{symbol}/recommendations")]
    public async Task<IActionResult> GetRecommendations(
        string symbol,
        [FromQuery] string? positionId = null,
        [FromQuery] bool? acknowledged = null,
        [FromQuery] RiskAction? action = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken token = default)
    {
        // Validate and normalize pagination parameters
        if (pageSize > 100)
        {
            pageSize = 100;
        }
        if (pageSize < 1)
        {
            pageSize = 20;
        }
        if (page < 1)
        {
            page = 1;
        }

        try
        {
            var query = dbContext.Set<RiskRecommendation>()
                .Where(rr => rr.Symbol == symbol)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(positionId))
            {
                query = query.Where(rr => rr.PositionId == positionId);
            }

            if (acknowledged.HasValue)
            {
                query = query.Where(rr => rr.Acknowledged == acknowledged.Value);
            }

            if (action.HasValue)
            {
                query = query.Where(rr => rr.Action == action.Value);
            }

            // Filter out expired recommendations (if ValidUntil is set and in the past)
            query = query.Where(rr => rr.ValidUntil == null || rr.ValidUntil >= DateTime.UtcNow);

            // Get total count before pagination
            var totalCount = await query.CountAsync(token);

            // Apply pagination
            var recommendations = await query
                .OrderByDescending(rr => rr.Priority)
                .ThenBy(rr => rr.RecommendedBy)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(token);

            // Map to DTOs (simplified - risk factors are typically empty per service implementation)
            var recommendationDtos = recommendations.Select(r => new RiskRecommendationDto
            {
                Id = r.Id,
                Symbol = r.Symbol,
                PositionId = r.PositionId,
                Action = r.Action,
                CurrentLeverage = r.CurrentLeverage,
                TargetLeverage = r.TargetLeverage,
                ReductionPercentage = r.ReductionPercentage,
                IncreasePercentage = r.IncreasePercentage,
                Reason = r.Reason,
                Priority = r.Priority,
                RecommendedBy = r.RecommendedBy,
                ValidUntil = r.ValidUntil,
                Acknowledged = r.Acknowledged,
                AcknowledgedAt = r.AcknowledgedAt,
                AcknowledgedBy = r.AcknowledgedBy,
                RelatedFactors = new List<RiskFactorDto>() // Typically empty per service implementation
            }).ToList();

            var responseDtos = recommendationDtos.Select(r => r.ToResponseDto()).ToList();
            
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var response = new RiskRecommendationListResponseDto
            {
                Symbol = symbol,
                Recommendations = responseDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error getting recommendations: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get return-to-baseline recommendations for all symbols (or filtered)
    /// </summary>
    /// <param name="symbols">Optional comma-separated list of symbols</param>
    /// <param name="acknowledged">Optional filter by acknowledged status</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>List of return-to-baseline recommendations</returns>
    [HttpGet("recommendations/return-to-baseline")]
    public async Task<IActionResult> GetReturnToBaselineRecommendations(
        [FromQuery] string? symbols = null,
        [FromQuery] bool? acknowledged = null,
        CancellationToken token = default)
    {
        try
        {
            var symbolList = string.IsNullOrEmpty(symbols)
                ? new List<string>()
                : symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

            var query = dbContext.Set<RiskRecommendation>()
                .Where(rr => rr.Action == RiskAction.ReturnToBaseline &&
                            (rr.ValidUntil == null || rr.ValidUntil >= DateTime.UtcNow))
                .AsQueryable();

            if (symbolList.Count > 0)
            {
                query = query.Where(rr => symbolList.Contains(rr.Symbol));
            }

            if (acknowledged.HasValue)
            {
                query = query.Where(rr => rr.Acknowledged == acknowledged.Value);
            }

            var recommendations = await query
                .OrderByDescending(rr => rr.Priority)
                .ThenBy(rr => rr.RecommendedBy)
                .ToListAsync(token);

            // Map to DTOs (simplified - no risk factors for this endpoint per spec)
            var responseDtos = recommendations.Select(r => new RiskRecommendationDto
            {
                Id = r.Id,
                Symbol = r.Symbol,
                PositionId = r.PositionId,
                Action = r.Action,
                CurrentLeverage = r.CurrentLeverage,
                TargetLeverage = r.TargetLeverage,
                ReductionPercentage = r.ReductionPercentage,
                IncreasePercentage = r.IncreasePercentage,
                Reason = r.Reason,
                Priority = r.Priority,
                RecommendedBy = r.RecommendedBy,
                ValidUntil = r.ValidUntil,
                Acknowledged = r.Acknowledged,
                AcknowledgedAt = r.AcknowledgedAt,
                AcknowledgedBy = r.AcknowledgedBy,
                RelatedFactors = new List<RiskFactorDto>()
            }).Select(r => r.ToResponseDto()).ToList();

            var response = new ReturnToBaselineRecommendationsResponseDto
            {
                Recommendations = responseDtos
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error getting return-to-baseline recommendations: {ex.Message}" });
        }
    }

    /// <summary>
    /// Acknowledge a risk recommendation
    /// </summary>
    /// <param name="id">Recommendation ID</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Acknowledgment confirmation</returns>
    [HttpPost("recommendation/{id:guid}/acknowledge")]
    public async Task<IActionResult> AcknowledgeRecommendation(
        Guid id,
        CancellationToken token = default)
    {
        // Get current user ID
        string? acknowledgedBy = User.FindFirst("AvatarId")?.Value ?? 
                                User.FindFirst("avatarId")?.Value ??
                                User.FindFirst("sub")?.Value;

        var result = await riskRecommendationService.AcknowledgeRecommendationAsync(id, acknowledgedBy, token);
        
        if (!result.IsSuccess)
        {
            return result.ToActionResult();
        }

        // Get the updated recommendation to return
        var recommendation = await dbContext.Set<RiskRecommendation>()
            .FirstOrDefaultAsync(rr => rr.Id == id, token);

        if (recommendation == null)
        {
            return NotFound(new { error = "Recommendation not found" });
        }

        var response = new AcknowledgeRecommendationResponseDto
        {
            Id = recommendation.Id,
            Acknowledged = recommendation.Acknowledged,
            AcknowledgedAt = recommendation.AcknowledgedAt ?? DateTime.UtcNow,
            AcknowledgedBy = recommendation.AcknowledgedBy
        };

        return Ok(response);
    }

    /// <summary>
    /// Get all active risk windows
    /// </summary>
    /// <param name="symbols">Optional comma-separated list of symbols</param>
    /// <param name="level">Optional filter by risk level</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>List of active risk windows</returns>
    [HttpGet("windows/active")]
    public async Task<IActionResult> GetActiveRiskWindows(
        [FromQuery] string? symbols = null,
        [FromQuery] RiskLevel? level = null,
        CancellationToken token = default)
    {
        var symbolList = string.IsNullOrEmpty(symbols)
            ? new List<string>()
            : symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

        var result = await riskWindowService.GetActiveRiskWindowsAsync(symbolList, token);
        
        if (!result.IsSuccess || result.Value == null)
        {
            return result.ToActionResult();
        }

        var riskWindows = result.Value;

        // Apply level filter if provided
        if (level.HasValue)
        {
            riskWindows = riskWindows.Where(rw => rw.Level == level.Value).ToList();
        }

        var responseItems = riskWindows.Select(rw => rw.ToActiveWindowItemDto()).ToList();

        var response = new ActiveRiskWindowsResponseDto
        {
            RiskWindows = responseItems,
            TotalCount = responseItems.Count
        };

        return Ok(response);
    }
}

