namespace Application.Extensions.Mappers;

/// <summary>
/// Provides mapping functionality for converting network token-related request models into domain entities.
/// This class contains methods for mapping Create, Update, and Delete network token requests to corresponding NetworkToken entity instances.
/// </summary>
public static class NetworkTokenMapper
{
    /// <summary>
    /// Maps a NetworkToken entity to a GetNetworkTokenResponse.
    /// </summary>
    /// <param name="token">The NetworkToken entity to be mapped.</param>
    /// <returns>A GetNetworkTokenResponse containing the mapped data.</returns>
    public static GetNetworkTokenResponse ToRead(this NetworkToken token)
        => new(
            token.Id,
            token.Symbol,
            token.Description,
            token.NetworkId);

    /// <summary>
    /// Maps a NetworkToken entity to a GetNetworkTokenDetailResponse.
    /// </summary>
    /// <param name="token">The NetworkToken entity to be mapped.</param>
    /// <returns>A GetNetworkTokenDetailResponse containing the mapped data.</returns>
    public static GetNetworkTokenDetailResponse ToReadDetail(this NetworkToken token)
        => new(
            token.Id,
            token.Symbol,
            token.Description,
            token.NetworkId);

    /// <summary>
    /// Maps an UpdateNetworkTokenRequest to an existing NetworkToken entity.
    /// </summary>
    /// <param name="token">The NetworkToken entity to be updated.</param>
    /// <param name="accessor">The IHttpContextAccessor to retrieve the current user and IP address information.</param>
    /// <param name="request">The UpdateNetworkTokenRequest containing the new data.</param>
    /// <returns>The updated NetworkToken entity.</returns>
    public static NetworkToken ToEntity(this NetworkToken token, IHttpContextAccessor accessor,
        UpdateNetworkTokenRequest request)
    {
        token.Update(accessor.GetId());
        token.Symbol = request.Symbol;
        token.Description = request.Description;
        token.NetworkId = request.NetworkId;
        token.UpdatedByIp!.Add(accessor.GetRemoteIpAddress());
        return token;
    }

    /// <summary>
    /// Maps a CreateNetworkTokenRequest to a new NetworkToken entity.
    /// </summary>
    /// <param name="request">The CreateNetworkTokenRequest containing the data for the new NetworkToken entity.</param>
    /// <param name="accessor">The IHttpContextAccessor to retrieve the current user and IP address information.</param>
    /// <returns>A new NetworkToken entity.</returns>
    public static NetworkToken ToEntity(this CreateNetworkTokenRequest request, IHttpContextAccessor accessor)
        => new()
        {
            Symbol = request.Symbol,
            Description = request.Description,
            NetworkId = request.NetworkId,
            CreatedBy = accessor.GetId(),
            CreatedByIp = accessor.GetRemoteIpAddress()
        };

    /// <summary>
    /// Maps a NetworkToken entity to an entity indicating deletion.
    /// </summary>
    /// <param name="token">The NetworkToken entity to be marked as deleted.</param>
    /// <param name="accessor">The IHttpContextAccessor to retrieve the current user and IP address information.</param>
    /// <returns>The updated NetworkToken entity indicating deletion.</returns>
    public static NetworkToken ToEntity(this NetworkToken token, IHttpContextAccessor accessor)
    {
        token.Delete(accessor.GetId());
        token.DeletedByIp = accessor.GetRemoteIpAddress();
        return token;
    }
}
