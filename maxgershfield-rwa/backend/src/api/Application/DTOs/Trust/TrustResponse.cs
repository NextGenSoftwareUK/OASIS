namespace Application.DTOs.Trust;

public record TrustResponse(
    Guid Id,
    string Name,
    string SettlorName,
    string TrusteeName,
    string? TrustProtector,
    string QualifiedCustodian,
    string Jurisdiction,
    int DurationYears,
    string TrustType,
    decimal MinimumPropertyValue,
    decimal AnnualDistributionRate,
    decimal ReserveFundRate,
    decimal TrusteeFeeRate,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
