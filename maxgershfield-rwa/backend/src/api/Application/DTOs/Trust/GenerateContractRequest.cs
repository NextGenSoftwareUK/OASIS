namespace Application.DTOs.Trust;

public record GenerateContractRequest(
    string ContractType = "WyomingTrustTokenization",
    string Blockchain = "Kadena",
    string? TokenName = null,
    string? TokenSymbol = null,
    int? TotalSupply = null,
    decimal? TokenPrice = null,
    bool RequiresNotarization = true,
    string[]? TransferRestrictions = null,
    Dictionary<string, object>? TokenMetadata = null
);
