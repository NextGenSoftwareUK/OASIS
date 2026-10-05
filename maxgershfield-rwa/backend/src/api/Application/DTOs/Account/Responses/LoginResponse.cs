namespace Application.DTOs.Account.Responses
{
    /// <summary>
    /// Represents the response data upon successful user login.
    /// Contains the authentication token and its expiration details.
    /// </summary>
    /// <param name="Token">
    /// The authentication token that the user can use for authorized requests.
    /// </param>
    /// <param name="StartTime">
    /// The timestamp when the token was issued.
    /// </param>
    /// <param name="ExpiresAt">
    /// The timestamp when the token will expire and no longer be valid.
    /// </param>
    public sealed record LoginResponse(
        string Token,
        DateTimeOffset StartTime,
        DateTimeOffset ExpiresAt
    );
}