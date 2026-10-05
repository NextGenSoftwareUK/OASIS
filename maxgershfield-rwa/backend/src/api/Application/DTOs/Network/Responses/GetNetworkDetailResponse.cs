namespace Application.DTOs.Network.Responses;

/// <summary>
/// Represents the detailed response data for a network.
/// This record contains comprehensive details about a network, including its unique identifier, name, optional description, and associated tokens.
/// </summary>
/// <param name="Id">
/// The unique identifier of the network.
/// </param>
/// <param name="Name">
/// The name of the network.
/// </param>
/// <param name="Description">
/// A description of the network, if available. This property is nullable, meaning it may be empty if no description is provided.
/// </param>
/// <param name="Tokens">
/// A list of tokens associated with the network.
/// </param>
public record GetNetworkDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    List<string> Tokens);