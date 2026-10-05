namespace ScGen.Lib.Shared.DTOs.Requests;

public record CompileContractRequest(
    [Required] IFormFile Source,
    [Required] SmartContractLanguage Language);