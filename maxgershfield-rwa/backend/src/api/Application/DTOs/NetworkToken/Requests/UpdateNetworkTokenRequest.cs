namespace Application.DTOs.NetworkToken.Requests;

/// <summary>
/// Represents the payload for updating an existing network token.
/// </summary>
/// <param name="Symbol">
/// The updated symbol of the network token. Must be unique within its network.
/// </param>
/// <param name="Description">
/// An optional updated description of the network token.
/// </param>
/// <param name="NetworkId">
/// The identifier of the network to which the token belongs.
/// </param>
public record UpdateNetworkTokenRequest(
    string Symbol,
    string? Description,
    Guid NetworkId
);