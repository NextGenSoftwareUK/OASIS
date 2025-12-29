namespace Application.DTOs.Trust;

public record CreateTrustRequest(
    string Name,
    string SettlorName,
    string TrusteeName,
    string? TrustProtector,
    string QualifiedCustodian,
    string Jurisdiction = "Wyoming",
    int DurationYears = 1000,
    string TrustType = "Statutory Trust",
    decimal MinimumPropertyValue = 25000000m,
    decimal AnnualDistributionRate = 90m,
    decimal ReserveFundRate = 10m,
    decimal TrusteeFeeRate = 1m
);
