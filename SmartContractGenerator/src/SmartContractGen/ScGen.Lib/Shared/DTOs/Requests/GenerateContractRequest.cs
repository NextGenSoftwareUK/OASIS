namespace ScGen.Lib.Shared.DTOs.Requests;

public record GenerateContractRequest(
    [Required] IFormFile JsonFile,
    [Required] SmartContractLanguage Language);