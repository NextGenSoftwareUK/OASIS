using Application.DTOs.RiskManagement;
using Domain.Entities;

namespace Application.Contracts;

/// <summary>
/// Service for identifying and managing risk windows
/// </summary>
public interface IRiskWindowService
{
    /// <summary>
    /// Identify risk window for a symbol at a specific date
    /// </summary>
    Task<Result<RiskWindowDto>> IdentifyRiskWindowAsync(
        string symbol,
        DateTime date,
        CancellationToken token = default);

    /// <summary>
    /// Get all active risk windows for a list of symbols
    /// </summary>
    Task<Result<List<RiskWindowDto>>> GetActiveRiskWindowsAsync(
        List<string> symbols,
        CancellationToken token = default);

    /// <summary>
    /// Get upcoming risk windows for a list of symbols
    /// </summary>
    Task<Result<List<RiskWindowDto>>> GetUpcomingRiskWindowsAsync(
        List<string> symbols,
        int daysAhead = 7,
        CancellationToken token = default);

    /// <summary>
    /// Get recent risk window for a symbol
    /// </summary>
    Task<Result<RiskWindowDto?>> GetRecentRiskWindowAsync(
        string symbol,
        int daysBack = 7,
        CancellationToken token = default);

    /// <summary>
    /// Save or update a risk window in the database
    /// </summary>
    Task<Result<RiskWindowDto>> SaveRiskWindowAsync(
        RiskWindow riskWindow,
        CancellationToken token = default);
}

