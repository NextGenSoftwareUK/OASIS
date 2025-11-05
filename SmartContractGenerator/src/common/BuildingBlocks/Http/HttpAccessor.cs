namespace BuildingBlocks.Http;

/// <summary>
/// A static helper class providing convenient extension methods for extracting user-related information
/// from the current HTTP context, including authenticated user ID, User-Agent string, remote IP address,
/// and correlation ID.
/// 
/// This class is particularly useful in layered applications where direct access to HttpContext is not ideal.
/// It promotes encapsulation and reuse of common HTTP-related logic.
/// 
/// Note:
/// - `SystemId` is a predefined fallback identifier, commonly used for system-level operations or background processes.
/// - `GetId` retrieves the user’s unique identifier (GUID) from the claims collection.
/// - Throws ArgumentNullException if the ID claim is missing or invalid — this encourages stricter security.
/// - `GetCorrelationId` retrieves or generates a correlation ID for tracing requests across services.
/// </summary>
public static class HttpAccessor
{
    private const string UserAgentHeader = "User-Agent";
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string AnyIpAddress = "0.0.0.0";

    /// <summary>
    /// Represents the ID of the system itself.
    /// Used when an action is performed by the system (e.g., background job, migration) rather than a user.
    /// </summary>
    public static readonly Guid SystemId = new("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// Extracts the authenticated user's ID from the current HTTP context claims.
    /// Throws if the claim is missing or not a valid GUID.
    /// </summary>
    public static Guid? GetId(this IHttpContextAccessor accessor)
        => accessor.HttpContext?.User.Claims
            .FirstOrDefault(x => x.Type == CustomClaimTypes.Id)?
            .Value is { } userIdString && Guid.TryParse(userIdString, out Guid userId)
            ? userId
            : null;

    /// <summary>
    /// Retrieves the User-Agent header from the incoming HTTP request.
    /// </summary>
    public static string? GetUserAgent(this IHttpContextAccessor accessor)
        => accessor.HttpContext?.Request.Headers[UserAgentHeader].ToString();

    /// <summary>
    /// Returns the IP address of the remote client making the HTTP request.
    /// </summary>
    public static string GetRemoteIpAddress(this IHttpContextAccessor accessor)
        => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? AnyIpAddress;

    /// <summary>
    /// Retrieves the correlation ID from the incoming HTTP request.
    /// If none is provided, a new GUID is generated.
    /// </summary>
    public static string GetCorrelationId(this IHttpContextAccessor accessor)
    {
        string? headerValue = accessor.HttpContext?.Request.Headers[CorrelationIdHeader].ToString();

        return headerValue ?? LoggingHelpers.CreateCorrelationId();
    }
}