namespace Domain.Entities;

public sealed class Trust : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string SettlorName { get; set; } = string.Empty;
    public string TrusteeName { get; set; } = string.Empty;
    public string? TrustProtector { get; set; }
    public string QualifiedCustodian { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = "Wyoming";
    public int DurationYears { get; set; } = 1000;
    public string TrustType { get; set; } = "Statutory Trust";
    public decimal MinimumPropertyValue { get; set; } = 25000000m;
    public decimal AnnualDistributionRate { get; set; } = 90m;
    public decimal ReserveFundRate { get; set; } = 10m;
    public decimal TrusteeFeeRate { get; set; } = 1m;

    // Navigation properties
    public ICollection<TrustProperty> Properties { get; set; } = [];
    public ICollection<ContractGeneration> ContractGenerations { get; set; } = [];
}
