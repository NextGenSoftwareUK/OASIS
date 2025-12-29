namespace Domain.Entities;

public sealed class ContractGeneration : BaseEntity
{
    public Guid TrustId { get; set; }
    public Guid? PropertyId { get; set; }
    public string ContractType { get; set; } = "WyomingTrustTokenization";
    public string Blockchain { get; set; } = "Kadena";
    public string GeneratedContract { get; set; } = string.Empty;
    public string ContractHash { get; set; } = string.Empty;
    public string? AbiData { get; set; }
    public string? BytecodeData { get; set; }
    public string? DeploymentAddress { get; set; }
    public string DeploymentStatus { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeployedAt { get; set; }

    // Navigation properties
    public Trust Trust { get; set; } = null!;
    public TrustProperty? Property { get; set; }
}
