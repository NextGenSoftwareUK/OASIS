namespace BuildingBlocks.Extensions.Http;

/// <summary>
/// A static helper class providing convenient extension methods for extracting user-related information
/// from the current HTTP context, including authenticated user ID, User-Agent string, and remote IP address.
/// 
/// This class is particularly useful in layered applications where direct access to HttpContext is not ideal.
/// It promotes encapsulation and reuse of common HTTP-related logic.
/// 
/// Note:
/// - `SystemId` is a predefined fallback identifier, commonly used for system-level operations or background processes.
/// - `GetId` retrieves the user’s unique identifier (GUID) from the claims collection.
/// - Throws ArgumentNullException if the ID claim is missing or invalid — this encourages stricter security.
/// </summary>
public static class HttpAccessor
{
    /// <summary>
    /// Represents the ID of the system itself.
    /// Used when an action is performed by the system (e.g., background job, migration) rather than a user.
    /// </summary>
    public static readonly Guid SystemId = new("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// Extracts the authenticated user's ID from the current HTTP context claims.
    /// Throws if the claim is missing or not a valid GUID.
    /// </summary>
    /// <param name="accessor">The current HTTP context accessor.</param>
    /// <returns>The GUID representing the user’s identity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the claim is missing or invalid.</exception>
    public static Guid GetId(this IHttpContextAccessor accessor)
        => accessor.HttpContext?.User.Claims
            .FirstOrDefault(x => x.Type == CustomClaimTypes.Id)?
            .Value is { } userIdString && Guid.TryParse(userIdString, out Guid userId)
            ? userId
            : throw new ArgumentNullException(nameof(userId));

    /// <summary>
    /// Retrieves the User-Agent header from the incoming HTTP request.
    /// </summary>
    /// <param name="accessor">The current HTTP context accessor.</param>
    /// <returns>The User-Agent string, or null if unavailable.</returns>
    public static string? GetUserAgent(this IHttpContextAccessor accessor)
        => accessor.HttpContext?.Request.Headers["User-Agent"].ToString();

    /// <summary>
    /// Returns the IP address of the remote client making the HTTP request.
    /// </summary>
    /// <param name="accessor">The current HTTP context accessor.</param>
    /// <returns>The IP address as a string, or "0.0.0.0" if unavailable.</returns>
    public static string GetRemoteIpAddress(this IHttpContextAccessor accessor)
        => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
}