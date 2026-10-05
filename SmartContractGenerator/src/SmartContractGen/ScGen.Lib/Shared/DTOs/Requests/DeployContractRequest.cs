namespace ScGen.Lib.Shared.DTOs.Requests;

public record DeployContractRequest(
    [Required] IFormFile CompiledContractFile,
    [Required] IFormFile? Schema,
    [Required] SmartContractLanguage Language,
    IFormFile? WalletKeypair);