namespace Application.DTOs.Account.OASIS;

/// <summary>
/// Generic result wrapper for OASIS API responses
/// </summary>
public sealed class OASISResult<T>
{
    public bool IsError { get; init; }
    public string? Message { get; init; }
    public T? Result { get; init; }
}



