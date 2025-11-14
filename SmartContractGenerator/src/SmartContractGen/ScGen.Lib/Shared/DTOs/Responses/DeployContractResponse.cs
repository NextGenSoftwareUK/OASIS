namespace ScGen.Lib.Shared.DTOs.Responses;

public readonly record struct DeployContractResponse(
    string ContractAddress,
    bool Success,
    string TransactionHash);