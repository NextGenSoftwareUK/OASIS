namespace Application.DTOs.Network.Requests;

/// <summary>
/// Represents the request data required to update a network.
/// This record contains the necessary fields to update a network, including the new name, description, and network type.
/// </summary>
/// <param name="Name">
/// The new name of the network.
/// </param>
/// <param name="Description">
/// The new description of the network. This property is nullable, meaning it may be empty if no description is provided.
/// </param>
/// <param name="NetworkType">
/// The type of the network, which defines its categorization or usage.
/// </param>
public record UpdateNetworkRequest(
    string Name,
    string? Description,
    NetworkType NetworkType
);