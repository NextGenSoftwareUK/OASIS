namespace Application.DTOs.Network.Responses;

/// <summary>
/// Represents the response returned after creating a network.
/// This record contains the unique identifier of the newly created network.
/// </summary>
/// <param name="Id">
/// The unique identifier of the newly created network.
/// </param>
public record CreateNetworkResponse(
    Guid Id);