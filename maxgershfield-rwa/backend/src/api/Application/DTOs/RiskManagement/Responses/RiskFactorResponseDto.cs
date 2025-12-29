namespace Application.DTOs.RiskManagement.Responses;

/// <summary>
/// Risk factor response DTO for API
/// </summary>
public sealed record RiskFactorResponseDto
{
    /// <summary>
    /// Type of risk factor as string
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Description of the risk factor
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Impact score (0-1 scale)
    /// </summary>
    public decimal Impact { get; init; }

    /// <summary>
    /// Effective date of this risk factor
    /// </summary>
    public DateTime EffectiveDate { get; init; }
}

