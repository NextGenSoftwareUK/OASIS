namespace API.Controllers.Base;

/// <summary>
/// Contains constants representing the API versioning scheme used in the application.
/// This helps ensure consistency across the API endpoints and makes it easier to manage
/// versioning as the application evolves.
/// </summary>
public static class ApiVersions
{
    /// <summary>
    /// Represents version 1 of the API.
    /// </summary>
    public const string V1 = "v1";

    /// <summary>
    /// Represents version 2 of the API.
    /// </summary>
    public const string V2 = "v2";
}

/// <summary>
/// Contains constants representing the base address structure for the API endpoints.
/// This is used to avoid hardcoding URLs and to make it easier to manage API routes.
/// </summary>
public static class ApiAddresses
{
    /// <summary>
    /// The base URL for version 1 of the API. Can be updated to include versioning changes.
    /// </summary>
    public const string Base = $"api/{ApiVersions.V1}";
}