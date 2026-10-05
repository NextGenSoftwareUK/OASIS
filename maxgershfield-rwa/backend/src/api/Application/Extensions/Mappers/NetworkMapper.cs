namespace Application.Extensions.Mappers;

/// <summary>
/// Provides mapping functionality for converting network-related request models into domain entities.
/// This class contains methods for mapping Create, Update, and Delete network requests to corresponding Network entity instances.
/// </summary>
public static class NetworkMapper
{
    /// <summary>
    /// Maps an UpdateNetworkRequest to an existing Network entity.
    /// </summary>
    /// <param name="network">The Network entity to be updated.</param>
    /// <param name="accessor">The IHttpContextAccessor to retrieve the current user and IP address information.</param>
    /// <param name="request">The UpdateNetworkRequest containing the new data.</param>
    /// <returns>The updated Network entity.</returns>
    public static Network ToEntity(this Network network, IHttpContextAccessor accessor, UpdateNetworkRequest request)
    {
        network.Update(accessor.GetId());
        network.Name = request.Name;
        network.Description = request.Description;
        network.NetworkType = request.NetworkType;
        network.UpdatedByIp!.Add(accessor.GetRemoteIpAddress());
        return network;
    }

    /// <summary>
    /// Maps a CreateNetworkRequest to a new Network entity.
    /// </summary>
    /// <param name="request">The CreateNetworkRequest containing the data for the new Network entity.</param>
    /// <param name="accessor">The IHttpContextAccessor to retrieve the current user and IP address information.</param>
    /// <returns>A new Network entity.</returns>
    public static Network ToEntity(this CreateNetworkRequest request, IHttpContextAccessor accessor)
        => new()
        {
            Name = request.Name,
            Description = request.Description,
            NetworkType = request.NetworkType,
            CreatedBy = accessor.GetId(),
            CreatedByIp = accessor.GetRemoteIpAddress()
        };

    /// <summary>
    /// Maps a network to an entity indicating deletion.
    /// </summary>
    /// <param name="network">The Network entity to be marked as deleted.</param>
    /// <param name="accessor">The IHttpContextAccessor to retrieve the current user and IP address information.</param>
    /// <returns>The updated Network entity indicating deletion.</returns>
    public static Network ToEntity(this Network network, IHttpContextAccessor accessor)
    {
        network.Delete(accessor.GetId());
        network.DeletedByIp = accessor.GetRemoteIpAddress();
        return network;
    }
}
