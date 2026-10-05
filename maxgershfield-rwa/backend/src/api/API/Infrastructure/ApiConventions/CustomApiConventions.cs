namespace API.Infrastructure.ApiConventions;

/// <summary>
/// Provides custom API conventions for common CRUD operations in controllers.
/// These conventions standardize the response types for HTTP status codes based on the operation type.
/// </summary>
public static class CustomApiConventions
{
    /// <summary>
    /// Convention for a GET request to retrieve a resource by its unique identifier (GUID).
    /// </summary>
    /// <param name="id">The unique identifier for the resource.</param>
    /// <param name="token">The cancellation token for the operation.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static void Get([FromRoute] Guid id, CancellationToken token = default)
    {
    }

    /// <summary>
    /// Convention for a GET request to retrieve a list of resources.
    /// </summary>
    /// <param name="token">The cancellation token for the operation.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static void Get(CancellationToken token = default)
    {
    }

    /// <summary>
    /// Convention for a GET request to retrieve resources based on query filters.
    /// </summary>
    /// <param name="filter">The filter criteria for querying the resources.</param>
    /// <param name="token">The cancellation token for the operation.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static void Get([FromQuery] BaseFilter filter, CancellationToken token = default)
    {
    }

    /// <summary>
    /// Convention for a POST request to create a new resource.
    /// </summary>
    /// <param name="entity">The resource to be created.</param>
    /// <param name="token">The cancellation token for the operation.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static void Create([FromBody] object entity, CancellationToken token = default)
    {
    }

    /// <summary>
    /// Convention for a PUT request to update an existing resource by its unique identifier (GUID).
    /// </summary>
    /// <param name="id">The unique identifier for the resource to update.</param>
    /// <param name="entity">The updated resource data.</param>
    /// <param name="token">The cancellation token for the operation.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static void Update([FromRoute] Guid id, [FromBody] object entity, CancellationToken token = default)
    {
    }

    /// <summary>
    /// Convention for a PUT request to update an existing resource without specifying its unique identifier (GUID).
    /// </summary>
    /// <param name="entity">The updated resource data.</param>
    /// <param name="token">The cancellation token for the operation.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static void Update([FromBody] object entity, CancellationToken token = default)
    {
    }

    /// <summary>
    /// Convention for a DELETE request to delete a resource by its unique identifier (GUID).
    /// </summary>
    /// <param name="id">The unique identifier for the resource to delete.</param>
    /// <param name="token">The cancellation token for the operation.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static void Delete([FromRoute] Guid id, CancellationToken token = default)
    {
    }

    /// <summary>
    /// Convention for a DELETE request to delete a resource without specifying its unique identifier (GUID).
    /// </summary>
    /// <param name="token">The cancellation token for the operation.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static void Delete(CancellationToken token = default)
    {
    }
}