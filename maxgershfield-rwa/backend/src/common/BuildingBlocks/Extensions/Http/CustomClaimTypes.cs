namespace BuildingBlocks.Extensions.Http;

/// <summary>
/// A centralized collection of custom claim type constants used throughout the authentication and authorization system.
/// 
/// This class defines standardized keys for accessing claim values (such as user ID, email, role, etc.)
/// from a JWT token or ClaimsPrincipal object. By storing them here, we avoid magic strings scattered across the codebase,
/// improve maintainability, and reduce the chance of typos or inconsistencies.
///
/// Notes:
/// - Standard role claim is mapped to the well-known URI-based format.
/// - Ensure consistency with the claims generated during token issuance in your Identity system.
/// </summary>
public static class CustomClaimTypes
{
    public const string Id = "Id";
    public const string Email = "Email";
    public const string Phone = "Phone";
    public const string Role = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
    public const string UserName = "UserName";
    public const string FirstName = "FirstName";
    public const string LastName = "LastName";
    public const string TokenVersion = "TokenVersion ";
}