using Application.DTOs.RiskManagement;

namespace Application.Contracts;

/// <summary>
/// Service for assessing risk levels for symbols and positions
/// </summary>
public interface IRiskAssessmentService
{
    /// <summary>
    /// Assess risk for a symbol (optionally with position data)
    /// </summary>
    Task<Result<RiskAssessmentDto>> AssessRiskAsync(
        string symbol,
        Position? position = null,
        CancellationToken token = default);

    /// <summary>
    /// Assess risk for multiple symbols in batch
    /// </summary>
    Task<Result<List<RiskAssessmentDto>>> AssessBatchRiskAsync(
        List<string> symbols,
        CancellationToken token = default);

    /// <summary>
    /// Assess risk for a specific position
    /// </summary>
    Task<Result<RiskAssessmentDto>> AssessPositionRiskAsync(
        string symbol,
        Position position,
        CancellationToken token = default);
}

