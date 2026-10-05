namespace Application.Filters;

/// <summary>
/// Filter parameters for querying network tokens.
/// </summary>
/// <param name="Symbol">Optional filter by the token's symbol (case-insensitive, partial match).</param>
/// <param name="Description">Optional filter by the token's description (partial match allowed).</param>
/// <param name="NetworkId">Optional filter to select tokens associated with a specific network.</param>
public sealed record NetworkTokenFilter(
    string? Symbol,
    string? Description,
    Guid? NetworkId) : BaseFilter;