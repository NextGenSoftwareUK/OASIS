namespace ScGen.Lib.Shared.DTOs.Responses;

public class CompileContractResponse
{
    public byte[] CompiledCode { get; set; } = [];
    public string CompiledCodeFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = MediaTypeNames.Application.Octet;

    public byte[]? Shema { get; set; } = [];
    public string? ShemaFileName { get; set; }
    public string? SchemaContentType { get; set; } = MediaTypeNames.Application.Json;
}