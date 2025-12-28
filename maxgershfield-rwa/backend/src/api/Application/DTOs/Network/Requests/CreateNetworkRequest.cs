namespace Application.DTOs.Network.Requests;

/// <summary>
/// Represents the data required to create a new network.
/// This record contains the necessary fields for creating a network, including its name, an optional description, and the type of network.
/// </summary>
/// <param name="Name">
/// The name of the network being created.
/// </param>
/// <param name="Description">
/// A description of the network, if provided. This property is nullable, meaning it may be empty if no description is provided.
/// </param>
/// <param name="NetworkType">
/// The type of the network, which defines its categorization or usage.
/// </param>
public record CreateNetworkRequest(
    string Name,
    string? Description,
    NetworkType NetworkType
);