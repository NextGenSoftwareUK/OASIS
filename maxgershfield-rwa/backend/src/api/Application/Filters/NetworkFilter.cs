namespace Application.Filters;

/// <summary>
/// Represents a filter for querying network entities based on optional Name and Description criteria.
/// This record extends <see cref="BaseFilter"/> and provides a foundation for filtering networks in queries.
/// Both Name and Description properties are nullable, allowing for flexible filtering options.
/// </summary>
/// <param name="Name">
/// Gets or sets the name of the network. Used to filter results based on network name.
/// This property is nullable, meaning no filtering will occur if it is not provided.
/// </param>
/// <param name="Description">
/// Gets or sets the description of the network. Used to filter results based on network description.
/// This property is nullable, meaning no filtering will occur if it is not provided.
/// </param>
public sealed record NetworkFilter(
    string? Name,
    string? Description) : BaseFilter;