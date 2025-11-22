namespace ScGen.Lib.Shared.DTOs.Requests;

public sealed record ExtractPropertyDataRequest
{
    public required string Url { get; init; }
}

