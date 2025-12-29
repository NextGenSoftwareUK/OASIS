namespace Application.DTOs.Network.Responses;

/// <summary>
/// Represents the response data for a network.
/// This record contains the details of a network, including its unique identifier, name, optional description, and associated tokens.
/// </summary>
/// <param name="Id">
/// The unique identifier of the network.
/// </param>
/// <param name="Name">
/// The name of the network.
/// </param>
/// <param name="Description">
/// An optional description of the network. This property may be null if no description is provided.
/// </param>
/// <param name="Tokens">
/// A list of tokens associated with the network.
/// </param>
public record GetNetworkResponse(
    Guid Id,
    string Name,
    string? Description,
    List<string> Tokens);