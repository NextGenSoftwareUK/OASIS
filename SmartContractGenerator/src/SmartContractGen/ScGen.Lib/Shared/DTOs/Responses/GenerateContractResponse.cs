namespace ScGen.Lib.Shared.DTOs.Responses;

public sealed class GenerateContractResponse
{
    public byte[] Content { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/plain"; 
}