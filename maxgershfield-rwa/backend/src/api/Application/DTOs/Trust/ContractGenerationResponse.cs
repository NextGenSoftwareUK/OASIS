namespace Application.DTOs.Trust;

public record ContractGenerationResponse(
    Guid Id,
    Guid TrustId,
    Guid? PropertyId,
    string ContractType,
    string Blockchain,
    string GeneratedContract,
    string ContractHash,
    string? AbiData,
    string? BytecodeData,
    string? DeploymentAddress,
    string DeploymentStatus,
    string? ErrorMessage,
    DateTime GeneratedAt,
    DateTime? DeployedAt
);
