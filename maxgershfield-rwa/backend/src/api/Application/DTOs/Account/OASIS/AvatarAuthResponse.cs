namespace Application.DTOs.Account.OASIS;

/// <summary>
/// Response from OASIS Avatar API authentication
/// </summary>
public sealed record AvatarAuthResponse
{
    public string JwtToken { get; init; } = string.Empty;
    public string AvatarId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? FullName { get; init; }
}



