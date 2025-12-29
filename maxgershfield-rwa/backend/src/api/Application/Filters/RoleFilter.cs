namespace Application.Filters;

/// <summary>
/// Filter parameters used for querying roles based on name, keyword, or description.
/// </summary>
/// <param name="Name">
/// Optional filter by the role's display name (case-insensitive, partial match supported).
/// </param>
/// <param name="Keyword">
/// Optional filter by the internal role key or identifier (case-insensitive, partial match supported).
/// </param>
/// <param name="Description">
/// Optional filter by role description (partial match supported).
/// </param>
public sealed record RoleFilter(
    string? Name,
    string? Keyword,
    string? Description) : BaseFilter;