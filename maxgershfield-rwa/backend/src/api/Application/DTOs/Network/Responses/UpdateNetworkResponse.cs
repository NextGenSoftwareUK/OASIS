namespace Application.DTOs.Network.Responses;

/// <summary>
/// Represents the response data returned after a network is updated.
/// This record contains the unique identifier of the updated network.
/// </summary>
/// <param name="Id">
/// The unique identifier of the updated network.
/// </param>
public record UpdateNetworkResponse(
    Guid Id);